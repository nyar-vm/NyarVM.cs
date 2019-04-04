using Xunit;

namespace VOA.ToolChain.Tests;

public sealed class VoaRouterTests
{
    [Fact]
    public void CreateRouter_HasEmptyRoutes()
    {
        var router = VoaRouterTestsHelper.create_router();

        Assert.NotNull(router);
        Assert.Empty(router.Routes);
        Assert.False(router.IsCompiled);
    }

    [Fact]
    public void RegisterRoute_AddsRouteToList()
    {
        var router = VoaRouterTestsHelper.create_router();
        router = VoaRouterTestsHelper.register_route(router, "/home", "HomePage");

        Assert.Single(router.Routes);
        Assert.Equal("/home", router.Routes[0].Path);
        Assert.Equal("HomePage", router.Routes[0].Component);
    }

    [Fact]
    public void ExtractParams_StaticSegments_ReturnsEmpty()
    {
        var route = VoaRouterTestsHelper.extract_params("/home/about");

        Assert.Empty(route);
    }

    [Fact]
    public void ExtractParams_DynamicSegments_ReturnsParamNames()
    {
        var route = VoaRouterTestsHelper.extract_params("/user/:id/profile/:section");

        Assert.Equal(2, route.Count);
        Assert.Contains("id", route);
        Assert.Contains("section", route);
    }

    [Fact]
    public void MatchRoute_ExactMatch_ReturnsRoute()
    {
        var router = VoaRouterTestsHelper.create_router();
        router = VoaRouterTestsHelper.register_route(router, "/about", "AboutPage");
        router = VoaRouterTestsHelper.register_route(router, "/", "HomePage");

        var match = VoaRouterTestsHelper.match_route(router, "/about");

        Assert.True(match.Found);
        Assert.Equal("AboutPage", match.Route.Component);
    }

    [Fact]
    public void MatchRoute_NoMatch_ReturnsNotFound()
    {
        var router = VoaRouterTestsHelper.create_router();
        router = VoaRouterTestsHelper.register_route(router, "/home", "HomePage");

        var match = VoaRouterTestsHelper.match_route(router, "/nonexistent");

        Assert.False(match.Found);
    }

    [Fact]
    public void MatchRoute_DynamicSegment_ExtractsParam()
    {
        var router = VoaRouterTestsHelper.create_router();
        router = VoaRouterTestsHelper.register_route(router, "/user/:id", "UserPage");

        var match = VoaRouterTestsHelper.match_route(router, "/user/123");

        Assert.True(match.Found);
        Assert.Equal("123", match.Params["id"]);
    }

    [Fact]
    public void MatchRoute_MultipleParams_ExtractsAll()
    {
        var router = VoaRouterTestsHelper.create_router();
        router = VoaRouterTestsHelper.register_route(router, "/post/:postId/comment/:commentId", "CommentPage");

        var match = VoaRouterTestsHelper.match_route(router, "/post/10/comment/20");

        Assert.True(match.Found);
        Assert.Equal("10", match.Params["postId"]);
        Assert.Equal("20", match.Params["commentId"]);
    }

    [Fact]
    public void MatchRoute_WrongMethod_SkipsRoute()
    {
        var router = VoaRouterTestsHelper.create_router();
        router = VoaRouterTestsHelper.register_route_with_method(router, "/api/data", "ApiData", "POST");

        var match = VoaRouterTestsHelper.match_route(router, "/api/data");

        Assert.False(match.Found);
    }

    [Fact]
    public void MatchRoute_CorrectMethod_MatchesRoute()
    {
        var router = VoaRouterTestsHelper.create_router();
        router = VoaRouterTestsHelper.register_route_with_method(router, "/api/data", "ApiData", "POST");

        var match = VoaRouterTestsHelper.match_route_full(router, "/api/data", "POST");

        Assert.True(match.Found);
        Assert.Equal("ApiData", match.Route.Component);
    }
}