using System;

namespace Api.Models
{
    public class QuestionSetAccess
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int QuestionSetNumber { get; set; }
        public bool IsPremium { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Subject Subject { get; set; }
    }
}
