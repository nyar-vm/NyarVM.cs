using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Alibaba;

/// <summary>
/// 阿里云 CDN 服务实现，使用 HMAC-SHA1 签名认证
/// </summary>
public sealed class AlibabaCdn : ICdnService
{
    private readonly HttpClient _http;
    private readonly string _access_key_id;
    private readonly string _access_key_secret;
    private const string Endpoint = "https://cdn.aliyuncs.com/";

    /// <summary>
    /// 初始化阿里云 CDN 服务
    /// </summary>
    /// <param name="accessKeyId">AccessKey 标识</param>
    /// <param name="accessKeySecret">AccessKey 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AlibabaCdn(string accessKeyId, string accessKeySecret, HttpClient? httpClient = null)
    {
        _access_key_id = accessKeyId;
        _access_key_secret = accessKeySecret;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<CdnResult> purge(string[] urls, CancellationToken ct = default)
    {
        return await execute_action("RefreshObjectCaches", urls, "Directory", ct);
    }

    /// <inheritdoc />
    public async Task<CdnResult> prefetch(string[] urls, CancellationToken ct = default)
    {
        return await execute_action("PushObjectCaches", urls, "Url", ct);
    }

    private async Task<CdnResult> execute_action(string action, string[] urls, string objectType, CancellationToken ct)
    {
        try
        {
            var object_path = string.Join("\n", urls);
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();

            var parameters = new SortedDictionary<string, string>
            {
                ["Action"] = action,
                ["Format"] = "JSON",
                ["Version"] = "2018-05-10",
                ["AccessKeyId"] = _access_key_id,
                ["SignatureMethod"] = "HMAC-SHA1",
                ["Timestamp"] = timestamp,
                ["SignatureVersion"] = "1.0",
                ["SignatureNonce"] = nonce,
                ["ObjectPath"] = object_path,
                ["ObjectType"] = objectType
            };

            var signature = compute_signature(parameters);
            parameters["Signature"] = signature;

            var query = string.Join("&", parameters.Select(p =>
                $"{percent_encode(p.Key)}={percent_encode(p.Value)}"));

            var url = $"{Endpoint}?{query}";

            using var http_request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            if (root.TryGetProperty("RequestId", out var request_id_el))
            {
                return CdnResult.ok(request_id_el.get_string());
            }

            if (root.TryGetProperty("Code", out var code_el))
            {
                var message = root.TryGetProperty("Message", out var msg_el) ? msg_el.get_string() ?? "未知错误" : "未知错误";
                return CdnResult.fail($"阿里云 CDN 错误: {message}");
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

    private string compute_signature(SortedDictionary<string, string> parameters)
    {
        var sorted_query = string.Join("&", parameters.Select(p =>
            $"{percent_encode(p.Key)}={percent_encode(p.Value)}"));

        var string_to_sign = $"GET&{percent_encode("/")}&{percent_encode(sorted_query)}";

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_access_key_secret + "&"));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(string_to_sign));
        return Convert.ToBase64String(hash);
    }

    private static string percent_encode(string value)
    {
        return HttpUtility.UrlEncode(value, Encoding.UTF8)
            .Replace("+", "%20")
            .Replace("*", "%2A")
            .Replace("%7E", "~");
    }
}
