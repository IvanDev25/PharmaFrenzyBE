using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    public class QuestionCreateDto
    {
        [Required]
        public int SubjectId { get; set; }

        [Range(1, int.MaxValue)]
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
        public bool IsQuestionSetPremium { get; set; }
    }
}
