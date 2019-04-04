using System.Net;
using System.Text;

namespace Valkyrie.PackageManager.Tests;

/// <summary>
///     测试用的 HTTP 消息处理器，返回预设的响应
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response_content;
    private readonly HttpStatusCode _status_code;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
    {
        _status_code = statusCode;
        _response_content = responseContent;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_status_code)
        {
            Content = new StringContent(_response_content, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}