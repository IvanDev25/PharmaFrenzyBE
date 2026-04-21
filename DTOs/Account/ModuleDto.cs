using System;

namespace Api.DTOs.Account
{
    public class ModuleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsPremium { get; set; }
        public bool RequiresSubscription { get; set; }
        public int SubjectCount { get; set; }
        public int QuestionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
