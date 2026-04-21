using Api.DTOs.Account;
using Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Authorize(Roles = "Student")]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("plans")]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetPlans()
        {
            return Ok(await _subscriptionService.GetPlansAsync());
        }

        [HttpGet("me")]
        public async Task<ActionResult<SubscriptionStatusDto>> GetMySubscription()
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            return Ok(await _subscriptionService.GetStudentSubscriptionStatusAsync(studentId));
        }

        [HttpPost("checkout")]
        public async Task<ActionResult<SubscriptionCheckoutDto>> CreateCheckout(CreateSubscriptionCheckoutDto model)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Unauthorized(new { Message = "User ID was not found in the token." });
            }

            try
            {
                return Ok(await _subscriptionService.CreateCheckoutAsync(studentId, model.PlanCode));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
