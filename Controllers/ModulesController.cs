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
using ExamModule = Api.Models.Module;

namespace Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly Context _context;
        private readonly IPremiumAccessService _premiumAccessService;

        public ModulesController(Context context, IPremiumAccessService premiumAccessService)
        {
            _context = context;
            _premiumAccessService = premiumAccessService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ModuleDto>>> GetModules()
        {
            var isStudent = User.IsInRole("Student");
            var hasPremiumAccess = await CurrentStudentHasPremiumAccessAsync();
            var modules = await _context.Modules
                .OrderBy(x => x.Name)
                .Select(x => new ModuleDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    IsPremium = x.IsPremium,
                    RequiresSubscription = isStudent && x.IsPremium && !hasPremiumAccess,
                    SubjectCount = x.Subjects.Count(s => s.IsActive),
                    QuestionCount = x.Subjects.SelectMany(s => s.Questions).Count(q => q.IsActive),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(modules);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ModuleDto>> GetModule(int id)
        {
            var isStudent = User.IsInRole("Student");
            var hasPremiumAccess = await CurrentStudentHasPremiumAccessAsync();
            var module = await _context.Modules
                .Where(x => x.Id == id)
                .Select(x => new ModuleDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    IsPremium = x.IsPremium,
                    RequiresSubscription = isStudent && x.IsPremium && !hasPremiumAccess,
                    SubjectCount = x.Subjects.Count(s => s.IsActive),
                    QuestionCount = x.Subjects.SelectMany(s => s.Questions).Count(q => q.IsActive),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (module == null)
            {
                return NotFound(new { Message = "Module not found." });
            }

            return Ok(module);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ModuleDto>> CreateModule(ModuleCreateDto model)
        {
            var normalizedName = model.Name.Trim();
            if (await _context.Modules.AnyAsync(x => x.Name.ToLower() == normalizedName.ToLower()))
            {
                return BadRequest(new { Message = "Module name already exists." });
            }

            var module = new ExamModule
            {
                Name = normalizedName,
                Description = model.Description?.Trim(),
                IsActive = model.IsActive,
                IsPremium = model.IsPremium,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetModule), new { id = module.Id }, MapModule(module, 0, 0, false));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ModuleDto>> UpdateModule(int id, ModuleCreateDto model)
        {
            var module = await _context.Modules
                .Include(x => x.Subjects)
                    .ThenInclude(s => s.Questions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (module == null)
            {
                return NotFound(new { Message = "Module not found." });
            }

            var normalizedName = model.Name.Trim();
            if (await _context.Modules.AnyAsync(x => x.Id != id && x.Name.ToLower() == normalizedName.ToLower()))
            {
                return BadRequest(new { Message = "Module name already exists." });
            }

            module.Name = normalizedName;
            module.Description = model.Description?.Trim();
            module.IsActive = model.IsActive;
            module.IsPremium = model.IsPremium;
            module.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapModule(
                module,
                module.Subjects.Count(s => s.IsActive),
                module.Subjects.SelectMany(s => s.Questions).Count(q => q.IsActive),
                false));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            var module = await _context.Modules
                .Include(x => x.Subjects)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (module == null)
            {
                return NotFound(new { Message = "Module not found." });
            }

            if (module.Subjects.Any())
            {
                return BadRequest(new { Message = "Cannot delete a module that already has subjects." });
            }

            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Module deleted successfully." });
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

        private static ModuleDto MapModule(ExamModule module, int subjectCount, int questionCount, bool requiresSubscription)
        {
            return new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                Description = module.Description,
                IsActive = module.IsActive,
                IsPremium = module.IsPremium,
                RequiresSubscription = requiresSubscription,
                SubjectCount = subjectCount,
                QuestionCount = questionCount,
                CreatedAt = module.CreatedAt,
                UpdatedAt = module.UpdatedAt
            };
        }
    }
}
