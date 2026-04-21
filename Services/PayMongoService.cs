using Api.Interface;
using Api.Models;
using Api.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Api.Services
{
    public class PayMongoService : IPayMongoService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PayMongoSettings _settings;

        public PayMongoService(IHttpClientFactory httpClientFactory, IOptions<PayMongoSettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
        }

        public async Task<PayMongoCheckoutResult> CreateCheckoutSessionAsync(
            SubscriptionPlan plan,
            SubscriptionPayment payment,
            User student)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey) ||
                _settings.SecretKey.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("PayMongo secret key is not configured.");
            }

            var amountInCentavos = ToCentavos(plan.Amount);
            var paymentMethods = _settings.PaymentMethodTypes?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (!paymentMethods.Any())
            {
                throw new InvalidOperationException("At least one PayMongo payment method type is required.");
            }

            var description = plan.IsLifetime
                ? "Lifetime premium access to PharmaFrenzy."
                : $"{plan.DurationMonths} months premium access to PharmaFrenzy.";

            var requestPayload = new
            {
                data = new
                {
                    attributes = new
                    {
                        billing = new
                        {
                            name = $"{student.FirstName} {student.LastName}".Trim(),
                            email = student.Email,
                            phone = student.PhoneNumber
                        },
                        cancel_url = AppendPaymentId(_settings.CancelUrl, payment.Id),
                        description = $"PharmaFrenzy Premium - {plan.Name}",
                        line_items = new[]
                        {
                            new
                            {
                                amount = amountInCentavos,
                                currency = _settings.Currency,
                                description,
                                name = plan.Name,
                                quantity = 1
                            }
                        },
                        metadata = new Dictionary<string, string>
                        {
                            ["student_id"] = payment.StudentId,
                            ["plan_id"] = payment.PlanId.ToString(CultureInfo.InvariantCulture),
                            ["plan_code"] = plan.Code,
                            ["subscription_payment_id"] = payment.Id.ToString(CultureInfo.InvariantCulture)
                        },
                        payment_method_types = paymentMethods,
                        reference_number = $"PF-{payment.Id}-{Guid.NewGuid():N}"[..32],
                        send_email_receipt = _settings.SendEmailReceipt,
                        show_description = true,
                        show_line_items = true,
                        success_url = AppendPaymentId(_settings.SuccessUrl, payment.Id)
                    }
                }
            };

            var client = _httpClientFactory.CreateClient("PayMongo");
            using var request = new HttpRequestMessage(HttpMethod.Post, "checkout_sessions")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.SecretKey}:")));

            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"PayMongo checkout creation failed: {responseBody}");
            }

            using var json = JsonDocument.Parse(responseBody);
            var data = json.RootElement.GetProperty("data");
            var attributes = data.GetProperty("attributes");
            var checkoutSessionId = data.GetProperty("id").GetString();
            var checkoutUrl = attributes.GetProperty("checkout_url").GetString();

            if (string.IsNullOrWhiteSpace(checkoutSessionId) || string.IsNullOrWhiteSpace(checkoutUrl))
            {
                throw new InvalidOperationException("PayMongo did not return a checkout URL.");
            }

            return new PayMongoCheckoutResult
            {
                CheckoutSessionId = checkoutSessionId,
                CheckoutUrl = checkoutUrl
            };
        }

        public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) ||
                _settings.WebhookSecret.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(signatureHeader))
            {
                return false;
            }

            var signatureParts = ParseSignatureHeader(signatureHeader);
            if (!signatureParts.TryGetValue("t", out var timestamp) || string.IsNullOrWhiteSpace(timestamp))
            {
                return false;
            }

            if (_settings.WebhookTimestampToleranceSeconds > 0 &&
                long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTimestamp))
            {
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
                var age = DateTimeOffset.UtcNow - requestTime;
                if (age.Duration() > TimeSpan.FromSeconds(_settings.WebhookTimestampToleranceSeconds))
                {
                    return false;
                }
            }

            var computedSignature = ComputeHmacSha256Hex($"{timestamp}.{rawBody}", _settings.WebhookSecret);
            var candidates = new[] { "te", "li" }
                .Select(key => signatureParts.TryGetValue(key, out var signature) ? signature : null)
                .Where(signature => !string.IsNullOrWhiteSpace(signature));

            return candidates.Any(candidate => FixedTimeEquals(computedSignature, candidate));
        }

        private static int ToCentavos(decimal amount)
        {
            return Convert.ToInt32(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
        }

        private static string AppendPaymentId(string url, int paymentId)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            var separator = url.Contains("?") ? "&" : "?";
            return $"{url}{separator}paymentId={paymentId}";
        }

        private static Dictionary<string, string> ParseSignatureHeader(string signatureHeader)
        {
            return signatureHeader
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(
                    parts => parts[0].Trim(),
                    parts => parts[1].Trim(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha256Hex(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);

            return leftBytes.Length == rightBytes.Length &&
                CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
