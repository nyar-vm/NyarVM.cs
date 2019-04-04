using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Push;

/// <summary>
/// Apple APNs 推送服务实现，使用 HTTP/2 API 和 ES256 JWT 认证
/// </summary>
public sealed class ApnsPushProvider : IPushNotificationService
{
    private readonly HttpClient _http;
    private readonly string _key_id;
    private readonly string _team_id;
    private readonly string _bundle_id;
    private readonly ECDsa _ecdsa;
    private readonly string _base_url;
    private string? _cached_jwt;
    private DateTimeOffset _jwt_expiry;

    /// <summary>
    /// 初始化 Apple APNs 推送服务
    /// </summary>
    /// <param name="keyId">APNs 密钥标识</param>
    /// <param name="teamId">开发者团队标识</param>
    /// <param name="bundleId">应用 Bundle 标识</param>
    /// <param name="privateKey">ES256 私钥（PEM 格式）</param>
    /// <param name="isProduction">是否为生产环境</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public ApnsPushProvider(
        string keyId, string teamId, string bundleId, string privateKey,
        bool isProduction = true, HttpClient? httpClient = null)
    {
        _key_id = keyId;
        _team_id = teamId;
        _bundle_id = bundleId;
        _base_url = isProduction ? "https://api.push.apple.com" : "https://api.sandbox.push.apple.com";
        _http = httpClient ?? new HttpClient();
        _ecdsa = ECDsa.Create();
        _ecdsa.ImportFromPem(privateKey);
    }

    /// <inheritdoc />
    public async Task<PushResult> push(PushRequest request, CancellationToken ct = default)
    {
        try
        {
            var jwt = get_or_create_jwt();
            var url = $"{_base_url}/3/device/{request.target}";

            var payload = new Dictionary<string, object>
            {
                ["aps"] = new Dictionary<string, object>
                {
                    ["alert"] = new Dictionary<string, string>
                    {
                        ["title"] = request.title,
                        ["body"] = request.body
                    },
                    ["mutable-content"] = 1
                }
            };

            if (request.data is not null)
            {
                foreach (var (key, value) in request.data)
                {
                    payload[key] = value;
                }
            }

            var json = JsonSerializer.Serialize(payload);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, url);
            http_request.Headers.Add("authorization", $"bearer {jwt}");
            http_request.Headers.Add("apns-topic", _bundle_id);
            http_request.Headers.Add("apns-push-type", "alert");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);

            if (response.IsSuccessStatusCode)
            {
                return PushResult.ok();
            }

            var error_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(error_body);
            var reason = doc.RootElement.TryGetProperty("reason", out var reason_el)
                ? reason_el.get_string() ?? response.StatusCode.ToString()
                : response.StatusCode.ToString();

            return PushResult.fail($"APNs 推送失败: {reason}");
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

    private string get_or_create_jwt()
    {
        if (_cached_jwt is not null && _jwt_expiry > DateTimeOffset.UtcNow)
        {
            return _cached_jwt;
        }

        var now = DateTimeOffset.UtcNow;
        _jwt_expiry = now.AddMinutes(50);

        var header = JsonSerializer.Serialize(new { alg = "ES256", kid = _key_id, typ = "JWT" });
        var payload = JsonSerializer.Serialize(new
        {
            iss = _team_id,
            iat = now.ToUnixTimeSeconds(),
            exp = _jwt_expiry.ToUnixTimeSeconds(),
            bid = _bundle_id
        });

        var headerBase64 = to_base64_url(header);
        var payloadBase64 = to_base64_url(payload);
        var signContent = $"{headerBase64}.{payloadBase64}";

        var signatureBytes = _ecdsa.SignData(Encoding.UTF8.GetBytes(signContent), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        var signature = to_base64_url_raw(signatureBytes);

        _cached_jwt = $"{signContent}.{signature}";
        return _cached_jwt;
    }

    private static string to_base64_url(string data)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string to_base64_url_raw(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
