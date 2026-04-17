using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class UpdateProfileDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [MaxLength(30)]
        public string PhoneNumber { get; set; }

        [MaxLength(150)]
        public string University { get; set; }

        [Required]
        public string Image { get; set; }
    }
}
