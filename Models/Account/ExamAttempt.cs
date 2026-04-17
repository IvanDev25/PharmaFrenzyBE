using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ExamAttempt
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int QuestionSetNumber { get; set; } = 1;

        [Required]
        public string StudentId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public int TotalScore { get; set; }
        public int TotalPossibleScore { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "InProgress";

        public string OverallFeedback { get; set; }
        public string QuestionOrderJson { get; set; }

        public Subject Subject { get; set; }
        public User Student { get; set; }
        public ICollection<ExamAttemptAnswer> Answers { get; set; } = new List<ExamAttemptAnswer>();
    }
}
