namespace StokSafe.Models;

public class ProofOfPayment
{
    public int Id { get; set; }
    
    // What is this POP for?
    public int? ContributionId { get; set; }
    public Contribution? Contribution { get; set; }
    
    public int? FineId { get; set; }
    public Fine? Fine { get; set; }

    public int UploadedByUserId { get; set; }
    public User UploadedBy { get; set; } = null!;

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    
    // Status: Pending, Approved, Rejected
    public string Status { get; set; } = "Pending";
    
    public string? AdminNotes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}