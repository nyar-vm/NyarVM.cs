using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Hermes.Storage.S3;

public sealed class S3Storage : IObjectStorage
{
    private readonly HttpClient _http;
    private readonly S3Options _options;
    private readonly string _scheme;

    public S3Storage(S3Options options)
    {
        _options = options;
        _scheme = options.UseHttps ? "https" : "http";

        _http = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
    }

    #region 写入

    public async Task PutAsync(string key, Stream data, string? contentType = null, CancellationToken ct = default)
    {
        var content = new StreamContent(data);
        if (contentType != null) content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var request = BuildRequest(HttpMethod.Put, key, content);
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region 删除

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Delete, key);
        var response = await _http.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        response.EnsureSuccessStatusCode();
        return true;
    }

    #endregion

    #region 存在性检查

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Head, key);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return false;

        response.EnsureSuccessStatusCode();
        return true;
    }

    #endregion

    #region 列举

    public async Task<IReadOnlyList<ObjectInfo>> ListAsync(string? prefix = null, CancellationToken ct = default)
    {
        var query = new StringBuilder();
        query.Append("list-type=2");

        if (!string.IsNullOrEmpty(prefix)) query.Append($"&prefix={Uri.EscapeDataString(prefix)}");

        var request = BuildRequest(HttpMethod.Get, "", query: query.ToString());
        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(ct);
        return ParseListResponse(xml);
    }

    #endregion

    #region 预签名 URL

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var credential = $"{_options.AccessKey}/{dateStamp}/{_options.Region}/s3/aws4_request";

        var host = BuildHost();
        var path = BuildPath(key);

        var canonicalHeaders = $"host:{host}\nx-amz-content-sha256:UNSIGNED-PAYLOAD\n";
        var signedHeaders = "host;x-amz-content-sha256";
        var canonicalQueryString = new StringBuilder();
        canonicalQueryString.Append("X-Amz-Algorithm=AWS4-HMAC-SHA256");
        canonicalQueryString.Append($"&X-Amz-Credential={Uri.EscapeDataString(credential)}");
        canonicalQueryString.Append($"&X-Amz-Date={amzDate}");
        canonicalQueryString.Append($"&X-Amz-Expires={Math.Max(1, (int)expiry.TotalSeconds)}");
        canonicalQueryString.Append($"&X-Amz-SignedHeaders={signedHeaders}");

        var canonicalRequest =
            $"GET\n{path}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\nUNSIGNED-PAYLOAD";
        var stringToSign =
            $"AWS4-HMAC-SHA256\n{amzDate}\n{dateStamp}/{_options.Region}/s3/aws4_request\n{Sha256Hex(canonicalRequest)}";
        var signingKey = DeriveSigningKey(dateStamp);
        var signature = HmacHex(signingKey, stringToSign);

        var url = $"{_scheme}://{host}{path}?{canonicalQueryString}&X-Amz-Signature={signature}";
        return Task.FromResult(url);
    }

    #endregion

    public void Dispose()
    {
        _http.Dispose();
    }

    #region 读取

    public async Task<ObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Head, key);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        return new ObjectMetadata
        {
            Key = key,
            Size = response.Content.Headers.ContentLength ?? 0,
            ContentType = response.Content.Headers.ContentType?.MediaType,
            ETag = response.Headers.ETag?.Tag,
            LastModified = response.Content.Headers.LastModified,
            Metadata = ExtractMetadata(response)
        };
    }

    public async Task<Stream> GetAsync(string key, CancellationToken ct = default)
    {
        var request = BuildRequest(HttpMethod.Get, key);
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    #endregion

    #region AWS Signature V4

    private HttpRequestMessage BuildRequest(HttpMethod method, string key, HttpContent? content = null,
        string? query = null)
    {
        var now = DateTimeOffset.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var dateStamp = now.ToString("yyyyMMdd");

        var host = BuildHost();
        var path = BuildPath(key);
        var url = $"{_scheme}://{host}{path}";
        if (!string.IsNullOrEmpty(query)) url += $"?{query}";

        var request = new HttpRequestMessage(method, url) { Content = content };

        request.Headers.Host = host;
        request.Headers.Add("x-amz-date", amzDate);
        request.Headers.Add("x-amz-content-sha256",
            content != null ? "UNSIGNED-PAYLOAD" : "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");

        var payloadHash = content != null
            ? "UNSIGNED-PAYLOAD"
            : "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        var canonicalHeaders = $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";

        var canonicalQuery = query ?? "";
        var canonicalRequest =
            $"{method}\n{path}\n{canonicalQuery}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        var stringToSign =
            $"AWS4-HMAC-SHA256\n{amzDate}\n{dateStamp}/{_options.Region}/s3/aws4_request\n{Sha256Hex(canonicalRequest)}";
        var signingKey = DeriveSigningKey(dateStamp);
        var signature = HmacHex(signingKey, stringToSign);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "AWS4-HMAC-SHA256",
            $"Credential={_options.AccessKey}/{dateStamp}/{_options.Region}/s3/aws4_request, SignedHeaders={signedHeaders}, Signature={signature}"
        );

        return request;
    }

    private byte[] DeriveSigningKey(string dateStamp)
    {
        var kDate = Hmac(Encoding.UTF8.GetBytes($"AWS4{_options.SecretKey}"), dateStamp);
        var kRegion = Hmac(kDate, _options.Region);
        var kService = Hmac(kRegion, "s3");
        return Hmac(kService, "aws4_request");
    }

    private static byte[] Hmac(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string HmacHex(byte[] key, string data)
    {
        var hash = Hmac(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Sha256Hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion

    #region 辅助方法

    private string BuildHost()
    {
        if (_options.PathStyle) return _options.Endpoint;

        return $"{_options.Bucket}.{_options.Endpoint}";
    }

    private string BuildPath(string key)
    {
        if (_options.PathStyle) return $"/{_options.Bucket}/{Uri.EscapeDataString(key)}";

        return $"/{Uri.EscapeDataString(key)}";
    }

    private static IReadOnlyDictionary<string, string> ExtractMetadata(HttpResponseMessage response)
    {
        var metadata = new Dictionary<string, string>();

        foreach (var header in response.Headers)
            if (header.Key.StartsWith("x-amz-meta-", StringComparison.OrdinalIgnoreCase))
                metadata[header.Key["x-amz-meta-".Length..]] = string.Join(",", header.Value);

        return metadata;
    }

    private static IReadOnlyList<ObjectInfo> ParseListResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var ns = XNamespace.Get("http://s3.amazonaws.com/doc/2006-03-01/");
        var results = new List<ObjectInfo>();

        foreach (var content in doc.Descendants(ns + "Contents"))
        {
            var key = content.Element(ns + "Key")?.Value ?? "";
            var size = long.TryParse(content.Element(ns + "Size")?.Value, out var s) ? s : 0;
            var lastModified = DateTimeOffset.TryParse(content.Element(ns + "LastModified")?.Value, out var lm)
                ? lm
                : (DateTimeOffset?)null;
            var etag = content.Element(ns + "ETag")?.Value?.Trim('"');

            results.Add(new ObjectInfo
            {
                Key = key,
                Size = size,
                LastModified = lastModified,
                ETag = etag != null ? $"\"{etag}\"" : null
            });
        }

        return results;
    }

    #endregion
}