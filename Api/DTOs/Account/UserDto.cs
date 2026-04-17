using System.Collections.Generic;

namespace Api.DTOs.Account
{
    public class UserDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string University { get; set; }
        public string Gender { get; set; }
        public string Status { get; set; }
        public string Image { get; set; }
        public string Role { get; set; }
        public decimal TotalPoints { get; set; }
        public decimal ExperiencePoints { get; set; }
        public decimal RxCoinBalance { get; set; }
        public decimal RxCoinOnHold { get; set; }
        public int Level { get; set; }
        public int CurrentStreak { get; set; }
        public bool CanRedeemDailyStreakToday { get; set; }
        public decimal DailyStreakRewardPoints { get; set; }
        public List<RankingBadgeDto> RankingBadges { get; set; } = new List<RankingBadgeDto>();
        public string JWT { get; set; }
    }
}
