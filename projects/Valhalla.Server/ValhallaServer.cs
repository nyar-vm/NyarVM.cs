using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Valhalla.Config;
using Valhalla.Server.Auth;
using Valhalla.Server.Storage;

namespace Valhalla.Server;

/// <summary>
///     瓦尓哈拉服务端入口，基于 Kestrel 的 HTTP 服务器
/// </summary>
public class ValhallaServer
{
    private WebApplication? _app;

    /// <summary>
    ///     创建瓦尓哈拉服务端
    /// </summary>
    /// <param name="config">服务端配置</param>
    public ValhallaServer(ValhallaConfig config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        storage = create_storage(config);
    }

    /// <summary>
    ///     获取存储后端
    /// </summary>
    public IStorage storage { get; }

    /// <summary>
    ///     获取服务端配置
    /// </summary>
    public ValhallaConfig config { get; }

    /// <summary>
    ///     启动服务器
    /// </summary>
    public async Task start(string[]? args = null)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS",
            $"http://0.0.0.0:{config.port}");

        var builder = WebApplication.CreateBuilder(args ?? []);

        var trustedKeys = load_trusted_keys();

        var app = builder.Build();

        // CORS 中间件
        if (config.cors.origins.Count > 0)
            app.UseCors(policy =>
            {
                policy.WithOrigins([.. config.cors.origins])
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

        // 速率限制中间件
        app.UseMiddleware<RateLimitMiddleware>(100, 60);

        // 请求日志中间件
        app.Use(async (context, next) =>
        {
            var startTime = DateTime.UtcNow;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var requestId = Guid.NewGuid().ToString("N")[..8];

            context.Response.Headers.Append("X-Request-Id", requestId);
            context.Response.Headers.Append("X-Powered-By", "Valhalla");

            await next(context);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;

            if (statusCode >= 500)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (statusCode >= 400) Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine($"[{requestId}] {method} {path} → {statusCode} ({elapsed:F0}ms)");
            Console.ResetColor();
        });

        // 全局异常处理中间件
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json; charset=utf-8";
                var errorResponse = ValhallaErrorResponse.internal_error("服务器内部错误",
                    new Dictionary<string, string> { ["exception"] = ex.GetType().Name });
                var json = JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(json);
            }
        });

        // Ed25519 认证中间件
        if (config.pubkey_required && trustedKeys.Count > 0) app.UseMiddleware<Ed25519AuthMiddleware>(trustedKeys);

        // 健康检查
        app.MapGet("/health", () => Results.Json(new
        {
            status = "healthy",
            config.name,
            version = "0.1.0",
            storage = config.storage.ToString().ToLowerInvariant(),
            uptime = Environment.TickCount64 / 1000
        }));

        // 就绪检查（包含存储后端连通性）
        app.MapGet("/ready", async (IStorage storage) =>
        {
            try
            {
                await storage.list("", CancellationToken.None);
                return Results.Json(new { status = "ready" });
            }
            catch
            {
                return Results.Json(new { status = "not_ready" }, statusCode: 503);
            }
        });

        // 注册 API 路由
        app.map_api_routes(storage);

        _app = app;

        print_startup_banner();
        await app.RunAsync();
    }

    /// <summary>
    ///     停止服务器
    /// </summary>
    public async Task stop()
    {
        if (_app is not null) await _app.StopAsync();
    }

    #region 静态入口

    /// <summary>
    ///     从命令行参数启动服务端
    /// </summary>
    public static async Task Main(string[] args)
    {
        var configPath = "./valhalla.von";

        // 解析 --config 和 --port 参数
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--config" && i + 1 < args.Length)
                configPath = args[++i];

        ValhallaConfig config;
        if (File.Exists(configPath))
        {
            config = await ValhallaConfig.load(configPath);
        }
        else
        {
            Console.WriteLine($"配置文件 {configPath} 不存在，使用默认配置");
            config = ValhallaConfig.create_default();
        }

        // --port 覆盖配置中的端口
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--port" && i + 1 < args.Length
                                    && int.TryParse(args[++i], out var port))
                config.port = port;

        var server = new ValhallaServer(config);
        await server.start(args);
    }

    #endregion

    #region 私有方法

    /// <summary>
    ///     打印启动横幅
    /// </summary>
    private void print_startup_banner()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ╔═══════════════════════════════════════╗");
        Console.WriteLine("  ║     ⚔  Valhalla Registry Server      ║");
        Console.WriteLine("  ╚═══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"  名称：{config.name}");
        Console.WriteLine($"  端口：{config.port}");
        Console.WriteLine($"  存储：{config.storage} ({config.storage_path})");
        Console.WriteLine($"  认证：{(config.pubkey_required ? "Ed25519 公钥认证" : "开放模式")}");
        Console.WriteLine($"  注册：{config.registration}");
        Console.WriteLine($"  CORS：{(config.cors.origins.Count > 0 ? string.Join(", ", config.cors.origins) : "未配置")}");
        Console.WriteLine();
        Console.WriteLine($"  端点：http://0.0.0.0:{config.port}/api/packages");
        Console.WriteLine($"  健康检查：http://0.0.0.0:{config.port}/health");
        Console.WriteLine($"  就绪检查：http://0.0.0.0:{config.port}/ready");
        Console.WriteLine();
        Console.WriteLine("  按 Ctrl+C 停止服务器");
        Console.WriteLine();
    }

    /// <summary>
    ///     根据配置创建存储后端
    /// </summary>
    private static IStorage create_storage(ValhallaConfig config)
    {
        return config.storage switch
        {
            StorageBackend.s3 => new S3Storage(
                config.storage_path,
                config.s3?.endpoint ?? string.Empty,
                config.s3?.region ?? "us-east-1"),
            _ => new LocalStorage(config.storage_path)
        };
    }

    /// <summary>
    ///     加载可信 Ed25519 公钥
    /// </summary>
    private Dictionary<string, byte[]> load_trusted_keys()
    {
        var keys = new Dictionary<string, byte[]>();
        var keyDir = Path.Combine(config.storage_path, "keys");

        if (!Directory.Exists(keyDir)) return keys;

        foreach (var file in Directory.GetFiles(keyDir, "*.pub"))
            try
            {
                var fingerprint = Path.GetFileNameWithoutExtension(file);
                var keyBytes = File.ReadAllBytes(file);
                keys[fingerprint] = keyBytes;
            }
            catch
            {
                // 跳过无效的密钥文件
            }

        return keys;
    }

    #endregion
}