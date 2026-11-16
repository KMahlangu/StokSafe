using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Configuration;
using StokSafe.Services.Interfaces;

namespace StokSafe.Services.Implementations;

public class PayFastService : IPayFastService
{
    private readonly IConfiguration _config;

    public PayFastService(IConfiguration config)
    {
        _config = config;
    }

    public string GeneratePaymentUrl(int referenceId, decimal amount, string itemName, string paymentType)
    {
        var isSandbox = bool.Parse(_config["PayFast:IsSandbox"] ?? "true");
        var baseUrl = _config["PayFast:BaseUrl"];
        var merchantId = _config["PayFast:MerchantId"];
        var merchantKey = _config["PayFast:MerchantKey"];
        var passphrase = _config["PayFast:Passphrase"]; // Make sure this is BLANK for now

        var pfUrl = isSandbox 
            ? "https://sandbox.payfast.co.za/eng/process" 
            : "https://www.payfast.co.za/eng/process";

        var returnPath = paymentType == "Fine" ? "/Member/Fine/Index" : "/Member/Contribution/Index";
        
        var data = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", merchantId!),
            new("merchant_key", merchantKey!),
            new("return_url", $"{baseUrl}{returnPath}"), 
            new("cancel_url", $"{baseUrl}{returnPath}"),
            new("notify_url", $"{baseUrl}/Payment/PayFastNotify"),
            new("name_first", "StokSafe"),
            new("name_last", "Member"),
            new("email_address", "test@example.com"),
            new("m_payment_id", $"{paymentType}_{referenceId}"),
            new("amount", amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
            new("item_name", itemName)
        };

        var signature = GenerateSignature(data, passphrase);
        data.Add(new KeyValuePair<string, string>("signature", signature));

        var queryString = string.Join("&", data.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
        return $"{pfUrl}?{queryString}";
    }

    private string GenerateSignature(List<KeyValuePair<string, string>> data, string? passphrase)
    {
        var parameterString = new StringBuilder();
        
        foreach (var pair in data)
        {
            if (string.IsNullOrWhiteSpace(pair.Value)) continue;
            parameterString.Append($"{pair.Key}={HttpUtility.UrlEncode(pair.Value)}&");
        }

        if (!string.IsNullOrWhiteSpace(passphrase))
        {
            parameterString.Append($"passphrase={HttpUtility.UrlEncode(passphrase)}");
        }
        else
        {
            if (parameterString.Length > 0) parameterString.Length--; 
        }

        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(parameterString.ToString());
        var hashBytes = md5.ComputeHash(inputBytes);
        
        var sb = new StringBuilder();
        foreach (var b in hashBytes) sb.Append(b.ToString("x2")); 
        return sb.ToString();
    }
}