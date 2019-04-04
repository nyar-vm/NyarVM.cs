using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Volcengine;

/// <summary>
/// 火山引擎密钥管理 KMS 实现，使用 HMAC-SHA256 签名
/// </summary>
public sealed class VolcengineKeyVaultService : IKeyVaultService
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private const string _service = "kms";

    /// <summary>
    /// 初始化火山引擎密钥管理服务
    /// </summary>
    /// <param name="accessKey">AccessKey</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，如 cn-beijing</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public VolcengineKeyVaultService(
        string accessKey, string secretKey,
        string region = "cn-beijing", HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<string?> get_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"kms.{_region}.volcengineapi.com";
        var payload = JsonSerializer.Serialize(new { KeyringName = keyName });

        var request = build_signed_request(host, "GetSecretValue", payload);
        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("ResponseMetadata", out var metadata))
        {
            if (metadata.TryGetProperty("Error", out var error))
            {
                var msg = error.TryGetProperty("Message", out var m) ? m.get_string() ?? "未知错误" : "未知错误";
                throw new InvalidOperationException($"火山引擎 KMS 获取密钥失败: {msg}");
            }
        }

        return doc.RootElement.TryGetProperty("SecretValue", out var val) ? val.get_string() : null;
    }

    /// <inheritdoc />
    public async Task set_secret(string keyName, string value, CancellationToken cancel = default)
    {
        var host = $"kms.{_region}.volcengineapi.com";
        var existing = await try_get_secret(keyName, cancel);

        var action = existing is not null ? "UpdateSecret" : "CreateSecret";
        var payload = action == "CreateSecret"
            ? JsonSerializer.Serialize(new { KeyringName = keyName, SecretValue = value })
            : JsonSerializer.Serialize(new { KeyringName = keyName, SecretValue = value });

        var request = build_signed_request(host, action, payload);
        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"kms.{_region}.volcengineapi.com";
        var payload = JsonSerializer.Serialize(new { KeyringName = keyName });

        var request = build_signed_request(host, "DeleteSecret", payload);
        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 尝试获取密钥值，用于判断密钥是否存在
    /// </summary>
    /// <param name="keyName">密钥名称</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>密钥值，不存在时返回 null</returns>
    private async Task<string?> try_get_secret(string keyName, CancellationToken cancel)
    {
        try
        {
            return await get_secret(keyName, cancel);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// 构建带签名的请求
    /// </summary>
    /// <param name="host">主机名</param>
    /// <param name="action">API 操作名</param>
    /// <param name="payload">请求体 JSON</param>
    /// <returns>已签名的 HTTP 请求</returns>
    private HttpRequestMessage build_signed_request(string host, string action, string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/?Action={action}&Version=2021-01-01")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json; charset=utf-8")
        };

        var auth = sign_request(payload, timestamp, host, action);
        request.Headers.Add("Authorization", auth);

        return request;
    }

    /// <summary>
    /// HMAC-SHA256 签名
    /// </summary>
    /// <param name="payload">请求体</param>
    /// <param name="timestamp">时间戳</param>
    /// <param name="host">主机名</param>
    /// <param name="action">操作名</param>
    /// <returns>签名字符串</returns>
    private string sign_request(string payload, long timestamp, string host, string action)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var credentialScope = $"{date}/{_region}/{_service}/request";

        var canonicalRequest = $"POST\n/\nAction={action}&Version=2021-01-01\n"
                               + $"content-type:application/json; charset=utf-8\n"
                               + $"host:{host}\n\n"
                               + $"content-type;host\n"
                               + sha256_hex(payload);

        var stringToSign = $"HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes(_secret_key), date);
        var secretRegion = hmac_sha256(secretDate, _region);
        var secretService = hmac_sha256(secretRegion, _service);
        var secretSigning = hmac_sha256(secretService, "request");
        var signature = BitConverter.ToString(hmac_sha256(secretSigning, stringToSign)).Replace("-", "").ToLowerInvariant();

        return $"HMAC-SHA256 Credential={_access_key}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";
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
}
