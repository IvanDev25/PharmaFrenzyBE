using System;
using System.Collections.Generic;

namespace Api.DTOs.Account
{
    public class ExamAttemptDto
    {
        public int AttemptId { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int QuestionSetNumber { get; set; }
        public string StudentId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; }
        public int TotalScore { get; set; }
        public int TotalPossibleScore { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public string OverallFeedback { get; set; }
        public decimal ExperienceGained { get; set; }
        public decimal StudentExperiencePoints { get; set; }
        public int PreviousStudentLevel { get; set; }
        public int StudentLevel { get; set; }
        public bool LeveledUp { get; set; }
        public List<StudentQuestionDto> Questions { get; set; } = new List<StudentQuestionDto>();
        public List<ExamAttemptAnswerResultDto> Answers { get; set; } = new List<ExamAttemptAnswerResultDto>();
    }
}
