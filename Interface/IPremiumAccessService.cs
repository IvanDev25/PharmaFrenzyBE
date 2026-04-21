using System.Threading.Tasks;

namespace Api.Interface
{
    public interface IPremiumAccessService
    {
        Task<bool> HasActivePremiumAccessAsync(string studentId);
        Task<bool> CanAccessQuestionSetAsync(string studentId, int subjectId, int questionSetNumber);
        Task<bool> IsQuestionSetPremiumAsync(int subjectId, int questionSetNumber);
    }
}
