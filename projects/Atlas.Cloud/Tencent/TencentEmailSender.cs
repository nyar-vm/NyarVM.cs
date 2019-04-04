using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云 SES 邮件发送服务实现，使用 TC3-HMAC-SHA256 签名
/// </summary>
public sealed class TencentEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private readonly string _region;
    private readonly string _from_address;

    /// <summary>
    /// 初始化腾讯云 SES 邮件发送服务
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，默认 ap-guangzhou</param>
    /// <param name="fromAddress">发件人地址</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentEmailSender(string secretId, string secretKey, string region = "ap-guangzhou", string? fromAddress = null, HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _region = region;
        _from_address = fromAddress ?? "noreply@atlas.local";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = $"ses.{_region}.tencentcloudapi.com";
        var service = "ses";
        var action = "SendEmail";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var payload = new
        {
            FromEmailAddress = _from_address,
            Destination = new[] { to },
            Subject = subject,
            Simple = new { Html = body }
        };

        var json_body = JsonSerializer.Serialize(payload);
        var payload_hash = sha256_hex(json_body);

        var canonical_request = $"POST\n/\n\ncontent-type:application/json; charset=utf-8\nhost:{host}\nx-tc-action:{action.ToLowerInvariant()}\n\ncontent-type;host;x-tc-action\n{payload_hash}";
        var credential_scope = $"{date}/{service}/tc3_request";
        var string_to_sign = $"TC3-HMAC-SHA256\n{timestamp}\n{credential_scope}\n{sha256_hex(canonical_request)}";

        var secret_date = hmac_sha256(Encoding.UTF8.GetBytes($"TC3{_secret_key}"), date);
        var secret_service = hmac_sha256(secret_date, service);
        var secret_signing = hmac_sha256(secret_service, "tc3_request");
        var signature = hmac_sha256_hex(secret_signing, string_to_sign);

        var authorization = $"TC3-HMAC-SHA256 Credential={_secret_id}/{credential_scope}, SignedHeaders=content-type;host;x-tc-action, Signature={signature}";

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}");
        request.Headers.Add("Authorization", authorization);
        request.Headers.Add("X-TC-Action", action);
        request.Headers.Add("X-TC-Timestamp", timestamp);
        request.Headers.Add("X-TC-Region", _region);
        request.Headers.Add("Host", host);
        request.Content = new StringContent(json_body, Encoding.UTF8, "application/json");

        await _http.SendAsync(request, cancellationToken);
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string hmac_sha256_hex(byte[] key, string data)
    {
        var hash = hmac_sha256(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
