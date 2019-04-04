using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Nyar.Language.Awsl.Asgard.Compiler;
using Nyar.Language.Valkyrie.Config;
using Nyar.Types.Targets;

/* VoaFetchService / VoaFetchCache / FetchResponse 等类型均在本命名空间内 */

namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     VOA 开发服务器，提供 HTTP 静态文件服务和 WebSocket 热重载
/// </summary>
public sealed class VoaDevServer : IDisposable
{
    private readonly VoaBridge _bridge;
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    private readonly AsgardMultiTargetBuilder _builder;
    private readonly VoaProjectConfig _config;
    private readonly VoaErrorOverlay _error_overlay;
    private readonly VoaFetchService _fetch_service;
    private readonly VoaFileWatcher? _file_watcher;
    private readonly string _host;
    private readonly HttpListener _http_listener;
    private readonly Dictionary<string, int> _isr_ttl = new(); /* path → revalidate seconds */
    private readonly Dictionary<string, string> _layout_table = new(); /* pageFilePath → layoutFilePath */
    private readonly int _port;
    private readonly string _project_dir;
    private readonly AwslRenderer _renderer;
    private readonly Dictionary<string, string> _route_table = new(); /* path → pageFilePath */
    private readonly Dictionary<string, DateTime> _ssg_build_times = new(); /* path → buildTime, ISR 过期检测 */
    private readonly AwslSsrRenderer _ssr_renderer;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private AsgardBuildResult? _last_build_result;

    public VoaDevServer(string projectDir, string host, int port, VoaProjectConfig config)
    {
        _project_dir = projectDir;
        _host = host;
        _port = port;
        _config = config;
        _renderer = new AwslRenderer();
        _ssr_renderer = new AwslSsrRenderer();
        _error_overlay = new VoaErrorOverlay();
        _bridge = new VoaBridge();
        _fetch_service = new VoaFetchService();

        _bridge.register_handler("voa.revalidateTag", param =>
        {
            var tag = param.TryGetProperty("tag", out var t) ? t.GetString() : null;
            if (string.IsNullOrEmpty(tag)) throw new ArgumentException("tag 参数缺失");

            var count = _fetch_service.cache.invalidate_by_tag(tag);
            return Task.FromResult<object?>(new Dictionary<string, object?>
            {
                ["invalidated"] = count
            });
        });

        _bridge.register_handler("voa.revalidatePath", param =>
        {
            var path = param.TryGetProperty("path", out var p) ? p.GetString() : null;
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("path 参数缺失");

            var count = _fetch_service.cache.invalidate_by_path(path);
            return Task.FromResult<object?>(new Dictionary<string, object?>
            {
                ["invalidated"] = count
            });
        });

        _bridge.register_handler("voa.cacheStats", _ =>
        {
            var stats = _fetch_service.cache.get_stats();
            return Task.FromResult<object?>(new Dictionary<string, object?>
            {
                ["entries"] = stats.entries,
                ["hits"] = stats.hits,
                ["misses"] = stats.misses,
                ["evictions"] = stats.evictions
            });
        });

        _bridge.register_handler("voa.clearCache", _ =>
        {
            _fetch_service.cache.clear();
            _fetch_service.clear_deduplication_map();
            return Task.FromResult<object?>(new Dictionary<string, object?> { ["cleared"] = true });
        });
        _builder = new AsgardMultiTargetBuilder();

        _http_listener = new HttpListener();
        _http_listener.Prefixes.Add($"http://{host}:{port}/");

        if (config.hot_reload.enabled)
        {
            _file_watcher = new VoaFileWatcher(
                projectDir,
                config.hot_reload.watch,
                config.hot_reload.ignore,
                config.hot_reload.debounce
            );
            _file_watcher.OnFileChanged += handle_file_changed;
            _file_watcher.OnError += handle_compile_error;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        stop();
        _file_watcher?.Dispose();
        _http_listener.Close();
    }

    public event Action<string, string>? OnLog;

    /// <summary>HMR 延迟测量事件，每次热重载完成时触发</summary>
    public event Action<HmrTiming>? OnHmrTiming;

    public async Task start(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _http_listener.Start();

        log("info", $"VOA 开发服务器已启动 → http://{_host}:{_port}/");
        log("info", $"项目目录：{_project_dir}");
        log("info", $"热重载：{(_config.hot_reload.enabled ? "开启" : "关闭")}");

        scan_route_table(_project_dir);

        if (_config.is_frontend)
        {
            if (_config.target == "wasm")
            {
                log("info", "模式：前端开发（VOA → WASM → WebView）");
                rebuild_wasm();
            }
            else
            {
                log("info", "模式：前端开发（AWSL → HTML/CSS/JS）");
            }
        }
        else if (_config.is_backend)
        {
            log("info", "模式：后端开发（API 服务）");
        }

        _file_watcher?.start();

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var context = await _http_listener.GetContextAsync();

                if (_cts.Token.IsCancellationRequested) break;

                _ = handle_request(context, _cts.Token);
            }
        }
        catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void stop()
    {
        _file_watcher?.stop();
        _cts?.Cancel();

        if (_http_listener.IsListening) _http_listener.Stop();

        foreach (var kvp in _clients)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch
            {
            }
        }

        _clients.Clear();
        log("info", "VOA 开发服务器已停止");
    }

    #region HTTP Request Handling

    private async Task handle_request(HttpListenerContext context, CancellationToken ct)
    {
        var path = context.Request.Url?.AbsolutePath ?? "/";
        var method = context.Request.HttpMethod;

        try
        {
            if (context.Request.IsWebSocketRequest)
            {
                await handle_web_socket(context, ct);
                return;
            }

            log("request", $"{method} {path}");

            switch (path)
            {
                case "/__voa_bridge":
                    await handle_bridge_request(context, ct);
                    break;
                case "/__voa_hmr":
                    await handle_hmr_connect(context, ct);
                    break;
                default:
                    await handle_static_file(context, path, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            log("error", $"请求处理失败：{ex.Message}");
            await send_error_response(context, 500, "内部服务器错误", ct);
        }
    }

    private async Task handle_static_file(HttpListenerContext context, string path, CancellationToken ct)
    {
        var sourceDir = Path.Combine(_project_dir, "source");
        var assetsDir = Path.Combine(_project_dir, "assets");
        var distDir = Path.Combine(_project_dir, _config.build.output);

        /* 路由表匹配优先 */
        if (_route_table.TryGetValue(path, out var pageFilePath) && File.Exists(pageFilePath))
        {
            await serve_page_with_layout(context, pageFilePath, ct);
            return;
        }

        /* 根路径特殊处理 */
        if (path is "/" or "/index.html" && _route_table.TryGetValue("/", out var indexFilePath))
        {
            await serve_page_with_layout(context, indexFilePath, ct);
            return;
        }

        string? filePath = null;

        if (path is "/" or "/index.html")
        {
            if (_config.target == "wasm" && _last_build_result is { success: true })
            {
                var htmlFile = _last_build_result.output_files.Find(f => f.EndsWith(".html")) ?? "index.html";
                filePath = Path.Combine(_last_build_result.output_directory, htmlFile);
                if (File.Exists(filePath))
                {
                    await serve_regular_file(context, filePath, ct);
                    return;
                }
            }

            filePath = find_index_file(sourceDir);
        }
        else if (_config.target == "wasm" && _last_build_result is { success: true })
        {
            var wasmDir = _last_build_result.output_directory;
            var wasmPath = Path.Combine(wasmDir, path.TrimStart('/'));
            if (File.Exists(wasmPath))
            {
                await serve_regular_file(context, wasmPath, ct);
                return;
            }

            var relativePath = path.TrimStart('/');
            filePath = find_file(sourceDir, assetsDir, distDir, relativePath);
        }
        else
        {
            var relativePath = path.TrimStart('/');
            filePath = find_file(sourceDir, assetsDir, distDir, relativePath);
        }

        if (filePath is null)
        {
            await send_error_response(context, 404, "文件未找到", ct);
            return;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".awsl")
            await serve_awsl_file(context, filePath, ct);
        else
            await serve_regular_file(context, filePath, ct);
    }

    private async Task serve_awsl_file(HttpListenerContext context, string filePath, CancellationToken ct)
    {
        try
        {
            var source = await File.ReadAllTextAsync(filePath, ct);

            var query = context.Request.Url?.Query ?? "";
            var useSsr = query.Contains("ssr=1") || query.Contains("ssr=true");

            if (useSsr)
            {
                var ssrResult = _ssr_renderer.render_ssr(source, filePath);
                var moduleName = Path.GetFileNameWithoutExtension(filePath);
                var ssrHtml = AwslSsrRenderer.generate_ssr_page(
                    moduleName,
                    [ssrResult],
                    _config.target == "wasm" ? $"{moduleName}.wasm" : null
                );

                if (_config.hot_reload.enabled) ssrHtml = _error_overlay.inject_hmr_script(ssrHtml, _host, _port);

                var ssrBytes = Encoding.UTF8.GetBytes(ssrHtml);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = ssrBytes.Length;
                await context.Response.OutputStream.WriteAsync(ssrBytes, ct);
            }
            else
            {
                var result = _renderer.render(source, filePath);

                var html = result.html;

                if (_config.hot_reload.enabled) html = _error_overlay.inject_hmr_script(html, _host, _port);

                var bytes = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, ct);
            }
        }
        catch (Exception ex)
        {
            var errorHtml = _error_overlay.render_error_page(ex.Message, filePath);
            var bytes = Encoding.UTF8.GetBytes(errorHtml);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.StatusCode = 500;
            await context.Response.OutputStream.WriteAsync(bytes, ct);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private async Task serve_regular_file(HttpListenerContext context, string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            await send_error_response(context, 404, "文件未找到", ct);
            return;
        }

        var contentType = get_content_type(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, ct);

        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    #endregion

    #region WebSocket HMR

    private async Task handle_web_socket(HttpListenerContext context, CancellationToken ct)
    {
        var wsContext = await context.AcceptWebSocketAsync(null);
        var ws = wsContext.WebSocket;
        var clientId = Guid.NewGuid().ToString("N")[..8];

        _clients[clientId] = ws;
        log("hmr", $"客户端连接：{clientId}（共 {_clients.Count} 个）");

        try
        {
            var buffer = new byte[4096];

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, ct);

                if (result.MessageType == WebSocketMessageType.Close) break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await handle_hmr_message(clientId, message, ct);
                }
            }
        }
        catch (WebSocketException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            log("hmr", $"客户端断开：{clientId}（剩余 {_clients.Count} 个）");

            try
            {
                ws.Dispose();
            }
            catch
            {
            }
        }
    }

    private async Task handle_hmr_message(string clientId, string message, CancellationToken ct)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<HmrMessage>(message);
            if (msg is null) return;

            switch (msg.type)
            {
                case "ping":
                    await send_to_client(clientId, new HmrMessage { type = "pong" }, ct);
                    break;
                case "bridge_call":
                    var bridgeResult = await _bridge.handle_call(msg.payload?.ToString() ?? "");
                    await send_to_client(clientId, new HmrMessage
                    {
                        type = "bridge_result",
                        payload = bridgeResult
                    }, ct);
                    break;
            }
        }
        catch (JsonException)
        {
        }
    }

    private async Task broadcast_hmr_update(string filePath, string updateType, CancellationToken ct)
    {
        if (_clients.IsEmpty) return;

        var message = new HmrMessage
        {
            type = "update",
            payload = new
            {
                path = filePath,
                updateType,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var deadClients = new List<string>();

        foreach (var kvp in _clients)
        {
            try
            {
                if (kvp.Value.State == WebSocketState.Open)
                    await kvp.Value.SendAsync(segment, WebSocketMessageType.Text, true, ct);
                else
                    deadClients.Add(kvp.Key);
            }
            catch
            {
                deadClients.Add(kvp.Key);
            }
        }

        foreach (var id in deadClients)
        {
            _clients.TryRemove(id, out _);
        }
    }

    private async Task broadcast_error(string error, string? filePath, CancellationToken ct)
    {
        if (_clients.IsEmpty) return;

        var message = new HmrMessage
        {
            type = "error",
            payload = new
            {
                error,
                filePath,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var kvp in _clients)
        {
            try
            {
                if (kvp.Value.State == WebSocketState.Open)
                    await kvp.Value.SendAsync(segment, WebSocketMessageType.Text, true, ct);
            }
            catch
            {
            }
        }
    }

    private Task send_to_client(string clientId, HmrMessage message, CancellationToken ct)
    {
        if (!_clients.TryGetValue(clientId, out var ws)) return Task.CompletedTask;

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        return ws.State == WebSocketState.Open
            ? ws.SendAsync(segment, WebSocketMessageType.Text, true, ct)
            : Task.CompletedTask;
    }

    #endregion

    #region Bridge

    private async Task handle_bridge_request(HttpListenerContext context, CancellationToken ct)
    {
        if (context.Request.HttpMethod != "POST")
        {
            await send_error_response(context, 405, "方法不允许", ct);
            return;
        }

        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);

        var result = await _bridge.handle_call(body);
        var responseJson = JsonSerializer.Serialize(result);

        var bytes = Encoding.UTF8.GetBytes(responseJson);
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    private async Task handle_hmr_connect(HttpListenerContext context, CancellationToken ct)
    {
        var html = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><title>VOA HMR</title></head>
<body><h1>VOA HMR Endpoint</h1><p>WebSocket 热重载端点</p></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    #endregion

    #region File Change Handling

    private void handle_file_changed(string filePath)
    {
        var relativePath = Path.GetRelativePath(_project_dir, filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var updateType = extension switch
        {
            ".awsl" => "component",
            ".v" => "module",
            ".css" => "style",
            _ => "asset"
        };

        var timing = new HmrTiming
        {
            file_changed_at = DateTime.UtcNow,
            file_path = relativePath
        };

        log("hmr", $"文件变更：{relativePath}（{updateType}）");

        if (_config.target == "wasm" && extension is ".v" or ".awsl")
        {
            timing = timing with { compile_start_at = DateTime.UtcNow };
            rebuild_wasm();
            timing = timing with { compile_end_at = DateTime.UtcNow };

            log("hmr_perf", $"HMR 编译耗时：{timing.compile_ms:F1}ms | 文件：{relativePath}");

            if (timing.compile_ms > 50) log("hmr_warn", $"⚠️ HMR 编译延迟 {timing.compile_ms:F1}ms 超过 50ms 目标");
        }

        _ = broadcast_hmr_update(relativePath, updateType, _cts?.Token ?? CancellationToken.None);

        timing = timing with { broadcast_at = DateTime.UtcNow };
        OnHmrTiming?.Invoke(timing);

        log("hmr_perf", $"HMR 总延迟：{timing.total_ms:F1}ms（检测 {timing.detect_ms:F1}ms + 编译 {timing.compile_ms:F1}ms）");
    }

    private void handle_compile_error(string error, string? filePath)
    {
        log("error", $"编译错误：{error}");
        _ = broadcast_error(error, filePath, _cts?.Token ?? CancellationToken.None);
    }

    #endregion

    #region Utility

    /// <summary>
    ///     扫描 source/pages/ 目录，构建路径→页面文件和 Layout→页面 的映射
    /// </summary>
    private void scan_route_table(string projectDir)
    {
        var pagesDir = Path.Combine(projectDir, "source", "pages");
        if (!Directory.Exists(pagesDir)) return;

        foreach (var file in Directory.GetFiles(pagesDir, "*.awsl", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(pagesDir, file);
            var route = file_path_to_route(relativePath);

            if (string.IsNullOrEmpty(route)) continue;

            _route_table[route] = file;
        }

        log("info", $"路由扫描完成：{_route_table.Count} 个页面");

        /* 为每个页面查找最近的 layout */
        foreach (var kvp in _route_table)
        {
            var layout = find_layout_for_page(pagesDir, kvp.Value);
            if (layout != null) _layout_table[kvp.Value] = layout;
        }

        log("info", $"Layout 映射完成：{_layout_table.Count} 个页面有 Layout");
    }

    /// <summary>
    ///     文件相对路径转路由路径
    /// </summary>
    private static string file_path_to_route(string relativePath)
    {
        var route = relativePath.Replace('\\', '/');
        route = Path.ChangeExtension(route, null);

        if (string.IsNullOrEmpty(route)) return "/";

        /* 处理 index */
        if (route == "index") return "/";

        if (route.EndsWith("/index")) route = route[..^6];

        /* 处理动态参数 [xxx] → :xxx */
        var segments = route.Split('/');
        for (var k = 0; k < segments.Length; k++)
        {
            var seg = segments[k];
            if (seg.StartsWith('[') && seg.EndsWith(']'))
            {
                var inner = seg[1..^1];
                segments[k] = ":" + inner;
            }
        }

        route = "/" + string.Join("/", segments);
        return route.TrimEnd('/');
    }

    /// <summary>
    ///     为页面文件查找最近的 layout 文件（向上遍历目录）
    /// </summary>
    private static string? find_layout_for_page(string pagesDir, string pageFilePath)
    {
        var pageDir = Path.GetDirectoryName(pageFilePath);
        if (pageDir is null) return null;

        /* 从页面所在目录向上查找 layout.awsl */
        while (pageDir != null && pageDir.StartsWith(pagesDir))
        {
            var layoutPath = Path.Combine(pageDir, "layout.awsl");
            if (File.Exists(layoutPath)) return layoutPath;

            if (pageDir == pagesDir) break;

            pageDir = Path.GetDirectoryName(pageDir);
        }

        return null;
    }

    /// <summary>
    ///     渲染页面并包裹 Layout（根据 renderMode 选择 SSR/CSR/ISR 路径）
    /// </summary>
    private async Task serve_page_with_layout(HttpListenerContext context, string pageFilePath,
        CancellationToken ct)
    {
        var requestPath = context.Request.Url?.AbsolutePath ?? "/";
        var renderMode = _config.get_render_mode(requestPath);
        var routeModeConfig = _config.routes?.Find(r => _config.get_render_mode(requestPath) == r.mode);

        try
        {
            var pageSource = await File.ReadAllTextAsync(pageFilePath, ct);
            var effectiveSource = pageSource;

            if (_layout_table.TryGetValue(pageFilePath, out var layoutFilePath) && File.Exists(layoutFilePath))
            {
                var layoutSource = await File.ReadAllTextAsync(layoutFilePath, ct);
                effectiveSource = wrap_with_layout(layoutSource, pageSource);
            }

            string html;
            var contentType = "text/html; charset=utf-8";

            switch (renderMode)
            {
                case "ssr":
                    /* 请求级 SSR：构造请求数据字典传入渲染器 */
                    var requestData = new Dictionary<string, object>
                    {
                        ["path"] = requestPath,
                        ["query"] = context.Request.Url?.Query ?? "",
                        ["params"] = extract_route_params(requestPath, pageFilePath)
                    };
                    var ssrResult = _ssr_renderer.render_ssr(effectiveSource, pageFilePath, requestData);
                    var moduleName = Path.GetFileNameWithoutExtension(pageFilePath);
                    html = AwslSsrRenderer.generate_ssr_page(
                        moduleName,
                        [ssrResult],
                        _config.target == "wasm" ? $"{moduleName}.wasm" : null
                    );
                    break;

                case "isr":
                    /* ISR：检查是否过期，过期则后台重建 */
                    var ttl = routeModeConfig?.revalidate ?? 60;
                    _isr_ttl[requestPath] = ttl;

                    if (_ssg_build_times.TryGetValue(requestPath, out var buildTime))
                    {
                        var age = (DateTime.UtcNow - buildTime).TotalSeconds;
                        if (age > ttl)
                            /* 后台异步重建 */
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var result = _renderer.render(effectiveSource, pageFilePath);
                                    _ssg_build_times[requestPath] = DateTime.UtcNow;
                                    log("info", $"ISR 后台重建完成：{requestPath}");
                                }
                                catch (Exception ex)
                                {
                                    log("warn", $"ISR 重建失败：{requestPath} — {ex.Message}");
                                }
                            }, CancellationToken.None);

                        /* 注入 Cache-Control 头：stale-while-revalidate */
                        context.Response.Headers.Add("Cache-Control",
                            $"public, s-maxage={ttl}, stale-while-revalidate");
                    }
                    else
                    {
                        _ssg_build_times[requestPath] = DateTime.UtcNow;
                    }

                    /* 首次请求走 CSR 渲染并缓存构建时间 */
                    var isrRender = _renderer.render(effectiveSource, pageFilePath);
                    html = isrRender.html;
                    break;

                default: /* csr */
                    var csrResult = _renderer.render(effectiveSource, pageFilePath);
                    html = csrResult.html;
                    break;
            }

            if (_config.hot_reload.enabled) html = _error_overlay.inject_hmr_script(html, _host, _port);

            var bytes = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, ct);
        }
        catch (Exception ex)
        {
            var errorHtml = _error_overlay.render_error_page(ex.Message, pageFilePath);
            var bytes = Encoding.UTF8.GetBytes(errorHtml);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.StatusCode = 500;
            await context.Response.OutputStream.WriteAsync(bytes, ct);
        }
        finally
        {
            context.Response.Close();
        }
    }

    /// <summary>
    ///     将页面内容注入到 Layout 的 slot 占位符中
    /// </summary>
    private static string wrap_with_layout(string layoutSource, string pageSource)
    {
        /* 将 slot 节点替换为页面 widget 内容 */
        /* 提取页面 widget 标签内容（不含 widget 标签本身） */
        var pageBody = extract_widget_body(pageSource);

        /* 替换 layout 中的 slot */
        return layoutSource.Replace("<slot />", pageBody)
            .Replace("<slot/>", pageBody)
            .Replace("<slot></slot>", pageBody);
    }

    /// <summary>
    ///     提取 widget 标签内的主体内容（去除 widget 标签）
    /// </summary>
    private static string extract_widget_body(string source)
    {
        var widgetStart = source.IndexOf("<widget>", StringComparison.Ordinal);
        if (widgetStart < 0) return source;

        /* 找到 widget props 属性的结束 */
        var afterStart = source.IndexOf('>', widgetStart);
        if (afterStart < 0) return source;

        var widgetEnd = source.IndexOf("</widget>", afterStart, StringComparison.Ordinal);
        if (widgetEnd < 0) return source;

        var body = source[(afterStart + 1)..widgetEnd];
        return body.Trim();
    }

    /// <summary>
    ///     从请求路径中提取动态路由参数（如 /blog/hello → {slug: "hello"}）
    /// </summary>
    private static Dictionary<string, object> extract_route_params(string requestPath, string pageFilePath)
    {
        var result = new Dictionary<string, object>();

        var pagesDir = Path.GetDirectoryName(pageFilePath);
        if (pagesDir is null) return result;

        /* 将文件路径转换为路由模式（file-router.v 的逆操作） */
        var fileName = Path.GetFileNameWithoutExtension(pageFilePath);
        var routeSegments = requestPath.Trim('/').Split('/');
        var patternSegments = fileName.Split('_'); /* 用 _ 模拟 [id] pattern */

        for (var i = 0; i < routeSegments.Length && i < patternSegments.Length; i++)
        {
            var seg = routeSegments[i];
            var pat = patternSegments[i];

            if (pat.StartsWith('[') && pat.EndsWith(']'))
            {
                var paramName = pat[1..^1];
                result[paramName] = seg;
            }
        }

        /* 也存入原始路径段列表供 catch-all 使用 */
        result["_segments"] = routeSegments;

        return result;
    }

    private void rebuild_wasm()
    {
        var outputDir = Path.Combine(_project_dir, _config.build.output);
        var config = _config.build ?? new VoaBuildConfig();
        config.app_name ??= _config.name;
        config.target_mode = TargetMode.dev;
        var result = _builder.build(config, _project_dir, outputDir, "wasm", false, TargetMode.dev);

        if (result.success)
        {
            _last_build_result = result;
            log("info", $"WASM 重新编译完成（{result.output_files.Count} 个产出文件）");
        }
        else
        {
            log("error", $"WASM 编译失败：{result.error}");
        }
    }

    private string? find_index_file(string sourceDir)
    {
        var candidates = new[] { "index.awsl", "index.html", "app.awsl", "app.html" };

        foreach (var candidate in candidates)
        {
            var path = Path.Combine(sourceDir, candidate);
            if (File.Exists(path)) return path;
        }

        var pagesDir = Path.Combine(sourceDir, "pages");
        if (Directory.Exists(pagesDir))
            foreach (var candidate in candidates)
            {
                var path = Path.Combine(pagesDir, candidate);
                if (File.Exists(path)) return path;
            }

        return null;
    }

    private static string? find_file(string sourceDir, string assetsDir, string distDir, string relativePath)
    {
        var paths = new[]
        {
            Path.Combine(sourceDir, relativePath),
            Path.Combine(assetsDir, relativePath),
            Path.Combine(distDir, relativePath)
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return path;
        }

        var awslPath = Path.Combine(sourceDir, Path.ChangeExtension(relativePath, ".awsl"));
        if (File.Exists(awslPath)) return awslPath;

        return null;
    }

    private static string get_content_type(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" => "text/html; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".mjs" => "application/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".wasm" => "application/wasm",
            ".wat" => "text/plain; charset=utf-8",
            ".map" => "application/json; charset=utf-8",
            ".xml" => "application/xml; charset=utf-8",
            _ => "application/octet-stream"
        };
    }

    private async Task send_error_response(HttpListenerContext context, int statusCode, string message,
        CancellationToken ct)
    {
        var errorHtml = _error_overlay.render_error_page(message, null, statusCode);
        var bytes = Encoding.UTF8.GetBytes(errorHtml);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, ct);
        context.Response.Close();
    }

    private void log(string category, string message)
    {
        OnLog?.Invoke(category, message);
    }

    #endregion
}
