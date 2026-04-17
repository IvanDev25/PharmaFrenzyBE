using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ExamAttemptAnswer
    {
        public int Id { get; set; }
        public int ExamAttemptId { get; set; }
        public int QuestionId { get; set; }

        [MaxLength(1)]
        public string SelectedAnswer { get; set; }

        [Required]
        [MaxLength(1)]
        public string CorrectAnswer { get; set; }

        public bool IsCorrect { get; set; }
        public int ScoreEarned { get; set; }
        public int TimeSpentSeconds { get; set; }
        public string Feedback { get; set; }
        public DateTime? AnsweredAt { get; set; }

        public ExamAttempt ExamAttempt { get; set; }
        public Question Question { get; set; }
    }
}
