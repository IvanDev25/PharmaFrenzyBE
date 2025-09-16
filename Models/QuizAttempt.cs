using System.Collections.Generic;
using System;

namespace Api.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int QuizId { get; set; }
        public DateTime AttemptedAt { get; set; }
        public int Score { get; set; }

        // Navigation properties
        public Student Student { get; set; }
        public Quiz Quiz { get; set; }
        public ICollection<Answer> Answers { get; set; }
    }
}
