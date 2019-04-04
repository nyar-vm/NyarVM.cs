using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Push;

/// <summary>
/// Google FCM 推送服务实现，使用 HTTP v1 API
/// </summary>
public sealed class FcmPushProvider : IPushNotificationService
{
    private readonly HttpClient _http;
    private readonly string _server_key;
    private readonly string _base_url;

    /// <summary>
    /// 初始化 Google FCM 推送服务
    /// </summary>
    /// <param name="serverKey">FCM 服务器密钥</param>
    /// <param name="projectId">GCP 项目标识</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public FcmPushProvider(string serverKey, string? projectId = null, HttpClient? httpClient = null)
    {
        _server_key = serverKey;
        _base_url = projectId is not null
            ? $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send"
            : "https://fcm.googleapis.com/fcm/send";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<PushResult> push(PushRequest request, CancellationToken ct = default)
    {
        try
        {
            object body;

            if (_base_url.Contains("/v1/"))
            {
                body = build_v1_payload(request);
            }
            else
            {
                body = build_legacy_payload(request);
            }

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
            http_request.Headers.Add("Authorization", $"key={_server_key}");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(response_body);
                var root = doc.RootElement;
                var message_id = root.TryGetProperty("name", out var name_el)
                    ? name_el.get_string()
                    : null;

                return PushResult.ok(message_id);
            }

            return PushResult.fail($"FCM 推送失败: {response.StatusCode} - {response_body}");
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

    private static object build_v1_payload(PushRequest request)
    {
        var message = new Dictionary<string, object>
        {
            ["token"] = request.target,
            ["notification"] = new Dictionary<string, string>
            {
                ["title"] = request.title,
                ["body"] = request.body
            }
        };

        if (request.data is not null)
        {
            message["data"] = request.data;
        }

        return new { message };
    }

    private static object build_legacy_payload(PushRequest request)
    {
        var payload = new Dictionary<string, object>
        {
            ["to"] = request.target,
            ["notification"] = new Dictionary<string, string>
            {
                ["title"] = request.title,
                ["body"] = request.body
            }
        };

        if (request.data is not null)
        {
            payload["data"] = request.data;
        }

        return payload;
    }
}
