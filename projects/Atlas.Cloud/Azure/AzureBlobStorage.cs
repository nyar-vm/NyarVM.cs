using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure Blob Storage 实现，使用 REST API SharedKey 认证
/// </summary>
public sealed class AzureBlobStorage : IBlobStorage
{
    private readonly HttpClient _http;
    private readonly string _account_name;
    private readonly byte[] _account_key;

    /// <summary>
    /// 初始化 Azure Blob Storage
    /// </summary>
    /// <param name="accountName">存储账户名称</param>
    /// <param name="accountKey">存储账户密钥（Base64 编码）</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureBlobStorage(
        string accountName, string accountKey,
        HttpClient? httpClient = null)
    {
        _account_name = accountName;
        _account_key = Convert.FromBase64String(accountKey);
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<BlobResult> put_object(
        string bucket, string key, byte[] data, string? contentType = null,
        CancellationToken cancel = default)
    {
        var url = build_url(bucket, key);
        var content = new ByteArrayContent(data);

        if (contentType is not null)
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        var date = DateTime.UtcNow.ToString("R");

        sign_request(request, "PUT", bucket, key, contentType, data.Length, date);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok(response.Headers.ETag?.Tag);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"Azure 上传失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<byte[]?> get_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = build_url(bucket, key);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var date = DateTime.UtcNow.ToString("R");

        sign_request(request, "GET", bucket, key, null, 0, date);

        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancel);
    }

    /// <inheritdoc />
    public async Task<BlobResult> delete_object(string bucket, string key, CancellationToken cancel = default)
    {
        var url = build_url(bucket, key);
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        var date = DateTime.UtcNow.ToString("R");

        sign_request(request, "DELETE", bucket, key, null, 0, date);

        var response = await _http.SendAsync(request, cancel);

        if (response.IsSuccessStatusCode)
        {
            return BlobResult.ok();
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancel);
        return BlobResult.fail($"Azure 删除失败: {response.StatusCode} - {errorBody}");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlobInfo>> list_objects(
        string bucket, string? prefix = null, int maxKeys = 100,
        CancellationToken cancel = default)
    {
        var url = $"https://{_account_name}.blob.core.windows.net/{bucket}?restype=container&comp=list&maxresults={maxKeys}";

        if (prefix is not null)
        {
            url += $"&prefix={Uri.EscapeDataString(prefix)}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var date = DateTime.UtcNow.ToString("R");

        sign_request(request, "GET", bucket, null, null, 0, date, true);

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(cancel);
        var doc = XDocument.Parse(xml);
        var results = new List<BlobInfo>();

        foreach (var blob in doc.Descendants("Blob"))
        {
            var props = blob.Element("Properties");
            results.Add(new BlobInfo
            {
                key = blob.Element("Name")?.Value ?? string.Empty,
                size = long.TryParse(props?.Element("Content-Length")?.Value, out var s) ? s : 0,
                last_modified = DateTime.TryParse(props?.Element("Last-Modified")?.Value, out var dt) ? dt : default,
                etag = props?.Element("Etag")?.Value
            });
        }

        return results;
    }

    /// <inheritdoc />
    public string generate_presigned_url(string bucket, string key, TimeSpan expiry, string method = "GET")
    {
        var sv = "2023-11-03";
        var sr = "b";
        var sp = method == "GET" ? "r" : "w";
        var st = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var se = DateTime.UtcNow.Add(expiry).ToString("yyyy-MM-ddTHH:mm:ssZ");

        var canonicalResource = $"/{_account_name}/{bucket}/{key}";
        var stringToSign = $"{sp}\n\n\n{st}\n{se}\n{canonicalResource}";

        using var hmac = new HMACSHA256(_account_key);
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        return $"https://{_account_name}.blob.core.windows.net/{bucket}/{Uri.EscapeDataString(key)}"
               + $"?sv={sv}&sr={sr}&sp={sp}&st={Uri.EscapeDataString(st)}&se={Uri.EscapeDataString(se)}"
               + $"&sig={Uri.EscapeDataString(signature)}";
    }

    private string build_url(string container, string? blob)
    {
        return blob is not null
            ? $"https://{_account_name}.blob.core.windows.net/{container}/{Uri.EscapeDataString(blob)}"
            : $"https://{_account_name}.blob.core.windows.net/{container}";
    }

    private void sign_request(
        HttpRequestMessage request, string method, string container, string? blob,
        string? contentType, int contentLength, string date, bool isList = false)
    {
        request.Headers.Add("x-ms-date", date);
        request.Headers.Add("x-ms-version", "2023-11-03");

        if (method == "PUT")
        {
            request.Headers.Add("x-ms-blob-type", "BlockBlob");
        }

        var contentLengthStr = contentLength > 0 ? contentLength.ToString() : string.Empty;
        var contentTypeStr = contentType ?? string.Empty;

        var canonicalizedHeaders = new StringBuilder();

        if (method == "PUT")
        {
            canonicalizedHeaders.Append("x-ms-blob-type:BlockBlob\n");
        }

        canonicalizedHeaders.Append($"x-ms-date:{date}\n");
        canonicalizedHeaders.Append("x-ms-version:2023-11-03\n");

        var canonicalizedResource = new StringBuilder();
        canonicalizedResource.Append($"/{_account_name}/{container}");

        if (blob is not null)
        {
            canonicalizedResource.Append($"/{blob}");
        }

        if (isList)
        {
            canonicalizedResource.Append("\ncomp:list\nrestype:container");
        }

        var stringToSign = $"{method}\n"
                           + "\n"
                           + "\n"
                           + $"{contentLengthStr}\n"
                           + "\n"
                           + $"{contentTypeStr}\n"
                           + "\n"
                           + "\n"
                           + "\n"
                           + "\n"
                           + "\n"
                           + "\n"
                           + canonicalizedHeaders
                           + canonicalizedResource;

        using var hmac = new HMACSHA256(_account_key);
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        request.Headers.Add("Authorization", $"SharedKey {_account_name}:{signature}");
    }
}
