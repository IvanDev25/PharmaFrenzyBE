using Api.Constant;
using Api.Data;
using Api.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Api.Services
{
    public class PremiumAccessService : IPremiumAccessService
    {
        private readonly Context _context;

        public PremiumAccessService(Context context)
        {
            _context = context;
        }

        public async Task<bool> HasActivePremiumAccessAsync(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return false;
            }

            var utcNow = DateTime.UtcNow;
            return await _context.StudentSubscriptions.AnyAsync(x =>
                x.StudentId == studentId &&
                x.Status == SubscriptionStatuses.Active &&
                (x.IsLifetime || (x.ExpiresAt.HasValue && x.ExpiresAt.Value > utcNow)));
        }

        public async Task<bool> CanAccessQuestionSetAsync(string studentId, int subjectId, int questionSetNumber)
        {
            if (!await IsQuestionSetPremiumAsync(subjectId, questionSetNumber))
            {
                return true;
            }

            return await HasActivePremiumAccessAsync(studentId);
        }

        public async Task<bool> IsQuestionSetPremiumAsync(int subjectId, int questionSetNumber)
        {
            var subject = await _context.Subjects
                .Include(x => x.Module)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == subjectId);

            if (subject == null)
            {
                return false;
            }

            if (subject.IsPremium || subject.Module.IsPremium)
            {
                return true;
            }

            return await _context.QuestionSetAccesses.AnyAsync(x =>
                x.SubjectId == subjectId &&
                x.QuestionSetNumber == questionSetNumber &&
                x.IsPremium);
        }
    }
}
