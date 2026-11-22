using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services.Implementations;

public class OzowPaymentService : IOzowPaymentService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly ILogger<OzowPaymentService> _logger;

    public OzowPaymentService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<OzowPaymentService> logger)
    {
        _config = config;
        _http = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<string> CreatePaymentUrlAsync(int referenceId, decimal amount, string itemName, string paymentType)
    {
        var isSandbox = bool.Parse(_config["Ozow:IsSandbox"] ?? "true");
        var baseUrl = _config["Ozow:BaseUrl"];
        var clientId = _config["Ozow:ClientId"];
        var clientSecret = _config["Ozow:ClientSecret"];
        var siteCode = _config["Ozow:SiteCode"];

        var apiUrl = isSandbox ? "https://stagingone.ozow.com" : "https://one.ozow.com";

        // 1. Get OAuth Token (Valid for 4 hours, but we get a new one each time for simplicity here)
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/v1/token");
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", clientId! },
            { "client_secret", clientSecret! },
            { "scope", "payments" },
            { "grant_type", "client_credentials" }
        });

        var tokenResponse = await _http.SendAsync(tokenRequest);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        
        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError($"Ozow Token Error: {tokenJson}");
            throw new Exception("Could not authenticate with Ozow.");
        }

        using var tokenDoc = JsonDocument.Parse(tokenJson);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

        // 2. Create Payment Request (NEW NESTED FORMAT)
        var returnPath = $"{baseUrl}/Payment/Return"; // Unified return URL
        
        var payload = new
        {
            siteCode = siteCode,
            amount = new 
            { 
                currency = "ZAR", 
                value = amount // Note: This is a number, not a string
            },
            merchantReference = $"{paymentType}_{referenceId}",
            beneficiaryReference = "StokSafe",
            expireAt = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            returnUrl = returnPath
        };

        var paymentRequest = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/v1/payments");
        paymentRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        paymentRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var paymentResponse = await _http.SendAsync(paymentRequest);
        var paymentJson = await paymentResponse.Content.ReadAsStringAsync();

        if (!paymentResponse.IsSuccessStatusCode)
        {
            _logger.LogError($"Ozow Payment Error: {paymentJson}");
            throw new Exception("Could not create Ozow payment link.");
        }

        using var paymentDoc = JsonDocument.Parse(paymentJson);
        
        // 3. Return the redirectUrl
        return paymentDoc.RootElement.GetProperty("redirectUrl").GetString() ?? throw new Exception("No redirect URL returned.");
    }
}