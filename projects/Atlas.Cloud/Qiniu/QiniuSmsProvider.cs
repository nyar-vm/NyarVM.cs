using System.Security.Cryptography;
using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Qiniu;

/// <summary>
/// 七牛云短信服务实现，使用七牛云 SMS API HMAC-SHA1 签名
/// </summary>
public sealed class QiniuSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private const string _endpoint = "sms.qiniuapi.com";

    /// <summary>
    /// 初始化七牛云短信服务
    /// </summary>
    /// <param name="accessKey">AccessKey</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public QiniuSmsProvider(string accessKey, string secretKey, HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
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
            ["template_id"] = templateCode
        };

        if (signName is not null)
        {
            payload["sign_id"] = signName;
        }

        if (templateParams is not null)
        {
            payload["parameters"] = templateParams;
        }

        var requestBody = System.Text.Json.JsonSerializer.Serialize(payload);
        var path = "/v1/message";
        var url = $"https://{_endpoint}{path}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        sign_request(request, path, requestBody);

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("error", out var error))
        {
            var msg = error.get_string() ?? "未知错误";
            return SmsResult.fail(msg);
        }

        var jobId = doc.RootElement.TryGetProperty("job_id", out var jid)
            ? jid.get_string()
            : null;

        return SmsResult.ok(jobId);
    }

    private void sign_request(HttpRequestMessage request, string path, string body)
    {
        var stringToSign = $"{path}\n{body}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_secret_key));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var token = $"Qiniu {_access_key}:{signature}";

        request.Headers.Add("Authorization", token);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
