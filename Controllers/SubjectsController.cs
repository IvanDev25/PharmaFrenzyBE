using Api.Data;
using Api.DTOs.Account;
using Api.Interface;
using Api.Models;
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
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectsController : ControllerBase
    {
        private readonly Context _context;
        private readonly IPremiumAccessService _premiumAccessService;

        public SubjectsController(Context context, IPremiumAccessService premiumAccessService)
        {
            _context = context;
            _premiumAccessService = premiumAccessService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjects([FromQuery] int? moduleId)
        {
            var query = _context.Subjects
                .Include(x => x.Module)
                .Include(x => x.Questions)
                .Include(x => x.QuestionSetAccesses)
                .AsQueryable();

            if (moduleId.HasValue)
            {
                query = query.Where(x => x.ModuleId == moduleId.Value);
            }

            var isStudent = User.IsInRole("Student");
            var hasPremiumAccess = await CurrentStudentHasPremiumAccessAsync();
            var subjectEntities = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            var subjects = subjectEntities
                .Select(x => MapSubject(
                    x,
                    x.Questions.Count(q => q.IsActive),
                    isStudent,
                    hasPremiumAccess))
                .ToList();

            return Ok(subjects);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SubjectDto>> GetSubject(int id)
        {
            var isStudent = User.IsInRole("Student");
            var hasPremiumAccess = await CurrentStudentHasPremiumAccessAsync();
            var subject = await _context.Subjects
                .Include(x => x.Module)
                .Include(x => x.Questions)
                .Include(x => x.QuestionSetAccesses)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (subject == null)
            {
                return NotFound(new { Message = "Subject not found." });
            }

            return Ok(MapSubject(subject, subject.Questions.Count(q => q.IsActive), isStudent, hasPremiumAccess));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<SubjectDto>> CreateSubject(SubjectCreateDto model)
        {
            var normalizedName = model.Name.Trim();
            if (!await _context.Modules.AnyAsync(x => x.Id == model.ModuleId))
            {
                return BadRequest(new { Message = "Module not found." });
            }

            if (await _context.Subjects.AnyAsync(x => x.ModuleId == model.ModuleId && x.Name.ToLower() == normalizedName.ToLower()))
            {
                return BadRequest(new { Message = "Subject name already exists in this module." });
            }

            var subject = new Subject
            {
                ModuleId = model.ModuleId,
                Name = normalizedName,
                Description = model.Description?.Trim(),
                IsActive = model.IsActive,
                IsPremium = model.IsPremium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            subject = await _context.Subjects
                .Include(x => x.Module)
                .Include(x => x.Questions)
                .Include(x => x.QuestionSetAccesses)
                .FirstAsync(x => x.Id == subject.Id);

            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, MapSubject(subject, 0, false, true));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<SubjectDto>> UpdateSubject(int id, SubjectCreateDto model)
        {
            var subject = await _context.Subjects
                .Include(x => x.Module)
                .Include(x => x.Questions)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (subject == null)
            {
                return NotFound(new { Message = "Subject not found." });
            }

            var normalizedName = model.Name.Trim();
            if (!await _context.Modules.AnyAsync(x => x.Id == model.ModuleId))
            {
                return BadRequest(new { Message = "Module not found." });
            }

            if (await _context.Subjects.AnyAsync(x => x.Id != id && x.ModuleId == model.ModuleId && x.Name.ToLower() == normalizedName.ToLower()))
            {
                return BadRequest(new { Message = "Subject name already exists in this module." });
            }

            subject.ModuleId = model.ModuleId;
            subject.Name = normalizedName;
            subject.Description = model.Description?.Trim();
            subject.IsActive = model.IsActive;
            subject.IsPremium = model.IsPremium;
            subject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            subject = await _context.Subjects
                .Include(x => x.Module)
                .Include(x => x.Questions)
                .Include(x => x.QuestionSetAccesses)
                .FirstAsync(x => x.Id == id);

            return Ok(MapSubject(subject, subject.Questions.Count(q => q.IsActive), false, true));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects.Include(x => x.Questions).FirstOrDefaultAsync(x => x.Id == id);
            if (subject == null)
            {
                return NotFound(new { Message = "Subject not found." });
            }

            if (subject.Questions.Any())
            {
                return BadRequest(new { Message = "Cannot delete a subject that already has questions." });
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Subject deleted successfully." });
        }

        private async Task<bool> CurrentStudentHasPremiumAccessAsync()
        {
            if (!User.IsInRole("Student"))
            {
                return true;
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _premiumAccessService.HasActivePremiumAccessAsync(studentId);
        }

        private static SubjectDto MapSubject(
            Subject subject,
            int questionCount,
            bool isStudent,
            bool hasPremiumAccess)
        {
            var questionSets = subject.Questions?
                .Where(q => q.IsActive)
                .GroupBy(q => q.QuestionSetNumber)
                .OrderBy(group => group.Key)
                .Select(group => new SubjectQuestionSetDto
                {
                    QuestionSetNumber = group.Key,
                    QuestionCount = group.Count(),
                    IsPremium = IsQuestionSetPremium(subject, group.Key),
                    RequiresSubscription = isStudent &&
                        IsQuestionSetPremium(subject, group.Key) &&
                        !hasPremiumAccess
                })
                .ToList() ?? new List<SubjectQuestionSetDto>();

            var subjectIsPremium = subject.IsPremium || subject.Module?.IsPremium == true;

            return new SubjectDto
            {
                Id = subject.Id,
                ModuleId = subject.ModuleId,
                ModuleName = subject.Module?.Name,
                Name = subject.Name,
                Description = subject.Description,
                IsActive = subject.IsActive,
                IsPremium = subject.IsPremium,
                RequiresSubscription = isStudent && subjectIsPremium && !hasPremiumAccess,
                QuestionCount = questionCount,
                SetCount = questionSets.Count,
                QuestionSets = questionSets,
                CreatedAt = subject.CreatedAt,
                UpdatedAt = subject.UpdatedAt
            };
        }

        private static bool IsQuestionSetPremium(Subject subject, int questionSetNumber)
        {
            return subject.Module?.IsPremium == true ||
                subject.IsPremium ||
                subject.QuestionSetAccesses?.Any(x => x.QuestionSetNumber == questionSetNumber && x.IsPremium) == true;
        }
    }
}
