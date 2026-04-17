namespace Api.DTOs.Account
{
    public class ExamAttemptAnswerResultDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string SelectedAnswer { get; set; }
        public string SelectedAnswerText { get; set; }
        public string CorrectAnswer { get; set; }
        public string CorrectAnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public int ScoreEarned { get; set; }
        public int TimeSpentSeconds { get; set; }
        public string Explanation { get; set; }
        public string Feedback { get; set; }
    }
}
