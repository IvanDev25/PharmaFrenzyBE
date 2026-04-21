using Api.DTOs.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Interface
{
    public interface ISubscriptionService
    {
        Task<List<SubscriptionPlanDto>> GetPlansAsync();
        Task<SubscriptionStatusDto> GetStudentSubscriptionStatusAsync(string studentId);
        Task<SubscriptionCheckoutDto> CreateCheckoutAsync(string studentId, string planCode);
        Task ProcessPayMongoWebhookAsync(string rawBody);
    }
}
