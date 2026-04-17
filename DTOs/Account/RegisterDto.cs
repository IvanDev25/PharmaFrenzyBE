using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class RegisterDto
    {
        [Required]
        [StringLength(15, MinimumLength = 3, ErrorMessage = "First name must be at least {2}, and maximum {1} characters ")]
        public string FirstName { get; set; }
        [Required]
        [StringLength(15, MinimumLength = 3, ErrorMessage = "Last name must be at least {2}, and maximum {1} characters ")]
        public string LastName { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
        [Phone]
        public string PhoneNumber { get; set; }
        [Required]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "University must be at least {2}, and maximum {1} characters ")]
        public string University { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Gender must be at least {2}, and maximum {1} characters ")]
        public string Gender { get; set; }
        [Required]
        [StringLength(15, MinimumLength = 6, ErrorMessage = "Password must be at least {2}, and maximum {1} characters ")]
        public string Password { get; set; }
        [Required]
        public string Image { get; set; }
        [Required]
        public string VerificationToken { get; set; }
    }
}
