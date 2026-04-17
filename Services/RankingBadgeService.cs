using Api.Data;
using Api.DTOs.Account;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Services
{
    public class RankingBadgeService
    {
        private const string ScopeGlobal = "Global";
        private const string ScopeModule = "Module";
        private const string PeriodTypeMonthly = "Monthly";
        private const string PeriodTypeWeekly = "Weekly";
        private const string DefaultRankingTimeZoneId = "Singapore Standard Time";
        public const decimal RxCoinPerPeso = 100m;

        private readonly Context _context;
        private readonly TimeZoneInfo _rankingTimeZone;

        public RankingBadgeService(Context context)
        {
            _context = context;
            _rankingTimeZone = ResolveRankingTimeZone();
        }

        public async Task EnsurePendingBadgesAwardedAsync()
        {
            var nowUtc = DateTime.UtcNow;

            await AwardGlobalPeriodIfNeededAsync(GetPreviousMonthlyPeriod(nowUtc), 3, PeriodTypeMonthly);

            var activeModuleIds = await _context.Modules
                .Select(x => x.Id)
                .ToListAsync();

            var previousWeeklyPeriod = GetPreviousWeeklyPeriod(nowUtc);
            foreach (var moduleId in activeModuleIds)
            {
                await AwardModulePeriodIfNeededAsync(moduleId, previousWeeklyPeriod, 3, PeriodTypeWeekly);
            }
        }

        public async Task EnsureGlobalBadgesAwardedAsync()
        {
            await AwardGlobalPeriodIfNeededAsync(GetPreviousMonthlyPeriod(DateTime.UtcNow), 3, PeriodTypeMonthly);
        }

        public async Task EnsureModuleBadgesAwardedAsync(int moduleId)
        {
            await AwardModulePeriodIfNeededAsync(moduleId, GetPreviousWeeklyPeriod(DateTime.UtcNow), 3, PeriodTypeWeekly);
        }

        public async Task<(int badgesAwarded, int periodsFinalized)> AwardCurrentRankingsForTestingAsync(int topCount = 2)
        {
            var normalizedTopCount = Math.Max(1, Math.Min(topCount, 3));
            var nowUtc = DateTime.UtcNow;
            var badgesAwarded = 0;
            var periodsFinalized = 0;

            var (globalBadgesAwarded, globalPeriodFinalized) = await AwardGlobalPeriodIfNeededAsync(
                GetMonthlyPeriod(nowUtc),
                normalizedTopCount,
                PeriodTypeMonthly);
            badgesAwarded += globalBadgesAwarded;
            periodsFinalized += globalPeriodFinalized ? 1 : 0;

            var moduleIds = await _context.Modules
                .Select(x => x.Id)
                .ToListAsync();

            foreach (var moduleId in moduleIds)
            {
                var (moduleBadgesAwarded, modulePeriodFinalized) = await AwardModulePeriodIfNeededAsync(
                    moduleId,
                    GetWeeklyPeriod(nowUtc),
                    normalizedTopCount,
                    PeriodTypeWeekly);
                badgesAwarded += moduleBadgesAwarded;
                periodsFinalized += modulePeriodFinalized ? 1 : 0;
            }

            return (badgesAwarded, periodsFinalized);
        }

        public async Task<List<RankingBadgeDto>> GetStudentBadgeSummaryAsync(string studentId)
        {
            return await _context.StudentRankingBadges
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .GroupBy(x => new
                {
                    x.Scope,
                    x.Rank,
                    x.PeriodType,
                    x.ModuleId,
                    ModuleName = x.Module != null ? x.Module.Name : null
                })
                .Select(x => new RankingBadgeDto
                {
                    Scope = x.Key.Scope,
                    Rank = x.Key.Rank,
                    PeriodType = x.Key.PeriodType,
                    ModuleId = x.Key.ModuleId,
                    ModuleName = x.Key.ModuleName,
                    Count = x.Count()
                })
                .OrderBy(x => x.Scope)
                .ThenBy(x => x.ModuleName)
                .ThenBy(x => x.Rank)
                .ToListAsync();
        }

        private async Task<(int badgesAwarded, bool periodFinalized)> AwardGlobalPeriodIfNeededAsync(
            RankingPeriod period,
            int topCount,
            string periodType)
        {
            if (await PeriodAlreadyAwardedAsync(ScopeGlobal, null, periodType, period))
            {
                return (0, false);
            }

            var topRows = await _context.ExamAttempts
                .Where(x =>
                    x.Status == "Submitted" &&
                    x.SubmittedAt.HasValue &&
                    x.SubmittedAt.Value >= period.StartUtc &&
                    x.SubmittedAt.Value < period.ResetAtUtc)
                .GroupBy(x => x.StudentId)
                .Select(x => new RankedStudent
                {
                    StudentId = x.Key,
                    Points = x.Sum(y => (decimal)y.TotalScore)
                })
                .OrderByDescending(x => x.Points)
                .ThenBy(x => x.StudentId)
                .Take(topCount)
                .ToListAsync();

            var rewardedStudents = topRows
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var studentsById = await _context.Users
                .Where(x => rewardedStudents.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            foreach (var entry in topRows.Select((value, index) => new { value.StudentId, Rank = index + 1 }))
            {
                var reward = GetReward(ScopeGlobal, entry.Rank);
                _context.StudentRankingBadges.Add(new StudentRankingBadge
                {
                    StudentId = entry.StudentId,
                    Scope = ScopeGlobal,
                    PeriodType = periodType,
                    Rank = entry.Rank,
                    PeriodStartUtc = period.StartUtc,
                    PeriodEndUtc = period.ResetAtUtc
                });

                if (studentsById.TryGetValue(entry.StudentId, out var student))
                {
                    student.RxCoinBalance += reward.rxCoin;
                    student.TotalPoints += reward.points;
                }
            }

            _context.RankingPeriodAwards.Add(new RankingPeriodAward
            {
                Scope = ScopeGlobal,
                PeriodType = periodType,
                PeriodStartUtc = period.StartUtc,
                PeriodEndUtc = period.ResetAtUtc
            });

            await _context.SaveChangesAsync();
            return (topRows.Count, true);
        }

        private async Task<(int badgesAwarded, bool periodFinalized)> AwardModulePeriodIfNeededAsync(
            int moduleId,
            RankingPeriod period,
            int topCount,
            string periodType)
        {
            if (await PeriodAlreadyAwardedAsync(ScopeModule, moduleId, periodType, period))
            {
                return (0, false);
            }

            var moduleExists = await _context.Modules.AnyAsync(x => x.Id == moduleId);
            if (!moduleExists)
            {
                return (0, false);
            }

            var topRows = await _context.ExamAttempts
                .Where(x =>
                    x.Status == "Submitted" &&
                    x.SubmittedAt.HasValue &&
                    x.SubmittedAt.Value >= period.StartUtc &&
                    x.SubmittedAt.Value < period.ResetAtUtc &&
                    x.Subject.ModuleId == moduleId)
                .GroupBy(x => x.StudentId)
                .Select(x => new RankedStudent
                {
                    StudentId = x.Key,
                    Points = x.Sum(y => (decimal)y.TotalScore)
                })
                .OrderByDescending(x => x.Points)
                .ThenBy(x => x.StudentId)
                .Take(topCount)
                .ToListAsync();

            var rewardedStudents = topRows
                .Select(x => x.StudentId)
                .Distinct()
                .ToList();

            var studentsById = await _context.Users
                .Where(x => rewardedStudents.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            foreach (var entry in topRows.Select((value, index) => new { value.StudentId, Rank = index + 1 }))
            {
                var reward = GetReward(ScopeModule, entry.Rank);
                _context.StudentRankingBadges.Add(new StudentRankingBadge
                {
                    StudentId = entry.StudentId,
                    Scope = ScopeModule,
                    ModuleId = moduleId,
                    PeriodType = periodType,
                    Rank = entry.Rank,
                    PeriodStartUtc = period.StartUtc,
                    PeriodEndUtc = period.ResetAtUtc
                });

                if (studentsById.TryGetValue(entry.StudentId, out var student))
                {
                    student.RxCoinBalance += reward.rxCoin;
                    student.TotalPoints += reward.points;
                }
            }

            _context.RankingPeriodAwards.Add(new RankingPeriodAward
            {
                Scope = ScopeModule,
                ModuleId = moduleId,
                PeriodType = periodType,
                PeriodStartUtc = period.StartUtc,
                PeriodEndUtc = period.ResetAtUtc
            });

            await _context.SaveChangesAsync();
            return (topRows.Count, true);
        }

        private Task<bool> PeriodAlreadyAwardedAsync(string scope, int? moduleId, string periodType, RankingPeriod period)
        {
            return _context.RankingPeriodAwards.AnyAsync(x =>
                x.Scope == scope &&
                x.ModuleId == moduleId &&
                x.PeriodType == periodType &&
                x.PeriodStartUtc == period.StartUtc &&
                x.PeriodEndUtc == period.ResetAtUtc);
        }

        private RankingPeriod GetPreviousMonthlyPeriod(DateTime nowUtc)
        {
            var currentPeriod = GetMonthlyPeriod(nowUtc);
            var currentLocalStart = TimeZoneInfo.ConvertTimeFromUtc(currentPeriod.StartUtc, _rankingTimeZone);
            var previousLocalStart = currentLocalStart.AddMonths(-1);

            return new RankingPeriod
            {
                StartUtc = TimeZoneInfo.ConvertTimeToUtc(previousLocalStart, _rankingTimeZone),
                ResetAtUtc = currentPeriod.StartUtc
            };
        }

        private RankingPeriod GetPreviousWeeklyPeriod(DateTime nowUtc)
        {
            var currentPeriod = GetWeeklyPeriod(nowUtc);
            var currentLocalStart = TimeZoneInfo.ConvertTimeFromUtc(currentPeriod.StartUtc, _rankingTimeZone);
            var previousLocalStart = currentLocalStart.AddDays(-7);

            return new RankingPeriod
            {
                StartUtc = TimeZoneInfo.ConvertTimeToUtc(previousLocalStart, _rankingTimeZone),
                ResetAtUtc = currentPeriod.StartUtc
            };
        }

        private RankingPeriod GetMonthlyPeriod(DateTime nowUtc)
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _rankingTimeZone);
            var startLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var resetLocal = startLocal.AddMonths(1);

            return new RankingPeriod
            {
                StartUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _rankingTimeZone),
                ResetAtUtc = TimeZoneInfo.ConvertTimeToUtc(resetLocal, _rankingTimeZone)
            };
        }

        private RankingPeriod GetWeeklyPeriod(DateTime nowUtc)
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _rankingTimeZone);
            var daysSinceMonday = ((int)localNow.DayOfWeek + 6) % 7;
            var startDate = localNow.Date.AddDays(-daysSinceMonday);
            var startLocal = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var resetLocal = startLocal.AddDays(7);

            return new RankingPeriod
            {
                StartUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, _rankingTimeZone),
                ResetAtUtc = TimeZoneInfo.ConvertTimeToUtc(resetLocal, _rankingTimeZone)
            };
        }

        private static TimeZoneInfo ResolveRankingTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DefaultRankingTimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.Utc;
            }
        }

        public static decimal ConvertRxCoinToPeso(decimal rxCoinAmount)
        {
            if (rxCoinAmount <= 0m)
            {
                return 0m;
            }

            return Math.Round(rxCoinAmount / RxCoinPerPeso, 2, MidpointRounding.AwayFromZero);
        }

        public static (decimal rxCoin, decimal points) GetReward(string scope, int rank)
        {
            if (rank <= 0)
            {
                return (0m, 0m);
            }

            if (string.Equals(scope, ScopeGlobal, StringComparison.OrdinalIgnoreCase))
            {
                return rank switch
                {
                    1 => (20m, 30m),
                    2 => (10m, 15m),
                    3 => (5m, 7m),
                    _ => (0m, 0m)
                };
            }

            return rank switch
            {
                1 => (10m, 15m),
                2 => (5m, 7m),
                3 => (2.5m, 3.5m),
                _ => (0m, 0m)
            };
        }

        private sealed class RankingPeriod
        {
            public DateTime StartUtc { get; set; }
            public DateTime ResetAtUtc { get; set; }
        }

        private sealed class RankedStudent
        {
            public string StudentId { get; set; }
            public decimal Points { get; set; }
        }
    }
}
