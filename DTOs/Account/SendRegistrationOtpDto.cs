using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class SendRegistrationOtpDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
    }
}
