using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云 DirectMail 邮件发送服务实现，使用 HMAC-SHA1 签名
/// </summary>
public sealed class AlibabaEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;
    private readonly string _region;
    private readonly string _from_address;

    /// <summary>
    /// 初始化阿里云 DirectMail 邮件发送服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="accessKeySecret">AccessKey Secret</param>
    /// <param name="region">区域，默认 cn-hangzhou</param>
    /// <param name="fromAddress">发件人地址</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaEmailSender(string accessKeyId, string accessKeySecret, string region = "cn-hangzhou", string? fromAddress = null, HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _region = region;
        _from_address = fromAddress ?? "noreply@atlas.local";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = $"dm.{_region}.aliyuncs.com";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var parameters = new SortedDictionary<string, string>
        {
            ["Action"] = "SingleSendMail",
            ["AccountName"] = _from_address,
            ["AddressType"] = "1",
            ["ReplyToAddress"] = "false",
            ["ToAddress"] = to,
            ["Subject"] = subject,
            ["TextBody"] = body,
            ["Format"] = "JSON",
            ["Version"] = "2015-11-23",
            ["AccessKeyId"] = _access_key_id,
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureVersion"] = "1.0",
            ["SignatureNonce"] = Guid.NewGuid().ToString(),
            ["Timestamp"] = timestamp
        };

        var canonicalized_query = string.Join("&", parameters.Select(p => $"{percent_encode(p.Key)}={percent_encode(p.Key)}={percent_encode(p.Value)}"));

        var string_to_sign = $"GET&{percent_encode("/")}&{percent_encode(canonicalized_query)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes($"{_access_key_secret}&"));
        var signature_bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(string_to_sign));
        var signature = Convert.ToBase64String(signature_bytes);

        var url = $"https://{host}/?{canonicalized_query}&Signature={percent_encode(signature)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        await _http.SendAsync(request, cancellationToken);
    }

    private static string percent_encode(string value)
    {
        return Uri.EscapeDataString(value)
            .Replace("+", "%20")
            .Replace("*", "%2A")
            .Replace("%7E", "~");
    }
}
