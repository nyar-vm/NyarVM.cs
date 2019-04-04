using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Huawei;

/// <summary>
/// 华为云密钥管理 DEW (数据加密服务) 实现，使用 HMAC-SHA256 签名
/// </summary>
public sealed class HuaweiKeyVaultService : IKeyVaultService
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _secret_access_key;
    private readonly string _region;
    private const string _service = "kms";

    /// <summary>
    /// 初始化华为云密钥管理服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="secretAccessKey">AccessKey Secret</param>
    /// <param name="region">区域，如 cn-north-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public HuaweiKeyVaultService(
        string accessKeyId, string secretAccessKey,
        string region = "cn-north-1", HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _secret_access_key = secretAccessKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<string?> get_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"kms.{_region}.myhuaweicloud.com";
        var payload = JsonSerializer.Serialize(new { secret_name = keyName, version_id = "v1" });

        var request = build_signed_request(host, "/v1/{project_id}/secrets/{secret_name}/versions/v1", payload);
        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("secret_value", out var val))
        {
            return val.get_string();
        }

        if (doc.RootElement.TryGetProperty("version", out var version))
        {
            return version.TryGetProperty("secret_value", out var vv) ? vv.get_string() : null;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task set_secret(string keyName, string value, CancellationToken cancel = default)
    {
        var host = $"kms.{_region}.myhuaweicloud.com";
        var existing = await try_get_secret(keyName, cancel);

        var path = existing is not null
            ? "/v1/{project_id}/secrets/{secret_name}/versions"
            : "/v1/{project_id}/secrets";

        var payload = existing is not null
            ? JsonSerializer.Serialize(new { secret_name = keyName, secret_value = value, version_id = "v1" })
            : JsonSerializer.Serialize(new { name = keyName, secret_string = value });

        var request = build_signed_request(host, path, payload);
        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"kms.{_region}.myhuaweicloud.com";
        var payload = JsonSerializer.Serialize(new { secret_name = keyName });

        var request = build_signed_request(host, "/v1/{project_id}/secrets/{secret_name}", payload, "DELETE");
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
    /// 构建带 OBS4-HMAC-SHA256 签名的请求
    /// </summary>
    /// <param name="host">主机名</param>
    /// <param name="path">请求路径</param>
    /// <param name="payload">请求体 JSON</param>
    /// <param name="method">HTTP 方法</param>
    /// <returns>已签名的 HTTP 请求</returns>
    private HttpRequestMessage build_signed_request(
        string host, string path, string payload, string method = "POST")
    {
        var httpMethod = new HttpMethod(method);
        var request = new HttpRequestMessage(httpMethod, $"https://{host}{path}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json; charset=utf-8")
        };

        sign_request(request, host, path, payload);

        return request;
    }

    /// <summary>
    /// OBS4-HMAC-SHA256 签名
    /// </summary>
    /// <param name="request">HTTP 请求消息</param>
    /// <param name="host">主机名</param>
    /// <param name="path">请求路径</param>
    /// <param name="body">请求体</param>
    private void sign_request(HttpRequestMessage request, string host, string path, string body)
    {
        var t = DateTimeOffset.UtcNow;
        var obsDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/request";

        request.Headers.Add("Host", host);
        request.Headers.Add("X-Obs-Date", obsDate);

        var payloadHash = sha256_hex(Encoding.UTF8.GetBytes(body));
        request.Headers.Add("X-Obs-Content-Sha256", payloadHash);

        var signedHeaders = "content-type;host;x-obs-content-sha256;x-obs-date";
        var canonicalRequest = $"{request.Method}\n{path}\n\n"
                               + $"content-type:application/json; charset=utf-8\n"
                               + $"host:{host}\n"
                               + $"x-obs-content-sha256:{payloadHash}\n"
                               + $"x-obs-date:{obsDate}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"OBS4-HMAC-SHA256\n{obsDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = derive_signing_key(datestamp);
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"OBS4-HMAC-SHA256 Credential={_access_key_id}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private byte[] derive_signing_key(string datestamp)
    {
        return hmac_sha256(
            hmac_sha256(
                hmac_sha256(
                    hmac_sha256(
                        Encoding.UTF8.GetBytes($"OBS4{_secret_access_key}"),
                        datestamp),
                    _region),
                _service),
            "request");
    }

    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string sha256_hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
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
