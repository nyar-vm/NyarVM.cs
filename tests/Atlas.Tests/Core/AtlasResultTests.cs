using System.Net;
using Std.App.Server.Core;
using Xunit;

namespace Atlas.Tests.Core;

/// <summary>
///     AtlasResult 响应结果测试
/// </summary>
public sealed class AtlasResultTests
{
    [Fact]
    public void ok_NoBody_SetsStatusCode200()
    {
        var result = AtlasResult.ok();
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.ok, context.response.status_code);
    }

    [Fact]
    public void ok_WithObject_ReturnsJson()
    {
        var result = AtlasResult.ok(new { name = "test", age = 30 });
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.ok, context.response.status_code);
        Assert.Contains("application/json",
            context.response.headers.TryGetValue("Content-Type", out var ct) ? ct : string.Empty);
    }

    [Fact]
    public void created_Returns201()
    {
        var result = AtlasResult.created(new { id = 1 });
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.created, context.response.status_code);
    }

    [Fact]
    public void no_content_Returns204()
    {
        var result = AtlasResult.no_content();
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.no_content, context.response.status_code);
    }

    [Fact]
    public void bad_request_Returns400WithJson()
    {
        var result = AtlasResult.bad_request("无效输入");
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.bad_request, context.response.status_code);
    }

    [Fact]
    public void unauthorized_Returns401()
    {
        var result = AtlasResult.unauthorized();
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.unauthorized, context.response.status_code);
    }

    [Fact]
    public void forbidden_Returns403()
    {
        var result = AtlasResult.forbidden();
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.forbidden, context.response.status_code);
    }

    [Fact]
    public void not_found_Returns404()
    {
        var result = AtlasResult.not_found();
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.not_found, context.response.status_code);
    }

    [Fact]
    public void internal_error_Returns500()
    {
        var result = AtlasResult.internal_error();
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.internal_server_error, context.response.status_code);
    }

    [Fact]
    public void json_WithStatusCode_ReturnsCorrectCode()
    {
        var result = AtlasResult.json(HttpStatusCode.created, new { id = 99 });
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.created, context.response.status_code);
        Assert.Contains("application/json",
            context.response.headers.TryGetValue("Content-Type", out var ct2) ? ct2 : string.Empty);
    }

    [Fact]
    public void text_ReturnsPlainText()
    {
        var result = AtlasResult.text(HttpStatusCode.ok, "Hello World");
        var context = new RouteContext(new HttpRequest());

        result.apply_to(context);

        Assert.Equal(HttpStatusCode.ok, context.response.status_code);
        Assert.Contains("text/plain",
            context.response.headers.TryGetValue("Content-Type", out var ct3) ? ct3 : string.Empty);
    }
}