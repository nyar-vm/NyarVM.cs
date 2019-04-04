using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Huawei;

/// <summary>
/// 华为推送服务实现，使用华为 Push Kit API
/// </summary>
public sealed class HuaweiPushProvider : IPushNotificationService
{
    private readonly HttpClient _http;
    private readonly string _app_id;
    private readonly string _app_secret;
    private string? _access_token;
    private DateTimeOffset _token_expiry;

    private const string TokenUrl = "https://oauth-login.cloud.huawei.com/oauth2/v3/token";

    /// <summary>
    /// 初始化华为推送服务
    /// </summary>
    /// <param name="appId">华为应用标识</param>
    /// <param name="appSecret">华为应用密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public HuaweiPushProvider(string appId, string appSecret, HttpClient? httpClient = null)
    {
        _app_id = appId;
        _app_secret = appSecret;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<PushResult> push(PushRequest request, CancellationToken ct = default)
    {
        try
        {
            var token = await get_access_token(ct);
            var url = $"https://push-api.cloud.huawei.com/v2/{_app_id}/messages:send";

            var message = new Dictionary<string, object>
            {
                ["token"] = new[] { request.target },
                ["android"] = new Dictionary<string, object>
                {
                    ["notification"] = new Dictionary<string, object>
                    {
                        ["title"] = request.title,
                        ["body"] = request.body,
                        ["click_action"] = new { type = 3 }
                    }
                }
            };

            if (request.data is not null)
            {
                message["data"] = JsonSerializer.Serialize(request.data);
            }

            var body = new { validate_only = false, message };
            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, url);
            http_request.Headers.Add("Authorization", $"Bearer {token}");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var code = root.TryGetProperty("code", out var code_el) ? code_el.get_string() : string.Empty;

            if (code == "80000000")
            {
                var msg_id = root.TryGetProperty("msgId", out var msg_id_el) ? msg_id_el.get_string() : null;
                return PushResult.ok(msg_id);
            }

            var msg = root.TryGetProperty("msg", out var msg_el) ? msg_el.get_string() ?? "未知错误" : "未知错误";
            return PushResult.fail($"华为推送失败: {msg}");
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

    private async Task<string> get_access_token(CancellationToken ct)
    {
        if (_access_token is not null && _token_expiry > DateTimeOffset.UtcNow)
        {
            return _access_token;
        }

        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _app_id,
            ["client_secret"] = _app_secret
        };

        using var http_request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
        http_request.Content = new FormUrlEncodedContent(body);

        var response = await _http.SendAsync(http_request, ct);
        var response_body = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(response_body);
        var root = doc.RootElement;

        _access_token = root.GetProperty("access_token").get_string()!;
        var expires_in = root.TryGetProperty("expires_in", out var exp_el) ? exp_el.GetInt32() : 3600;
        _token_expiry = DateTimeOffset.UtcNow.AddSeconds(expires_in - 60);

        return _access_token;
    }
}
