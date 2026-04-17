using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class StudentRankingBadge
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Scope { get; set; }

        public int? ModuleId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PeriodType { get; set; }

        public int Rank { get; set; }
        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
        public DateTime AwardedAtUtc { get; set; } = DateTime.UtcNow;

        public User Student { get; set; }
        public Module Module { get; set; }
    }
}
