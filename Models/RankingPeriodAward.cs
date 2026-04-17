using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class RankingPeriodAward
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Scope { get; set; }

        public int? ModuleId { get; set; }

        [Required]
        [MaxLength(20)]
        public string PeriodType { get; set; }

        public DateTime PeriodStartUtc { get; set; }
        public DateTime PeriodEndUtc { get; set; }
        public DateTime AwardedAtUtc { get; set; } = DateTime.UtcNow;

        public Module Module { get; set; }
    }
}
