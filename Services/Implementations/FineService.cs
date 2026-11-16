using Microsoft.EntityFrameworkCore;
using StokSafe.Data;
using StokSafe.Models;
using StokSafe.Models.Enums;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services.Implementations;

public class FineService : IFineService
{
    private readonly ApplicationDbContext _db;

    public FineService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Fine>> GetMemberFinesAsync(int clubMemberId)
    {
        return await _db.Fines
            .Where(f => f.ClubMemberId == clubMemberId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Fine?> GetFineAsync(int fineId)
    {
        return await _db.Fines
            .Include(f => f.ClubMember)
            .Include(f => f.Club)
            .FirstOrDefaultAsync(f => f.Id == fineId);
    }

    public async Task<bool> ProcessFinePaymentAsync(int fineId, decimal amount, string? reference)
    {
        if (amount <= 0) return false;

        var fine = await _db.Fines.FirstOrDefaultAsync(f => f.Id == fineId);
        if (fine is null) return false;

        // If it's already fully paid, do nothing
        if (fine.Status == FineStatus.Paid) return false;

        // Cap the payment at the remaining balance
        if (amount > fine.Balance) amount = fine.Balance;

        // 1. Record the payment
        _db.FinePayments.Add(new FinePayment
        {
            FineId = fineId,
            Amount = amount,
            Reference = reference ?? "Member Payment",
            PaidAt = DateTime.UtcNow
        });

        // 2. Update the fine's running totals
        fine.PaidAmount += amount;
        fine.Balance = fine.CurrentAmount - fine.PaidAmount;

        // 3. CRITICAL: Update the status based on the new balance
        if (fine.Balance <= 0)
        {
            fine.Balance = 0;
            fine.Status = FineStatus.Paid;
            fine.PaidDate = DateTime.UtcNow;
        }
        else
        {
            fine.Status = FineStatus.PartiallyPaid;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> WaiveFineAsync(int fineId, string reason)
    {
        var fine = await _db.Fines.FirstOrDefaultAsync(f => f.Id == fineId);
        if (fine is null) return false;

        if (fine.Status == FineStatus.Paid) return false;

        fine.Status = FineStatus.Waived;
        fine.Balance = 0;
        fine.Notes = string.IsNullOrWhiteSpace(fine.Notes)
            ? $"Waived: {reason}"
            : $"{fine.Notes}\nWaived: {reason}";

        await _db.SaveChangesAsync();
        return true;
    }

}