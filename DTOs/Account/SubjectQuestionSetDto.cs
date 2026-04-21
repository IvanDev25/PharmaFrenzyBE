namespace Api.DTOs.Account
{
    public class SubjectQuestionSetDto
    {
        public int QuestionSetNumber { get; set; }
        public int QuestionCount { get; set; }
        public bool IsPremium { get; set; }
        public bool RequiresSubscription { get; set; }
    }
}
