namespace Api.DTOs.Account
{
    public class SubmitSingleExamAnswerResultDto
    {
        public bool AttemptCompleted { get; set; }
        public ExamAttemptAnswerResultDto Answer { get; set; }
        public ExamAttemptDto Attempt { get; set; }
    }
}
