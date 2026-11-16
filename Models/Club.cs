using StokSafe.Models.Enums;

namespace StokSafe.Models;

public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClubMember> Members { get; set; } = new List<ClubMember>();
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
