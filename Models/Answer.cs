namespace Api.Models
{
    public class Answer
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int QuizAttemptId { get; set; }
        public string SelectedOption { get; set; }

        // Navigation properties
        public Question Question { get; set; }
        public QuizAttempt QuizAttempt { get; set; }
    }
}
