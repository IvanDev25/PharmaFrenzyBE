namespace Api.DTOs.Account
{
    public class SubscriptionCheckoutDto
    {
        public int PaymentId { get; set; }
        public string CheckoutUrl { get; set; }
        public string PaymongoCheckoutSessionId { get; set; }
    }
}
