using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace StokSafe.Controllers;

[ApiController]
[Route("api/ozow/payout")]
public class OzowPayoutWebhookController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<OzowPayoutWebhookController> _logger;

    public OzowPayoutWebhookController(IConfiguration config, ILogger<OzowPayoutWebhookController> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpPost("verify")]
    public IActionResult VerifyPayout([FromBody] JsonElement payload)
    {
        // 1. Validate Bearer Token (Ozow sends an Authorization header)
        // You must set this token in the Ozow Dashboard when creating the webhook.
        var authHeader = Request.Headers["Authorization"].ToString();
        // Add your validation logic here...

        // 2. Extract Payout Details
        var payoutId = payload.GetProperty("payoutId").GetString();
        var accountNumber = payload.GetProperty("accountNumber").GetString(); // Encrypted
        var amount = payload.GetProperty("amount").GetDecimal();
        
        _logger.LogInformation($"Verification requested for Payout {payoutId}");

        // 3. Return the required response to Ozow
        // You MUST return IsVerified: true and the DecryptionKey
        var response = new
        {
            IsVerified = true,
            DecryptionKey = _config["Ozow:EncryptionKey"] // The key you use to encrypt/decrypt account numbers
        };

        return Ok(response);
    }

    [HttpPost("notify")]
    public IActionResult PayoutNotification([FromBody] JsonElement payload)
    {
        // Ozow will send "verificationSuccess" and "payoutComplete" messages here
        var status = payload.GetProperty("status").GetString();
        var payoutId = payload.GetProperty("payoutId").GetString();
        
        _logger.LogInformation($"Payout Notification: {payoutId} is {status}");
        
        // Update your database here (e.g., mark loan as disbursed)
        return Ok();
    }
}