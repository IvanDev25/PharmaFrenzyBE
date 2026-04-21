using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class StudentSubscription
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        public int PlanId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsLifetime { get; set; }

        public User Student { get; set; }
        public SubscriptionPlan Plan { get; set; }
    }
}
