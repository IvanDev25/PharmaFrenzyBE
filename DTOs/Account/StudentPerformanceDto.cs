using System;

namespace Api.DTOs.Account
{
    public class StudentPerformanceDto
    {
        public int AttemptId { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int QuestionSetNumber { get; set; }
        public string Status { get; set; }
        public int TotalScore { get; set; }
        public int TotalPossibleScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
