using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Volcengine;

/// <summary>
/// 火山引擎 CDN 服务实现，使用 HMAC-SHA256 签名认证
/// </summary>
public sealed class VolcengineCdn : ICdnService
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private const string Endpoint = "https://cdn.volcengineapi.com";
    private const string Service = "CDN";

    /// <summary>
    /// 初始化火山引擎 CDN 服务
    /// </summary>
    /// <param name="accessKey">访问密钥</param>
    /// <param name="secretKey">密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public VolcengineCdn(string accessKey, string secretKey, HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<CdnResult> purge(string[] urls, CancellationToken ct = default)
    {
        var body = new
        {
            Type = "url",
            Urls = urls
        };

        return await execute_action("SubmitRefreshTask", body, ct);
    }

    /// <inheritdoc />
    public async Task<CdnResult> prefetch(string[] urls, CancellationToken ct = default)
    {
        var body = new
        {
            Type = "url",
            Urls = urls
        };

        return await execute_action("SubmitPreloadTask", body, ct);
    }

    private async Task<CdnResult> execute_action(string action, object payload, CancellationToken ct)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var date = DateTimeOffset.UtcNow.ToString("yyyyMMdd");

            var body_json = JsonSerializer.Serialize(payload);

            var credential_scope = $"{date}/{Service}/request";
            var signed_headers = "content-type;host;x-content-sha256;x-date";
            var content_sha256 = sha256_hex(body_json);

            var canonical_request = $"POST\n/\n\n"
                                    + $"content-type:application/json\n"
                                    + $"host:cdn.volcengineapi.com\n"
                                    + $"x-content-sha256:{content_sha256}\n"
                                    + $"x-date:{timestamp}\n\n"
                                    + $"{signed_headers}\n"
                                    + content_sha256;

            var string_to_sign = $"HMAC-SHA256\n{timestamp}\n{credential_scope}\n{sha256_hex(canonical_request)}";

            var secret_date = hmac_sha256(Encoding.UTF8.GetBytes(_secret_key), date);
            var secret_service = hmac_sha256(secret_date, Service);
            var secret_signing = hmac_sha256(secret_service, "request");
            var signature = hmac_sha256_hex(secret_signing, string_to_sign);

            var authorization = $"HMAC-SHA256 Credential={_access_key}/{credential_scope}, SignedHeaders={signed_headers}, Signature={signature}";

            using var http_request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            http_request.Headers.Add("Authorization", authorization);
            http_request.Headers.Add("X-Date", timestamp);
            http_request.Headers.Add("X-Content-Sha256", content_sha256);
            http_request.Headers.Add("X-Action", action);
            http_request.Headers.Add("X-Version", "2021-03-01");
            http_request.Content = new StringContent(body_json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            if (root.TryGetProperty("ResponseMetadata", out var meta)
                && meta.TryGetProperty("Error", out var error))
            {
                var message = error.TryGetProperty("Message", out var msg_el) ? msg_el.get_string() ?? "未知错误" : "未知错误";
                return CdnResult.fail($"火山引擎 CDN 错误: {message}");
            }

            var task_id = root.TryGetProperty("Result", out var result)
                          && result.TryGetProperty("TaskId", out var task_id_el)
                ? task_id_el.get_string()
                : null;

            return CdnResult.ok(task_id);
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
