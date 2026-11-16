using System.ComponentModel.DataAnnotations.Schema;
using StokSafe.Models.Enums;

namespace StokSafe.Models;

public class Fine
{
    public int Id { get; set; }

    // ---- Relationships ----
    public int ClubMemberId { get; set; }
    public ClubMember ClubMember { get; set; } = null!;

    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public int? ContributionId { get; set; }
    public Contribution? Contribution { get; set; }

    // ---- Classification ----
    public FineType FineType { get; set; }

    // ---- Money ----
    [Column(TypeName = "decimal(10,2)")]
    public decimal OriginalAmount { get; set; }        // First fine (e.g. R 50)

    [Column(TypeName = "decimal(10,2)")]
    public decimal CurrentAmount { get; set; }         // After escalations (R 50 → 100 → 200...)

    [Column(TypeName = "decimal(10,2)")]
    public decimal PaidAmount { get; set; }            // Running total paid

    [Column(TypeName = "decimal(10,2)")]
    public decimal Balance { get; set; }               // CurrentAmount - PaidAmount

    // ---- Lifecycle ----
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public FineStatus Status { get; set; } = FineStatus.Pending;
    public FineEscalationLevel EscalationLevel { get; set; } = FineEscalationLevel.Level1;
    public int WeeksOverdue { get; set; }

    // ---- Metadata ----
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsCompulsory { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Navigation ----
    public ICollection<FinePayment> Payments { get; set; } = new List<FinePayment>();

    // =================================================================
    // STATIC POLICY HELPERS
    // =================================================================

    /// <summary>Default fine amounts per type.</summary>
    public static decimal GetFineAmount(FineType type) => type switch
    {
        FineType.LateMeetingAttendance => 30m,
        FineType.NoBanking             => 50m,
        FineType.NoProofOfPayment      => 30m,
        FineType.ServiceFee            => 100m,
        FineType.LateContribution      => 50m,   // ← matches your rule
        FineType.Penalty               => 0m,
        _                              => 0m
    };

    public static bool IsCompulsoryFine(FineType type) => type == FineType.ServiceFee;

    /// <summary>Only late-contribution fines compound weekly at 100%.</summary>
    public static bool EscalatesWeekly(FineType type) => type == FineType.LateContribution;

    // =================================================================
    // ESCALATION — the 100% weekly doubling rule
    // =================================================================

    /// <summary>
    /// Called when a fine becomes past due. For LateContribution fines,
    /// the amount doubles (100% increase) and WeeksOverdue is bumped.
    /// Other fine types do not compound.
    /// </summary>
    public void ApplyWeeklyEscalation()
    {
        if (!EscalatesWeekly(FineType)) return;

        // Double the current amount — compound, not tiered
        CurrentAmount = Math.Round(CurrentAmount * 2m, 2);
        Balance = CurrentAmount - PaidAmount;
        WeeksOverdue += 1;

        // Push due date forward one week so we don't double again this week
        DueDate = DateTime.UtcNow.AddDays(7);

        // Escalation level capped at Level5
        var nextLevel = Math.Min((int)EscalationLevel + 1, 5);
        EscalationLevel = (FineEscalationLevel)nextLevel;

        if (Status == FineStatus.Pending || Status == FineStatus.PartiallyPaid)
            Status = FineStatus.Escalated;

        // Audit trail
        Notes = string.IsNullOrWhiteSpace(Notes)
            ? $"[{DateTime.UtcNow:yyyy-MM-dd}] Doubled to R {CurrentAmount:N2} (week {WeeksOverdue})"
            : Notes + $"\n[{DateTime.UtcNow:yyyy-MM-dd}] Doubled to R {CurrentAmount:N2} (week {WeeksOverdue})";
    }

    // =================================================================
    // PAYMENT
    // =================================================================

    public void AddPayment(FinePayment p)
    {
        if (p.Amount <= 0) return;

        PaidAmount += p.Amount;
        Balance = Math.Max(0, CurrentAmount - PaidAmount);

        if (Balance <= 0)
        {
            Status = FineStatus.Paid;
            PaidDate = DateTime.UtcNow;
        }
        else if (PaidAmount > 0)
        {
            Status = FineStatus.PartiallyPaid;
        }
    }

    public void Waive(string reason)
    {
        Status = FineStatus.Waived;
        Balance = 0;
        Notes = string.IsNullOrWhiteSpace(Notes)
            ? $"[{DateTime.UtcNow:yyyy-MM-dd}] Waived: {reason}"
            : Notes + $"\n[{DateTime.UtcNow:yyyy-MM-dd}] Waived: {reason}";
    }

    // =================================================================
    // HELPERS
    // =================================================================

    public decimal GetRemainingAmount() => Math.Max(0, CurrentAmount - PaidAmount);

    public string GetEscalationDescription() => EscalationLevel switch
    {
        FineEscalationLevel.Level1 => "Standard",
        FineEscalationLevel.Level2 => "Doubled (1 week overdue)",
        FineEscalationLevel.Level3 => "4× (2 weeks overdue)",
        FineEscalationLevel.Level4 => "8× (3 weeks overdue)",
        FineEscalationLevel.Level5 => "16×+ (4+ weeks overdue)",
        _                          => "Unknown"
    };

    /// <summary>Human-readable summary for logs / views.</summary>
    public override string ToString() =>
        $"{FineType} #{Id}: R {Balance:N2} owed ({Status})";
}