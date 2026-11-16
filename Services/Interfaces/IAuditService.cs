namespace StokSafe.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityName, string entityId, string details);
}