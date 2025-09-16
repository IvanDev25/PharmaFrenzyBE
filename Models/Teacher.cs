using System.Collections.Generic;

namespace Api.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Subject { get; set; }

        // Navigation property for quizzes
        public ICollection<Quiz> Quizzes { get; set; }
    }
}
