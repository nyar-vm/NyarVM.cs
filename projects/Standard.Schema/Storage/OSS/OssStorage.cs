using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Hermes.Storage.Oss;

public sealed class OssStorage : IObjectStorage
{
    private readonly HttpClient _http;
    private readonly OssOptions _options;
    private readonly string _scheme;

    public OssStorage(OssOptions options)
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
        query.Append("max-keys=1000");

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
        var expires = (int)expiry.TotalSeconds;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expireTime = now + expires;

        var resource = $"/{_options.Bucket}/{key}";
        var stringToSign = $"GET\n\n\n{expireTime}\n{resource}";
        var signature = ComputeSignature(stringToSign);

        var url =
            $"{_scheme}://{BuildHost()}/{Uri.EscapeDataString(key)}?OSSAccessKeyId={Uri.EscapeDataString(_options.AccessKey)}&Expires={expireTime}&Signature={Uri.EscapeDataString(signature)}";
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

    #region OSS Signature V1

    private HttpRequestMessage BuildRequest(HttpMethod method, string key, HttpContent? content = null,
        string? query = null)
    {
        var date = DateTimeOffset.UtcNow.ToString("R");
        var path = $"/{_options.Bucket}/{key}";
        var url = $"{_scheme}://{BuildHost()}{path}";
        if (!string.IsNullOrEmpty(query)) url += $"?{query}";

        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Add("Date", date);

        var contentMd5 = "";
        var contentType = "";
        var ossHeaders = new SortedDictionary<string, string>();

        if (content != null) contentType = content.Headers.ContentType?.MediaType ?? "";

        foreach (var header in request.Headers)
            if (header.Key.StartsWith("x-oss-", StringComparison.OrdinalIgnoreCase))
                ossHeaders[header.Key.ToLowerInvariant()] = string.Join(",", header.Value);

        if (content != null)
        {
            var bodyBytes = content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            contentMd5 = Convert.ToBase64String(MD5.HashData(bodyBytes));
            request.Headers.Add("Content-MD5", contentMd5);
        }

        var canonicalizedOssHeaders = "";
        foreach (var (headerName, headerValue) in ossHeaders)
            canonicalizedOssHeaders += $"{headerName}:{headerValue}\n";

        var canonicalizedResource = $"/{_options.Bucket}/{key}";
        if (!string.IsNullOrEmpty(query))
        {
            var subResources = new[]
            {
                "acl", "uploads", "uploadId", "partNumber", "response-content-type", "response-content-language",
                "response-cache-control", "response-content-encoding", "response-content-disposition",
                "response-expires"
            };
            var queryParams = query.Split('&');
            var subParts = new List<string>();

            foreach (var param in queryParams)
            {
                var kv = param.Split('=', 2);
                if (subResources.Contains(kv[0])) subParts.Add(kv.Length == 2 ? $"{kv[0]}={kv[1]}" : kv[0]);
            }

            if (subParts.Count > 0) canonicalizedResource += "?" + string.Join("&", subParts.OrderBy(p => p));
        }

        var stringToSign =
            $"{method}\n{contentMd5}\n{contentType}\n{date}\n{canonicalizedOssHeaders}{canonicalizedResource}";
        var signature = ComputeSignature(stringToSign);

        request.Headers.Authorization = new AuthenticationHeaderValue("OSS", $"{_options.AccessKey}:{signature}");

        return request;
    }

    private string ComputeSignature(string stringToSign)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_options.SecretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(hash);
    }

    #endregion

    #region 辅助方法

    private string BuildHost()
    {
        return $"{_options.Bucket}.{_options.Endpoint}";
    }

    private static IReadOnlyDictionary<string, string> ExtractMetadata(HttpResponseMessage response)
    {
        var metadata = new Dictionary<string, string>();

        foreach (var header in response.Headers)
            if (header.Key.StartsWith("x-oss-meta-", StringComparison.OrdinalIgnoreCase))
                metadata[header.Key["x-oss-meta-".Length..]] = string.Join(",", header.Value);

        return metadata;
    }

    private static IReadOnlyList<ObjectInfo> ParseListResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var results = new List<ObjectInfo>();

        foreach (var content in doc.Descendants().Where(e => e.Name.LocalName == "Contents"))
        {
            var key = content.elements().FirstOrDefault(e => e.Name.LocalName == "Key")?.Value ?? "";
            var sizeStr = content.elements().FirstOrDefault(e => e.Name.LocalName == "Size")?.Value;
            var size = long.TryParse(sizeStr, out var s) ? s : 0;
            var lastModifiedStr = content.elements().FirstOrDefault(e => e.Name.LocalName == "LastModified")?.Value;
            var lastModified = DateTimeOffset.TryParse(lastModifiedStr, out var lm) ? lm : (DateTimeOffset?)null;
            var etag = content.elements().FirstOrDefault(e => e.Name.LocalName == "ETag")?.Value?.Trim('"');

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