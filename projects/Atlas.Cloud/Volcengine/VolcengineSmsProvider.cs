using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Volcengine;

/// <summary>
/// 火山引擎短信服务实现，使用 HMAC-SHA256 签名（类似腾讯云 TC3-HMAC-SHA256）
/// </summary>
public sealed class VolcengineSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;

    private const string _endpoint = "sms.volcengineapi.com";
    private const string _service = "sms";

    /// <summary>
    /// 初始化火山引擎短信服务
    /// </summary>
    /// <param name="accessKey">AccessKey</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，如 cn-beijing</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public VolcengineSmsProvider(
        string accessKey, string secretKey,
        string region = "cn-beijing",
        HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
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
        var payload = new Dictionary<string, object?>
        {
            ["PhoneNumbers"] = phoneNumber,
            ["TemplateID"] = templateCode
        };

        if (signName is not null)
        {
            payload["SmsAccount"] = signName;
            payload["Sign"] = signName;
        }

        if (templateParams is not null)
        {
            payload["TemplateParam"] = System.Text.Json.JsonSerializer.Serialize(templateParams);
        }

        var requestBody = System.Text.Json.JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_endpoint}/?Action=SendSms&Version=2021-01-01")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        var auth = sign_request(requestBody, timestamp);
        request.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("ResponseMetadata", out var metadata))
        {
            if (metadata.TryGetProperty("Error", out var error))
            {
                var msg = error.TryGetProperty("Message", out var m) ? m.get_string() ?? "未知错误" : "未知错误";
                return SmsResult.fail(msg);
            }
        }

        var requestId = root.TryGetProperty("ResponseMetadata", out var meta)
            ? meta.TryGetProperty("RequestId", out var rid) ? rid.get_string() : null
            : null;

        return SmsResult.ok(requestId);
    }

    private string sign_request(string payload, long timestamp)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var credentialScope = $"{date}/{_region}/{_service}/request";

        var canonicalRequest = $"POST\n/\nAction=SendSms&Version=2021-01-01\n"
                               + $"content-type:application/json; charset=utf-8\n"
                               + $"host:{_endpoint}\n\n"
                               + $"content-type;host\n"
                               + sha256_hex(payload);

        var stringToSign = $"HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes(_secret_key), date);
        var secretRegion = hmac_sha256(secretDate, _region);
        var secretService = hmac_sha256(secretRegion, _service);
        var secretSigning = hmac_sha256(secretService, "request");
        var signature = BitConverter.ToString(hmac_sha256(secretSigning, stringToSign)).Replace("-", "").ToLowerInvariant();

        return $"HMAC-SHA256 Credential={_access_key}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";
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
