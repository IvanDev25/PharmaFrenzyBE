using System;
using System.Collections.Generic;

namespace Api.DTOs.Account
{
    public class RankingBoardDto
    {
        public string Scope { get; set; }
        public int? ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string PeriodLabel { get; set; }
        public DateTime PeriodStartUtc { get; set; }
        public DateTime ResetAtUtc { get; set; }
        public int CurrentStudentRank { get; set; }
        public decimal CurrentStudentPoints { get; set; }
        public List<RankingEntryDto> Entries { get; set; } = new List<RankingEntryDto>();
    }
}
