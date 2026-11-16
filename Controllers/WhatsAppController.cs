using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace StokSafe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(ApplicationDbContext db, IConfiguration config, ILogger<WhatsAppController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveMessage([FromForm] string From, [FromForm] string Body)
    {
        _logger.LogInformation($"WhatsApp Message from {From}: {Body}");

        // Clean the incoming number (remove "whatsapp:" prefix and "+")
        var cleanNumber = From.Replace("whatsapp:", "").Replace("+", "").Trim();
        
        // 1. Find the user by phone number
        var user = await _db.Users
            .Include(u => u.ClubMembers)
            .ThenInclude(cm => cm.Club)
            .FirstOrDefaultAsync(u => u.PhoneNumber != null && u.PhoneNumber.Contains(cleanNumber));

        string replyMessage = "";

        if (user == null)
        {
            replyMessage = "?? Welcome to StokSafe!\n\nWe couldn't find your phone number in our system. Please register on our website first to join a stokvel.";
        }
        else
        {
            var member = user.ClubMembers.FirstOrDefault();
            if (member == null)
            {
                replyMessage = $"Hi {user.Name}! You are not currently linked to any stokvel. Please contact support.";
            }
            else
            {
                // 2. Find the Admin/Head of that specific club
                var admin = await _db.ClubMembers
                    .Include(cm => cm.User)
                    .FirstOrDefaultAsync(cm => cm.ClubId == member.ClubId && 
                        (cm.Role == Models.Enums.UserRole.Admin || cm.Role == Models.Enums.UserRole.Head));

                var adminPhone = admin?.User?.PhoneNumber;
                
                if (string.IsNullOrEmpty(adminPhone))
                {
                    replyMessage = $"Hi {user.Name}! You belong to *{member.Club?.Name}*, but your Admin hasn't set up their phone number yet.";
                }
                else
                {
                    // Format the admin's number for WhatsApp click-to-chat
                    var adminWhatsAppLink = $"https://wa.me/{adminPhone.Replace("+", "").Trim()}";
                    
                    replyMessage = $"Hi {user.Name}! ??\n\nYou are a member of *{member.Club?.Name}*.\n\n" +
                                   $"To speak to your Stokvel Admin, click here:\n{adminWhatsAppLink}\n\n" +
                                   $"Have a great day!";
                }
            }
        }

        // 3. Send the reply via Twilio WhatsApp
        await SendWhatsAppMessageAsync(From, replyMessage);

        return Ok(); // Twilio expects a 200 OK to stop retrying
    }

    private async Task SendWhatsAppMessageAsync(string to, string body)
    {
        try
        {
            var accountSid = _config["TwilioSettings:AccountSid"];
            var authToken = _config["TwilioSettings:AuthToken"];
            // Use the Twilio WhatsApp sandbox number for testing
            var fromWhatsApp = _config["TwilioSettings:WhatsAppNumber"] ?? "whatsapp:+14155238886"; 

            if (string.IsNullOrEmpty(accountSid) || accountSid.Contains("your_")) return;

            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                body: body,
                from: new PhoneNumber(fromWhatsApp),
                to: new PhoneNumber(to)
            );

            _logger.LogInformation($"WhatsApp reply sent. SID: {message.Sid}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message");
        }
    }
}