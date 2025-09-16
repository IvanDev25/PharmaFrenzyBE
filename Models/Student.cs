using System.Collections.Generic;

namespace Api.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Section { get; set; }
        public string EmailAddress { get; set; }

        // Navigation property for quiz attempts
        public ICollection<QuizAttempt> QuizAttempts { get; set; }
    }
}
