using Api.Data;
using Api.DTOs.Account;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectsController : ControllerBase
    {
        private readonly Context _context;

        public SubjectsController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjects([FromQuery] int? moduleId)
        {
            var query = _context.Subjects.Include(x => x.Module).AsQueryable();

            if (moduleId.HasValue)
            {
                query = query.Where(x => x.ModuleId == moduleId.Value);
            }

            var subjects = await query
                .OrderBy(x => x.Name)
                .Select(x => new SubjectDto
                {
                    Id = x.Id,
                    ModuleId = x.ModuleId,
                    ModuleName = x.Module.Name,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    QuestionCount = x.Questions.Count(q => q.IsActive),
                    SetCount = x.Questions.Where(q => q.IsActive).Select(q => q.QuestionSetNumber).Distinct().Count(),
                    QuestionSets = x.Questions
                        .Where(q => q.IsActive)
                        .GroupBy(q => q.QuestionSetNumber)
                        .OrderBy(group => group.Key)
                        .Select(group => new SubjectQuestionSetDto
                        {
                            QuestionSetNumber = group.Key,
                            QuestionCount = group.Count()
                        })
                        .ToList(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(subjects);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SubjectDto>> GetSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(x => x.Module)
                .Where(x => x.Id == id)
                .Select(x => new SubjectDto
                {
                    Id = x.Id,
                    ModuleId = x.ModuleId,
                    ModuleName = x.Module.Name,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    QuestionCount = x.Questions.Count(q => q.IsActive),
                    SetCount = x.Questions.Where(q => q.IsActive).Select(q => q.QuestionSetNumber).Distinct().Count(),
                    QuestionSets = x.Questions
                        .Where(q => q.IsActive)
                        .GroupBy(q => q.QuestionSetNumber)
                        .OrderBy(group => group.Key)
                        .Select(group => new SubjectQuestionSetDto
                        {
                            QuestionSetNumber = group.Key,
                            QuestionCount = group.Count()
                        })
                        .ToList(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (subject == null)
            {
                return NotFound(new { Message = "Subject not found." });
            }

            return Ok(subject);
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            subject = await _context.Subjects.Include(x => x.Module).FirstAsync(x => x.Id == subject.Id);

            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, MapSubject(subject, 0));
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
            subject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            subject = await _context.Subjects
                .Include(x => x.Module)
                .Include(x => x.Questions)
                .FirstAsync(x => x.Id == id);

            return Ok(MapSubject(subject, subject.Questions.Count(q => q.IsActive)));
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

        private static SubjectDto MapSubject(Subject subject, int questionCount)
        {
            var questionSets = subject.Questions?
                .Where(q => q.IsActive)
                .GroupBy(q => q.QuestionSetNumber)
                .OrderBy(group => group.Key)
                .Select(group => new SubjectQuestionSetDto
                {
                    QuestionSetNumber = group.Key,
                    QuestionCount = group.Count()
                })
                .ToList() ?? new List<SubjectQuestionSetDto>();

            return new SubjectDto
            {
                Id = subject.Id,
                ModuleId = subject.ModuleId,
                ModuleName = subject.Module?.Name,
                Name = subject.Name,
                Description = subject.Description,
                IsActive = subject.IsActive,
                QuestionCount = questionCount,
                SetCount = questionSets.Count,
                QuestionSets = questionSets,
                CreatedAt = subject.CreatedAt,
                UpdatedAt = subject.UpdatedAt
            };
        }
    }
}
