using Api.Data;
using Api.DTOs.Account;
using Api.Interface;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Authorize(Roles = "Student,Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ExamAttemptsController : ControllerBase
    {
        private const decimal FirstPerfectExperienceReward = 5m;
        private const decimal PerfectedSubjectRetakeExperienceReward = 2m;
        private readonly Context _context;
        private readonly IPremiumAccessService _premiumAccessService;

        public ExamAttemptsController(Context context, IPremiumAccessService premiumAccessService)
        {
            _context = context;
            _premiumAccessService = premiumAccessService;
        }

        [Authorize(Roles = "Student")]
        [HttpPost("start")]
        public async Task<ActionResult<ExamAttemptDto>> StartAttempt(StartExamAttemptDto model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            if (!await _premiumAccessService.CanAccessQuestionSetAsync(studentId, model.SubjectId, model.QuestionSetNumber))
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    Message = "This premium question set requires an active subscription."
                });
            }

            var existingAttempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(x =>
                    x.SubjectId == model.SubjectId &&
                    x.QuestionSetNumber == model.QuestionSetNumber &&
                    x.StudentId == studentId &&
                    x.Status == "InProgress");

            if (existingAttempt != null)
            {
                return Ok(await BuildAttemptResultAsync(existingAttempt.Id, studentId));
            }

            var subject = await _context.Subjects
                .AsNoTracking()
                .Include(x => x.Module)
                .Include(x => x.Questions)
                .FirstOrDefaultAsync(x => x.Id == model.SubjectId && x.IsActive);

            if (subject == null)
            {
                return NotFound(new { Message = "Subject not found." });
            }

            var activeQuestions = subject.Questions
                .Where(x => x.IsActive && x.QuestionSetNumber == model.QuestionSetNumber)
                .ToList();

            if (!activeQuestions.Any())
            {
                return BadRequest(new { Message = $"Question Set {model.QuestionSetNumber} does not have active questions yet." });
            }

            var orderedSetNumbers = subject.Questions
                .Where(x => x.IsActive)
                .Select(x => x.QuestionSetNumber)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var currentSetIndex = orderedSetNumbers.IndexOf(model.QuestionSetNumber);
            if (currentSetIndex > 0)
            {
                var previousSetNumber = orderedSetNumbers[currentSetIndex - 1];
                var previousSetPerfected = await _context.ExamAttempts.AnyAsync(x =>
                    x.StudentId == studentId &&
                    x.SubjectId == model.SubjectId &&
                    x.QuestionSetNumber == previousSetNumber &&
                    x.Status == "Submitted" &&
                    x.TotalPossibleScore > 0 &&
                    x.TotalScore >= x.TotalPossibleScore);

                if (!previousSetPerfected)
                {
                    return BadRequest(new
                    {
                        Message = $"Set {model.QuestionSetNumber} is locked. Perfect Set {previousSetNumber} first."
                    });
                }
            }

            var questionOrder = BuildAttemptQuestionOrder(activeQuestions);

            var attempt = new ExamAttempt
            {
                SubjectId = subject.Id,
                QuestionSetNumber = model.QuestionSetNumber,
                StudentId = studentId,
                StartedAt = DateTime.UtcNow,
                Status = "InProgress",
                QuestionOrderJson = JsonSerializer.Serialize(questionOrder),
                TotalQuestions = activeQuestions.Count,
                TotalPossibleScore = activeQuestions.Sum(x => x.Score)
            };

            _context.ExamAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return Ok(new ExamAttemptDto
            {
                AttemptId = attempt.Id,
                ModuleId = subject.ModuleId,
                ModuleName = subject.Module.Name,
                SubjectId = subject.Id,
                SubjectName = subject.Name,
                QuestionSetNumber = attempt.QuestionSetNumber,
                StudentId = studentId,
                StartedAt = attempt.StartedAt,
                Status = attempt.Status,
                TotalQuestions = attempt.TotalQuestions,
                TotalPossibleScore = attempt.TotalPossibleScore,
                Questions = BuildStudentQuestionList(activeQuestions, questionOrder)
            });
        }

        [Authorize(Roles = "Student")]
        [HttpPost("{attemptId:int}/answer")]
        public async Task<ActionResult<SubmitSingleExamAnswerResultDto>> SubmitAnswer(int attemptId, SubmitExamAnswerDto model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            var attempt = await _context.ExamAttempts
                .Include(x => x.Subject)
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == attemptId && x.StudentId == studentId);

            if (attempt == null)
            {
                return NotFound(new { Message = "Exam attempt not found." });
            }

            if (!string.Equals(attempt.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Message = "This exam attempt has already been submitted." });
            }

            var questions = await _context.Questions
                .Where(x => x.SubjectId == attempt.SubjectId && x.QuestionSetNumber == attempt.QuestionSetNumber && x.IsActive)
                .ToListAsync();

            var orderedQuestions = GetOrderedQuestions(attempt.QuestionOrderJson, questions);
            if (!orderedQuestions.Any())
            {
                return BadRequest(new { Message = "No active questions were found for this subject." });
            }

            var nextQuestionItem = orderedQuestions
                .FirstOrDefault(x => attempt.Answers.All(answer => answer.QuestionId != x.Question.Id));

            if (nextQuestionItem == null)
            {
                return BadRequest(new { Message = "All questions for this exam have already been answered." });
            }

            var nextQuestion = nextQuestionItem.Question;
            if (nextQuestion.Id != model.QuestionId)
            {
                return BadRequest(new { Message = "You can only answer the current question." });
            }

            var selectedAnswer = NormalizeAnswer(model.SelectedAnswer);
            var timeSpentSeconds = model.TimeSpentSeconds;
            if (nextQuestion.TimeLimitSeconds.HasValue && nextQuestion.TimeLimitSeconds.Value > 0)
            {
                timeSpentSeconds = Math.Min(timeSpentSeconds, nextQuestion.TimeLimitSeconds.Value);
            }

            var isCorrect = selectedAnswer != null &&
                string.Equals(selectedAnswer, nextQuestion.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

            var answerEntity = new ExamAttemptAnswer
            {
                ExamAttemptId = attempt.Id,
                QuestionId = nextQuestion.Id,
                SelectedAnswer = selectedAnswer,
                CorrectAnswer = nextQuestion.CorrectAnswer,
                IsCorrect = isCorrect,
                ScoreEarned = isCorrect ? nextQuestion.Score : 0,
                TimeSpentSeconds = timeSpentSeconds,
                Feedback = BuildFeedback(nextQuestion, isCorrect),
                AnsweredAt = DateTime.UtcNow
            };

            _context.ExamAttemptAnswers.Add(answerEntity);
            attempt.Answers.Add(answerEntity);

            RecalculateAttemptTotals(attempt, questions);

            var answeredQuestionCount = attempt.Answers
                .Select(x => x.QuestionId)
                .Distinct()
                .Count();

            var attemptCompleted = answeredQuestionCount >= questions.Count;
            if (attemptCompleted)
            {
                attempt.SubmittedAt = DateTime.UtcNow;
                attempt.Status = "Submitted";
                attempt.OverallFeedback = BuildOverallFeedback(attempt.TotalScore, attempt.TotalPossibleScore);
                await AwardPointsForAttemptAsync(attempt);
            }

            await _context.SaveChangesAsync();

            var result = new SubmitSingleExamAnswerResultDto
            {
                AttemptCompleted = attemptCompleted,
                Answer = new ExamAttemptAnswerResultDto
                {
                    QuestionId = nextQuestion.Id,
                    QuestionText = nextQuestion.QuestionText,
                    SelectedAnswer = selectedAnswer,
                    SelectedAnswerText = GetOptionText(nextQuestion, selectedAnswer),
                    CorrectAnswer = nextQuestion.CorrectAnswer,
                    CorrectAnswerText = GetOptionText(nextQuestion, nextQuestion.CorrectAnswer),
                    IsCorrect = isCorrect,
                    ScoreEarned = answerEntity.ScoreEarned,
                    TimeSpentSeconds = timeSpentSeconds,
                    Explanation = nextQuestion.Explanation,
                    Feedback = answerEntity.Feedback
                }
            };

            if (attemptCompleted)
            {
                result.Attempt = await BuildAttemptResultAsync(attempt.Id, studentId);
            }

            return Ok(result);
        }

        [Authorize(Roles = "Student")]
        [HttpPost("{attemptId:int}/submit")]
        public async Task<ActionResult<ExamAttemptDto>> SubmitAttempt(int attemptId, SubmitExamAttemptDto model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            var attempt = await _context.ExamAttempts
                .Include(x => x.Subject)
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == attemptId && x.StudentId == studentId);

            if (attempt == null)
            {
                return NotFound(new { Message = "Exam attempt not found." });
            }

            if (!string.Equals(attempt.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { Message = "This exam attempt has already been submitted." });
            }

            var questions = await _context.Questions
                .Where(x => x.SubjectId == attempt.SubjectId && x.QuestionSetNumber == attempt.QuestionSetNumber && x.IsActive)
                .ToListAsync();

            var orderedQuestions = GetOrderedQuestions(attempt.QuestionOrderJson, questions);
            if (!orderedQuestions.Any())
            {
                return BadRequest(new { Message = "No active questions were found for this subject." });
            }

            var submittedAnswers = model.Answers
                .GroupBy(x => x.QuestionId)
                .Select(x => x.First())
                .ToDictionary(x => x.QuestionId, x => x);

            foreach (var existingAnswer in attempt.Answers.ToList())
            {
                _context.ExamAttemptAnswers.Remove(existingAnswer);
            }

            var answerEntities = new List<ExamAttemptAnswer>();
            foreach (var question in orderedQuestions.Select(x => x.Question))
            {
                submittedAnswers.TryGetValue(question.Id, out var submittedAnswer);

                var selectedAnswer = NormalizeAnswer(submittedAnswer?.SelectedAnswer);
                var isCorrect = selectedAnswer != null &&
                    string.Equals(selectedAnswer, question.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

                answerEntities.Add(new ExamAttemptAnswer
                {
                    ExamAttemptId = attempt.Id,
                    QuestionId = question.Id,
                    SelectedAnswer = selectedAnswer,
                    CorrectAnswer = question.CorrectAnswer,
                    IsCorrect = isCorrect,
                    ScoreEarned = isCorrect ? question.Score : 0,
                    TimeSpentSeconds = submittedAnswer?.TimeSpentSeconds ?? 0,
                    Feedback = BuildFeedback(question, isCorrect),
                    AnsweredAt = submittedAnswer == null ? null : DateTime.UtcNow
                });
            }

            _context.ExamAttemptAnswers.AddRange(answerEntities);

            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.Answers = answerEntities;
            RecalculateAttemptTotals(attempt, questions);
            attempt.Status = "Submitted";
            attempt.OverallFeedback = BuildOverallFeedback(attempt.TotalScore, attempt.TotalPossibleScore);
            await AwardPointsForAttemptAsync(attempt);

            await _context.SaveChangesAsync();

            return Ok(await BuildAttemptResultAsync(attempt.Id, studentId));
        }

        [Authorize(Roles = "Student")]
        [HttpGet("my-attempts")]
        public async Task<ActionResult<IEnumerable<ExamAttemptDto>>> GetMyAttempts()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            var attempts = await _context.ExamAttempts
                .AsNoTracking()
                .Include(x => x.Subject)
                    .ThenInclude(x => x.Module)
                .Where(x => x.StudentId == studentId)
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new ExamAttemptDto
                {
                    AttemptId = x.Id,
                    ModuleId = x.Subject.ModuleId,
                    ModuleName = x.Subject.Module.Name,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject.Name,
                    QuestionSetNumber = x.QuestionSetNumber,
                    StudentId = x.StudentId,
                    StartedAt = x.StartedAt,
                    SubmittedAt = x.SubmittedAt,
                    Status = x.Status,
                    TotalScore = x.TotalScore,
                    TotalPossibleScore = x.TotalPossibleScore,
                    TotalQuestions = x.TotalQuestions,
                    CorrectAnswers = x.CorrectAnswers,
                    WrongAnswers = x.WrongAnswers,
                    OverallFeedback = x.OverallFeedback
                })
                .ToListAsync();

            return Ok(attempts);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("performance")]
        public async Task<ActionResult<IEnumerable<StudentPerformanceDto>>> GetPerformance([FromQuery] int? moduleId, [FromQuery] int? subjectId, [FromQuery] string studentId)
        {
            var query = _context.ExamAttempts
                .AsNoTracking()
                .Include(x => x.Subject)
                    .ThenInclude(x => x.Module)
                .Include(x => x.Student)
                .AsQueryable();

            if (moduleId.HasValue)
            {
                query = query.Where(x => x.Subject.ModuleId == moduleId.Value);
            }

            if (subjectId.HasValue)
            {
                query = query.Where(x => x.SubjectId == subjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(studentId))
            {
                query = query.Where(x => x.StudentId == studentId);
            }

            var performance = await query
                .OrderByDescending(x => x.SubmittedAt ?? x.StartedAt)
                .Select(x => new StudentPerformanceDto
                {
                    AttemptId = x.Id,
                    StudentId = x.StudentId,
                    StudentName = $"{x.Student.FirstName} {x.Student.LastName}",
                    StudentEmail = x.Student.Email,
                    ModuleId = x.Subject.ModuleId,
                    ModuleName = x.Subject.Module.Name,
                    SubjectId = x.SubjectId,
                    SubjectName = x.Subject.Name,
                    QuestionSetNumber = x.QuestionSetNumber,
                    Status = x.Status,
                    TotalScore = x.TotalScore,
                    TotalPossibleScore = x.TotalPossibleScore,
                    CorrectAnswers = x.CorrectAnswers,
                    WrongAnswers = x.WrongAnswers,
                    StartedAt = x.StartedAt,
                    SubmittedAt = x.SubmittedAt
                })
                .ToListAsync();

            return Ok(performance);
        }

        [HttpGet("{attemptId:int}")]
        public async Task<ActionResult<ExamAttemptDto>> GetAttempt(int attemptId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            var attempt = await _context.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId);
            if (attempt == null)
            {
                return NotFound(new { Message = "Exam attempt not found." });
            }

            if (!User.IsInRole("Admin") && attempt.StudentId != currentUserId)
            {
                return Forbid();
            }

            return Ok(await BuildAttemptResultAsync(attemptId, User.IsInRole("Admin") ? null : currentUserId));
        }

        private async Task<ExamAttemptDto> BuildAttemptResultAsync(int attemptId, string studentId)
        {
            var attempt = await _context.ExamAttempts
                .Include(x => x.Subject)
                    .ThenInclude(x => x.Module)
                .Include(x => x.Subject)
                    .ThenInclude(x => x.Questions)
                .Include(x => x.Answers)
                    .ThenInclude(x => x.Question)
                .FirstAsync(x => x.Id == attemptId && (studentId == null || x.StudentId == studentId));

            var activeQuestions = attempt.Subject.Questions.Where(x => x.IsActive).ToList();
            activeQuestions = activeQuestions
                .Where(x => x.QuestionSetNumber == attempt.QuestionSetNumber)
                .ToList();
            var latestAnswers = GetLatestAnswers(attempt.Answers);
            var totalPossibleScore = activeQuestions.Sum(x => x.Score);
            var totalScore = latestAnswers.Sum(x => x.ScoreEarned);
            var correctAnswers = latestAnswers.Count(x => x.IsCorrect);
            var wrongAnswers = latestAnswers.Count(x => !x.IsCorrect);

            var dto = new ExamAttemptDto
            {
                AttemptId = attempt.Id,
                ModuleId = attempt.Subject.ModuleId,
                ModuleName = attempt.Subject.Module.Name,
                SubjectId = attempt.SubjectId,
                SubjectName = attempt.Subject.Name,
                QuestionSetNumber = attempt.QuestionSetNumber,
                StudentId = attempt.StudentId,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                Status = attempt.Status,
                TotalScore = totalScore,
                TotalPossibleScore = totalPossibleScore,
                TotalQuestions = activeQuestions.Count,
                CorrectAnswers = correctAnswers,
                WrongAnswers = wrongAnswers,
                OverallFeedback = BuildOverallFeedback(totalScore, totalPossibleScore)
            };

            if (string.Equals(attempt.Status, "Submitted", StringComparison.OrdinalIgnoreCase))
            {
                var hasPreviouslyPerfectedSubject = await HasPreviouslyPerfectedSubjectAsync(attempt);
                dto.ExperienceGained = CalculateExperienceReward(
                    totalScore,
                    totalPossibleScore,
                    hasPreviouslyPerfectedSubject);
            }

            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == attempt.StudentId);

            if (student != null)
            {
                dto.StudentExperiencePoints = student.ExperiencePoints;
                dto.PreviousStudentLevel = CalculateLevel(student.ExperiencePoints - dto.ExperienceGained);
                dto.StudentLevel = student.Level;
                dto.LeveledUp = dto.StudentLevel > dto.PreviousStudentLevel;
            }

            if (string.Equals(attempt.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
            {
                var orderedQuestions = GetOrderedQuestions(
                    attempt.QuestionOrderJson,
                    activeQuestions);

                dto.Questions = orderedQuestions
                    .Select(x => MapQuestionForStudent(x.Question, x.OptionOrder))
                    .ToList();

                dto.Answers = latestAnswers
                    .OrderBy(x => orderedQuestions.FindIndex(question => question.Question.Id == x.QuestionId))
                    .Select(MapAnswerResult)
                    .ToList();

                return dto;
            }

            var submittedOrderedQuestions = GetOrderedQuestions(
                attempt.QuestionOrderJson,
                activeQuestions);

            dto.Answers = latestAnswers
                .OrderBy(x => submittedOrderedQuestions.FindIndex(question => question.Question.Id == x.QuestionId))
                .Select(MapAnswerResult)
                .ToList();

            return dto;
        }

        private static List<StudentQuestionDto> BuildStudentQuestionList(
            List<Question> questions,
            List<AttemptQuestionOrderItem> questionOrder)
        {
            return GetOrderedQuestions(JsonSerializer.Serialize(questionOrder), questions)
                .Select(x => MapQuestionForStudent(x.Question, x.OptionOrder))
                .ToList();
        }

        private static StudentQuestionDto MapQuestionForStudent(Question question, List<string> optionOrder)
        {
            return new StudentQuestionDto
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                Options = optionOrder
                    .Select(optionKey => new StudentQuestionOptionDto
                    {
                        Value = optionKey,
                        Text = GetOptionText(question, optionKey)
                    })
                    .ToArray(),
                Score = question.Score,
                TimeLimitSeconds = question.TimeLimitSeconds
            };
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

        private static string BuildFeedback(Question question, bool isCorrect)
        {
            if (isCorrect)
            {
                return string.IsNullOrWhiteSpace(question.DefaultFeedback) ? "Correct answer." : question.DefaultFeedback;
            }

            return string.IsNullOrWhiteSpace(question.Explanation) ? "Incorrect answer." : question.Explanation;
        }

        private static string BuildOverallFeedback(int totalScore, int totalPossibleScore)
        {
            if (totalPossibleScore <= 0)
            {
                return "No score available.";
            }

            var percentage = (double)totalScore / totalPossibleScore * 100;
            if (percentage >= 90) return "Excellent work.";
            if (percentage >= 75) return "Good job.";
            if (percentage >= 50) return "Fair attempt, but there is room to improve.";
            return "Keep practicing and review the explanations carefully.";
        }

        private static ExamAttemptAnswerResultDto MapAnswerResult(ExamAttemptAnswer answer)
        {
            return new ExamAttemptAnswerResultDto
            {
                QuestionId = answer.QuestionId,
                QuestionText = answer.Question?.QuestionText,
                SelectedAnswer = answer.SelectedAnswer,
                SelectedAnswerText = GetOptionText(answer.Question, answer.SelectedAnswer),
                CorrectAnswer = answer.CorrectAnswer,
                CorrectAnswerText = GetOptionText(answer.Question, answer.CorrectAnswer),
                IsCorrect = answer.IsCorrect,
                ScoreEarned = answer.ScoreEarned,
                TimeSpentSeconds = answer.TimeSpentSeconds,
                Explanation = answer.Question?.Explanation,
                Feedback = answer.Feedback
            };
        }

        private static void RecalculateAttemptTotals(ExamAttempt attempt, List<Question> questions)
        {
            var latestAnswers = GetLatestAnswers(attempt.Answers);
            attempt.TotalQuestions = questions.Count;
            attempt.TotalPossibleScore = questions.Sum(x => x.Score);
            attempt.TotalScore = latestAnswers.Sum(x => x.ScoreEarned);
            attempt.CorrectAnswers = latestAnswers.Count(x => x.IsCorrect);
            attempt.WrongAnswers = latestAnswers.Count(x => !x.IsCorrect);
        }

        private static List<ExamAttemptAnswer> GetLatestAnswers(IEnumerable<ExamAttemptAnswer> answers)
        {
            return answers
                .GroupBy(x => x.QuestionId)
                .Select(group => group
                    .OrderByDescending(x => x.AnsweredAt ?? DateTime.MinValue)
                    .ThenByDescending(x => x.Id)
                    .First())
                .ToList();
        }

        private static List<AttemptQuestionOrderItem> BuildAttemptQuestionOrder(List<Question> questions)
        {
            var shuffledQuestions = questions.ToList();
            Shuffle(shuffledQuestions);

            return shuffledQuestions
                .Select(question => new AttemptQuestionOrderItem
                {
                    QuestionId = question.Id,
                    OptionOrder = BuildRandomOptionOrder()
                })
                .ToList();
        }

        private static List<OrderedQuestionItem> GetOrderedQuestions(string questionOrderJson, List<Question> questions)
        {
            var questionMap = questions.ToDictionary(x => x.Id);
            var configuredOrder = DeserializeQuestionOrder(questionOrderJson);
            var orderedQuestions = new List<OrderedQuestionItem>();

            foreach (var item in configuredOrder)
            {
                if (!questionMap.TryGetValue(item.QuestionId, out var question))
                {
                    continue;
                }

                orderedQuestions.Add(new OrderedQuestionItem
                {
                    Question = question,
                    OptionOrder = NormalizeOptionOrder(item.OptionOrder)
                });
            }

            foreach (var question in questions.OrderBy(x => x.Id))
            {
                if (orderedQuestions.Any(x => x.Question.Id == question.Id))
                {
                    continue;
                }

                orderedQuestions.Add(new OrderedQuestionItem
                {
                    Question = question,
                    OptionOrder = DefaultOptionOrder()
                });
            }

            return orderedQuestions;
        }

        private static List<AttemptQuestionOrderItem> DeserializeQuestionOrder(string questionOrderJson)
        {
            if (string.IsNullOrWhiteSpace(questionOrderJson))
            {
                return new List<AttemptQuestionOrderItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<AttemptQuestionOrderItem>>(questionOrderJson) ??
                    new List<AttemptQuestionOrderItem>();
            }
            catch
            {
                return new List<AttemptQuestionOrderItem>();
            }
        }

        private static List<string> BuildRandomOptionOrder()
        {
            var optionOrder = DefaultOptionOrder();
            Shuffle(optionOrder);
            return optionOrder;
        }

        private static List<string> NormalizeOptionOrder(List<string> optionOrder)
        {
            if (optionOrder == null)
            {
                return DefaultOptionOrder();
            }

            var normalized = optionOrder
                .Select(NormalizeAnswer)
                .Where(x => x != null)
                .Distinct()
                .ToList();

            return normalized.Count == 4 ? normalized : DefaultOptionOrder();
        }

        private static List<string> DefaultOptionOrder()
        {
            return new List<string> { "A", "B", "C", "D" };
        }

        private static string GetOptionText(Question question, string optionKey)
        {
            if (question == null || string.IsNullOrWhiteSpace(optionKey))
            {
                return null;
            }

            return NormalizeAnswer(optionKey) switch
            {
                "A" => question.OptionA,
                "B" => question.OptionB,
                "C" => question.OptionC,
                "D" => question.OptionD,
                _ => null
            };
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Shared.Next(i + 1);
                (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
            }
        }

        private sealed class AttemptQuestionOrderItem
        {
            public int QuestionId { get; set; }
            public List<string> OptionOrder { get; set; } = new List<string>();
        }

        private sealed class OrderedQuestionItem
        {
            public Question Question { get; set; }
            public List<string> OptionOrder { get; set; } = new List<string>();
        }

        private async Task AwardPointsForAttemptAsync(ExamAttempt attempt)
        {
            if (attempt == null || attempt.Subject == null)
            {
                return;
            }

            var student = await _context.Users.FirstOrDefaultAsync(x => x.Id == attempt.StudentId);
            if (student == null)
            {
                return;
            }

            if (attempt.TotalScore > 0)
            {
                student.TotalPoints += attempt.TotalScore;
            }

            var hasPreviouslyPerfectedSubject = await HasPreviouslyPerfectedSubjectAsync(attempt);
            student.ExperiencePoints += CalculateExperienceReward(
                attempt.TotalScore,
                attempt.TotalPossibleScore,
                hasPreviouslyPerfectedSubject);
            student.Level = CalculateLevel(student.ExperiencePoints);

            if (attempt.TotalScore <= 0)
            {
                return;
            }

            var modulePoints = await _context.StudentModulePoints
                .FirstOrDefaultAsync(x => x.StudentId == attempt.StudentId && x.ModuleId == attempt.Subject.ModuleId);

            if (modulePoints == null)
            {
                modulePoints = new StudentModulePoint
                {
                    StudentId = attempt.StudentId,
                    ModuleId = attempt.Subject.ModuleId,
                    Points = attempt.TotalScore,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.StudentModulePoints.Add(modulePoints);
                return;
            }

            modulePoints.Points += attempt.TotalScore;
            modulePoints.UpdatedAt = DateTime.UtcNow;
        }

        private async Task<bool> HasPreviouslyPerfectedSubjectAsync(ExamAttempt attempt)
        {
            return await _context.ExamAttempts.AnyAsync(x =>
                x.StudentId == attempt.StudentId &&
                x.SubjectId == attempt.SubjectId &&
                x.QuestionSetNumber == attempt.QuestionSetNumber &&
                x.Id != attempt.Id &&
                x.Status == "Submitted" &&
                x.TotalPossibleScore > 0 &&
                x.TotalScore >= x.TotalPossibleScore);
        }

        private static decimal CalculateExperienceReward(int totalScore, int totalPossibleScore, bool hasPreviouslyPerfectedSubject)
        {
            if (totalPossibleScore <= 0 || totalScore <= 0)
            {
                return 0m;
            }

            var scoreRatio = Math.Min(1m, (decimal)totalScore / totalPossibleScore);
            var maximumReward = hasPreviouslyPerfectedSubject
                ? PerfectedSubjectRetakeExperienceReward
                : FirstPerfectExperienceReward;

            return Math.Round(scoreRatio * maximumReward, 2, MidpointRounding.AwayFromZero);
        }

        private static int CalculateLevel(decimal experiencePoints)
        {
            if (experiencePoints <= 0)
            {
                return 1;
            }

            var level = 1;
            var remainingExperience = experiencePoints;

            while (remainingExperience >= level * 100m)
            {
                remainingExperience -= level * 100m;
                level++;
            }

            return level;
        }

    }
}
