using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云短信服务实现，使用阿里云 SMS API 签名
/// </summary>
public sealed class AlibabaSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;

    private const string _endpoint = "dysmsapi.aliyuncs.com";

    /// <summary>
    /// 初始化阿里云短信服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="accessKeySecret">AccessKey Secret</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaSmsProvider(string accessKeyId, string accessKeySecret, HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<SmsResult> send(
        string phoneNumber, string templateCode,
        IReadOnlyDictionary<string, string>? templateParams = null,
        string? signName = null,
        CancellationToken cancel = default)
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessKeyId"] = _access_key_id,
            ["Action"] = "SendSms",
            ["Format"] = "JSON",
            ["PhoneNumbers"] = phoneNumber,
            ["RegionId"] = "cn-hangzhou",
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureVersion"] = "1.0",
            ["SignatureNonce"] = Guid.NewGuid().ToString("N"),
            ["TemplateCode"] = templateCode,
            ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["Version"] = "2017-05-25"
        };

        if (signName is not null)
        {
            parameters["SignName"] = signName;
        }

        if (templateParams is not null)
        {
            parameters["TemplateParam"] = System.Text.Json.JsonSerializer.Serialize(templateParams);
        }

        var canonicalQuery = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var stringToSign = $"GET&{Uri.EscapeDataString("/")}&{Uri.EscapeDataString(canonicalQuery)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret + "&"));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var url = $"https://{_endpoint}/?{canonicalQuery}&Signature={Uri.EscapeDataString(signature)}";

        var response = await _http.GetAsync(url, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

        var code = doc.RootElement.TryGetProperty("Code", out var codeElement)
            ? codeElement.get_string() ?? string.Empty
            : string.Empty;

        if (code == "OK")
        {
            var requestId = doc.RootElement.TryGetProperty("RequestId", out var rid)
                ? rid.get_string()
                : null;
            return SmsResult.ok(requestId);
        }

        var message = doc.RootElement.TryGetProperty("Message", out var msg)
            ? msg.get_string() ?? code
            : code;

        return SmsResult.fail(message);
    }
}