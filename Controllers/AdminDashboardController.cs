using Api.Constant;
using Api.Data;
using Api.DTOs.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/dashboard")]
    [ApiController]
    public class AdminDashboardController : ControllerBase
    {
        private readonly Context _context;

        public AdminDashboardController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<AdminDashboardStatsDto>> GetDashboardStats()
        {
            var utcNow = DateTime.UtcNow;
            var totalUsers = await _context.Users.AsNoTracking().CountAsync();

            var roleCounts = await (
                    from userRole in _context.UserRoles.AsNoTracking()
                    join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    group userRole by role.NormalizedName into roleGroup
                    select new
                    {
                        Role = roleGroup.Key,
                        Count = roleGroup.Select(x => x.UserId).Distinct().Count()
                    })
                .ToListAsync();

            var studentCount = roleCounts.FirstOrDefault(x => x.Role == "STUDENT")?.Count ?? 0;
            var adminCount = roleCounts.FirstOrDefault(x => x.Role == "ADMIN")?.Count ?? 0;
            var activePremiumStudentCount = await _context.StudentSubscriptions
                .AsNoTracking()
                .Where(x =>
                    x.Status == SubscriptionStatuses.Active &&
                    (x.IsLifetime || (x.ExpiresAt.HasValue && x.ExpiresAt.Value > utcNow)))
                .Select(x => x.StudentId)
                .Distinct()
                .CountAsync();

            var paidPayments = _context.SubscriptionPayments
                .AsNoTracking()
                .Where(x => x.Status == SubscriptionPaymentStatuses.Paid);

            var totalPaidAmount = await paidPayments
                .Select(x => (decimal?)x.Amount)
                .SumAsync() ?? 0m;

            var paidPaymentCount = await paidPayments.CountAsync();
            var latestPaidAt = await paidPayments
                .Where(x => x.PaidAt.HasValue)
                .MaxAsync(x => (DateTime?)x.PaidAt);

            var revenueByPlan = await (
                    from payment in _context.SubscriptionPayments.AsNoTracking()
                    join plan in _context.SubscriptionPlans.AsNoTracking() on payment.PlanId equals plan.Id
                    where payment.Status == SubscriptionPaymentStatuses.Paid
                    group payment by new { plan.Id, plan.Name } into planGroup
                    orderby planGroup.Sum(x => x.Amount) descending
                    select new AdminDashboardPlanRevenueDto
                    {
                        PlanId = planGroup.Key.Id,
                        PlanName = planGroup.Key.Name,
                        Amount = planGroup.Sum(x => x.Amount),
                        PaymentCount = planGroup.Count()
                    })
                .ToListAsync();

            var paymentsByStatus = await _context.SubscriptionPayments
                .AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(statusGroup => new AdminDashboardPaymentStatusDto
                {
                    Status = statusGroup.Key,
                    Amount = statusGroup.Sum(x => x.Amount),
                    PaymentCount = statusGroup.Count()
                })
                .ToListAsync();

            return Ok(new AdminDashboardStatsDto
            {
                TotalUsers = totalUsers,
                StudentCount = studentCount,
                AdminCount = adminCount,
                OtherUserCount = Math.Max(0, totalUsers - studentCount - adminCount),
                ActivePremiumStudentCount = activePremiumStudentCount,
                FreeStudentCount = Math.Max(0, studentCount - activePremiumStudentCount),
                TotalPaidAmount = totalPaidAmount,
                PaidPaymentCount = paidPaymentCount,
                PendingPaymentCount = paymentsByStatus.FirstOrDefault(x => x.Status == SubscriptionPaymentStatuses.Pending)?.PaymentCount ?? 0,
                FailedPaymentCount = paymentsByStatus.FirstOrDefault(x => x.Status == SubscriptionPaymentStatuses.Failed)?.PaymentCount ?? 0,
                LatestPaidAt = latestPaidAt,
                RevenueByPlan = revenueByPlan,
                PaymentsByStatus = paymentsByStatus
            });
        }
    }
}
