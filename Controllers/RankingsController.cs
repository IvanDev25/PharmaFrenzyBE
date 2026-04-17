using Api.Data;
using Api.DTOs.Account;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Authorize(Roles = "Student,Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class RankingsController : ControllerBase
    {
        private const string DefaultRankingTimeZoneId = "Singapore Standard Time";
        private readonly Context _context;
        private readonly RankingBadgeService _rankingBadgeService;
        private readonly TimeZoneInfo _rankingTimeZone;

        public RankingsController(Context context, RankingBadgeService rankingBadgeService)
        {
            _context = context;
            _rankingBadgeService = rankingBadgeService;
            _rankingTimeZone = ResolveRankingTimeZone();
        }

        [HttpGet("global")]
        public async Task<ActionResult<RankingBoardDto>> GetGlobalRanking([FromQuery] int limit = 20)
        {
            var currentStudentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentStudentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            await _rankingBadgeService.EnsureGlobalBadgesAwardedAsync();

            limit = NormalizeLimit(limit);
            var nowUtc = DateTime.UtcNow;
            var period = GetMonthlyPeriod(nowUtc);

            var periodPoints = await _context.ExamAttempts
                .Where(x =>
                    x.Status == "Submitted" &&
                    x.SubmittedAt.HasValue &&
                    x.SubmittedAt.Value >= period.StartUtc &&
                    x.SubmittedAt.Value < period.ResetAtUtc)
                .GroupBy(x => x.StudentId)
                .Select(x => new
                {
                    StudentId = x.Key,
                    Points = x.Sum(y => (decimal)y.TotalScore)
                })
                .ToListAsync();

            var pointsByStudentId = periodPoints.ToDictionary(x => x.StudentId, x => x.Points);

            var rows = await _context.Users
                .Where(x => pointsByStudentId.Keys.Contains(x.Id))
                .Select(x => new RankingRow
                {
                    StudentId = x.Id,
                    StudentName = (x.FirstName + " " + x.LastName).Trim(),
                    StudentEmail = x.Email,
                    University = x.University,
                    Image = x.Image,
                    ExperiencePoints = x.ExperiencePoints,
                    Points = 0m
                })
                .ToListAsync();

            foreach (var row in rows)
            {
                row.Points = pointsByStudentId.TryGetValue(row.StudentId, out var points) ? points : 0m;
            }

            rows = rows
                .OrderByDescending(x => x.Points)
                .ThenBy(x => x.StudentName)
                .ToList();

            return Ok(BuildBoard("Global", null, null, period.Label, period.StartUtc, period.ResetAtUtc, currentStudentId, rows, limit));
        }

        [HttpGet("modules/{moduleId:int}")]
        public async Task<ActionResult<RankingBoardDto>> GetModuleRanking(int moduleId, [FromQuery] int limit = 20)
        {
            var currentStudentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentStudentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            await _rankingBadgeService.EnsureModuleBadgesAwardedAsync(moduleId);

            limit = NormalizeLimit(limit);
            var nowUtc = DateTime.UtcNow;
            var period = GetWeeklyPeriod(nowUtc);

            var module = await _context.Modules
                .Where(x => x.Id == moduleId)
                .Select(x => new { x.Id, x.Name })
                .FirstOrDefaultAsync();

            if (module == null)
            {
                return NotFound(new { Message = "Module not found." });
            }

            var periodPoints = await _context.ExamAttempts
                .Where(x =>
                    x.Status == "Submitted" &&
                    x.SubmittedAt.HasValue &&
                    x.SubmittedAt.Value >= period.StartUtc &&
                    x.SubmittedAt.Value < period.ResetAtUtc &&
                    x.Subject.ModuleId == moduleId)
                .GroupBy(x => x.StudentId)
                .Select(x => new
                {
                    StudentId = x.Key,
                    Points = x.Sum(y => (decimal)y.TotalScore)
                })
                .ToListAsync();

            var pointsByStudentId = periodPoints.ToDictionary(x => x.StudentId, x => x.Points);

            var rows = await _context.Users
                .Where(x => pointsByStudentId.Keys.Contains(x.Id))
                .Select(x => new RankingRow
                {
                    StudentId = x.Id,
                    StudentName = (x.FirstName + " " + x.LastName).Trim(),
                    StudentEmail = x.Email,
                    University = x.University,
                    Image = x.Image,
                    ExperiencePoints = x.ExperiencePoints,
                    Points = 0m
                })
                .ToListAsync();

            foreach (var row in rows)
            {
                row.Points = pointsByStudentId.TryGetValue(row.StudentId, out var points) ? points : 0m;
            }

            rows = rows
                .OrderByDescending(x => x.Points)
                .ThenBy(x => x.StudentName)
                .ToList();

            return Ok(BuildBoard("Module", module.Id, module.Name, period.Label, period.StartUtc, period.ResetAtUtc, currentStudentId, rows, limit));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("test-award-current")]
        public async Task<ActionResult> AwardCurrentRankingsForTesting([FromQuery] int topCount = 2)
        {
            var result = await _rankingBadgeService.AwardCurrentRankingsForTestingAsync(topCount);

            return Ok(new
            {
                Message = "Current ranking badges were awarded for testing.",
                TopCount = Math.Max(1, Math.Min(topCount, 3)),
                BadgesAwarded = result.badgesAwarded,
                PeriodsFinalized = result.periodsFinalized
            });
        }

        private static RankingBoardDto BuildBoard(
            string scope,
            int? moduleId,
            string moduleName,
            string periodLabel,
            DateTime periodStartUtc,
            DateTime resetAtUtc,
            string currentStudentId,
            List<RankingRow> rows,
            int limit)
        {
            var rankedRows = RankRows(rows, currentStudentId);
            var currentStudent = rankedRows.FirstOrDefault(x => x.StudentId == currentStudentId);

            return new RankingBoardDto
            {
                Scope = scope,
                ModuleId = moduleId,
                ModuleName = moduleName,
                PeriodLabel = periodLabel,
                PeriodStartUtc = periodStartUtc,
                ResetAtUtc = resetAtUtc,
                CurrentStudentRank = currentStudent?.Rank ?? 0,
                CurrentStudentPoints = currentStudent?.Points ?? 0m,
                Entries = rankedRows
                    .Take(limit)
                    .Select(x => new RankingEntryDto
                    {
                        Rank = x.Rank,
                        StudentId = x.StudentId,
                        StudentName = x.StudentName,
                        StudentEmail = x.StudentEmail,
                        University = x.University,
                        Image = x.Image,
                        Level = CalculateLevel(x.ExperiencePoints),
                        Points = x.Points,
                        IsCurrentStudent = x.StudentId == currentStudentId
                    })
                    .ToList()
            };
        }

        private static List<RankedRow> RankRows(List<RankingRow> rows, string currentStudentId)
        {
            var rankedRows = new List<RankedRow>();

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                rankedRows.Add(new RankedRow
                {
                    Rank = i + 1,
                    StudentId = row.StudentId,
                    StudentName = string.IsNullOrWhiteSpace(row.StudentName) ? "Unknown Student" : row.StudentName,
                    StudentEmail = row.StudentEmail,
                    University = row.University,
                    Image = row.Image,
                    ExperiencePoints = row.ExperiencePoints,
                    Points = row.Points,
                    IsCurrentStudent = row.StudentId == currentStudentId
                });
            }

            return rankedRows;
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit < 1)
            {
                return 20;
            }

            return Math.Min(limit, 100);
        }

        private static int CalculateLevel(decimal experiencePoints)
        {
            var level = 1;
            var remainingExperience = experiencePoints;

            while (remainingExperience >= level * 100m)
            {
                remainingExperience -= level * 100m;
                level++;
            }

            return level;
        }

        private RankingPeriod GetMonthlyPeriod(DateTime nowUtc)
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _rankingTimeZone);
            var startLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var resetLocal = startLocal.AddMonths(1);

            return new RankingPeriod
            {
                Label = "Resets every month",
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
                Label = "Resets every week",
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

        private class RankingRow
        {
            public string StudentId { get; set; }
            public string StudentName { get; set; }
            public string StudentEmail { get; set; }
            public string University { get; set; }
            public string Image { get; set; }
            public decimal ExperiencePoints { get; set; }
            public int Level { get; set; }
            public decimal Points { get; set; }
        }

        private sealed class RankedRow : RankingRow
        {
            public int Rank { get; set; }
            public bool IsCurrentStudent { get; set; }
        }

        private sealed class RankingPeriod
        {
            public string Label { get; set; }
            public DateTime StartUtc { get; set; }
            public DateTime ResetAtUtc { get; set; }
        }
    }
}
