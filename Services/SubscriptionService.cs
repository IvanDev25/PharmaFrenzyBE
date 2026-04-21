using Api.Constant;
using Api.Data;
using Api.DTOs.Account;
using Api.Interface;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Api.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private const string PaidCheckoutSessionEventType = "checkout_session.payment.paid";
        private readonly Context _context;
        private readonly IPayMongoService _payMongoService;

        public SubscriptionService(Context context, IPayMongoService payMongoService)
        {
            _context = context;
            _payMongoService = payMongoService;
        }

        public async Task<List<SubscriptionPlanDto>> GetPlansAsync()
        {
            return await _context.SubscriptionPlans
                .Where(x => x.IsActive)
                .OrderBy(x => x.Amount)
                .Select(x => new SubscriptionPlanDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Amount = x.Amount,
                    DurationMonths = x.DurationMonths,
                    IsLifetime = x.IsLifetime,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<SubscriptionStatusDto> GetStudentSubscriptionStatusAsync(string studentId)
        {
            var utcNow = DateTime.UtcNow;
            var expiredSubscriptions = await _context.StudentSubscriptions
                .Where(x =>
                    x.StudentId == studentId &&
                    x.Status == SubscriptionStatuses.Active &&
                    !x.IsLifetime &&
                    x.ExpiresAt.HasValue &&
                    x.ExpiresAt.Value <= utcNow)
                .ToListAsync();

            if (expiredSubscriptions.Any())
            {
                foreach (var subscription in expiredSubscriptions)
                {
                    subscription.Status = SubscriptionStatuses.Expired;
                }

                await _context.SaveChangesAsync();
            }

            var activeSubscription = await _context.StudentSubscriptions
                .Include(x => x.Plan)
                .Where(x =>
                    x.StudentId == studentId &&
                    x.Status == SubscriptionStatuses.Active &&
                    (x.IsLifetime || (x.ExpiresAt.HasValue && x.ExpiresAt.Value > utcNow)))
                .OrderByDescending(x => x.IsLifetime)
                .ThenByDescending(x => x.ExpiresAt)
                .ThenByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync();

            if (activeSubscription == null)
            {
                return new SubscriptionStatusDto
                {
                    HasActivePremiumAccess = false,
                    Status = "None",
                    CheckedAtUtc = utcNow
                };
            }

            return new SubscriptionStatusDto
            {
                HasActivePremiumAccess = true,
                Status = activeSubscription.Status,
                PlanCode = activeSubscription.Plan.Code,
                PlanName = activeSubscription.Plan.Name,
                StartedAt = activeSubscription.StartedAt,
                ExpiresAt = activeSubscription.ExpiresAt,
                IsLifetime = activeSubscription.IsLifetime,
                CheckedAtUtc = utcNow
            };
        }

        public async Task<SubscriptionCheckoutDto> CreateCheckoutAsync(string studentId, string planCode)
        {
            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new InvalidOperationException("Plan code is required.");
            }

            var student = await _context.Users.FirstOrDefaultAsync(x => x.Id == studentId);
            if (student == null)
            {
                throw new InvalidOperationException("Student not found.");
            }

            var normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(x =>
                x.Code == normalizedPlanCode &&
                x.IsActive);

            if (plan == null)
            {
                throw new InvalidOperationException("Subscription plan not found.");
            }

            var currentStatus = await GetStudentSubscriptionStatusAsync(studentId);
            if (plan.IsLifetime && currentStatus.HasActivePremiumAccess && currentStatus.IsLifetime)
            {
                throw new InvalidOperationException("You already have lifetime premium access.");
            }

            var payment = new SubscriptionPayment
            {
                StudentId = studentId,
                PlanId = plan.Id,
                Amount = plan.Amount,
                Status = SubscriptionPaymentStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubscriptionPayments.Add(payment);
            await _context.SaveChangesAsync();

            try
            {
                var checkout = await _payMongoService.CreateCheckoutSessionAsync(plan, payment, student);
                payment.PaymongoCheckoutSessionId = checkout.CheckoutSessionId;
                payment.CheckoutUrl = checkout.CheckoutUrl;
                await _context.SaveChangesAsync();

                return new SubscriptionCheckoutDto
                {
                    PaymentId = payment.Id,
                    CheckoutUrl = checkout.CheckoutUrl,
                    PaymongoCheckoutSessionId = checkout.CheckoutSessionId
                };
            }
            catch
            {
                payment.Status = SubscriptionPaymentStatuses.Failed;
                await _context.SaveChangesAsync();
                throw;
            }
        }

        public async Task ProcessPayMongoWebhookAsync(string rawBody)
        {
            using var payload = JsonDocument.Parse(rawBody);
            var root = payload.RootElement;
            var attributes = root.GetProperty("data").GetProperty("attributes");
            var eventType = attributes.GetProperty("type").GetString();

            if (!string.Equals(eventType, PaidCheckoutSessionEventType, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var checkoutSession = attributes.GetProperty("data");
            var checkoutSessionId = checkoutSession.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(checkoutSessionId))
            {
                return;
            }

            var checkoutAttributes = checkoutSession.GetProperty("attributes");
            var paymentId = ExtractFirstPaymentId(checkoutAttributes);
            var paidAt = ExtractPaidAt(checkoutAttributes) ?? DateTime.UtcNow;
            var paidAmount = ExtractPaidAmount(checkoutAttributes);

            using var transaction = await _context.Database.BeginTransactionAsync();

            var payment = await _context.SubscriptionPayments
                .Include(x => x.Plan)
                .FirstOrDefaultAsync(x => x.PaymongoCheckoutSessionId == checkoutSessionId);

            if (payment == null)
            {
                await transaction.CommitAsync();
                return;
            }

            if (payment.Status == SubscriptionPaymentStatuses.Paid)
            {
                await transaction.CommitAsync();
                return;
            }

            if (paidAmount.HasValue && paidAmount.Value != payment.Amount)
            {
                payment.Status = SubscriptionPaymentStatuses.Failed;
                payment.PaymongoPaymentId = paymentId;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return;
            }

            payment.Status = SubscriptionPaymentStatuses.Paid;
            payment.PaymongoPaymentId = paymentId;
            payment.PaidAt = paidAt;

            if (payment.Plan.IsLifetime)
            {
                var hasLifetimeSubscription = await _context.StudentSubscriptions.AnyAsync(x =>
                    x.StudentId == payment.StudentId &&
                    x.Status == SubscriptionStatuses.Active &&
                    x.IsLifetime);

                if (!hasLifetimeSubscription)
                {
                    _context.StudentSubscriptions.Add(BuildSubscription(payment, paidAt));
                }
            }
            else
            {
                _context.StudentSubscriptions.Add(BuildSubscription(payment, paidAt));
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        private static StudentSubscription BuildSubscription(SubscriptionPayment payment, DateTime paidAt)
        {
            return new StudentSubscription
            {
                StudentId = payment.StudentId,
                PlanId = payment.PlanId,
                Status = SubscriptionStatuses.Active,
                StartedAt = paidAt,
                ExpiresAt = payment.Plan.IsLifetime ? null : paidAt.AddMonths(payment.Plan.DurationMonths ?? 0),
                IsLifetime = payment.Plan.IsLifetime
            };
        }

        private static string ExtractFirstPaymentId(JsonElement checkoutAttributes)
        {
            if (!checkoutAttributes.TryGetProperty("payments", out var payments) ||
                payments.ValueKind != JsonValueKind.Array ||
                payments.GetArrayLength() == 0)
            {
                return null;
            }

            return payments[0].TryGetProperty("id", out var paymentId)
                ? paymentId.GetString()
                : null;
        }

        private static decimal? ExtractPaidAmount(JsonElement checkoutAttributes)
        {
            if (!checkoutAttributes.TryGetProperty("payments", out var payments) ||
                payments.ValueKind != JsonValueKind.Array ||
                payments.GetArrayLength() == 0)
            {
                return null;
            }

            if (!payments[0].TryGetProperty("attributes", out var paymentAttributes) ||
                !paymentAttributes.TryGetProperty("amount", out var amountElement) ||
                amountElement.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            return amountElement.GetDecimal() / 100m;
        }

        private static DateTime? ExtractPaidAt(JsonElement checkoutAttributes)
        {
            if (checkoutAttributes.TryGetProperty("paid_at", out var checkoutPaidAt) &&
                checkoutPaidAt.ValueKind == JsonValueKind.Number &&
                checkoutPaidAt.TryGetInt64(out var checkoutPaidAtUnix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(checkoutPaidAtUnix).UtcDateTime;
            }

            if (checkoutAttributes.TryGetProperty("payments", out var payments) &&
                payments.ValueKind == JsonValueKind.Array &&
                payments.GetArrayLength() > 0 &&
                payments[0].TryGetProperty("attributes", out var paymentAttributes) &&
                paymentAttributes.TryGetProperty("paid_at", out var paymentPaidAt) &&
                paymentPaidAt.ValueKind == JsonValueKind.Number &&
                paymentPaidAt.TryGetInt64(out var paymentPaidAtUnix))
            {
                return DateTimeOffset.FromUnixTimeSeconds(paymentPaidAtUnix).UtcDateTime;
            }

            return null;
        }
    }
}
