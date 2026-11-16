using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using StokSafe.Services.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace StokSafe.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IConfiguration config, ILogger<NotificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, byte[]? attachment = null, string? attachmentName = null)
    {
        try
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var smtpPortStr = _config["EmailSettings:SmtpPort"];
            var senderName = _config["EmailSettings:SenderName"];
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Email skipped: SenderEmail or Username is missing in appsettings.json.");
                return;
            }

            int smtpPort = string.IsNullOrWhiteSpace(smtpPortStr) ? 587 : int.Parse(smtpPortStr);

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName ?? "StokSafe", senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpServer!, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username!, password!);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation($"Email successfully sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}");
        }
    }

    public Task SendSmsAsync(string toPhoneNumber, string message)
    {
        try
        {
            var accountSid = _config["TwilioSettings:AccountSid"];
            var authToken = _config["TwilioSettings:AuthToken"];
            var fromNumber = _config["TwilioSettings:FromNumber"];

            if (string.IsNullOrEmpty(accountSid) || accountSid.Contains("your_"))
            {
                _logger.LogWarning("SMS skipped: Twilio credentials are not configured.");
                return Task.CompletedTask;
            }

            TwilioClient.Init(accountSid, authToken);

            // TRIAL ACCOUNT FIX: Use a predefined template name as the body
            // 'sms_account_alerts' is a standard Twilio trial template
            var messageResource = MessageResource.Create(
                body: "sms_account_alerts", 
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(toPhoneNumber)
            );

            _logger.LogInformation($"SMS successfully sent to {toPhoneNumber}. SID: {messageResource.Sid}");
        }
        catch (Twilio.Exceptions.ApiException ex)
        {
            _logger.LogError($"Twilio API Error! Code: {ex.Code}, Message: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"General failure sending SMS to {toPhoneNumber}");
        }

        return Task.CompletedTask;
    }
}