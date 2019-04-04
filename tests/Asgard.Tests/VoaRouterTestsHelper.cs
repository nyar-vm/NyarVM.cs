namespace VOA.ToolChain.Tests;

internal static class VoaRouterTestsHelper
{
    public static VoaRouter create_router()
    {
        return new VoaRouter();
    }

    public static VoaRouter register_route(VoaRouter router, string path, string component)
    {
        return router with
        {
            Routes = [.. router.Routes, new VoaRoute { Path = path, Component = component, Method = "GET" }]
        };
    }

    public static VoaRouter register_route_with_method(VoaRouter router, string path, string component, string method)
    {
        return router with
        {
            Routes = [.. router.Routes, new VoaRoute { Path = path, Component = component, Method = method }]
        };
    }

    public static VoaRouteMatch match_route(VoaRouter router, string path)
    {
        return match_route_full(router, path, "GET");
    }

    public static VoaRouteMatch match_route_full(VoaRouter router, string path, string method)
    {
        var segs = split_path(path);
        foreach (var route in router.Routes)
        {
            if (!string.IsNullOrEmpty(route.Method) && route.Method != method) continue;

            var routeSegs = split_path(route.Path);
            if (segs.Count != routeSegs.Count) continue;

            var @params = new Dictionary<string, string>();
            var matched = true;
            for (var i = 0; i < segs.Count; i++)
            {
                var pat = routeSegs[i];
                if (pat.StartsWith(":"))
                {
                    @params[pat[1..]] = segs[i];
                }
                else if (pat != segs[i])
                {
                    matched = false;
                    break;
                }
            }

            if (matched) return new VoaRouteMatch { Found = true, Route = route, Params = @params };
        }

        return new VoaRouteMatch { Found = false };
    }

    public static List<string> extract_params(string path)
    {
        var segs = split_path(path);
        return [.. segs.Where(s => s.StartsWith(":")).Select(s => s[1..])];
    }

    private static List<string> split_path(string path)
    {
        if (path == "/") return [];

        return [.. path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)];
    }
}