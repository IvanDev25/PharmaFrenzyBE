namespace Api.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public int QuizId { get; set; }

        // Navigation property
        public Quiz Quiz { get; set; }
    }
}
