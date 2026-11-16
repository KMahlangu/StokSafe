using StokSafe.Models;

namespace StokSafe.Services.Interfaces;

public interface IFineService
{
    // existing methods...

    // ✅ New methods used by FineController
    Task<IEnumerable<Fine>> GetMemberFinesAsync(int memberId);
    Task<Fine?> GetFineAsync(int fineId);
    Task<bool> ProcessFinePaymentAsync(int fineId, decimal amount, string? reference);
    Task<bool> WaiveFineAsync( int fineId, string reason);
}