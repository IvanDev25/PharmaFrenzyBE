using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class SubjectCreateDto
    {
        [Required]
        public int ModuleId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
