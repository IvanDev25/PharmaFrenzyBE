namespace Api.DTOs.Account
{
    public class StudentQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public StudentQuestionOptionDto[] Options { get; set; } = System.Array.Empty<StudentQuestionOptionDto>();
        public int Score { get; set; }
        public int? TimeLimitSeconds { get; set; }
    }
}
