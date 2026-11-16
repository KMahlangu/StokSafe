namespace StokSafe.Services.Interfaces;

public interface IOzowPaymentService
{
    Task<string> CreatePaymentUrlAsync(int referenceId, decimal amount, string itemName, string paymentType);
}