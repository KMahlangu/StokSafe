namespace StokSafe.Models;

public class MonthlyStatement
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalContributions { get; set; }
    public decimal TotalFines { get; set; }
    public decimal TotalLoans { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}