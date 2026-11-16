namespace StokSafe.Models;

public class FinePayment
{
    public int Id { get; set; }

    public int FineId { get; set; }
    public Fine Fine { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
}
