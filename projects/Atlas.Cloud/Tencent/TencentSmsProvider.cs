using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云短信服务实现，使用腾讯云 SMS API v3 签名
/// </summary>
public sealed class TencentSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private readonly string _app_id;

    private const string _endpoint = "sms.tencentcloudapi.com";
    private const string _service = "sms";

    /// <summary>
    /// 初始化腾讯云短信服务
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="appId">短信应用 ID</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentSmsProvider(string secretId, string secretKey, string appId, HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _app_id = appId;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<SmsResult> send(
        string phoneNumber, string templateCode,
        IReadOnlyDictionary<string, string>? templateParams = null,
        string? signName = null,
        CancellationToken cancel = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["PhoneNumberSet"] = new[] { $"+86{phoneNumber}" },
            ["SmsSdkAppId"] = _app_id,
            ["TemplateId"] = templateCode
        };

        if (signName is not null)
        {
            payload["SignName"] = signName;
        }

        if (templateParams is not null)
        {
            payload["TemplateParamSet"] = templateParams.Values.ToArray();
        }

        var requestBody = System.Text.Json.JsonSerializer.Serialize(payload);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_endpoint}")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json; charset=utf-8")
        };

        request.Headers.Add("X-TC-Action", "SendSms");
        request.Headers.Add("X-TC-Version", "2021-01-11");
        request.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        request.Headers.Add("X-TC-Region", "ap-guangzhou");

        var auth = sign_v3(requestBody, timestamp);
        request.Headers.Add("Authorization", auth);

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("Response", out var resp))
        {
            if (resp.TryGetProperty("Error", out var error))
            {
                var msg = error.TryGetProperty("Message", out var m) ? m.get_string() ?? "未知错误" : "未知错误";
                return SmsResult.fail(msg);
            }

            var requestId = resp.TryGetProperty("RequestId", out var rid) ? rid.get_string() : null;
            return SmsResult.ok(requestId);
        }

        return SmsResult.fail("无效响应");
    }

    private string sign_v3(string payload, long timestamp)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var canonicalRequest = $"POST\n/\n\ncontent-type:application/json; charset=utf-8\nhost:{_endpoint}\n\ncontent-type;host\n{sha256_hex(payload)}";
        var credentialScope = $"{date}/{_service}/tc3_request";
        var stringToSign = $"TC3-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes($"TC3{_secret_key}"), date);
        var secretService = hmac_sha256(secretDate, _service);
        var secretSigning = hmac_sha256(secretService, "tc3_request");
        var signature = BitConverter.ToString(hmac_sha256(secretSigning, stringToSign)).Replace("-", "").ToLowerInvariant();

        return $"TC3-HMAC-SHA256 Credential={_secret_id}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";
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