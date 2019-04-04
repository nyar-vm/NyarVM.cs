using Microsoft.Extensions.DependencyInjection;
using Std.App.Server.Core;
using Xunit;

namespace Atlas.Tests.Core;

/// <summary>
///     AtlasHost 构建器测试
/// </summary>
public sealed class AtlasHostTests
{
    [Fact]
    public void Build_WithControllers_RegistersRoutes()
    {
        var host = new AtlasHost();
        host.use_controllers<TestUserController>();
        host.use_port(9999);

        var netApp = host.build();

        var request = new HttpRequest
        {
            method = HttpMethod.get,
            path = "/api/users"
        };

        var handler = host.router.match(request);
        Assert.True(handler.IsSome);
    }

    [Fact]
    public void Build_WithoutControllers_EmptyRouter()
    {
        var host = new AtlasHost();
        host.use_port(3000);

        var netApp = host.build();

        var request = new HttpRequest
        {
            method = HttpMethod.get,
            path = "/api/anything"
        };

        var handler = host.router.match(request);
        Assert.False(handler.IsSome);
    }

    [Fact]
    public void Build_CalledTwice_UsesSameConfig()
    {
        var host = new AtlasHost();
        host.use_controllers<TestUserController>();
        host.use_port(4000);

        var app1 = host.build();
        var app2 = host.build();

        Assert.NotNull(app1);
        Assert.NotNull(app2);
    }

    [Fact]
    public void UseServices_WithConfigure_ProvidesServices()
    {
        var host = new AtlasHost();
        host.use_services(services => { services.AddTransient<TestUserController>(); });

        var netApp = host.build();
        Assert.NotNull(netApp);
    }
}