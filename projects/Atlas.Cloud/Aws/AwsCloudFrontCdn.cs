using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Aws;

/// <summary>
/// AWS CloudFront CDN 服务实现，使用 AWS Signature V4 认证
/// </summary>
public sealed class AwsCloudFrontCdn : ICdnService
{
    private readonly HttpClient _http;
    private readonly string _access_key;
    private readonly string _secret_key;
    private readonly string _region;
    private readonly string _distribution_id;
    private const string Service = "cloudfront";

    /// <summary>
    /// 初始化 AWS CloudFront CDN 服务
    /// </summary>
    /// <param name="accessKey">AWS Access Key</param>
    /// <param name="secretKey">AWS Secret Key</param>
    /// <param name="region">区域，如 us-east-1</param>
    /// <param name="distributionId">CloudFront 分发标识</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AwsCloudFrontCdn(
        string accessKey, string secretKey, string region = "us-east-1",
        string distributionId = "", HttpClient? httpClient = null)
    {
        _access_key = accessKey;
        _secret_key = secretKey;
        _region = region;
        _distribution_id = distributionId;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<CdnResult> purge(string[] urls, CancellationToken ct = default)
    {
        return await create_invalidation(urls, ct);
    }

    /// <inheritdoc />
    public async Task<CdnResult> prefetch(string[] urls, CancellationToken ct = default)
    {
        return await create_invalidation(urls, ct);
    }

    private async Task<CdnResult> create_invalidation(string[] urls, CancellationToken ct)
    {
        try
        {
            var caller_reference = Guid.NewGuid().ToString();
            var paths = urls.Select(u => new Uri(u).AbsolutePath).ToList();

            var body = new
            {
                InvalidationBatch = new
                {
                    CallerReference = caller_reference,
                    Paths = new
                    {
                        Quantity = paths.Count,
                        Items = paths
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var host = $"cloudfront.amazonaws.com";
            var path = $"/2020-05-31/distribution/{_distribution_id}/invalidation";

            using var http_request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}{path}");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            sign_request_v4(http_request, host, path, json);

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(response_body);
                var task_id = doc.RootElement.TryGetProperty("Invalidation", out var inv)
                              && inv.TryGetProperty("Id", out var id_el)
                    ? id_el.get_string()
                    : null;

                return CdnResult.ok(task_id);
            }

            return CdnResult.fail($"CloudFront 刷新失败: {response.StatusCode} - {response_body}");
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

    private void sign_request_v4(HttpRequestMessage request, string host, string path, string body)
    {
        var t = DateTimeOffset.UtcNow;
        var amz_date = t.ToString("yyyyMMddTHHmmssZ");
        var date_stamp = t.ToString("yyyyMMdd");
        var credential_scope = $"{date_stamp}/{_region}/{Service}/aws4_request";

        request.Headers.Add("Host", host);
        request.Headers.Add("X-Amz-Date", amz_date);

        var payload_hash = sha256_hex(body);
        request.Headers.Add("X-Amz-Content-Sha256", payload_hash);

        var signed_headers = "host;x-amz-content-sha256;x-amz-date";
        var canonical_request = $"{request.Method}\n{path}\n\n"
                                + $"host:{host}\n"
                                + $"x-amz-content-sha256:{payload_hash}\n"
                                + $"x-amz-date:{amz_date}\n\n"
                                + $"{signed_headers}\n"
                                + payload_hash;

        var string_to_sign = $"AWS4-HMAC-SHA256\n{amz_date}\n{credential_scope}\n{sha256_hex(canonical_request)}";

        var signing_key = hmac_sha256(hmac_sha256(hmac_sha256(hmac_sha256(
            Encoding.UTF8.GetBytes($"AWS4{_secret_key}"), date_stamp),
            _region), Service), "aws4_request");
        var signature = hmac_sha256_hex(signing_key, string_to_sign);

        request.Headers.Add("Authorization",
            $"AWS4-HMAC-SHA256 Credential={_access_key}/{credential_scope}, SignedHeaders={signed_headers}, Signature={signature}");
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
