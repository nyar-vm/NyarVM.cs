using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Tencent;

/// <summary>
/// 腾讯云密钥管理 SSM (Secrets Manager) 实现，使用 TC3-HMAC-SHA256 签名
/// </summary>
public sealed class TencentKeyVaultService : IKeyVaultService
{
    private readonly HttpClient _http;
    private readonly string _secret_id;
    private readonly string _secret_key;
    private readonly string _region;
    private const string _service = "ssm";

    /// <summary>
    /// 初始化腾讯云密钥管理服务
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey</param>
    /// <param name="region">区域，如 ap-guangzhou</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TencentKeyVaultService(
        string secretId, string secretKey,
        string region = "ap-guangzhou", HttpClient? httpClient = null)
    {
        _secret_id = secretId;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<string?> get_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"ssm.{_region}.tencentcloudapi.com";
        var payload = JsonSerializer.Serialize(new { SecretName = keyName });

        var request = build_tc3_request(host, "GetSecretValue", payload);
        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("Response", out var resp))
        {
            if (resp.TryGetProperty("Error", out var error))
            {
                if (error.TryGetProperty("Code", out var code) && code.get_string() == "ResourceNotFound")
                {
                    return null;
                }

                var msg = error.TryGetProperty("Message", out var m) ? m.get_string() ?? "未知错误" : "未知错误";
                throw new InvalidOperationException($"腾讯云 SSM 获取密钥失败: {msg}");
            }

            return resp.TryGetProperty("SecretValue", out var val) ? val.get_string() : null;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task set_secret(string keyName, string value, CancellationToken cancel = default)
    {
        var host = $"ssm.{_region}.tencentcloudapi.com";
        var existing = await try_get_secret(keyName, cancel);

        var action = existing is not null ? "UpdateSecret" : "CreateSecret";
        var payload = action == "CreateSecret"
            ? JsonSerializer.Serialize(new { SecretName = keyName, SecretString = value })
            : JsonSerializer.Serialize(new { SecretName = keyName, SecretString = value });

        var request = build_tc3_request(host, action, payload);
        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"ssm.{_region}.tencentcloudapi.com";
        var payload = JsonSerializer.Serialize(new { SecretName = keyName });

        var request = build_tc3_request(host, "DeleteSecret", payload);
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
    /// 构建 TC3-HMAC-SHA256 签名请求
    /// </summary>
    /// <param name="host">主机名</param>
    /// <param name="action">API 操作名</param>
    /// <param name="payload">请求体 JSON</param>
    /// <returns>已签名的 HTTP 请求</returns>
    private HttpRequestMessage build_tc3_request(string host, string action, string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json; charset=utf-8")
        };

        request.Headers.Add("Host", host);
        request.Headers.Add("X-TC-Action", action);
        request.Headers.Add("X-TC-Version", "2019-09-23");
        request.Headers.Add("X-TC-Timestamp", timestamp.ToString());
        request.Headers.Add("X-TC-Region", _region);

        var auth = sign_v3(payload, timestamp, host);
        request.Headers.Add("Authorization", auth);

        return request;
    }

    /// <summary>
    /// TC3-HMAC-SHA256 签名
    /// </summary>
    /// <param name="payload">请求体</param>
    /// <param name="timestamp">时间戳</param>
    /// <param name="host">主机名</param>
    /// <returns>签名字符串</returns>
    private string sign_v3(string payload, long timestamp, string host)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToString("yyyy-MM-dd");
        var canonicalRequest = $"POST\n/\n\ncontent-type:application/json; charset=utf-8\nhost:{host}\n\ncontent-type;host\n{sha256_hex(payload)}";
        var credentialScope = $"{date}/{_service}/tc3_request";
        var stringToSign = $"TC3-HMAC-SHA256\n{timestamp}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var secretDate = hmac_sha256(Encoding.UTF8.GetBytes($"TC3{_secret_key}"), date);
        var secretService = hmac_sha256(secretDate, _service);
        var secretSigning = hmac_sha256(secretService, "tc3_request");
        var signature = BitConverter.ToString(hmac_sha256(secretSigning, stringToSign)).Replace("-", "").ToLowerInvariant();

        return $"TC3-HMAC-SHA256 Credential={_secret_id}/{credentialScope}, SignedHeaders=content-type;host, Signature={signature}";
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
