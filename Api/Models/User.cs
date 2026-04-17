using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string University { get; set; }
        public string Gender { get; set; }
        public string Status { get; set; } = "Active";
        public string Image { get; set; }
        public DateTime MyProperty { get; set; } = DateTime.UtcNow;
        public decimal TotalPoints { get; set; }
        public decimal ExperiencePoints { get; set; }
        public decimal RxCoinBalance { get; set; }
        public decimal RxCoinOnHold { get; set; }
        public int Level { get; set; } = 1;
        public int CurrentStreak { get; set; }
        public DateTime? LastDailyStreakClaimedAtUtc { get; set; }
        public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
        public ICollection<StudentModulePoint> ModulePoints { get; set; } = new List<StudentModulePoint>();
        public ICollection<StudentRankingBadge> RankingBadges { get; set; } = new List<StudentRankingBadge>();
        public ICollection<StudentWithdrawalRequest> WithdrawalRequests { get; set; } = new List<StudentWithdrawalRequest>();
    }
}
