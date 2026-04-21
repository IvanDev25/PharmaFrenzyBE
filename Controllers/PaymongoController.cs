using Api.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [AllowAnonymous]
    [Route("api/paymongo")]
    [ApiController]
    public class PaymongoController : ControllerBase
    {
        private readonly IPayMongoService _payMongoService;
        private readonly ISubscriptionService _subscriptionService;

        public PaymongoController(
            IPayMongoService payMongoService,
            ISubscriptionService subscriptionService)
        {
            _payMongoService = payMongoService;
            _subscriptionService = subscriptionService;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();
            var signatureHeader = Request.Headers["Paymongo-Signature"].ToString();

            if (!_payMongoService.VerifyWebhookSignature(rawBody, signatureHeader))
            {
                return Unauthorized(new { Message = "Invalid PayMongo webhook signature." });
            }

            try
            {
                await _subscriptionService.ProcessPayMongoWebhookAsync(rawBody);
                return Ok(new { Message = "Webhook processed." });
            }
            catch (JsonException)
            {
                return BadRequest(new { Message = "Invalid webhook payload." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
