using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class StudentModulePoint
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        public int ModuleId { get; set; }
        public decimal Points { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User Student { get; set; }
        public Module Module { get; set; }
    }
}
