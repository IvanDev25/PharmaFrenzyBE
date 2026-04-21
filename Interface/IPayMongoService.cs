using Api.Models;
using Api.Services;
using System.Threading.Tasks;

namespace Api.Interface
{
    public interface IPayMongoService
    {
        Task<PayMongoCheckoutResult> CreateCheckoutSessionAsync(
            SubscriptionPlan plan,
            SubscriptionPayment payment,
            User student);

        bool VerifyWebhookSignature(string rawBody, string signatureHeader);
    }
}
