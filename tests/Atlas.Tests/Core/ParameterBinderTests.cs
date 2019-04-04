using Std.App.Server.Core;
using Xunit;

namespace Atlas.Tests.Core;

/// <summary>
///     参数绑定器测试
/// </summary>
public sealed class ParameterBinderTests
{
    [Fact]
    public void BindParameters_RouteParam_BindsCorrectly()
    {
        var context = new RouteContext(new HttpRequest
        {
            method = HttpMethod.get,
            path = "/users/42"
        });
        context.request.route_params["id"] = "42";

        var method = typeof(TestBindingController).GetMethod(nameof(TestBindingController.get_by_route))!;
        var args = ParameterBinder.bind_parameters(method.GetParameters(), context);

        Assert.Equal(42, args[0]);
    }

    [Fact]
    public void BindParameters_QueryParam_BindsCorrectly()
    {
        var context = new RouteContext(new HttpRequest
        {
            method = HttpMethod.get,
            path = "/users"
        });
        context.request.query_string = "page=5";

        var method = typeof(TestBindingController).GetMethod(nameof(TestBindingController.get_by_query))!;
        var args = ParameterBinder.bind_parameters(method.GetParameters(), context);

        Assert.Equal(5, args[0]);
    }

    [Fact]
    public void BindParameters_StringParam_BindsCorrectly()
    {
        var context = new RouteContext(new HttpRequest
        {
            method = HttpMethod.get,
            path = "/users/search"
        });
        context.request.query_string = "keyword=test";

        var method = typeof(TestBindingController).GetMethod(nameof(TestBindingController.search))!;
        var args = ParameterBinder.bind_parameters(method.GetParameters(), context);

        Assert.Equal("test", args[0]);
    }

    [Fact]
    public void BindParameters_MissingParam_ReturnsDefault()
    {
        var context = new RouteContext(new HttpRequest
        {
            method = HttpMethod.get,
            path = "/users"
        });

        var method = typeof(TestBindingController).GetMethod(nameof(TestBindingController.get_by_route))!;
        var args = ParameterBinder.bind_parameters(method.GetParameters(), context);

        Assert.Equal(0, args[0]);
    }

    [Fact]
    public void BindParameters_BooleanParam_BindsCorrectly()
    {
        var context = new RouteContext(new HttpRequest
        {
            method = HttpMethod.get,
            path = "/users"
        });
        context.request.query_string = "active=true";

        var method = typeof(TestBindingController).GetMethod(nameof(TestBindingController.get_active))!;
        var args = ParameterBinder.bind_parameters(method.GetParameters(), context);

        Assert.True((bool)args[0]!);
    }

    [Fact]
    public void BindParameters_HttpRequest_Injected()
    {
        var context = new RouteContext(new HttpRequest
        {
            method = HttpMethod.get,
            path = "/users"
        });

        var method = typeof(TestBindingController).GetMethod(nameof(TestBindingController.with_request))!;
        var args = ParameterBinder.bind_parameters(method.GetParameters(), context);

        Assert.Same(context.request, args[0]);
    }
}

/// <summary>
///     参数绑定测试用控制器
/// </summary>
public sealed class TestBindingController : IController
{
    public void get_by_route(int id)
    {
    }

    public void get_by_query(int page)
    {
    }

    public void search(string keyword)
    {
    }

    public void get_active(bool active)
    {
    }

    public void with_request(HttpRequest request)
    {
    }
}