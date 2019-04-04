using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;

namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     VOA Fetch 服务 — HTTP 后端 + 缓存 + 去重，仅供 DevServer/SSR 使用
/// </summary>
public sealed class VoaFetchService : IDisposable
{
    private readonly ConcurrentDictionary<string, Task<FetchResponse>> _deduplication_map = new();
    private readonly HttpClient _http_client;
    private bool _disposed;

    public VoaFetchService(VoaFetchCache? cache = null)
    {
        _http_client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http_client.DefaultRequestHeaders.Add("User-Agent", "Asgard-VOA-Fetch/0.1");
        this.cache = cache ?? new VoaFetchCache();
    }

    public VoaFetchCache cache { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _http_client.Dispose();
    }

    /// <summary>
    ///     执行 HTTP 请求并返回结构化响应（含缓存逻辑）
    /// </summary>
    public async Task<FetchResponse> fetch(string url, FetchOptions? options = null)
    {
        var opt = options ?? new FetchOptions();
        var cacheKey = build_cache_key(url, opt);

        if (_deduplication_map.TryGetValue(cacheKey, out var pendingTask))
        {
            return await pendingTask;
        }

        var task = fetch_internal(url, opt, cacheKey);
        _deduplication_map[cacheKey] = task;

        try
        {
            return await task;
        }
        finally
        {
            _deduplication_map.TryRemove(cacheKey, out _);
        }
    }

    private async Task<FetchResponse> fetch_internal(string url, FetchOptions opt, string cacheKey)
    {
        if (opt.cache == "force-cache" || opt.next_revalidate > 0)
        {
            if (cache.try_get(cacheKey, out var cachedResponse))
            {
                cachedResponse.cached = true;
                return cachedResponse;
            }
        }

        try
        {
            var request = new HttpRequestMessage(new HttpMethod(opt.method ?? "GET"), url);

            if (opt.headers?.Count > 0)
            {
                foreach (var kvp in opt.headers)
                {
                    request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }

            if (!string.IsNullOrEmpty(opt.body) && opt.method is "POST" or "PUT" or "PATCH")
            {
                request.Content = new StringContent(opt.body, Encoding.UTF8, "application/json");
            }

            var response = await _http_client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            var fetchResponse = new FetchResponse
            {
                ok = response.IsSuccessStatusCode,
                status = (int)response.StatusCode,
                status_text = response.ReasonPhrase ?? "",
                headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                body = body,
                cached = false,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            if (opt.cache == "force-cache" || opt.next_revalidate > 0)
            {
                cache.set(cacheKey, fetchResponse, opt.next_revalidate > 0 ? opt.next_revalidate : 3600);
            }

            return fetchResponse;
        }
        catch (Exception ex)
        {
            return new FetchResponse
            {
                ok = false,
                status = 0,
                status_text = ex.Message,
                headers = new Dictionary<string, string>(),
                body = "",
                cached = false,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }

    public async Task fetch_stream(string url, Func<string, Task> onChunk, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _http_client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var buffer = new StringBuilder();

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                break;
            }

            if (line.StartsWith("data: "))
            {
                buffer.Append(line[6..]);
            }
            else if (string.IsNullOrEmpty(line) && buffer.Length > 0)
            {
                await onChunk(buffer.ToString());
                buffer.Clear();
            }
        }

        if (buffer.Length > 0)
        {
            await onChunk(buffer.ToString());
        }
    }

    public void clear_deduplication_map()
    {
        _deduplication_map.Clear();
    }

    private static string build_cache_key(string url, FetchOptions opt)
    {
        var key = $"{opt.method ?? "GET"}:{url}";
        if (!string.IsNullOrEmpty(opt.body))
        {
            key += $":{opt.body}";
        }

        return key;
    }
}
