namespace StokSafe.Models;

public class LoanPayment
{
    public int Id { get; set; }

    public int LoanId { get; set; }
    public Loan Loan { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
}