using System.Collections.Generic;
using System;

namespace Api.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TeacherId { get; set; }

        // Navigation properties
        public Teacher Teacher { get; set; }
        public ICollection<Question> Questions { get; set; }
    }
}
