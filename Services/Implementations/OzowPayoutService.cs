using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace StokSafe.Services.Implementations;

public class OzowPayoutService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly ILogger<OzowPayoutService> _logger;

    public OzowPayoutService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<OzowPayoutService> logger)
    {
        _config = config;
        _http = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<string?> RequestPayoutAsync(string accountNumber, string branchCode, string bankGroupId, decimal amount, string merchantReference, string customerBankReference, string notifyUrl)
    {
        var isSandbox = bool.Parse(_config["Ozow:IsSandbox"] ?? "true");
        var baseUrl = isSandbox ? "https://stagingpayoutsapi.ozow.com/v1" : "https://payoutsapi.ozow.com/v1";
        
        var siteCode = _config["Ozow:SiteCode"];
        var apiKey = _config["Ozow:PayoutApiKey"]; // Payouts use a different API Key
        var privateKey = _config["Ozow:PrivateKey"]; // Used for the hash

        // 1. Prepare data for Hash (Order is CRITICAL)
        // 1. siteCode, 2. amount in cents (rounded), 3. merchantReference, 4. customerBankReference, 5. isRtc, 6. notifyUrl, 7. bankGroupId, 8. encryptedAccountNumber (placeholder for now), 9. branchCode, 10. PrivateKey
        
        // Note: For actual production, the account number must be AES encrypted using the decryption key sent to your verification webhook.
        // For testing, Ozow's staging may accept a placeholder or requires specific test encryption.
        var accountNumberForHash = accountNumber; // In production, this must be the encrypted string
        var amountInCents = (int)Math.Round(amount * 100);
        var isRtc = "false"; // Must be false in staging [citation:3]

        var rawString = $"{siteCode}{amountInCents}{merchantReference}{customerBankReference}{isRtc}{notifyUrl}{bankGroupId}{accountNumberForHash}{branchCode}{privateKey}";
        
        // 2. Generate Hash (SHA512, lowercase)
        var hash = GenerateSha512(rawString.ToLower());

        // 3. Build Request Payload
        var payload = new
        {
            siteCode = siteCode,
            amount = amount,
            merchantReference = merchantReference,
            customerBankReference = customerBankReference,
            isRtc = false,
            notifyUrl = notifyUrl,
            bankingDetails = new
            {
                bankGroupId = bankGroupId,
                accountNumber = accountNumberForHash, // Must be encrypted in prod
                branchCode = branchCode
            },
            hashCheck = hash
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/requestpayout");
        request.Headers.Add("ApiKey", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Ozow Payout Error: {responseBody}");
            return null;
        }

        // 4. Parse Response
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        
        // Check if the request was accepted (payoutId populated) or rejected
        if (root.TryGetProperty("payoutId", out var payoutId) && !string.IsNullOrEmpty(payoutId.GetString()))
        {
            _logger.LogInformation($"Payout requested successfully. ID: {payoutId}");
            return payoutId.GetString();
        }
        else
        {
            var error = root.GetProperty("payoutStatus").GetProperty("errorMessage").GetString();
            _logger.LogError($"Payout rejected: {error}");
            return null;
        }
    }

    private string GenerateSha512(string input)
    {
        using var sha512 = SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha512.ComputeHash(bytes);
        
        var sb = new StringBuilder();
        foreach (var b in hash) sb.Append(b.ToString("x2")); // lowercase hex
        return sb.ToString();
    }
}