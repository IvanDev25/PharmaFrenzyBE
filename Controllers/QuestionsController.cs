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
    public class QuestionsController : ControllerBase
    {
        private readonly Context _context;

        public QuestionsController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuestionDto>>> GetQuestions([FromQuery] int? subjectId, [FromQuery] int? moduleId)
        {
            var query = _context.Questions.Include(x => x.Subject).AsQueryable();

            if (subjectId.HasValue)
            {
                query = query.Where(x => x.SubjectId == subjectId.Value);
            }

            if (moduleId.HasValue)
            {
                query = query.Where(x => x.Subject.ModuleId == moduleId.Value);
            }

            var questions = await query
                .OrderBy(x => x.Subject.Module.Name)
                .ThenBy(x => x.Subject.Name)
                .ThenBy(x => x.QuestionSetNumber)
                .ThenBy(x => x.Id)
                .Select(x => new QuestionDto
                {
                    Id = x.Id,
                    ModuleId = x.Subject.ModuleId,
                    ModuleName = x.Subject.Module.Name,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject.Name,
                    QuestionSetNumber = x.QuestionSetNumber,
                    QuestionText = x.QuestionText,
                    OptionA = x.OptionA,
                    OptionB = x.OptionB,
                    OptionC = x.OptionC,
                    OptionD = x.OptionD,
                    CorrectAnswer = x.CorrectAnswer,
                    Explanation = x.Explanation,
                    DefaultFeedback = x.DefaultFeedback,
                    Score = x.Score,
                    TimeLimitSeconds = x.TimeLimitSeconds,
                    IsActive = x.IsActive,
                    IsQuestionSetPremium = _context.QuestionSetAccesses
                        .Where(access => access.SubjectId == x.SubjectId && access.QuestionSetNumber == x.QuestionSetNumber)
                        .Select(access => access.IsPremium)
                        .FirstOrDefault(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return Ok(questions);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<QuestionDto>> GetQuestion(int id)
        {
            var question = await _context.Questions
                .Include(x => x.Subject)
                .Where(x => x.Id == id)
                .Select(x => new QuestionDto
                {
                    Id = x.Id,
                    ModuleId = x.Subject.ModuleId,
                    ModuleName = x.Subject.Module.Name,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject.Name,
                    QuestionSetNumber = x.QuestionSetNumber,
                    QuestionText = x.QuestionText,
                    OptionA = x.OptionA,
                    OptionB = x.OptionB,
                    OptionC = x.OptionC,
                    OptionD = x.OptionD,
                    CorrectAnswer = x.CorrectAnswer,
                    Explanation = x.Explanation,
                    DefaultFeedback = x.DefaultFeedback,
                    Score = x.Score,
                    TimeLimitSeconds = x.TimeLimitSeconds,
                    IsActive = x.IsActive,
                    IsQuestionSetPremium = _context.QuestionSetAccesses
                        .Where(access => access.SubjectId == x.SubjectId && access.QuestionSetNumber == x.QuestionSetNumber)
                        .Select(access => access.IsPremium)
                        .FirstOrDefault(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (question == null)
            {
                return NotFound(new { Message = "Question not found." });
            }

            return Ok(question);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<QuestionDto>> CreateQuestion(QuestionCreateDto model)
        {
            if (!await _context.Subjects.AnyAsync(x => x.Id == model.SubjectId))
            {
                return BadRequest(new { Message = "Subject not found." });
            }

            var correctAnswer = NormalizeAnswer(model.CorrectAnswer);
            if (correctAnswer == null)
            {
                return BadRequest(new { Message = "CorrectAnswer must be A, B, C, or D." });
            }

            if (!await CanSaveQuestionInSetAsync(model.SubjectId, model.QuestionSetNumber))
            {
                return BadRequest(new { Message = "Each subject set can only contain up to 20 active questions." });
            }

            var question = new Question
            {
                SubjectId = model.SubjectId,
                QuestionSetNumber = model.QuestionSetNumber,
                QuestionText = model.QuestionText?.Trim(),
                OptionA = model.OptionA?.Trim(),
                OptionB = model.OptionB?.Trim(),
                OptionC = model.OptionC?.Trim(),
                OptionD = model.OptionD?.Trim(),
                CorrectAnswer = correctAnswer,
                Explanation = model.Explanation?.Trim(),
                DefaultFeedback = model.DefaultFeedback?.Trim(),
                Score = model.Score,
                TimeLimitSeconds = model.TimeLimitSeconds,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Questions.Add(question);
            await UpsertQuestionSetAccessAsync(model.SubjectId, model.QuestionSetNumber, model.IsQuestionSetPremium);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, await BuildQuestionDtoAsync(question.Id));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<QuestionDto>> UpdateQuestion(int id, QuestionCreateDto model)
        {
            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id == id);
            if (question == null)
            {
                return NotFound(new { Message = "Question not found." });
            }

            if (!await _context.Subjects.AnyAsync(x => x.Id == model.SubjectId))
            {
                return BadRequest(new { Message = "Subject not found." });
            }

            var correctAnswer = NormalizeAnswer(model.CorrectAnswer);
            if (correctAnswer == null)
            {
                return BadRequest(new { Message = "CorrectAnswer must be A, B, C, or D." });
            }

            if (!await CanSaveQuestionInSetAsync(model.SubjectId, model.QuestionSetNumber, id))
            {
                return BadRequest(new { Message = "Each subject set can only contain up to 20 active questions." });
            }

            question.SubjectId = model.SubjectId;
            question.QuestionSetNumber = model.QuestionSetNumber;
            question.QuestionText = model.QuestionText?.Trim();
            question.OptionA = model.OptionA?.Trim();
            question.OptionB = model.OptionB?.Trim();
            question.OptionC = model.OptionC?.Trim();
            question.OptionD = model.OptionD?.Trim();
            question.CorrectAnswer = correctAnswer;
            question.Explanation = model.Explanation?.Trim();
            question.DefaultFeedback = model.DefaultFeedback?.Trim();
            question.Score = model.Score;
            question.TimeLimitSeconds = model.TimeLimitSeconds;
            question.IsActive = model.IsActive;
            question.UpdatedAt = DateTime.UtcNow;

            await UpsertQuestionSetAccessAsync(model.SubjectId, model.QuestionSetNumber, model.IsQuestionSetPremium);
            await _context.SaveChangesAsync();

            return Ok(await BuildQuestionDtoAsync(question.Id));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions.FirstOrDefaultAsync(x => x.Id == id);
            if (question == null)
            {
                return NotFound(new { Message = "Question not found." });
            }

            if (await _context.ExamAttemptAnswers.AnyAsync(x => x.QuestionId == id))
            {
                return BadRequest(new { Message = "Cannot delete a question that has already been used in an exam attempt." });
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Question deleted successfully." });
        }

        private async Task<QuestionDto> BuildQuestionDtoAsync(int questionId)
        {
            return await _context.Questions
                .Include(x => x.Subject)
                .Where(x => x.Id == questionId)
                .Select(x => new QuestionDto
                {
                    Id = x.Id,
                    ModuleId = x.Subject.ModuleId,
                    ModuleName = x.Subject.Module.Name,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject.Name,
                    QuestionSetNumber = x.QuestionSetNumber,
                    QuestionText = x.QuestionText,
                    OptionA = x.OptionA,
                    OptionB = x.OptionB,
                    OptionC = x.OptionC,
                    OptionD = x.OptionD,
                    CorrectAnswer = x.CorrectAnswer,
                    Explanation = x.Explanation,
                    DefaultFeedback = x.DefaultFeedback,
                    Score = x.Score,
                    TimeLimitSeconds = x.TimeLimitSeconds,
                    IsActive = x.IsActive,
                    IsQuestionSetPremium = _context.QuestionSetAccesses
                        .Where(access => access.SubjectId == x.SubjectId && access.QuestionSetNumber == x.QuestionSetNumber)
                        .Select(access => access.IsPremium)
                        .FirstOrDefault(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstAsync();
        }

        private async Task<bool> CanSaveQuestionInSetAsync(int subjectId, int questionSetNumber, int? currentQuestionId = null)
        {
            var activeQuestionCount = await _context.Questions.CountAsync(x =>
                x.SubjectId == subjectId &&
                x.QuestionSetNumber == questionSetNumber &&
                x.IsActive &&
                (!currentQuestionId.HasValue || x.Id != currentQuestionId.Value));

            return activeQuestionCount < 20;
        }

        private async Task UpsertQuestionSetAccessAsync(int subjectId, int questionSetNumber, bool isPremium)
        {
            var access = await _context.QuestionSetAccesses.FirstOrDefaultAsync(x =>
                x.SubjectId == subjectId &&
                x.QuestionSetNumber == questionSetNumber);

            if (access == null)
            {
                if (!isPremium)
                {
                    return;
                }

                _context.QuestionSetAccesses.Add(new QuestionSetAccess
                {
                    SubjectId = subjectId,
                    QuestionSetNumber = questionSetNumber,
                    IsPremium = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                return;
            }

            access.IsPremium = isPremium;
            access.UpdatedAt = DateTime.UtcNow;
        }

        private static string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return null;
            }

            var normalized = answer.Trim().ToUpperInvariant();
            return normalized is "A" or "B" or "C" or "D" ? normalized : null;
        }
    }
}
