using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Aws;

/// <summary>
/// AWS SES 邮件发送服务实现，调用 AWS SES SendEmail API（Signature V4 签名）
/// </summary>
public sealed class AwsSesEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private readonly string _from_address;

    /// <summary>
    /// 初始化 AWS SES 邮件发送服务
    /// </summary>
    /// <param name="accessKey">AWS Access Key</param>
    /// <param name="secretKey">AWS Secret Key</param>
    /// <param name="region">AWS 区域，默认 us-east-1</param>
    /// <param name="fromAddress">发件人地址，默认 noreply@atlas.local</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AwsSesEmailSender(string accessKey, string secretKey, string region = "us-east-1", string? fromAddress = null, HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _region = region;
        _from_address = fromAddress ?? "noreply@atlas.local";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = $"email.{_region}.amazonaws.com";
        var endpoint = $"https://{host}/";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var date = DateTime.UtcNow.ToString("yyyyMMdd");

        var parameters = new SortedDictionary<string, string>
        {
            ["Action"] = "SendEmail",
            ["Source"] = _from_address,
            ["Destination.ToAddresses.member.1"] = to,
            ["Message.Subject.Data"] = subject,
            ["Message.Body.Text.Data"] = body,
            ["Version"] = "2010-12-01",
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = $"{_access_key}/{date}/{_region}/ses/aws4_request",
            ["X-Amz-Date"] = timestamp,
            ["X-Amz-SignedHeaders"] = "host"
        };

        var canonical_querystring = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var canonical_request = $"GET\n/\n{canonical_querystring}\nhost:{host}\n\nhost\nUNSIGNED-PAYLOAD";
        var string_to_sign = $"AWS4-HMAC-SHA256\n{timestamp}\n{date}/{_region}/ses/aws4_request\n{sha256_hex(canonical_request)}";

        var signing_key = get_signing_key(date);
        var signature = hmac_sha256_hex(signing_key, string_to_sign);

        var url = $"{endpoint}?{canonical_querystring}&X-Amz-Signature={signature}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        await _http.SendAsync(request, cancellationToken);
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private byte[] get_signing_key(string date)
    {
        var k_date = hmac_sha256(Encoding.UTF8.GetBytes($"AWS4{_secret_key}"), date);
        var k_region = hmac_sha256(k_date, _region);
        var k_service = hmac_sha256(k_region, "ses");
        return hmac_sha256(k_service, "aws4_request");
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
