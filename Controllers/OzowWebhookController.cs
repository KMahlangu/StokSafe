using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StokSafe.Data;
using StokSafe.Models.Enums;

namespace StokSafe.Controllers;

[ApiController]
[Route("api/ozow")]
public class OzowWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<OzowWebhookController> _logger;

    public OzowWebhookController(ApplicationDbContext db, IConfiguration config, ILogger<OzowWebhookController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var svixId = Request.Headers["svix-id"].ToString();
        var svixTimestamp = Request.Headers["svix-timestamp"].ToString();
        var svixSignature = Request.Headers["svix-signature"].ToString();
        var webhookSecret = _config["Ozow:WebhookSecret"]; 

        if (string.IsNullOrEmpty(webhookSecret) || !IsValidSignature(rawBody, svixId, svixTimestamp, svixSignature, webhookSecret))
        {
            _logger.LogWarning("Invalid Ozow webhook signature.");
            return Unauthorized("Invalid signature");
        }

        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        
        if (root.GetProperty("type").GetString() != "transaction.complete") return Ok();

        var data = root.GetProperty("data");
        var status = data.GetProperty("status").GetString(); // "Successful", "Error", "Pending", "Incomplete"
        var merchantReference = data.GetProperty("merchantReference").GetString(); // e.g., "Fine_12"
        
        if (string.IsNullOrEmpty(merchantReference)) return Ok();

        var parts = merchantReference.Split('_');
        if (parts.Length != 2 || !int.TryParse(parts[1], out int entityId)) return Ok();

        var paymentType = parts[0];

        if (status == "Successful")
        {
            if (paymentType == "Fine")
            {
                var fine = await _db.Fines.FindAsync(entityId);
                if (fine != null && fine.Status != FineStatus.Paid)
                {
                    fine.Status = FineStatus.Paid;
                    fine.PaidDate = DateTime.UtcNow;
                    fine.PaidAmount = fine.CurrentAmount;
                    fine.Balance = 0;
                }
            }
            else if (paymentType == "Contribution")
            {
                var contrib = await _db.Contributions.FindAsync(entityId);
                if (contrib != null && contrib.Status != ContributionStatus.Paid)
                {
                    contrib.Status = ContributionStatus.Paid;
                    contrib.PaidAt = DateTime.UtcNow;
                }
            }
            await _db.SaveChangesAsync();
            _logger.LogInformation($"Ozow payment successful for {merchantReference}");
        }
        else if (status == "Error" || status == "Incomplete")
        {
            _logger.LogWarning($"Ozow payment {status} for {merchantReference}");
        }

        return Ok();
    }

    private bool IsValidSignature(string payload, string svixId, string svixTimestamp, string svixSignature, string secret)
    {
        if (string.IsNullOrEmpty(svixId) || string.IsNullOrEmpty(svixTimestamp) || string.IsNullOrEmpty(svixSignature)) return false;

        if (!long.TryParse(svixTimestamp, out var timestamp)) return false;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > 300) return false; // 5 min replay protection

        var secretBytes = Convert.FromBase64String(secret.Replace("whsec_", ""));
        var signedContent = $"{svixId}.{svixTimestamp}.{payload}";
        
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent));
        var computedSignature = Convert.ToBase64String(hash);

        foreach (var sig in svixSignature.Split(' '))
        {
            var parts = sig.Split(',');
            if (parts.Length == 2 && parts[0] == "v1")
            {
                if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(parts[1]), Encoding.UTF8.GetBytes(computedSignature)))
                    return true;
            }
        }
        return false;
    }
}