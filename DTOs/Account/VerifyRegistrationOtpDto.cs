using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class VerifyRegistrationOtpDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
        public string Otp { get; set; }
    }
}
