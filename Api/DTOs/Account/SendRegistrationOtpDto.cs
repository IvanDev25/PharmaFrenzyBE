using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class SendRegistrationOtpDto
    {
        [Required]
        [RegularExpression("^\\w+@[a-zA-Z_]+?\\.[a-zA-Z]{2,3}$", ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
    }
}
