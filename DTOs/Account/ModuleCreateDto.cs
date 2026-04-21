using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class ModuleCreateDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsPremium { get; set; }
    }
}
