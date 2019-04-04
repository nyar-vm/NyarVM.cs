using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Huawei;

/// <summary>
/// 华为云短信服务实现，使用华为云 SMS API HMAC-SHA256 签名
/// </summary>
public sealed class HuaweiSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _secret_access_key;
    private readonly string _region;
    private const string _service = "sms";

    /// <summary>
    /// 初始化华为云短信服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="secretAccessKey">AccessKey Secret</param>
    /// <param name="region">区域，如 cn-north-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public HuaweiSmsProvider(
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
        var endpoint = $"sms.{_region}.myhuaweicloud.com";
        var url = $"https://{endpoint}/sms/batchSendSms/v1";

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["from"] = signName ?? string.Empty,
            ["to"] = $"+86{phoneNumber}",
            ["templateId"] = templateCode,
            ["templateParas"] = templateParams is not null
                ? System.Text.Json.JsonSerializer.Serialize(templateParams)
                : string.Empty
        };

        var body = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        sign_request(request, endpoint, body);

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("code", out var code))
        {
            var codeStr = code.get_string() ?? string.Empty;

            if (codeStr == "000000")
            {
                var requestId = doc.RootElement.TryGetProperty("smsMsgId", out var rid)
                    ? rid.get_string()
                    : null;
                return SmsResult.ok(requestId);
            }

            var message = doc.RootElement.TryGetProperty("description", out var desc)
                ? desc.get_string() ?? codeStr
                : codeStr;
            return SmsResult.fail(message);
        }

        return SmsResult.fail("无效响应");
    }

    private void sign_request(HttpRequestMessage request, string host, string body)
    {
        var t = DateTimeOffset.UtcNow;
        var obsDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/request";

        request.Headers.Add("Host", host);
        request.Headers.Add("X-Obs-Date", obsDate);

        var payloadHash = sha256_hex(Encoding.UTF8.GetBytes(body));
        request.Headers.Add("X-Obs-Content-Sha256", payloadHash);

        var signedHeaders = "content-type;host;x-obs-content-sha256;x-obs-date";
        var canonicalRequest = $"POST\n/sms/batchSendSms/v1\n\n"
                               + $"content-type:application/x-www-form-urlencoded\n"
                               + $"host:{host}\n"
                               + $"x-obs-content-sha256:{payloadHash}\n"
                               + $"x-obs-date:{obsDate}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"OBS4-HMAC-SHA256\n{obsDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = derive_signing_key(datestamp);
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"OBS4-HMAC-SHA256 Credential={_access_key_id}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private byte[] derive_signing_key(string datestamp)
    {
        return hmac_sha256(
            hmac_sha256(
                hmac_sha256(
                    hmac_sha256(
                        Encoding.UTF8.GetBytes($"OBS4{_secret_access_key}"),
                        datestamp),
                    _region),
                _service),
            "request");
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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

    private static string hmac_sha256_hex(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
