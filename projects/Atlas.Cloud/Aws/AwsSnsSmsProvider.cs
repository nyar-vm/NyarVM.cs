using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Aws;

/// <summary>
/// AWS SNS 短信服务实现，使用 AWS Signature V4 签名
/// </summary>
public sealed class AwsSnsSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private const string _service = "sns";

    /// <summary>
    /// 初始化 AWS SNS 短信服务
    /// </summary>
    /// <param name="accessKey">AWS Access Key</param>
    /// <param name="secretKey">AWS Secret Key</param>
    /// <param name="region">区域，如 us-east-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AwsSnsSmsProvider(
        string accessKey, string secretKey,
        string region = "us-east-1", HttpClient? httpClient = null)
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
        var host = $"sns.{_region}.amazonaws.com";
        var url = $"https://{host}/";

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Action"] = "Publish",
            ["PhoneNumber"] = phoneNumber,
            ["Message"] = templateCode,
            ["Version"] = "2010-03-31"
        };

        if (signName is not null)
        {
            parameters["Subject"] = signName;
        }

        if (templateParams is not null)
        {
            parameters["MessageStructure"] = "json";
        }

        var canonicalQuery = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(canonicalQuery, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        sign_request_v4(request, host, canonicalQuery);

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("PublishResponse", out var publishResp))
        {
            if (publishResp.TryGetProperty("PublishResult", out var result))
            {
                var messageId = result.TryGetProperty("MessageId", out var mid) ? mid.get_string() : null;
                return SmsResult.ok(messageId);
            }
        }

        if (doc.RootElement.TryGetProperty("Error", out var error))
        {
            var msg = error.TryGetProperty("Message", out var m) ? m.get_string() ?? "未知错误" : "未知错误";
            return SmsResult.fail(msg);
        }

        return SmsResult.fail($"SNS 发送失败: {response.StatusCode}");
    }

    private void sign_request_v4(HttpRequestMessage request, string host, string body)
    {
        var t = DateTimeOffset.UtcNow;
        var amzDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/aws4_request";

        request.Headers.Add("Host", host);
        request.Headers.Add("X-Amz-Date", amzDate);

        var payloadHash = sha256_hex(Encoding.UTF8.GetBytes(body));
        request.Headers.Add("X-Amz-Content-Sha256", payloadHash);

        var signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = $"POST\n/\n\n"
                               + $"content-type:application/x-www-form-urlencoded\n"
                               + $"host:{host}\n"
                               + $"x-amz-content-sha256:{payloadHash}\n"
                               + $"x-amz-date:{amzDate}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = hmac_sha256(hmac_sha256(hmac_sha256(hmac_sha256(
            Encoding.UTF8.GetBytes($"AWS4{_secret_key}"), datestamp),
            _region), _service), "aws4_request");
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_access_key}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
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
