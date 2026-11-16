using StokSafe.Models.ViewModels;

namespace StokSafe.Services.Interfaces;

public interface IStatementService
{
    Task<StatementViewModel?> BuildStatementAsync(int clubMemberId, int year, int month);
    Task<IEnumerable<(int Year, int Month)>> GetAvailableMonthsAsync(int clubMemberId);
    Task<bool> SaveSnapshotAsync(int clubMemberId, int year, int month);
}