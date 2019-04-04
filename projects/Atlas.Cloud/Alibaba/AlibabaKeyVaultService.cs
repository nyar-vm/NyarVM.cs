using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云 KMS 密钥管理实现，使用 HMAC-SHA1 签名
/// </summary>
public sealed class AlibabaKeyVaultService : IKeyVaultService
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;
    private readonly string _region;

    /// <summary>
    /// 初始化阿里云 KMS 密钥管理服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey ID</param>
    /// <param name="accessKeySecret">AccessKey Secret</param>
    /// <param name="region">区域，如 cn-hangzhou</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaKeyVaultService(
        string accessKeyId, string accessKeySecret,
        string region = "cn-hangzhou", HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _region = region;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<string?> get_secret(string keyName, CancellationToken cancel = default)
    {
        var parameters = build_common_parameters("GetSecretValue");
        parameters["SecretName"] = keyName;

        var url = build_signed_url(parameters);
        var response = await _http.GetAsync(url, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("SecretData", out var secretData))
        {
            var base64Data = secretData.get_string();
            if (base64Data is null)
            {
                return null;
            }

            var bytes = Convert.FromBase64String(base64Data);
            return Encoding.UTF8.GetString(bytes);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task set_secret(string keyName, string value, CancellationToken cancel = default)
    {
        var parameters = build_common_parameters("CreateSecret");
        parameters["SecretName"] = keyName;
        parameters["SecretData"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        parameters["VersionId"] = "1";

        var url = build_signed_url(parameters);
        var response = await _http.GetAsync(url, cancel);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_secret(string keyName, CancellationToken cancel = default)
    {
        var parameters = build_common_parameters("DeleteSecret");
        parameters["SecretName"] = keyName;

        var url = build_signed_url(parameters);
        var response = await _http.GetAsync(url, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 构建阿里云 API 公共参数
    /// </summary>
    /// <param name="action">API 操作名</param>
    /// <returns>参数字典</returns>
    private SortedDictionary<string, string> build_common_parameters(string action)
    {
        return new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessKeyId"] = _access_key_id,
            ["Action"] = action,
            ["Format"] = "JSON",
            ["RegionId"] = _region,
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureVersion"] = "1.0",
            ["SignatureNonce"] = Guid.NewGuid().ToString("N"),
            ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["Version"] = "2016-01-20"
        };
    }

    /// <summary>
    /// 构建带签名的请求 URL
    /// </summary>
    /// <param name="parameters">请求参数</param>
    /// <returns>完整签名的 URL</returns>
    private string build_signed_url(SortedDictionary<string, string> parameters)
    {
        var canonicalQuery = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var stringToSign = $"GET&{Uri.EscapeDataString("/")}&{Uri.EscapeDataString(canonicalQuery)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret + "&"));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        return $"https://kms.{_region}.aliyuncs.com/?{canonicalQuery}&Signature={Uri.EscapeDataString(signature)}";
    }
}
