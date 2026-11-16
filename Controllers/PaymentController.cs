using Microsoft.AspNetCore.Mvc;
using StokSafe.Data;
using StokSafe.Models.Enums;

namespace StokSafe.Controllers;

[Route("Payment")]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(ApplicationDbContext db, ILogger<PaymentController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("PayFastNotify")]
    public IActionResult PayFastNotify() => Ok("PayFast webhook is listening.");

    [HttpPost("PayFastNotify")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PayFastNotify([FromForm] IFormCollection form)
    {
        _logger.LogInformation("PayFast ITN received!");

        if (form.TryGetValue("m_payment_id", out var paymentIdStr) && 
            form.TryGetValue("payment_status", out var status) && 
            status == "COMPLETE")
        {
            var parts = paymentIdStr.ToString().Split('_');
            if (parts.Length == 2 && int.TryParse(parts[1], out var id))
            {
                var type = parts[0];

                if (type == "Fine")
                {
                    var fine = await _db.Fines.FindAsync(id);
                    if (fine != null && fine.Status != FineStatus.Paid)
                    {
                        fine.Status = FineStatus.Paid;
                        fine.PaidDate = DateTime.UtcNow;
                        fine.PaidAmount = fine.CurrentAmount;
                        fine.Balance = 0;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation($"Fine {id} marked as Paid via PayFast.");
                    }
                }
                else if (type == "Contribution")
                {
                    var contrib = await _db.Contributions.FindAsync(id);
                    if (contrib != null && contrib.Status != ContributionStatus.Paid)
                    {
                        contrib.Status = ContributionStatus.Paid;
                        contrib.PaidAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation($"Contribution {id} marked as Paid via PayFast.");
                    }
                }
            }
        }

        // Always return 200 OK so PayFast stops retrying
        return Ok();
    }
}