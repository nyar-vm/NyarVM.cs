using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Valhalla.Tests;

/// <summary>
///     测试用 HTTP 消息处理器，按方法+路径路由，支持查询参数和二进制响应
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly List<RouteEntry> _routes = [];
    private readonly List<SequentialRouteEntry> _sequential_routes = [];

    public void add_get(string pathAndQuery, HttpStatusCode status, string content,
        Dictionary<string, string>? headers = null)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(content);
        _routes.Add(new RouteEntry(HttpMethod.Get, pathAndQuery, status, bodyBytes,
            "application/json; charset=utf-8", headers));
    }

    public void add_get_bytes(string pathAndQuery, HttpStatusCode status, byte[] content,
        Dictionary<string, string>? headers = null)
    {
        _routes.Add(new RouteEntry(HttpMethod.Get, pathAndQuery, status, content,
            "application/octet-stream", headers));
    }

    public void add_post(string pathAndQuery, HttpStatusCode status, string content,
        Dictionary<string, string>? headers = null)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(content);
        _routes.Add(new RouteEntry(HttpMethod.Post, pathAndQuery, status, bodyBytes,
            "application/json; charset=utf-8", headers));
    }

    /// <summary>
    ///     添加顺序响应的 GET 路由，每次请求依次返回数组中的响应
    /// </summary>
    /// <param name="pathAndQuery">路径与查询参数</param>
    /// <param name="responses">顺序响应数组，每个元素为 (状态码, 响应内容)</param>
    public void add_sequential_get(string pathAndQuery, (HttpStatusCode Status, string Content)[] responses)
    {
        var entries = responses.Select(r =>
        {
            var bodyBytes = Encoding.UTF8.GetBytes(r.Content);
            return new RouteEntry(HttpMethod.Get, pathAndQuery, r.Status, bodyBytes,
                "application/json; charset=utf-8", null);
        }).ToArray();

        _sequential_routes.Add(new SequentialRouteEntry(pathAndQuery, entries));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var fullUrl = request.RequestUri?.PathAndQuery ?? string.Empty;

        foreach (var seqRoute in _sequential_routes)
            if (fullUrl == seqRoute.path_and_query)
            {
                var entry = seqRoute.consume_next();
                var response = build_response(entry);
                return Task.FromResult(response);
            }

        foreach (var route in _routes)
            if (request.Method == route.method && fullUrl == route.path_and_query)
                return Task.FromResult(build_response(route));

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(@"{""error"":""not found""}",
                Encoding.UTF8, "application/json")
        });
    }

    private static HttpResponseMessage build_response(RouteEntry entry)
    {
        var response = new HttpResponseMessage(entry.status)
        {
            Content = new ByteArrayContent(entry.body_bytes)
        };
        response.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(entry.content_type);

        foreach (var (key, value) in entry.headers) response.Headers.TryAddWithoutValidation(key, value);

        return response;
    }

    private record RouteEntry(
        HttpMethod method,
        string path_and_query,
        HttpStatusCode status,
        byte[] body_bytes,
        string content_type,
        Dictionary<string, string>? headers
    )
    {
        public Dictionary<string, string> headers { get; } = headers ?? new Dictionary<string, string>();
    }

    /// <summary>
    ///     顺序路由条目，维护消费索引
    /// </summary>
    private class SequentialRouteEntry
    {
        private readonly RouteEntry[] _entries;
        private int _index;

        public SequentialRouteEntry(string pathAndQuery, RouteEntry[] entries)
        {
            path_and_query = pathAndQuery;
            _entries = entries;
            _index = 0;
        }

        public string path_and_query { get; }

        public RouteEntry consume_next()
        {
            var entry = _entries[_index];
            if (_index < _entries.Length - 1) _index++;

            return entry;
        }
    }
}