using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Volcengine;

/// <summary>
/// 火山引擎推送服务实现，使用 HMAC-SHA256 签名认证
/// </summary>
public sealed class VolcenginePushProvider : IPushNotificationService
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _app_id;
    private readonly string _base_url;

    /// <summary>
    /// 初始化火山引擎推送服务
    /// </summary>
    /// <param name="accessKey">访问密钥</param>
    /// <param name="secretKey">密钥</param>
    /// <param name="appId">应用标识</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public VolcenginePushProvider(string accessKey, string secretKey, string appId, HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _app_id = appId;
        _base_url = "https://mcs.volcengineapi.com/";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<PushResult> push(PushRequest request, CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                AppId = _app_id,
                PushType = "device_token",
                Target = request.target,
                Title = request.title,
                Content = request.body,
                Alert = new { Title = request.title, Body = request.body }
            };

            var json = JsonSerializer.Serialize(body);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var nonce = Guid.NewGuid().ToString();

            var url = $"{_base_url}?Action=Push&Version=2022-10-01";

            using var http_request = new HttpRequestMessage(HttpMethod.Post, url);
            http_request.Headers.Add("X-Date", timestamp);
            http_request.Headers.Add("X-Access-Key", _access_key);
            http_request.Headers.Add("X-Nonce", nonce);
            http_request.Headers.Add("X-Signature", compute_signature(timestamp, nonce));
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            if (root.TryGetProperty("ResponseMetadata", out var meta)
                && meta.TryGetProperty("Error", out var error))
            {
                var message = error.TryGetProperty("Message", out var msg_el) ? msg_el.get_string() ?? "未知错误" : "未知错误";
                return PushResult.fail($"火山引擎推送失败: {message}");
            }

            var message_id = root.TryGetProperty("Result", out var result)
                             && result.TryGetProperty("TaskId", out var task_id_el)
                ? task_id_el.get_string()
                : null;

            return PushResult.ok(message_id);
        }
        catch (HttpRequestException ex)
        {
            return PushResult.fail($"HTTP 请求失败: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return PushResult.fail($"JSON 解析失败: {ex.Message}");
        }
    }

    private string compute_signature(string timestamp, string nonce)
    {
        var string_to_sign = $"{_access_key}\n{timestamp}\n{nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret_key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(string_to_sign));
        return Convert.ToBase64String(hash);
    }
}
