using System;
using System.Collections.Generic;

namespace Api.DTOs.Account
{
    public class SubjectDto
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsPremium { get; set; }
        public bool RequiresSubscription { get; set; }
        public int QuestionCount { get; set; }
        public int SetCount { get; set; }
        public List<SubjectQuestionSetDto> QuestionSets { get; set; } = new List<SubjectQuestionSetDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
