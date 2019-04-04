using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Std.Net.Signal;

/// <summary>
///     SignalR 客户端基类，提供 Hub 方法调用的核心功能，
///     包括 Send（不等待返回）、Invoke（等待返回值）和 Stream（流式调用）。
/// </summary>
public abstract class SignalRClient
{
    /// <summary>
    ///     HTTP 客户端实例。
    /// </summary>
    protected readonly HttpClient _http_client;

    /// <summary>
    ///     Hub 名称。
    /// </summary>
    protected readonly string _hub_name;

    /// <summary>
    ///     Hub 连接地址。
    /// </summary>
    protected readonly string _hub_url;

    /// <summary>
    ///     初始化 <see cref="SignalRClient" /> 的新实例。
    /// </summary>
    /// <param name="hubUrl">Hub 连接地址。</param>
    /// <param name="hubName">Hub 名称。</param>
    protected SignalRClient(string hubUrl, string hubName)
    {
        _hub_url = hubUrl;
        _hub_name = hubName;
        _http_client = new HttpClient { BaseAddress = new Uri(hubUrl) };
    }

    /// <summary>
    ///     向 Hub 发送消息，不等待服务端返回值。
    /// </summary>
    /// <param name="method">Hub 方法名称。</param>
    /// <param name="args">调用参数。</param>
    protected async Task send(string method, params object?[] args)
    {
        var payload = build_invoke_payload(method, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/hubs/{_hub_name}/send", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///     调用 Hub 方法并等待返回值。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="method">Hub 方法名称。</param>
    /// <param name="args">调用参数。</param>
    /// <returns>Hub 方法的返回值。</returns>
    protected async Task<T> invoke<T>(string method, params object?[] args)
    {
        var payload = build_invoke_payload(method, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/hubs/{_hub_name}/invoke", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseBody)!;
    }

    /// <summary>
    ///     调用 Hub 方法（无返回值）。
    /// </summary>
    /// <param name="method">Hub 方法名称。</param>
    /// <param name="args">调用参数。</param>
    protected async Task invoke(string method, params object?[] args)
    {
        var payload = build_invoke_payload(method, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/hubs/{_hub_name}/invoke", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///     发起 Hub 流式调用。
    /// </summary>
    /// <typeparam name="T">流式响应元素类型。</typeparam>
    /// <param name="method">Hub 方法名称。</param>
    /// <param name="args">调用参数。</param>
    /// <returns>流式响应的异步可枚举序列。</returns>
    protected async Task<IAsyncEnumerable<T>> stream<T>(string method, params object?[] args)
    {
        var payload = build_invoke_payload(method, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/hubs/{_hub_name}/stream", content);
        response.EnsureSuccessStatusCode();

        return stream_response<T>(response);
    }

    /// <summary>
    ///     构建 Hub 调用的 JSON 负载。
    /// </summary>
    /// <param name="method">方法名称。</param>
    /// <param name="args">参数数组。</param>
    /// <returns>JSON 格式的调用负载。</returns>
    private static string build_invoke_payload(string method, object?[] args)
    {
        return JsonSerializer.Serialize(new
        {
            method,
            @params = args
        });
    }

    /// <summary>
    ///     从 HTTP 响应中读取流式数据。
    /// </summary>
    /// <typeparam name="T">流元素类型。</typeparam>
    /// <param name="response">HTTP 响应消息。</param>
    /// <returns>流式响应的异步可枚举序列。</returns>
    private static async IAsyncEnumerable<T> stream_response<T>(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(line)) continue;

            var item = JsonSerializer.Deserialize<T>(line);

            if (item is not null) yield return item;
        }
    }
}