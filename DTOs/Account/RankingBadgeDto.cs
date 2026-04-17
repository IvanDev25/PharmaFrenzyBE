namespace Api.DTOs.Account
{
    public class RankingBadgeDto
    {
        public string Scope { get; set; }
        public int Rank { get; set; }
        public string PeriodType { get; set; }
        public int? ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int Count { get; set; }
    }
}
