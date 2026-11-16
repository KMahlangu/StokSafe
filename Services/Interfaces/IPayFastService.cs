namespace StokSafe.Services.Interfaces;

public interface IPayFastService
{
    string GeneratePaymentUrl(int referenceId, decimal amount, string itemName, string paymentType);
}