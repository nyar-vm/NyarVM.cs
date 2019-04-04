using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Aws;

/// <summary>
/// AWS Secrets Manager 密钥管理实现，使用 AWS Signature V4 签名
/// </summary>
public sealed class AwsKeyVaultService : IKeyVaultService
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private const string _service = "secretsmanager";

    /// <summary>
    /// 初始化 AWS Secrets Manager 密钥管理服务
    /// </summary>
    /// <param name="accessKey">AWS Access Key</param>
    /// <param name="secretKey">AWS Secret Key</param>
    /// <param name="region">区域，如 us-east-1</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AwsKeyVaultService(string accessKey, string secretKey, string region = "us-east-1", HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<string?> get_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"secretsmanager.{_region}.amazonaws.com";
        var url = $"https://{host}/";
        var body = JsonSerializer.Serialize(new { SecretId = keyName });

        var request = build_secrets_request(url, host, "secretsmanager.GetSecretValue", body);
        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("SecretString", out var secretString))
        {
            return secretString.get_string();
        }

        return null;
    }

    /// <inheritdoc />
    public async Task set_secret(string keyName, string value, CancellationToken cancel = default)
    {
        var host = $"secretsmanager.{_region}.amazonaws.com";
        var url = $"https://{host}/";

        var existing = await try_get_secret_raw(keyName, cancel);
        var target = existing is not null ? "secretsmanager.UpdateSecret" : "secretsmanager.CreateSecret";

        var body = target == "secretsmanager.CreateSecret"
            ? JsonSerializer.Serialize(new { Name = keyName, SecretString = value })
            : JsonSerializer.Serialize(new { SecretId = keyName, SecretString = value });

        var request = build_secrets_request(url, host, target, body);
        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_secret(string keyName, CancellationToken cancel = default)
    {
        var host = $"secretsmanager.{_region}.amazonaws.com";
        var url = $"https://{host}/";
        var body = JsonSerializer.Serialize(new { SecretId = keyName, RecoveryWindowInDays = 7 });

        var request = build_secrets_request(url, host, "secretsmanager.DeleteSecret", body);
        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 尝试获取密钥原始响应，用于判断密钥是否存在
    /// </summary>
    /// <param name="keyName">密钥名称</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>响应体字符串，不存在时返回 null</returns>
    private async Task<string?> try_get_secret_raw(string keyName, CancellationToken cancel)
    {
        try
        {
            var host = $"secretsmanager.{_region}.amazonaws.com";
            var url = $"https://{host}/";
            var body = JsonSerializer.Serialize(new { SecretId = keyName });

            var request = build_secrets_request(url, host, "secretsmanager.GetSecretValue", body);
            var response = await _http.SendAsync(request, cancel);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancel);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// 构建 AWS Secrets Manager 请求，包含 X-Amz-Target 头和签名
    /// </summary>
    /// <param name="url">请求 URL</param>
    /// <param name="host">主机名</param>
    /// <param name="target">X-Amz-Target 值</param>
    /// <param name="body">请求体 JSON</param>
    /// <returns>已签名的 HTTP 请求</returns>
    private HttpRequestMessage build_secrets_request(string url, string host, string target, string body)
    {
        var content = new StringContent(body, Encoding.UTF8, "application/x-amz-json-1.1");
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        request.Headers.Add("Host", host);
        request.Headers.Add("X-Amz-Target", target);

        sign_request_v4(request, host, body);

        return request;
    }

    /// <summary>
    /// AWS Signature V4 签名
    /// </summary>
    /// <param name="request">HTTP 请求消息</param>
    /// <param name="host">主机名</param>
    /// <param name="body">请求体</param>
    private void sign_request_v4(HttpRequestMessage request, string host, string body)
    {
        var t = DateTimeOffset.UtcNow;
        var amzDate = t.ToString("yyyyMMddTHHmmssZ");
        var datestamp = t.ToString("yyyyMMdd");
        var credentialScope = $"{datestamp}/{_region}/{_service}/aws4_request";

        request.Headers.Add("X-Amz-Date", amzDate);

        var payloadHash = sha256_hex(Encoding.UTF8.GetBytes(body));
        request.Headers.Add("X-Amz-Content-Sha256", payloadHash);

        var signedHeaders = "host;x-amz-content-sha256;x-amz-date;x-amz-target";
        var canonicalRequest = $"{request.Method}\n/\n\n"
                               + $"host:{host}\n"
                               + $"x-amz-content-sha256:{payloadHash}\n"
                               + $"x-amz-date:{amzDate}\n"
                               + $"x-amz-target:{request.Headers.GetValues("X-Amz-Target").First()}\n\n"
                               + $"{signedHeaders}\n"
                               + payloadHash;

        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{sha256_hex(canonicalRequest)}";

        var signingKey = hmac_sha256(hmac_sha256(hmac_sha256(hmac_sha256(
            Encoding.UTF8.GetBytes($"AWS4{_secret_key}"), datestamp),
            _region), _service), "aws4_request");
        var signature = hmac_sha256_hex(signingKey, stringToSign);

        request.Headers.Add("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_access_key}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    /// <summary>
    /// 计算字符串的 SHA256 十六进制哈希
    /// </summary>
    /// <param name="data">输入字符串</param>
    /// <returns>小写十六进制哈希值</returns>
    private static string sha256_hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 计算字节数组的 SHA256 十六进制哈希
    /// </summary>
    /// <param name="data">输入字节数组</param>
    /// <returns>小写十六进制哈希值</returns>
    private static string sha256_hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// HMAC-SHA256 计算，返回字节数组
    /// </summary>
    /// <param name="key">密钥</param>
    /// <param name="data">数据</param>
    /// <returns>HMAC 哈希字节数组</returns>
    private static byte[] hmac_sha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    /// HMAC-SHA256 计算，返回小写十六进制字符串
    /// </summary>
    /// <param name="key">密钥</param>
    /// <param name="data">数据</param>
    /// <returns>小写十六进制 HMAC 哈希值</returns>
    private static string hmac_sha256_hex(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
