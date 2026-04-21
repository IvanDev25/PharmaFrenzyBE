using System;

namespace Api.DTOs.Account
{
    public class SubscriptionStatusDto
    {
        public bool HasActivePremiumAccess { get; set; }
        public string Status { get; set; }
        public string PlanCode { get; set; }
        public string PlanName { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsLifetime { get; set; }
        public DateTime CheckedAtUtc { get; set; }
    }
}
