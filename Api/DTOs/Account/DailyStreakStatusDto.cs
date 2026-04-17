namespace Api.DTOs.Account
{
    public class DailyStreakStatusDto
    {
        public int CurrentStreak { get; set; }
        public bool CanRedeemToday { get; set; }
        public decimal RewardPoints { get; set; }
        public decimal TotalPoints { get; set; }
        public string CurrentLocalDate { get; set; }
        public string LastClaimLocalDate { get; set; }
    }
}
