using Api.Data;
using Api.DTOs.Account;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class RxWalletController : ControllerBase
    {
        private const string StatusPending = "Pending";
        private const string StatusApproved = "Approved";
        private const string StatusRejected = "Rejected";
        private const string StatusPaid = "Paid";

        private readonly Context _context;
        private readonly UserManager<User> _userManager;

        public RxWalletController(Context context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Student")]
        [HttpGet("my")]
        public async Task<ActionResult<RxWalletDto>> GetMyWallet()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            return Ok(await BuildWalletDtoAsync(studentId));
        }

        [Authorize(Roles = "Student")]
        [HttpPost("withdrawals")]
        public async Task<ActionResult<RxWalletDto>> CreateWithdrawalRequest(CreateWithdrawalRequestDto model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            var student = await _userManager.FindByIdAsync(studentId);
            if (student == null)
            {
                return NotFound(new { Message = "Student not found." });
            }

            var rxCoinAmount = Math.Round(model.RxCoinAmount, 2, MidpointRounding.AwayFromZero);
            if (rxCoinAmount <= 0m)
            {
                return BadRequest(new { Message = "RxCoin amount must be greater than zero." });
            }

            if (student.RxCoinBalance < rxCoinAmount)
            {
                return BadRequest(new { Message = "You do not have enough available RxCoin for this withdrawal request." });
            }

            var gcashNumber = model.GCashNumber?.Trim();
            var gcashName = model.GCashName?.Trim();
            if (string.IsNullOrWhiteSpace(gcashNumber) || string.IsNullOrWhiteSpace(gcashName))
            {
                return BadRequest(new { Message = "GCash number and GCash name are required." });
            }

            student.RxCoinBalance -= rxCoinAmount;
            student.RxCoinOnHold += rxCoinAmount;

            _context.StudentWithdrawalRequests.Add(new StudentWithdrawalRequest
            {
                StudentId = studentId,
                RxCoinAmount = rxCoinAmount,
                PesoAmount = RankingBadgeService.ConvertRxCoinToPeso(rxCoinAmount),
                GCashNumber = gcashNumber,
                GCashName = gcashName,
                Status = StatusPending,
                RequestedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(await BuildWalletDtoAsync(studentId));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/withdrawals")]
        public async Task<ActionResult<IEnumerable<StudentWithdrawalRequestDto>>> GetWithdrawalRequests([FromQuery] string status = null)
        {
            var query = _context.StudentWithdrawalRequests
                .AsNoTracking()
                .Include(x => x.Student)
                .Include(x => x.ReviewedByAdmin)
                .AsQueryable();

            var normalizedStatus = NormalizeStatus(status, allowEmpty: true);
            if (!string.IsNullOrWhiteSpace(normalizedStatus))
            {
                query = query.Where(x => x.Status == normalizedStatus);
            }

            var results = await query
                .OrderByDescending(x => x.RequestedAtUtc)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return Ok(results.Select(MapWithdrawalRequest).ToList());
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/withdrawals/{id:int}/status")]
        public async Task<ActionResult<StudentWithdrawalRequestDto>> UpdateWithdrawalStatus(int id, UpdateWithdrawalRequestStatusDto model)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            var withdrawal = await _context.StudentWithdrawalRequests
                .Include(x => x.Student)
                .Include(x => x.ReviewedByAdmin)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (withdrawal == null)
            {
                return NotFound(new { Message = "Withdrawal request not found." });
            }

            var nextStatus = NormalizeStatus(model.Status);
            if (nextStatus == null)
            {
                return BadRequest(new { Message = "Status must be Pending, Approved, Rejected, or Paid." });
            }

            var transitionError = ApplyStatusTransition(withdrawal, nextStatus);
            if (transitionError != null)
            {
                return BadRequest(new { Message = transitionError });
            }

            withdrawal.AdminNotes = string.IsNullOrWhiteSpace(model.AdminNotes) ? null : model.AdminNotes.Trim();

            if (withdrawal.Status == StatusPending)
            {
                withdrawal.ReviewedAtUtc = null;
                withdrawal.ReviewedByAdminId = null;
            }
            else
            {
                withdrawal.ReviewedAtUtc = DateTime.UtcNow;
                withdrawal.ReviewedByAdminId = adminId;
            }

            await _context.SaveChangesAsync();

            withdrawal = await _context.StudentWithdrawalRequests
                .AsNoTracking()
                .Include(x => x.Student)
                .Include(x => x.ReviewedByAdmin)
                .FirstAsync(x => x.Id == id);

            return Ok(MapWithdrawalRequest(withdrawal));
        }

        private static string ApplyStatusTransition(StudentWithdrawalRequest withdrawal, string nextStatus)
        {
            if (withdrawal.Status == nextStatus)
            {
                return null;
            }

            if (withdrawal.Status == StatusPaid)
            {
                return "Paid withdrawal requests can no longer be changed.";
            }

            if (withdrawal.Status == StatusRejected)
            {
                return "Rejected withdrawal requests can no longer be changed.";
            }

            if (withdrawal.Status == StatusPending)
            {
                if (nextStatus == StatusApproved)
                {
                    withdrawal.Status = nextStatus;
                    return null;
                }

                if (nextStatus == StatusRejected)
                {
                    RefundHeldBalance(withdrawal);
                    withdrawal.Status = nextStatus;
                    return null;
                }

                if (nextStatus == StatusPending)
                {
                    return null;
                }

                return "Pending withdrawals must be approved before they can be marked as paid.";
            }

            if (withdrawal.Status == StatusApproved)
            {
                if (nextStatus == StatusPaid)
                {
                    withdrawal.Student.RxCoinOnHold = Math.Max(0m, withdrawal.Student.RxCoinOnHold - withdrawal.RxCoinAmount);
                    withdrawal.Status = nextStatus;
                    return null;
                }

                if (nextStatus == StatusRejected)
                {
                    RefundHeldBalance(withdrawal);
                    withdrawal.Status = nextStatus;
                    return null;
                }

                if (nextStatus == StatusPending)
                {
                    withdrawal.Status = nextStatus;
                    return null;
                }
            }

            return "Unsupported withdrawal status change.";
        }

        private static void RefundHeldBalance(StudentWithdrawalRequest withdrawal)
        {
            withdrawal.Student.RxCoinOnHold = Math.Max(0m, withdrawal.Student.RxCoinOnHold - withdrawal.RxCoinAmount);
            withdrawal.Student.RxCoinBalance += withdrawal.RxCoinAmount;
        }

        private async Task<RxWalletDto> BuildWalletDtoAsync(string studentId)
        {
            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == studentId);

            var requests = await _context.StudentWithdrawalRequests
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .Include(x => x.Student)
                .Include(x => x.ReviewedByAdmin)
                .OrderByDescending(x => x.RequestedAtUtc)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return new RxWalletDto
            {
                AvailableRxCoinBalance = student?.RxCoinBalance ?? 0m,
                PendingRxCoinBalance = student?.RxCoinOnHold ?? 0m,
                TotalRxCoinBalance = (student?.RxCoinBalance ?? 0m) + (student?.RxCoinOnHold ?? 0m),
                AvailablePesoEquivalent = RankingBadgeService.ConvertRxCoinToPeso(student?.RxCoinBalance ?? 0m),
                PendingPesoEquivalent = RankingBadgeService.ConvertRxCoinToPeso(student?.RxCoinOnHold ?? 0m),
                ConversionRateRxCoinPerPeso = RankingBadgeService.RxCoinPerPeso,
                WithdrawalRequests = requests.Select(MapWithdrawalRequest).ToList()
            };
        }

        private static StudentWithdrawalRequestDto MapWithdrawalRequest(StudentWithdrawalRequest request)
        {
            return new StudentWithdrawalRequestDto
            {
                Id = request.Id,
                RxCoinAmount = request.RxCoinAmount,
                PesoAmount = request.PesoAmount,
                GCashNumber = request.GCashNumber,
                GCashName = request.GCashName,
                Status = request.Status,
                AdminNotes = request.AdminNotes,
                RequestedAtUtc = request.RequestedAtUtc,
                ReviewedAtUtc = request.ReviewedAtUtc,
                StudentId = request.StudentId,
                StudentName = request.Student == null ? null : $"{request.Student.FirstName} {request.Student.LastName}".Trim(),
                StudentEmail = request.Student?.Email,
                ReviewedByAdminId = request.ReviewedByAdminId,
                ReviewedByAdminName = request.ReviewedByAdmin == null ? null : $"{request.ReviewedByAdmin.FirstName} {request.ReviewedByAdmin.LastName}".Trim()
            };
        }

        private static string NormalizeStatus(string status, bool allowEmpty = false)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return allowEmpty ? null : null;
            }

            if (string.Equals(status.Trim(), StatusPending, StringComparison.OrdinalIgnoreCase))
            {
                return StatusPending;
            }

            if (string.Equals(status.Trim(), StatusApproved, StringComparison.OrdinalIgnoreCase))
            {
                return StatusApproved;
            }

            if (string.Equals(status.Trim(), StatusRejected, StringComparison.OrdinalIgnoreCase))
            {
                return StatusRejected;
            }

            if (string.Equals(status.Trim(), StatusPaid, StringComparison.OrdinalIgnoreCase))
            {
                return StatusPaid;
            }

            return null;
        }
    }
}
