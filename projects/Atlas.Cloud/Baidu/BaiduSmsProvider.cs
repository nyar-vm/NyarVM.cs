using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Baidu;

/// <summary>
/// 百度云短信服务实现，使用百度云 SMS API HMAC-SHA256 签名
/// </summary>
public sealed class BaiduSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _secret_access_key;
    private const string _endpoint = "smsv3.bj.baidubce.com";
    private const string _service = "bce";

    /// <summary>
    /// 初始化百度云短信服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="secretAccessKey">AccessKey Secret</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public BaiduSmsProvider(string accessKeyId, string secretAccessKey, HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _secret_access_key = secretAccessKey;
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
            ["mobile"] = phoneNumber,
            ["template"] = templateCode
        };

        if (signName is not null)
        {
            payload["signatureId"] = signName;
        }

        if (templateParams is not null)
        {
            payload["contentVar"] = templateParams;
        }

        var requestBody = System.Text.Json.JsonSerializer.Serialize(payload);
        var path = "/api/v3/sms/sendSms";
        var url = $"https://{_endpoint}{path}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        sign_request(request, path, requestBody);

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("code", out var code))
        {
            var codeStr = code.get_string() ?? string.Empty;

            if (codeStr == "1000")
            {
                var requestId = doc.RootElement.TryGetProperty("requestId", out var rid)
                    ? rid.get_string()
                    : null;
                return SmsResult.ok(requestId);
            }

            var message = doc.RootElement.TryGetProperty("message", out var msg)
                ? msg.get_string() ?? codeStr
                : codeStr;
            return SmsResult.fail(message);
        }

        return SmsResult.fail("无效响应");
    }

    private void sign_request(HttpRequestMessage request, string path, string body)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        request.Headers.Add("Host", _endpoint);
        request.Headers.Add("x-bce-date", timestamp);

        var payloadHash = sha256_hex(Encoding.UTF8.GetBytes(body));
        var signedHeaders = "host;x-bce-date";

        var canonicalRequest = $"POST\n{path}\n\n"
                               + $"host:{_endpoint}\n"
                               + $"x-bce-date:{timestamp}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var signingKey = hmac_sha256(Encoding.UTF8.GetBytes(_secret_access_key), canonicalRequest);
        var signature = Convert.ToBase64String(signingKey);

        request.Headers.Add("Authorization",
            $"bce-auth-v1/{_access_key_id}/{timestamp}/3600/{signedHeaders}/{signature}");
    }

    private static string sha256_hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}
