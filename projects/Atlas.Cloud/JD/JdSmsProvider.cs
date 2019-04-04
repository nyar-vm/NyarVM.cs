using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.JD;

/// <summary>
/// 京东云短信服务实现，使用京东云 SMS API HMAC-SHA256 签名
/// </summary>
public sealed class JdSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _secret_access_key;
    private readonly string _region;
    private const string _service = "sms";

    /// <summary>
    /// 初始化京东云短信服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="secretAccessKey">AccessKey Secret</param>
    /// <param name="region">区域，如 cn-north-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public JdSmsProvider(
        string accessKeyId, string secretAccessKey,
        string region = "cn-north-1", HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _secret_access_key = secretAccessKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<SmsResult> send(
        string phoneNumber, string templateCode,
        IReadOnlyDictionary<string, string>? templateParams = null,
        string? signName = null,
        CancellationToken cancel = default)
    {
        var endpoint = $"sms.{_region}.jdcloud-api.com";
        var payload = new Dictionary<string, object?>
        {
            ["phone"] = phoneNumber,
            ["templateId"] = templateCode
        };

        if (signName is not null)
        {
            payload["signId"] = signName;
        }

        if (templateParams is not null)
        {
            payload["params"] = templateParams;
        }

        var requestBody = System.Text.Json.JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var url = $"https://{endpoint}/?Action=SendSms&Version=2021-02-01";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        var auth = sign_request(requestBody, timestamp, endpoint);
        request.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("error", out var error))
        {
            var msg = error.TryGetProperty("message", out var m) ? m.get_string() ?? "未知错误" : "未知错误";
            return SmsResult.fail(msg);
        }

        var requestId = doc.RootElement.TryGetProperty("requestId", out var rid)
            ? rid.get_string()
            : null;

        return SmsResult.ok(requestId);
    }

    private string sign_request(string payload, long timestamp, string endpoint)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var credentialScope = $"{date}/{_region}/{_service}/request";

        var canonicalRequest = $"POST\n/\nAction=SendSms&Version=2021-02-01\n"
                               + $"content-type:application/json; charset=utf-8\n"
                               + $"host:{endpoint}\n\n"
                               + $"content-type;host\n"
                               + sha256_hex(payload);

        var stringToSign = $"HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes(_secret_access_key), date);
        var secretRegion = hmac_sha256(secretDate, _region);
        var secretService = hmac_sha256(secretRegion, _service);
        var secretSigning = hmac_sha256(secretService, "request");
        var signature = BitConverter.ToString(hmac_sha256(secretSigning, stringToSign)).Replace("-", "").ToLowerInvariant();

        return $"HMAC-SHA256 Credential={_access_key_id}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
