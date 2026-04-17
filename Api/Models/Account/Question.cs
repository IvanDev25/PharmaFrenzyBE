using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int QuestionSetNumber { get; set; } = 1;

        [Required]
        public string QuestionText { get; set; }

        [Required]
        [MaxLength(255)]
        public string OptionA { get; set; }

        [Required]
        [MaxLength(255)]
        public string OptionB { get; set; }

        [Required]
        [MaxLength(255)]
        public string OptionC { get; set; }

        [Required]
        [MaxLength(255)]
        public string OptionD { get; set; }

        [Required]
        [MaxLength(1)]
        public string CorrectAnswer { get; set; }

        public string Explanation { get; set; }
        public string DefaultFeedback { get; set; }

        [Range(1, int.MaxValue)]
        public int Score { get; set; } = 1;

        public int? TimeLimitSeconds { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Subject Subject { get; set; }
        public ICollection<ExamAttemptAnswer> ExamAttemptAnswers { get; set; } = new List<ExamAttemptAnswer>();
    }
}
