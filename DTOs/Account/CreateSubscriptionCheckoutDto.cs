using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class CreateSubscriptionCheckoutDto
    {
        [Required]
        public string PlanCode { get; set; }
    }
}
