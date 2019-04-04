using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Push;

/// <summary>
/// 小米推送服务实现，使用小米 Push API
/// </summary>
public sealed class XiaomiPushProvider : IPushNotificationService
{
    private readonly HttpClient _http;
    private readonly string _app_secret;
    private readonly string? _package_name;
    private readonly string _base_url;

    /// <summary>
    /// 初始化小米推送服务
    /// </summary>
    /// <param name="appSecret">小米应用密钥</param>
    /// <param name="packageName">应用包名</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public XiaomiPushProvider(string appSecret, string? packageName = null, HttpClient? httpClient = null)
    {
        _app_secret = appSecret;
        _package_name = packageName;
        _base_url = "https://api.xmpush.xiaomi.com/v3/message/regid";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<PushResult> push(PushRequest request, CancellationToken ct = default)
    {
        try
        {
            var payload = new Dictionary<string, string>
            {
                ["registration_id"] = request.target,
                ["title"] = request.title,
                ["description"] = request.body,
                ["payload"] = JsonSerializer.Serialize(new { title = request.title, body = request.body }),
                ["notify_type"] = "1",
                ["notify_id"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            };

            if (_package_name is not null)
            {
                payload["package_name"] = _package_name;
            }

            if (request.data is not null)
            {
                payload["extra.payload"] = JsonSerializer.Serialize(request.data);
            }

            using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
            http_request.Headers.Add("Authorization", $"key={_app_secret}");
            http_request.Content = new FormUrlEncodedContent(payload);

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var code = root.TryGetProperty("code", out var code_el) ? code_el.GetInt32() : -1;

            if (code == 0)
            {
                var message_id = root.TryGetProperty("data", out var data_el)
                                 && data_el.TryGetProperty("id", out var id_el)
                    ? id_el.get_string()
                    : null;

                return PushResult.ok(message_id);
            }

            var reason = root.TryGetProperty("reason", out var reason_el) ? reason_el.get_string() ?? "未知错误" : "未知错误";
            return PushResult.fail($"小米推送失败: {reason}");
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
}
