using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Std.App.Server.Attributes;
using Std.App.Server.Core;
using Xunit;

namespace Atlas.Tests.Core;

/// <summary>
///     控制器扫描器测试
/// </summary>
public sealed class ControllerScannerTests
{
    [Fact]
    public void ScanAndRegister_FindsControllerWithRoute()
    {
        var router = new Router();
        var services = new ServiceCollection().BuildServiceProvider();

        ControllerScanner.scan_and_register(router, typeof(TestUserController).Assembly, services);

        var request = new HttpRequest
        {
            method = HttpMethod.get,
            path = "/api/users"
        };

        var handler = router.match(request);
        Assert.True(handler.IsSome);
    }

    [Fact]
    public void ScanAndRegister_HandlesPostRoute()
    {
        var router = new Router();
        var services = new ServiceCollection().BuildServiceProvider();

        ControllerScanner.scan_and_register(router, typeof(TestUserController).Assembly, services);

        var request = new HttpRequest
        {
            method = HttpMethod.post,
            path = "/api/users"
        };

        var handler = router.match(request);
        Assert.True(handler.IsSome);
    }

    [Fact]
    public void ScanAndRegister_HandlesRouteParameter()
    {
        var router = new Router();
        var services = new ServiceCollection().BuildServiceProvider();

        ControllerScanner.scan_and_register(router, typeof(TestUserController).Assembly, services);

        var request = new HttpRequest
        {
            method = HttpMethod.get,
            path = "/api/users/42"
        };

        var handler = router.match(request);
        Assert.True(handler.IsSome);
        Assert.Equal("42", request.route_params["id"]);
    }

    [Fact]
    public void ScanAndRegister_NoHandlerForNonExistentRoute()
    {
        var router = new Router();
        var services = new ServiceCollection().BuildServiceProvider();

        ControllerScanner.scan_and_register(router, typeof(TestUserController).Assembly, services);

        var request = new HttpRequest
        {
            method = HttpMethod.get,
            path = "/api/nonexistent"
        };

        var handler = router.match(request);
        Assert.False(handler.IsSome);
    }

    [Fact]
    public void ScanAndRegister_IgnoresNonControllerTypes()
    {
        var router = new Router();
        var services = new ServiceCollection().BuildServiceProvider();

        ControllerScanner.scan_and_register(router, typeof(TestUserController).Assembly, services);

        var request = new HttpRequest
        {
            method = HttpMethod.get,
            path = "/not-a-controller"
        };

        var handler = router.match(request);
        Assert.False(handler.IsSome);
    }
}

/// <summary>
///     测试用控制器，使用 <see cref="WebRequestMethods.Http" /> 命名空间下的特性标记
/// </summary>
[RoutePrefix("/api/users")]
public sealed class TestUserController : IController
{
    [Get("")]
    public string list()
    {
        return "[]";
    }

    [Get(":id")]
    public string get_by_id()
    {
        return "{}";
    }

    [Post("")]
    public string create()
    {
        return "created";
    }

    [Put(":id")]
    public string update()
    {
        return "updated";
    }

    [Delete(":id")]
    public string delete()
    {
        return "deleted";
    }
}