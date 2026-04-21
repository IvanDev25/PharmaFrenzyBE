namespace Api.DTOs.Account
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public int? DurationMonths { get; set; }
        public bool IsLifetime { get; set; }
        public bool IsActive { get; set; }
    }
}
