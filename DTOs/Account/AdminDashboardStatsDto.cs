using System;
using System.Collections.Generic;

namespace Api.DTOs.Account
{
    public class AdminDashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int StudentCount { get; set; }
        public int AdminCount { get; set; }
        public int OtherUserCount { get; set; }
        public int ActivePremiumStudentCount { get; set; }
        public int FreeStudentCount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public int PaidPaymentCount { get; set; }
        public int PendingPaymentCount { get; set; }
        public int FailedPaymentCount { get; set; }
        public DateTime? LatestPaidAt { get; set; }
        public List<AdminDashboardPlanRevenueDto> RevenueByPlan { get; set; } = new();
        public List<AdminDashboardPaymentStatusDto> PaymentsByStatus { get; set; } = new();
    }

    public class AdminDashboardPlanRevenueDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; }
        public decimal Amount { get; set; }
        public int PaymentCount { get; set; }
    }

    public class AdminDashboardPaymentStatusDto
    {
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public int PaymentCount { get; set; }
    }
}
