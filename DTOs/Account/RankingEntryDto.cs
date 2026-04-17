namespace Api.DTOs.Account
{
    public class RankingEntryDto
    {
        public int Rank { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string University { get; set; }
        public string Image { get; set; }
        public int Level { get; set; }
        public decimal Points { get; set; }
        public bool IsCurrentStudent { get; set; }
    }
}
