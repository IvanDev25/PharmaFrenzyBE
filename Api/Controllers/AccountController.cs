using Api.DTOs.Account;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private static readonly HashSet<string> AllowedAvatarImages = new(StringComparer.OrdinalIgnoreCase)
        {
            "assets/1.png",
            "assets/2.png",
            "assets/3.png",
            "assets/4.png",
            "assets/5.png",
            "assets/6.png",
            "assets/7.png",
            "assets/8.png",
            "assets/9.png"
        };

        private readonly JWTService _jwtService;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly EmailService _emailService;
        private readonly RankingBadgeService _rankingBadgeService;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeZoneInfo _dailyStreakTimeZone;

        private const string RegistrationOtpCachePrefix = "registration-otp:";
        private const string RegistrationVerificationCachePrefix = "registration-verified:";
        private const string DefaultDailyStreakTimeZoneId = "Singapore Standard Time";

        public AccountController(JWTService jwtService, 
            SignInManager<User> signInManager, 
            UserManager<User> userManager,
            EmailService emailService,
            RankingBadgeService rankingBadgeService,
            IConfiguration config,
            IMemoryCache memoryCache)
        {
            _jwtService = jwtService;
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
            _rankingBadgeService = rankingBadgeService;
            _config = config;
            _memoryCache = memoryCache;
            _dailyStreakTimeZone = ResolveDailyStreakTimeZone(config["DailyStreak:TimeZoneId"]);
        }

        [Authorize]
        [HttpGet("refresh-user-token")]
        public async Task<ActionResult<UserDto>> refreshUserToken()
        {
            var user = await _userManager.FindByNameAsync(User.FindFirst(ClaimTypes.Email)?.Value);
            return await CreateApplicationUserDto(user);
        }

        [Authorize]
        [HttpPut("update-profile")]
        public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            var normalizedEmail = model.Email.Trim().ToLower();
            var existingUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.Id != user.Id);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "That email address is already being used by another account." });
            }

            var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
            var normalizedAvatarPath = NormalizeAvatarPath(model.Image);
            if (normalizedAvatarPath == null)
            {
                return BadRequest(new { Message = "Please select a valid avatar." });
            }

            user.FirstName = model.FirstName.Trim().ToLower();
            user.LastName = model.LastName.Trim().ToLower();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.University = string.IsNullOrWhiteSpace(model.University) ? null : model.University.Trim();
            user.Image = normalizedAvatarPath;

            if (emailChanged)
            {
                user.Email = normalizedEmail;
                user.UserName = normalizedEmail;
                user.NormalizedEmail = _userManager.NormalizeEmail(normalizedEmail);
                user.NormalizedUserName = _userManager.NormalizeName(normalizedEmail);
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return await CreateApplicationUserDto(user);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null) return Unauthorized("Invalid username or password");

            if (user.EmailConfirmed == false) return Unauthorized("Please Confirm you email");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded) return Unauthorized("Invalid username or password");

            return await CreateApplicationUserDto(user);
        }

        [Authorize(Roles = "Student")]
        [HttpGet("daily-streak")]
        public async Task<ActionResult<DailyStreakStatusDto>> GetDailyStreakStatus()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { Message = "User was not found." });
            }

            return Ok(BuildDailyStreakStatus(user));
        }

        [Authorize(Roles = "Student")]
        [HttpPost("daily-streak/redeem")]
        public async Task<ActionResult<DailyStreakStatusDto>> RedeemDailyStreak()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { Message = "User was not found." });
            }

            var status = BuildDailyStreakStatus(user);
            if (!status.CanRedeemToday)
            {
                return BadRequest(new { Message = "You have already redeemed today's streak reward." });
            }

            var utcNow = DateTime.UtcNow;
            var localToday = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _dailyStreakTimeZone).Date;
            var lastClaimLocalDate = GetLastClaimLocalDate(user);

            user.CurrentStreak = lastClaimLocalDate.HasValue && lastClaimLocalDate.Value == localToday.AddDays(-1)
                ? user.CurrentStreak + 1
                : 1;

            var rewardPoints = CalculateDailyStreakReward(user.CurrentStreak);
            user.LastDailyStreakClaimedAtUtc = utcNow;
            user.TotalPoints += rewardPoints;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(BuildDailyStreakStatus(user));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("daily-streak/test-enable/{studentId}")]
        public async Task<ActionResult<DailyStreakStatusDto>> EnableDailyStreakForTesting(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return BadRequest(new { Message = "Student ID is required." });
            }

            var student = await _userManager.FindByIdAsync(studentId);
            if (student == null)
            {
                return NotFound(new { Message = "Student not found." });
            }

            if (!await _userManager.IsInRoleAsync(student, "Student"))
            {
                return BadRequest(new { Message = "The selected user is not a student." });
            }

            var utcNow = DateTime.UtcNow;
            student.LastDailyStreakClaimedAtUtc = utcNow.AddDays(-1);

            var result = await _userManager.UpdateAsync(student);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(BuildDailyStreakStatus(student));
        }

        [AllowAnonymous]
        [HttpPost("send-registration-otp")]
        public async Task<IActionResult> SendRegistrationOtp(SendRegistrationOtpDto model)
        {
            var normalizedEmail = model.Email.Trim().ToLower();
            if (await CheckEmailExistsAsync(normalizedEmail))
            {
                return BadRequest($"An existing account is using {normalizedEmail}, email address. Please try another email address");
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            _memoryCache.Set(GetRegistrationOtpCacheKey(normalizedEmail), new RegistrationOtpEntry
            {
                Otp = otp
            }, TimeSpan.FromMinutes(10));
            _memoryCache.Remove(GetRegistrationVerificationCacheKey(normalizedEmail));

            var body = $"<p>Hello,</p>" +
                "<p>Your registration OTP is:</p>" +
                $"<h2 style=\"letter-spacing: 4px;\">{otp}</h2>" +
                "<p>This code will expire in 10 minutes.</p>" +
                $"<br>{_config["Email:ApplicationName"]}";

            var emailSend = new EmailSendDto(normalizedEmail, "Your registration OTP", body);
            var emailSent = await _emailService.SendEmailAsync(emailSend);

            if (!emailSent)
            {
                return BadRequest("Failed to send OTP. Please try again.");
            }

            return Ok(new
            {
                title = "OTP Sent",
                message = "We sent a 6-digit OTP to your email address."
            });
        }

        [AllowAnonymous]
        [HttpPost("verify-registration-otp")]
        public IActionResult VerifyRegistrationOtp(VerifyRegistrationOtpDto model)
        {
            var normalizedEmail = model.Email.Trim().ToLower();
            if (!_memoryCache.TryGetValue(GetRegistrationOtpCacheKey(normalizedEmail), out RegistrationOtpEntry otpEntry) ||
                otpEntry == null)
            {
                return BadRequest("Your OTP has expired. Please request a new one.");
            }

            if (!string.Equals(otpEntry.Otp, model.Otp.Trim(), StringComparison.Ordinal))
            {
                return BadRequest("Invalid OTP. Please try again.");
            }

            var verificationToken = Guid.NewGuid().ToString("N");
            _memoryCache.Set(GetRegistrationVerificationCacheKey(normalizedEmail), verificationToken, TimeSpan.FromMinutes(30));
            _memoryCache.Remove(GetRegistrationOtpCacheKey(normalizedEmail));

            return Ok(new
            {
                title = "Email Verified",
                message = "Your email has been verified. Continue with your account details.",
                verificationToken
            });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto model)
        {
            var normalizedEmail = model.Email.Trim().ToLower();

            if (await CheckEmailExistsAsync(normalizedEmail))
            {
                return BadRequest($"An existing account is using {normalizedEmail}, email address. Please try another email address");
            }

            if (!_memoryCache.TryGetValue(GetRegistrationVerificationCacheKey(normalizedEmail), out string cachedVerificationToken) ||
                string.IsNullOrWhiteSpace(cachedVerificationToken) ||
                !string.Equals(cachedVerificationToken, model.VerificationToken, StringComparison.Ordinal))
            {
                return BadRequest("Please verify your email address before creating an account.");
            }

            var normalizedAvatarPath = NormalizeAvatarPath(model.Image);
            if (normalizedAvatarPath == null)
            {
                return BadRequest("Please select a valid avatar.");
            }

            var userToAdd = new User
            {
                FirstName = model.FirstName.ToLower(),
                LastName = model.LastName.ToLower(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                University = model.University.Trim(),
                Gender = model.Gender.Trim(),
                Status = "Active",
                Image = normalizedAvatarPath,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(userToAdd, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            var roleResult = await _userManager.AddToRoleAsync(userToAdd, "Student");
            if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);

            _memoryCache.Remove(GetRegistrationVerificationCacheKey(normalizedEmail));
            return await CreateApplicationUserDto(userToAdd);
        }
        [AllowAnonymous]
        [HttpPut("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized("This email address has not been registered yet");

            if (user.EmailConfirmed == true) return BadRequest("Your email was confirmed before. Please login to your account");

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if(result.Succeeded)
                {
                    return Ok(new JsonResult(new {title = "Email confirmed", message = "Your email addres is confirmed. You can login now"}));
                }

                return BadRequest("Invalid token. Please try again");

            }
            catch(Exception)
            {
                return BadRequest("Invalid token. Please try again");
            }


        }

        [AllowAnonymous]
        [HttpPost("resend-email-confirmation-link/{email}")]
        public async Task<IActionResult> ResendEmailConfirmationLink(string email)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Invalid email");
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized("This email address has not been registered yet");
            if (user.EmailConfirmed == true) return BadRequest("Your email address was confirmed before. Please login to your account");

            try
            {
                if(await SendConfirmEmailAsync(user))
                {
                    return Ok(new JsonResult(new { title = "Confirmation link sent", message = "Please confirm your email address" }));
                }

                return BadRequest("Failed to send email. Please contact admin");
            }
            catch (Exception)
            {
                return BadRequest("Failed to send email. Please contact admin");
            }
        }

        [AllowAnonymous]
        [HttpPost("forgot-username-or-password/{email}")] 
        public async Task<IActionResult> ForgotUsernameOrPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Invalid email");
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized("This email address has not been registered yet");
            if (user.EmailConfirmed == false) return BadRequest("Please confirm your email address first");

            try
            {
                if (await SendForgotUsernameorPasswordEmail(user))
                {
                    return Ok(new JsonResult(new { title = "Forgot username or password email sent", message = "Please check your email" }));
                }

                return BadRequest("Failed to send emil. Please contact admin");
            }
            catch(Exception)
            {
                return BadRequest("Failed to send emil. Please contact admin");
            }
        }

        [AllowAnonymous]
        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized("Tis email address has not been registered yet");
            if (user.EmailConfirmed == false) return BadRequest("Please confirm your emil address first");

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);
                if (result.Succeeded)
                {
                    return Ok(new JsonResult(new { title = "Password reset success", message = "Your password has been reset." }));
                }

                return BadRequest("Invalid token. Please try again");
            }
            catch(Exception)
            {
                return BadRequest("Invalid token. Please try again");
            }
        }

        [HttpGet("get-all-users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userDtos = new List<UserDto>();

                foreach (var user in users)
                {
                    userDtos.Add(await MapUserToDtoAsync(user));
                }

                // If no users found
                if (userDtos == null || !userDtos.Any())
                {
                    return NotFound(new { Message = "No users found." });
                }

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                // Log the exception if needed, and return an internal server error
                return StatusCode(500, new { Message = "An error occurred while retrieving users.", Details = ex.Message });
            }
        }

        [HttpGet("get-users-by-role/{role}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return BadRequest(new { Message = "Role is required." });
            }

            var normalizedRole = role.Trim();
            var allowedRoles = new[] { "Student", "Admin" };

            if (!allowedRoles.Any(r => string.Equals(r, normalizedRole, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { Message = "Role must be Student or Admin." });
            }

            try
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(
                    allowedRoles.First(r => string.Equals(r, normalizedRole, StringComparison.OrdinalIgnoreCase)));

                var userDtos = new List<UserDto>();

                foreach (var user in usersInRole)
                {
                    userDtos.Add(await MapUserToDtoAsync(user));
                }

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving users by role.", Details = ex.Message });
            }
        }

        [HttpGet("get-user/{id}")]
        public async Task<ActionResult<UserDto>> GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { Message = "The user ID is required." });
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            await _rankingBadgeService.EnsurePendingBadgesAwardedAsync();

            var userDto = new UserDto
            {
                Id= user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                University = user.University,
                Gender = user.Gender,
                Status = user.Status,
                Image = user.Image,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault(),
                TotalPoints = user.TotalPoints,
                ExperiencePoints = user.ExperiencePoints,
                RxCoinBalance = user.RxCoinBalance,
                RxCoinOnHold = user.RxCoinOnHold,
                Level = CalculateLevel(user.ExperiencePoints),
                CurrentStreak = user.CurrentStreak,
                CanRedeemDailyStreakToday = BuildDailyStreakStatus(user).CanRedeemToday,
                DailyStreakRewardPoints = BuildDailyStreakStatus(user).RewardPoints,
                RankingBadges = await _rankingBadgeService.GetStudentBadgeSummaryAsync(user.Id),
                JWT = "" // Optional, you can return JWT if needed
            };

            return Ok(userDto);
        }



        [HttpDelete("delete-user/{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            try
            {
                // Find the user by the provided id
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { Message = "User not found." });
                }

                // Delete the user
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new { Message = "User deleted successfully." });
                }
                else
                {
                    // If the deletion failed, return the errors
                    return BadRequest(new { Message = "Error deleting user.", Errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary, then return a server error response
                return StatusCode(500, new { Message = "An error occurred while deleting the user.", Details = ex.Message });
            }
        }


        #region Private Helper Methods
        private async Task<UserDto> CreateApplicationUserDto(User user)
        {
            await _rankingBadgeService.EnsurePendingBadgesAwardedAsync();
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName= user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                University = user.University,
                Gender = user.Gender,
                Status = user.Status,
                Image = user.Image,
                Role = role,
                TotalPoints = user.TotalPoints,
                ExperiencePoints = user.ExperiencePoints,
                RxCoinBalance = user.RxCoinBalance,
                RxCoinOnHold = user.RxCoinOnHold,
                Level = CalculateLevel(user.ExperiencePoints),
                CurrentStreak = user.CurrentStreak,
                CanRedeemDailyStreakToday = BuildDailyStreakStatus(user).CanRedeemToday,
                DailyStreakRewardPoints = BuildDailyStreakStatus(user).RewardPoints,
                RankingBadges = await _rankingBadgeService.GetStudentBadgeSummaryAsync(user.Id),
                JWT= await _jwtService.CreateJWTAsync(user),
            };
        }

        private async Task<UserDto> MapUserToDtoAsync(User user)
        {
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                University = user.University,
                Gender = user.Gender,
                Status = user.Status,
                Image = user.Image,
                Role = role,
                TotalPoints = user.TotalPoints,
                ExperiencePoints = user.ExperiencePoints,
                RxCoinBalance = user.RxCoinBalance,
                RxCoinOnHold = user.RxCoinOnHold,
                Level = CalculateLevel(user.ExperiencePoints),
                CurrentStreak = user.CurrentStreak,
                CanRedeemDailyStreakToday = BuildDailyStreakStatus(user).CanRedeemToday,
                DailyStreakRewardPoints = BuildDailyStreakStatus(user).RewardPoints,
                JWT = ""
            };
        }

        private async Task<User> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _userManager.FindByIdAsync(userId);
        }

        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
        }

        private static string GetRegistrationOtpCacheKey(string email) => $"{RegistrationOtpCachePrefix}{email}";

        private static string GetRegistrationVerificationCacheKey(string email) => $"{RegistrationVerificationCachePrefix}{email}";

        private static string NormalizeAvatarPath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return null;
            }

            var normalizedPath = imagePath.Trim().Replace('\\', '/');
            if (normalizedPath.StartsWith("/"))
            {
                normalizedPath = normalizedPath[1..];
            }

            return AllowedAvatarImages.Contains(normalizedPath) ? normalizedPath : null;
        }

        private DailyStreakStatusDto BuildDailyStreakStatus(User user)
        {
            var utcNow = DateTime.UtcNow;
            var currentLocalDate = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _dailyStreakTimeZone).Date;
            var lastClaimLocalDate = GetLastClaimLocalDate(user);
            var canRedeemToday = !lastClaimLocalDate.HasValue || lastClaimLocalDate.Value < currentLocalDate;
            var effectiveStreak = !canRedeemToday
                ? user.CurrentStreak
                : lastClaimLocalDate.HasValue && lastClaimLocalDate.Value == currentLocalDate.AddDays(-1)
                    ? user.CurrentStreak + 1
                    : 1;

            return new DailyStreakStatusDto
            {
                CurrentStreak = effectiveStreak,
                CanRedeemToday = canRedeemToday,
                RewardPoints = CalculateDailyStreakReward(effectiveStreak),
                TotalPoints = user.TotalPoints,
                CurrentLocalDate = currentLocalDate.ToString("yyyy-MM-dd"),
                LastClaimLocalDate = lastClaimLocalDate?.ToString("yyyy-MM-dd")
            };
        }

        private DateTime? GetLastClaimLocalDate(User user)
        {
            if (!user.LastDailyStreakClaimedAtUtc.HasValue)
            {
                return null;
            }

            return TimeZoneInfo.ConvertTimeFromUtc(user.LastDailyStreakClaimedAtUtc.Value, _dailyStreakTimeZone).Date;
        }

        private static decimal CalculateDailyStreakReward(int streak)
        {
            if (streak <= 0)
            {
                return 0m;
            }

            if (streak <= 3)
            {
                return 0.20m;
            }

            return 0.50m * (((streak - 1) / 3));
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

        private static TimeZoneInfo ResolveDailyStreakTimeZone(string configuredTimeZoneId)
        {
            var timeZoneId = string.IsNullOrWhiteSpace(configuredTimeZoneId)
                ? DefaultDailyStreakTimeZoneId
                : configuredTimeZoneId.Trim();

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DefaultDailyStreakTimeZoneId);
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DefaultDailyStreakTimeZoneId);
            }
        }

        private class RegistrationOtpEntry
        {
            public string Otp { get; set; }
        }

        private async Task<bool> SendConfirmEmailAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_config["JWT:ClientUrl"]}/{_config["Email:ConfirmEmailPath"]}?token={token}&email={user.Email}";

            var body = $"<p>Hello: {user.FirstName} {user.LastName}</p>" +
                "<p> Please confirm your email address by clicking on the following link. </p>" +
                $"<p><a href=\"{url}\">Click here</a></p>" +
                "<p> Thank you, </p>" +
                $"<br>{_config["Email:ApplicationName"]}";

            var emailSend = new EmailSendDto(user.Email, "Confirm your email", body);

            return await _emailService.SendEmailAsync(emailSend);
        }

        private async Task<bool> SendForgotUsernameorPasswordEmail(User user)
        {

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_config["JWT:ClientUrl"]}/{_config["Email:ResetPasswordPath"]}?token={token}&email={user.Email}";


            var body = $"<p>Hello: {user.FirstName} {user.LastName}</p>" +
                $"<p>Username: {user.UserName}. </p>" +
                "<p> In order to reset your password, plese click on the following link</p>" +
                $"<p><a href=\"{url}\">Click here</a></p>" +
                "<p> Thank you, </p>" +
                $"<br>{_config["Email:ApplicationName"]}";

            var emailSend = new EmailSendDto(user.Email, "Forgot username or password", body);

            return await _emailService.SendEmailAsync(emailSend);
        }
        #endregion
    }
}
