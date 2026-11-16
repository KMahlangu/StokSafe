namespace StokSafe.Services.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage, byte[]? attachment = null, string? attachmentName = null);
    Task SendSmsAsync(string toPhoneNumber, string message);
}