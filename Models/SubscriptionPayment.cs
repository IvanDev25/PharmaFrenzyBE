using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class SubscriptionPayment
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        public int PlanId { get; set; }
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; }

        [MaxLength(120)]
        public string PaymongoCheckoutSessionId { get; set; }

        [MaxLength(120)]
        public string PaymongoPaymentId { get; set; }

        [MaxLength(1000)]
        public string CheckoutUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public User Student { get; set; }
        public SubscriptionPlan Plan { get; set; }
    }
}
