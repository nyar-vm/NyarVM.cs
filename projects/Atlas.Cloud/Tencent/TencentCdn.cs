using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云 CDN 服务实现，使用 TC3-HMAC-SHA256 签名认证
/// </summary>
public sealed class TencentCdn : ICdnService
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private const string Endpoint = "https://cdn.tencentcloudapi.com";
    private const string Service = "cdn";
    private const string Version = "2018-06-06";

    /// <summary>
    /// 初始化腾讯云 CDN 服务
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentCdn(string secretId, string secretKey, HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<CdnResult> purge(string[] urls, CancellationToken ct = default)
    {
        return await execute_action("PurgeUrlsCache", new { Urls = urls }, ct);
    }

    /// <inheritdoc />
    public async Task<CdnResult> prefetch(string[] urls, CancellationToken ct = default)
    {
        return await execute_action("PushUrlsCache", new { Urls = urls }, ct);
    }

    private async Task<CdnResult> execute_action(string action, object payload, CancellationToken ct)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

            var body = JsonSerializer.Serialize(payload);

            var credential_scope = $"{date}/{Service}/tc3_request";
            var signed_headers = "content-type;host";
            var canonical_request = $"POST\n/\n\n"
                                    + $"content-type:application/json; charset=utf-8\n"
                                    + $"host:cdn.tencentcloudapi.com\n\n"
                                    + $"{signed_headers}\n"
                                    + sha256_hex(body);

            var string_to_sign = $"TC3-HMAC-SHA256\n{timestamp}\n{credential_scope}\n{sha256_hex(canonical_request)}";

            var secret_date = hmac_sha256(Encoding.UTF8.GetBytes($"TC3{_secret_key}"), date);
            var secret_service = hmac_sha256(secret_date, Service);
            var secret_signing = hmac_sha256(secret_service, "tc3_request");
            var signature = hmac_sha256_hex(secret_signing, string_to_sign);

            var authorization = $"TC3-HMAC-SHA256 Credential={_secret_id}/{credential_scope}, SignedHeaders={signed_headers}, Signature={signature}";

            using var http_request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            http_request.Headers.Add("Authorization", authorization);
            http_request.Headers.Add("X-TC-Action", action);
            http_request.Headers.Add("X-TC-Version", Version);
            http_request.Headers.Add("X-TC-Timestamp", timestamp);
            http_request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            if (root.TryGetProperty("Response", out var resp))
            {
                if (resp.TryGetProperty("Error", out var error))
                {
                    var message = error.TryGetProperty("Message", out var msg_el) ? msg_el.get_string() ?? "未知错误" : "未知错误";
                    return CdnResult.fail($"腾讯云 CDN 错误: {message}");
                }

                var task_id = resp.TryGetProperty("TaskId", out var task_id_el) ? task_id_el.get_string() : null;
                var request_id = resp.TryGetProperty("RequestId", out var req_id_el) ? req_id_el.get_string() : null;
                return CdnResult.ok(task_id ?? request_id);
            }

            return CdnResult.ok();
        }
        catch (HttpRequestException ex)
        {
            return CdnResult.fail($"HTTP 请求失败: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return CdnResult.fail($"JSON 解析失败: {ex.Message}");
        }
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string hmac_sha256_hex(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
