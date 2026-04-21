using System.Collections.Generic;

namespace Api.Settings
{
    public class PayMongoSettings
    {
        public const string SectionName = "PayMongo";

        public string BaseUrl { get; set; } = "https://api.paymongo.com/v1";
        public string SecretKey { get; set; }
        public string WebhookSecret { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        public string Currency { get; set; } = "PHP";
        public int TimeoutSeconds { get; set; } = 30;
        public int WebhookTimestampToleranceSeconds { get; set; }
        public bool SendEmailReceipt { get; set; } = true;
        public List<string> PaymentMethodTypes { get; set; } = new List<string>
        {
            "card",
            "gcash",
            "paymaya"
        };
    }
}
