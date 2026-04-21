using System;

namespace Api.DTOs.Account
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int QuestionSetNumber { get; set; }
        public string QuestionText { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
        public string DefaultFeedback { get; set; }
        public int Score { get; set; }
        public int? TimeLimitSeconds { get; set; }
        public bool IsActive { get; set; }
        public bool IsQuestionSetPremium { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
