using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Std.Net.Rpc;

/// <summary>
///     RPC 客户端基类，提供 RPC 方法调用的核心功能，
///     包括单向调用、服务端流、客户端流和双向流。
/// </summary>
public abstract class RpcClient
{
    /// <summary>
    ///     RPC 服务基础地址。
    /// </summary>
    protected readonly string _base_url;

    /// <summary>
    ///     HTTP 客户端实例。
    /// </summary>
    protected readonly HttpClient _http_client;

    /// <summary>
    ///     RPC 服务名称。
    /// </summary>
    protected readonly string _service_name;

    /// <summary>
    ///     初始化 <see cref="RpcClient" /> 的新实例。
    /// </summary>
    /// <param name="baseUrl">RPC 服务基础地址。</param>
    /// <param name="serviceName">RPC 服务名称。</param>
    protected RpcClient(string baseUrl, string serviceName)
    {
        _base_url = baseUrl;
        _service_name = serviceName;
        _http_client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    ///     发起单向 RPC 调用并返回指定类型的结果。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="methodName">RPC 方法名称。</param>
    /// <param name="args">调用参数。</param>
    /// <returns>RPC 调用结果。</returns>
    protected async Task<T> invoke<T>(string methodName, params object?[] args)
    {
        var payload = build_request_payload(methodName, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/{_service_name}/{methodName}", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseBody)!;
    }

    /// <summary>
    ///     发起单向 RPC 调用（无返回值）。
    /// </summary>
    /// <param name="methodName">RPC 方法名称。</param>
    /// <param name="args">调用参数。</param>
    protected async Task invoke(string methodName, params object?[] args)
    {
        var payload = build_request_payload(methodName, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/{_service_name}/{methodName}", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///     发起服务端流式 RPC 调用。
    /// </summary>
    /// <typeparam name="T">流式响应元素类型。</typeparam>
    /// <param name="methodName">RPC 方法名称。</param>
    /// <param name="args">调用参数。</param>
    /// <returns>流式响应的异步可枚举序列。</returns>
    protected async Task<IAsyncEnumerable<T>> server_stream<T>(string methodName, params object?[] args)
    {
        var payload = build_request_payload(methodName, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/{_service_name}/{methodName}/stream", content);
        response.EnsureSuccessStatusCode();

        return stream_response<T>(response);
    }

    /// <summary>
    ///     发起客户端流式 RPC 调用。
    /// </summary>
    /// <typeparam name="T">返回值类型。</typeparam>
    /// <param name="methodName">RPC 方法名称。</param>
    /// <param name="args">调用参数。</param>
    /// <returns>RPC 调用结果。</returns>
    protected async Task<T> client_stream<T>(string methodName, params object?[] args)
    {
        var payload = build_request_payload(methodName, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/{_service_name}/{methodName}/client-stream", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseBody)!;
    }

    /// <summary>
    ///     发起客户端流式 RPC 调用（无返回值）。
    /// </summary>
    /// <param name="methodName">RPC 方法名称。</param>
    /// <param name="args">调用参数。</param>
    protected async Task client_stream(string methodName, params object?[] args)
    {
        var payload = build_request_payload(methodName, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/{_service_name}/{methodName}/client-stream", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    ///     发起双向流式 RPC 调用。
    /// </summary>
    /// <typeparam name="T">流式响应元素类型。</typeparam>
    /// <param name="methodName">RPC 方法名称。</param>
    /// <param name="args">调用参数。</param>
    /// <returns>流式响应的异步可枚举序列。</returns>
    protected async Task<IAsyncEnumerable<T>> duplex_stream<T>(string methodName, params object?[] args)
    {
        var payload = build_request_payload(methodName, args);
        var content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        var response = await _http_client.PostAsync($"/{_service_name}/{methodName}/duplex", content);
        response.EnsureSuccessStatusCode();

        return stream_response<T>(response);
    }

    /// <summary>
    ///     构建 RPC 请求的 JSON 负载。
    /// </summary>
    /// <param name="methodName">方法名称。</param>
    /// <param name="args">参数数组。</param>
    /// <returns>JSON 格式的请求负载。</returns>
    private static string build_request_payload(string methodName, object?[] args)
    {
        return JsonSerializer.Serialize(new
        {
            method = methodName,
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