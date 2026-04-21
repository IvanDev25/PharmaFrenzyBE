using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        public decimal Amount { get; set; }
        public int? DurationMonths { get; set; }
        public bool IsLifetime { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<StudentSubscription> StudentSubscriptions { get; set; } = new List<StudentSubscription>();
        public ICollection<SubscriptionPayment> SubscriptionPayments { get; set; } = new List<SubscriptionPayment>();
    }
}
