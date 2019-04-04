namespace Hermes.Rpc.Middleware.Versioning;

/// <summary>
///     RPC 版本兼容中间件——处理 Schema 演化时的向前兼容
/// </summary>
public sealed class VersioningMiddleware : IHermesRpcMiddleware
{
    private readonly VersioningOptions _options;

    public VersioningMiddleware(VersioningOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task HandleAsync(HttpContext context, Func<Task> next)
    {
        var clientVersion = ExtractClientVersion(context);
        var serverVersion = _options.CurrentVersion;

        if (clientVersion != null && clientVersion > serverVersion)
        {
            context.Response.StatusCode = 422;
            await context.Response.WriteAsync($"客户端版本 {clientVersion} 高于服务端版本 {serverVersion}，请升级服务端");
            return;
        }

        context.Response.Headers["X-Api-Version"] = serverVersion.ToString();

        if (clientVersion != null && clientVersion < serverVersion)
        {
            context.Items["ClientVersion"] = clientVersion;
            context.Items["IsDeprecated"] = _options.IsDeprecated(clientVersion);
        }

        await next();
    }

    private static Version? ExtractClientVersion(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Api-Version", out var versionHeader))
            return Version.TryParse(versionHeader.ToString(), out var v) ? v : null;

        if (context.Request.Query.TryGetValue("api-version", out var queryVersion))
            return Version.TryParse(queryVersion.ToString(), out var v) ? v : null;

        return null;
    }
}

/// <summary>
///     版本兼容选项
/// </summary>
public sealed class VersioningOptions
{
    /// <summary>
    ///     当前服务端版本
    /// </summary>
    public Version CurrentVersion { get; set; } = new(1, 0);

    /// <summary>
    ///     已弃用的版本列表
    /// </summary>
    public List<Version> DeprecatedVersions { get; set; } = [];

    /// <summary>
    ///     判断版本是否已弃用
    /// </summary>
    public bool IsDeprecated(Version version)
    {
        return DeprecatedVersions.Contains(version);
    }
}