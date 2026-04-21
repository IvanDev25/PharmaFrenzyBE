using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsPremium { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Module Module { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<QuestionSetAccess> QuestionSetAccesses { get; set; } = new List<QuestionSetAccess>();
    }
}
