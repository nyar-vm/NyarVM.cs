using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.RPC;

public sealed class RpcGenerator : IGenerator
{
    public string Name => "rpc";
    public string[] SupportedTargets => ["rpc-client", "rpc-server", "rpc-full", "rpc-sse"];

    public GeneratorResult Generate(GeneratorContext context)
    {
        var result = new GeneratorResult();
        var ns = GetNamespace(context.Options);
        var target = GetTarget(context.Options);
        var serialization = GetSerialization(context.Options);

        foreach (var service in context.Schema.Services)
        {
            if (target is "rpc-client" or "rpc-full")
            {
                result.Files.Add(
                    GenerateClientProxy(service, ns, context.SchemaPath, context.OutputPath, serialization));
                result.Files.Add(GenerateHttpClientFactory(service, ns, context.SchemaPath, context.OutputPath,
                    serialization));
            }

            if (target is "rpc-server" or "rpc-full")
            {
                result.Files.Add(GenerateServerInterface(service, ns, context.SchemaPath, context.OutputPath));
                result.Files.Add(GenerateServerBase(service, ns, context.SchemaPath, context.OutputPath));
                result.Files.Add(GenerateServerController(service, ns, context.SchemaPath, context.OutputPath,
                    serialization));
            }

            if (target is "rpc-sse")
                result.Files.Add(GenerateSseClient(service, ns, context.SchemaPath, context.OutputPath));
        }

        if (target is "rpc-server" or "rpc-full")
        {
            result.Files.Add(GenerateRpcException(ns, context.SchemaPath, context.OutputPath));
            result.Files.Add(GenerateMiddlewareInterface(ns, context.SchemaPath, context.OutputPath));
        }

        if (serialization is "msgpack" or "dual")
            result.Files.Add(GenerateMessagePackSerializerHelper(ns, context.SchemaPath, context.OutputPath));

        return result;
    }

    #region 服务端 Controller 生成

    private GeneratedFile GenerateServerController(ServiceDefinition service, string ns, string schemaPath,
        string outputPath, string serialization)
    {
        var sb = new StringBuilder();
        var controllerName = $"{service.Name}Controller";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {service.Name} 服务端控制器（自动路由分发）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[ApiController]");
        sb.AppendLine($"[Route(\"api/{ToKebabCase(service.Name)}\")]");
        sb.AppendLine($"public sealed class {controllerName} : ControllerBase");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly I{service.Name}Service _service;");
        sb.AppendLine();
        sb.AppendLine($"    public {controllerName}(I{service.Name}Service service)");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var endpoint in service.Endpoints)
        {
            var methodName = ToPascalCase(endpoint.Name);
            var returnTypeName = MapCSharpType(endpoint.ReturnType);
            var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {endpoint.Name}");
            sb.AppendLine("    /// </summary>");

            if (endpoint.IsDeprecated)
            {
                var obsoleteMsg = endpoint.DeprecatedMessage ?? (endpoint.ReplaceWith is not null
                    ? $"请使用 {ToPascalCase(endpoint.ReplaceWith)}Async 替代"
                    : $"端点 {endpoint.Name} 已废弃");
                sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");
            }

            if (endpoint is HttpEndpoint httpEp)
            {
                var httpMethod = httpEp.HttpMethod.ToUpperInvariant();
                var routeAttr = httpMethod switch
                {
                    "GET" => "HttpGet",
                    "POST" => "HttpPost",
                    "PUT" => "HttpPut",
                    "DELETE" => "HttpDelete",
                    "PATCH" => "HttpPatch",
                    _ => "HttpGet"
                };

                var routePath = httpEp.Path ?? $"/{ToKebabCase(endpoint.Name)}";
                sb.AppendLine($"    [{routeAttr}(\"{routePath}\")]");
            }
            else
            {
                sb.AppendLine("    [HttpPost]");
            }

            var returnType = isVoid ? "Task<IActionResult>" : "Task<IActionResult>";

            sb.Append($"    public async {returnType} {methodName}Async(");

            for (var i = 0; i < endpoint.Parameters.Count; i++)
            {
                var param = endpoint.Parameters[i];
                var paramType = MapCSharpType(param.ParameterType);
                var comma = i < endpoint.Parameters.Count - 1 ? ", " : "";

                if (endpoint is HttpEndpoint ep && ep.HttpMethod.ToUpperInvariant() is "POST" or "PUT" or "PATCH" &&
                    i == 0)
                    sb.Append($"[FromBody] {paramType} {param.Name}{comma}");
                else
                    sb.Append($"[FromQuery] {paramType} {param.Name}{comma}");
            }

            var hasCt = endpoint.Parameters.Count > 0;
            if (hasCt)
                sb.Append(", CancellationToken cancellationToken = default)");
            else
                sb.Append("CancellationToken cancellationToken = default)");

            sb.AppendLine();
            sb.AppendLine("    {");
            sb.AppendLine("        try");
            sb.AppendLine("        {");

            var args = string.Join(", ", endpoint.Parameters.Select(p => p.Name)) +
                       (hasCt ? ", cancellationToken" : ", cancellationToken");

            if (isVoid)
            {
                sb.AppendLine($"            await _service.{methodName}Async({args});");
                sb.AppendLine("            return NoContent();");
            }
            else
            {
                sb.AppendLine($"            var result = await _service.{methodName}Async({args});");
                sb.AppendLine("            return Ok(result);");
            }

            sb.AppendLine("        }");
            sb.AppendLine("        catch (HermesRpcException ex)");
            sb.AppendLine("        {");
            sb.AppendLine("            return StatusCode(ex.StatusCode, new { ex.Message, ex.Detail });");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (ArgumentException ex)");
            sb.AppendLine("        {");
            sb.AppendLine("            return BadRequest(new { ex.Message });");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (Exception ex)");
            sb.AppendLine("        {");
            sb.AppendLine("            return StatusCode(500, new { Message = \"内部服务错误\", Detail = ex.Message });");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{controllerName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = controllerName
        };
    }

    #endregion

    #region SSE 客户端生成

    private GeneratedFile GenerateSseClient(ServiceDefinition service, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        var clientName = $"{service.Name}SseClient";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {service.Name} SSE 客户端（Server-Sent Events）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {clientName}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly HttpClient _httpClient;");
        sb.AppendLine("    private readonly JsonSerializerOptions _jsonOptions;");
        sb.AppendLine();
        sb.AppendLine($"    public {clientName}(HttpClient httpClient, JsonSerializerOptions? jsonOptions = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        _httpClient = httpClient;");
        sb.AppendLine(
            "        _jsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var endpoint in service.Endpoints)
        {
            var methodName = ToPascalCase(endpoint.Name);

            if (endpoint.StreamingMode is not StreamingMode.Server and not StreamingMode.Duplex) continue;

            var innerType = MapCSharpType(endpoint.ReturnType);
            var path = endpoint is HttpEndpoint httpEp ? httpEp.Path : $"/api/{ToKebabCase(endpoint.Name)}";

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {endpoint.Name}（SSE 流式订阅）");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public async IAsyncEnumerable<{innerType}> {methodName}StreamAsync(");

            for (var i = 0; i < endpoint.Parameters.Count; i++)
            {
                var param = endpoint.Parameters[i];
                var paramType = MapCSharpType(param.ParameterType);
                var comma = i < endpoint.Parameters.Count - 1 ? "," : "";
                sb.AppendLine($"        {paramType} {param.Name}{comma}");
            }

            if (endpoint.Parameters.Count > 0)
                sb.AppendLine("        [EnumeratorCancellation] CancellationToken cancellationToken = default)");
            else
                sb.AppendLine("        [EnumeratorCancellation] CancellationToken cancellationToken = default)");

            sb.AppendLine("    {");
            sb.AppendLine($"        var url = $\"{path}\";");

            if (endpoint.Parameters.Count > 0)
            {
                sb.AppendLine("        var queryParams = new List<string>();");
                foreach (var param in endpoint.Parameters)
                    sb.AppendLine(
                        $"        if ({param.Name} != null) queryParams.Add($\"{param.Name}={{{param.Name}}}\");");
                sb.AppendLine("        if (queryParams.Count > 0) url += \"?\" + string.Join(\"&\", queryParams);");
            }

            sb.AppendLine();
            sb.AppendLine("        var request = new HttpRequestMessage(HttpMethod.Get, url);");
            sb.AppendLine("        request.Headers.Add(\"Accept\", \"text/event-stream\");");
            sb.AppendLine();
            sb.AppendLine(
                "        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);");
            sb.AppendLine("        response.EnsureSuccessStatusCode();");
            sb.AppendLine();
            sb.AppendLine("        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);");
            sb.AppendLine("        using var reader = new System.IO.StreamReader(stream);");
            sb.AppendLine();
            sb.AppendLine("        var eventData = new StringBuilder();");
            sb.AppendLine("        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)");
            sb.AppendLine("        {");
            sb.AppendLine("            var line = await reader.ReadLineAsync(cancellationToken);");
            sb.AppendLine("            if (line == null) break;");
            sb.AppendLine("            if (string.IsNullOrEmpty(line))");
            sb.AppendLine("            {");
            sb.AppendLine("                if (eventData.Length > 0)");
            sb.AppendLine("                {");
            sb.AppendLine(
                $"                    var item = JsonSerializer.Deserialize<{innerType}>(eventData.ToString(), _jsonOptions);");
            sb.AppendLine("                    if (item != null) yield return item;");
            sb.AppendLine("                    eventData.Clear();");
            sb.AppendLine("                }");
            sb.AppendLine("                continue;");
            sb.AppendLine("            }");
            sb.AppendLine("            if (line.StartsWith(\"data: \"))");
            sb.AppendLine("            {");
            sb.AppendLine("                eventData.AppendLine(line[6..]);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{clientName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = clientName
        };
    }

    #endregion

    #region MessagePack 序列化辅助

    /// <summary>
    ///     生成 RPC 序列化格式枚举和 MessagePack 辅助类。
    /// </summary>
    private static GeneratedFile GenerateMessagePackSerializerHelper(string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 序列化格式");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public enum RpcSerializationFormat");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// JSON 序列化（默认）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    Json = 0,");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// MessagePack 二进制序列化");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    MessagePack = 1,");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 双格式：根据请求 Content-Type 自动选择");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    Dual = 2");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 序列化辅助工具");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class RpcSerializationHelper");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 根据请求 Content-Type 判断序列化格式");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static RpcSerializationFormat DetectFormat(string? contentType)");
        sb.AppendLine("    {");
        sb.AppendLine(
            "        if (contentType is not null && contentType.Contains(\"msgpack\", StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("        {");
        sb.AppendLine("            return RpcSerializationFormat.MessagePack;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return RpcSerializationFormat.Json;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 获取响应 Content-Type");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static string GetContentType(RpcSerializationFormat format)");
        sb.AppendLine("    {");
        sb.AppendLine("        return format == RpcSerializationFormat.MessagePack");
        sb.AppendLine("            ? \"application/x-msgpack\"");
        sb.AppendLine("            : \"application/json\";");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, "RpcSerialization.cs"),
            Content = sb.ToString(),
            Generator = "rpc",
            TypeName = "RpcSerializationHelper"
        };
    }

    #endregion

    #region 客户端代理生成

    private GeneratedFile GenerateClientProxy(ServiceDefinition service, string ns, string schemaPath,
        string outputPath, string serialization)
    {
        var sb = new StringBuilder();
        var proxyName = $"{service.Name}RpcClient";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");

        if (serialization is "msgpack" or "dual") sb.AppendLine("using MessagePack;");

        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {service.Name} RPC 客户端代理");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public sealed class {proxyName}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly HttpClient _httpClient;");
        sb.AppendLine("    private readonly JsonSerializerOptions _jsonOptions;");
        sb.AppendLine("    private readonly string _baseUrl;");
        sb.AppendLine("    private readonly RpcSerializationFormat _serializationFormat;");
        sb.AppendLine();
        sb.AppendLine(
            $"    public {proxyName}(HttpClient httpClient, JsonSerializerOptions? jsonOptions = null, RpcSerializationFormat serializationFormat = RpcSerializationFormat.Json)");
        sb.AppendLine("    {");
        sb.AppendLine("        _httpClient = httpClient;");
        sb.AppendLine("        _baseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;");
        sb.AppendLine(
            "        _jsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };");
        sb.AppendLine("        _serializationFormat = serializationFormat;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 从 WebSocket 流式读取消息");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine(
            "    private async IAsyncEnumerable<T> StreamWebSocketMessages<T>(System.Net.WebSockets.ClientWebSocket ws, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        var buffer = new byte[8192];");
        sb.AppendLine(
            "        while (ws.State == System.Net.WebSockets.WebSocketState.Open && !cancellationToken.IsCancellationRequested)");
        sb.AppendLine("        {");
        sb.AppendLine(
            "            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);");
        sb.AppendLine("            if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)");
        sb.AppendLine("            {");
        sb.AppendLine("                await CloseWebSocketAsync(ws, cancellationToken);");
        sb.AppendLine("                yield break;");
        sb.AppendLine("            }");
        sb.AppendLine("            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);");
        sb.AppendLine("            yield return JsonSerializer.Deserialize<T>(json, _jsonOptions)!;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 从消息队列订阅流");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine(
            "    private async IAsyncEnumerable<T> SubscribeStream<T>(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: 集成 Hermes.Stream.IStreamBackend 订阅");
        sb.AppendLine("        throw new NotImplementedException(\"消息订阅流集成待运行时实现\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 安全关闭 WebSocket");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine(
            "    private static async Task CloseWebSocketAsync(System.Net.WebSockets.ClientWebSocket ws, CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ws.State == System.Net.WebSockets.WebSocketState.Open)");
        sb.AppendLine("        {");
        sb.AppendLine(
            "            await ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var endpoint in service.Endpoints)
        {
            var methodName = ToPascalCase(endpoint.Name);

            if (endpoint is HttpEndpoint httpEp)
                GenerateHttpClientMethod(sb, httpEp, methodName);
            else if (endpoint is GrpcEndpoint grpcEp)
                GenerateGrpcClientMethod(sb, grpcEp, methodName);
            else if (endpoint is WsEndpoint wsEp)
                GenerateWsClientMethod(sb, wsEp, methodName);
            else if (endpoint is MessageEndpoint msgEp) GenerateMessageClientMethod(sb, msgEp, methodName);

            if (endpoint.IsDeprecated && endpoint.ReplaceWith is not null)
                GenerateDeprecatedForwardingMethod(sb, endpoint, methodName);
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{proxyName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = proxyName
        };
    }

    private void GenerateHttpClientMethod(StringBuilder sb, HttpEndpoint endpoint, string methodName)
    {
        var httpMethod = endpoint.HttpMethod.ToUpperInvariant();
        var returnTypeName = MapCSharpType(endpoint.ReturnType);
        var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// {endpoint.Name}");
        sb.AppendLine("    /// </summary>");

        var parameters = endpoint.Parameters;
        foreach (var param in parameters) sb.AppendLine($"    /// <param name=\"{param.Name}\">{param.Name}</param>");

        if (!isVoid) sb.AppendLine($"    /// <returns>{returnTypeName}</returns>");

        if (endpoint.IsDeprecated)
        {
            var obsoleteMsg = endpoint.DeprecatedMessage ?? (endpoint.ReplaceWith is not null
                ? $"请使用 {ToPascalCase(endpoint.ReplaceWith)}Async 替代"
                : $"端点 {endpoint.Name} 已废弃");
            sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");
        }

        var returnType = isVoid ? "Task" : "Task<" + returnTypeName + ">";
        sb.AppendLine($"    public async {returnType} {methodName}Async(");

        for (var i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramType = MapCSharpType(param.ParameterType);
            var comma = i < parameters.Count - 1 ? "," : "";
            sb.AppendLine($"        {paramType} {param.Name}{comma}");
        }

        if (parameters.Count > 0)
            sb.AppendLine("        CancellationToken cancellationToken = default)");
        else
            sb.AppendLine("        CancellationToken cancellationToken = default)");

        sb.AppendLine("    {");

        var path = endpoint.Path ?? $"/api/{ToKebabCase(endpoint.Name)}";
        sb.AppendLine($"        var url = $\"{path}\";");
        sb.AppendLine();

        if (httpMethod is "POST" or "PUT" or "PATCH" && parameters.Count > 0)
        {
            var bodyParam = parameters[0];
            sb.AppendLine("        HttpContent content;");
            sb.AppendLine("        if (_serializationFormat == RpcSerializationFormat.MessagePack)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var msgpackBytes = MessagePackSerializer.Serialize({bodyParam.Name});");
            sb.AppendLine("            content = new ByteArrayContent(msgpackBytes);");
            sb.AppendLine(
                "            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(\"application/x-msgpack\");");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine($"            var json = JsonSerializer.Serialize({bodyParam.Name}, _jsonOptions);");
            sb.AppendLine("            content = new StringContent(json, Encoding.UTF8, \"application/json\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine(
                $"        var response = await _httpClient.{GetHttpMethodName(httpMethod)}Async(url, content, cancellationToken);");
        }
        else
        {
            if (parameters.Count > 0)
            {
                sb.AppendLine("        var queryParams = new List<string>();");
                foreach (var param in parameters)
                    sb.AppendLine(
                        $"        if ({param.Name} != null) queryParams.Add($\"{param.Name}={{{param.Name}}}\");");

                sb.AppendLine("        if (queryParams.Count > 0)");
                sb.AppendLine("        {");
                sb.AppendLine("            url += \"?\" + string.Join(\"&\", queryParams);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine(
                $"        var response = await _httpClient.{GetHttpMethodName(httpMethod)}Async(url, cancellationToken);");
        }

        sb.AppendLine();
        sb.AppendLine("        if (!response.IsSuccessStatusCode)");
        sb.AppendLine("        {");
        sb.AppendLine("            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);");
        sb.AppendLine("            throw new HermesRpcException(");
        sb.AppendLine("                (int)response.StatusCode,");
        sb.AppendLine("                $\"RPC 调用失败: {methodName} — HTTP {(int)response.StatusCode}\",");
        sb.AppendLine("                errorBody);");
        sb.AppendLine("        }");
        sb.AppendLine();

        if (isVoid)
        {
            sb.AppendLine("        await Task.CompletedTask;");
        }
        else
        {
            sb.AppendLine("        if (_serializationFormat == RpcSerializationFormat.MessagePack)");
            sb.AppendLine("        {");
            sb.AppendLine(
                "            var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);");
            if (endpoint.ReturnType is PrimitiveType pt && pt.TypeName == "utf8")
                sb.AppendLine("            return MessagePackSerializer.Deserialize<string>(responseBytes);");
            else
                sb.AppendLine("            return MessagePackSerializer.Deserialize<" + returnTypeName +
                              ">(responseBytes);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);");
            if (endpoint.ReturnType is PrimitiveType pt2 && pt2.TypeName == "utf8")
                sb.AppendLine("        return jsonResponse;");
            else
                sb.AppendLine("        return JsonSerializer.Deserialize<" + returnTypeName +
                              ">(jsonResponse, _jsonOptions)!;");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void GenerateGrpcClientMethod(StringBuilder sb, GrpcEndpoint endpoint, string methodName)
    {
        var returnTypeName = MapCSharpType(endpoint.ReturnType);
        var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";
        var fullReturnType = GetCSharpReturnType(endpoint.ReturnType, endpoint.StreamingMode);

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// {endpoint.Name}（gRPC {GetStreamingModeDescription(endpoint.StreamingMode)}调用）");
        sb.AppendLine("    /// </summary>");

        if (endpoint.IsDeprecated)
        {
            var obsoleteMsg = endpoint.DeprecatedMessage ?? (endpoint.ReplaceWith is not null
                ? $"请使用 {ToPascalCase(endpoint.ReplaceWith)}Async 替代"
                : $"端点 {endpoint.Name} 已废弃");
            sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");
        }

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Unary:
            {
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                GenerateParameterList(sb, endpoint.Parameters);
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                sb.AppendLine("    {");
                sb.AppendLine(
                    $"        // TODO: 集成 Acorn 序列化实现 gRPC 一元调用 — {endpoint.ServiceName}.{endpoint.MethodName}");
                sb.AppendLine("        throw new NotImplementedException(\"gRPC 一元调用运行时集成待实现\");");
                break;
            }
            case StreamingMode.Server:
            {
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                GenerateParameterList(sb, endpoint.Parameters);
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                sb.AppendLine("    {");
                sb.AppendLine($"        // TODO: 集成 gRPC 服务端流 — {endpoint.ServiceName}.{endpoint.MethodName}");
                sb.AppendLine("        throw new NotImplementedException(\"gRPC 服务端流运行时集成待实现\");");
                break;
            }
            case StreamingMode.Client:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                sb.AppendLine($"        IAsyncEnumerable<{requestType}> requests,");
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                sb.AppendLine("    {");
                sb.AppendLine($"        // TODO: 集成 gRPC 客户端流 — {endpoint.ServiceName}.{endpoint.MethodName}");
                sb.AppendLine("        throw new NotImplementedException(\"gRPC 客户端流运行时集成待实现\");");
                break;
            }
            case StreamingMode.Duplex:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                sb.AppendLine($"        IAsyncEnumerable<{requestType}> requests,");
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                sb.AppendLine("    {");
                sb.AppendLine($"        // TODO: 集成 gRPC 双向流 — {endpoint.ServiceName}.{endpoint.MethodName}");
                sb.AppendLine("        throw new NotImplementedException(\"gRPC 双向流运行时集成待实现\");");
                break;
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void GenerateWsClientMethod(StringBuilder sb, WsEndpoint endpoint, string methodName)
    {
        var returnTypeName = MapCSharpType(endpoint.ReturnType);
        var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";
        var fullReturnType = GetCSharpReturnType(endpoint.ReturnType, endpoint.StreamingMode);

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// {endpoint.Name}（WebSocket {GetStreamingModeDescription(endpoint.StreamingMode)}调用）");
        sb.AppendLine("    /// </summary>");

        if (endpoint.IsDeprecated)
        {
            var obsoleteMsg = endpoint.DeprecatedMessage ?? (endpoint.ReplaceWith is not null
                ? $"请使用 {ToPascalCase(endpoint.ReplaceWith)}Async 替代"
                : $"端点 {endpoint.Name} 已废弃");
            sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");
        }

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Unary:
            {
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                GenerateParameterList(sb, endpoint.Parameters);
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
            case StreamingMode.Server:
            {
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                GenerateParameterList(sb, endpoint.Parameters);
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
            case StreamingMode.Client:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                sb.AppendLine($"        IAsyncEnumerable<{requestType}> requests,");
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
            case StreamingMode.Duplex:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                sb.AppendLine($"        IAsyncEnumerable<{requestType}> requests,");
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
        }

        sb.AppendLine("    {");
        sb.AppendLine($"        var wsUrl = _baseUrl + \"{endpoint.Path ?? $"/ws/{ToKebabCase(endpoint.Name)}"}\";");
        sb.AppendLine("        var ws = new System.Net.WebSockets.ClientWebSocket();");
        sb.AppendLine("        await ws.ConnectAsync(new Uri(wsUrl), cancellationToken);");
        sb.AppendLine();

        if (endpoint.StreamingMode is StreamingMode.Client or StreamingMode.Duplex)
        {
            var requestType = endpoint.Parameters.Count > 0
                ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                : "object";
            sb.AppendLine("        var sendTask = Task.Run(async () =>");
            sb.AppendLine("        {");
            sb.AppendLine("            await foreach (var item in requests.WithCancellation(cancellationToken))");
            sb.AppendLine("            {");
            sb.AppendLine("                var json = JsonSerializer.Serialize(item, _jsonOptions);");
            sb.AppendLine("                var bytes = Encoding.UTF8.GetBytes(json);");
            sb.AppendLine(
                "                await ws.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken);");
            sb.AppendLine("            }");
            sb.AppendLine("        }, cancellationToken);");
            sb.AppendLine();
        }
        else if (endpoint.Parameters.Count > 0)
        {
            sb.AppendLine("        var connectMsg = JsonSerializer.Serialize(new");
            sb.AppendLine("        {");
            foreach (var param in endpoint.Parameters) sb.AppendLine($"            {param.Name} = {param.Name},");
            sb.AppendLine("        }, _jsonOptions);");
            sb.AppendLine("        var connectBytes = Encoding.UTF8.GetBytes(connectMsg);");
            sb.AppendLine(
                "        await ws.SendAsync(new ArraySegment<byte>(connectBytes), System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken);");
            sb.AppendLine();
        }

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Server or StreamingMode.Duplex:
                sb.AppendLine("        return StreamWebSocketMessages(ws, cancellationToken);");
                break;
            case StreamingMode.Client:
            {
                if (isVoid)
                {
                    sb.AppendLine("        await sendTask;");
                    sb.AppendLine("        await CloseWebSocketAsync(ws, cancellationToken);");
                }
                else
                {
                    sb.AppendLine("        await sendTask;");
                    sb.AppendLine("        var buffer = new byte[8192];");
                    sb.AppendLine(
                        "        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);");
                    sb.AppendLine("        await CloseWebSocketAsync(ws, cancellationToken);");
                    sb.AppendLine("        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);");
                    sb.AppendLine($"        return JsonSerializer.Deserialize<{returnTypeName}>(json, _jsonOptions)!;");
                }

                break;
            }
            default:
            {
                if (isVoid)
                {
                    sb.AppendLine("        await CloseWebSocketAsync(ws, cancellationToken);");
                }
                else
                {
                    sb.AppendLine("        var buffer = new byte[8192];");
                    sb.AppendLine(
                        "        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);");
                    sb.AppendLine("        await CloseWebSocketAsync(ws, cancellationToken);");
                    sb.AppendLine("        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);");
                    sb.AppendLine($"        return JsonSerializer.Deserialize<{returnTypeName}>(json, _jsonOptions)!;");
                }

                break;
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void GenerateMessageClientMethod(StringBuilder sb, MessageEndpoint endpoint, string methodName)
    {
        var returnTypeName = MapCSharpType(endpoint.ReturnType);
        var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";
        var fullReturnType = GetCSharpReturnType(endpoint.ReturnType, endpoint.StreamingMode);

        sb.AppendLine("    /// <summary>");
        sb.AppendLine(
            $"    /// {endpoint.Name}（消息队列 {GetStreamingModeDescription(endpoint.StreamingMode)}调用 — topic: {endpoint.Topic}）");
        sb.AppendLine("    /// </summary>");

        if (endpoint.IsDeprecated)
        {
            var obsoleteMsg = endpoint.DeprecatedMessage ?? (endpoint.ReplaceWith is not null
                ? $"请使用 {ToPascalCase(endpoint.ReplaceWith)}Async 替代"
                : $"端点 {endpoint.Name} 已废弃");
            sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");
        }

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Unary:
            {
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                GenerateParameterList(sb, endpoint.Parameters);
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
            case StreamingMode.Server:
            {
                sb.AppendLine($"    public {fullReturnType} {methodName}Async(");
                GenerateParameterList(sb, endpoint.Parameters);
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
            case StreamingMode.Client:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine($"    public async {fullReturnType} {methodName}Async(");
                sb.AppendLine($"        IAsyncEnumerable<{requestType}> requests,");
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
            case StreamingMode.Duplex:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine($"    public {fullReturnType} {methodName}Async(");
                sb.AppendLine($"        IAsyncEnumerable<{requestType}> requests,");
                sb.AppendLine("        CancellationToken cancellationToken = default)");
                break;
            }
        }

        sb.AppendLine("    {");

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Server:
            {
                var innerType = MapCSharpType(endpoint.ReturnType);
                sb.AppendLine($"        return SubscribeStream<{innerType}>(\"{endpoint.Topic}\", cancellationToken);");
                break;
            }
            case StreamingMode.Client:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine("        await foreach (var item in requests.WithCancellation(cancellationToken))");
                sb.AppendLine("        {");
                sb.AppendLine("            var message = JsonSerializer.Serialize(item, _jsonOptions);");
                sb.AppendLine($"            // TODO: 集成 Hermes.Stream.IStreamBackend 发布消息到 topic: {endpoint.Topic}");
                sb.AppendLine("        }");
                if (!isVoid)
                {
                    sb.AppendLine("        // TODO: 等待聚合响应");
                    sb.AppendLine("        throw new NotImplementedException(\"消息队列客户端流聚合响应待实现\");");
                }

                break;
            }
            case StreamingMode.Duplex:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapCSharpType(endpoint.Parameters[0].ParameterType)
                    : "object";
                sb.AppendLine($"        // TODO: 集成 Hermes.Stream 实现双向流 — topic: {endpoint.Topic}");
                sb.AppendLine("        throw new NotImplementedException(\"消息队列双向流运行时集成待实现\");");
                break;
            }
            default:
            {
                sb.AppendLine($"        // 发布消息到 topic: {endpoint.Topic}");
                sb.AppendLine("        var message = JsonSerializer.Serialize(new");
                sb.AppendLine("        {");
                foreach (var param in endpoint.Parameters) sb.AppendLine($"            {param.Name} = {param.Name},");
                sb.AppendLine("        }, _jsonOptions);");
                sb.AppendLine();
                sb.AppendLine("        // TODO: 集成 Hermes.Stream.IStreamBackend 发布消息");
                sb.AppendLine("        throw new NotImplementedException(\"消息发布集成待运行时实现\");");
                break;
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private GeneratedFile GenerateHttpClientFactory(ServiceDefinition service, string ns, string schemaPath,
        string outputPath, string serialization)
    {
        var sb = new StringBuilder();
        var factoryName = $"{service.Name}RpcClientFactory";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {service.Name} RPC 客户端工厂");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {factoryName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public static {service.Name}RpcClient Create(HttpClient httpClient)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {service.Name}RpcClient(httpClient);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine(
            $"    public static {service.Name}RpcClient Create(HttpClient httpClient, JsonSerializerOptions jsonOptions)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {service.Name}RpcClient(httpClient, jsonOptions);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine(
            $"    public static {service.Name}RpcClient Create(HttpClient httpClient, RpcSerializationFormat serializationFormat)");
        sb.AppendLine("    {");
        sb.AppendLine(
            $"        return new {service.Name}RpcClient(httpClient, serializationFormat: serializationFormat);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine(
            $"    public static {service.Name}RpcClient Create(string baseUrl, JsonSerializerOptions? jsonOptions = null, RpcSerializationFormat serializationFormat = RpcSerializationFormat.Json)");
        sb.AppendLine("    {");
        sb.AppendLine("        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };");
        sb.AppendLine("        return new " + service.Name +
                      "RpcClient(httpClient, jsonOptions, serializationFormat);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{factoryName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = factoryName
        };
    }

    /// <summary>
    ///     生成参数列表
    /// </summary>
    private static void GenerateParameterList(StringBuilder sb, IReadOnlyList<ParameterDefinition> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramType = MapCSharpType(param.ParameterType);
            var comma = i < parameters.Count - 1 ? "," : "";
            sb.AppendLine($"        {paramType} {param.Name}{comma}");
        }
    }

    /// <summary>
    ///     获取流式模式的中文描述
    /// </summary>
    private static string GetStreamingModeDescription(StreamingMode mode)
    {
        return mode switch
        {
            StreamingMode.Unary => "一元",
            StreamingMode.Server => "服务端流",
            StreamingMode.Client => "客户端流",
            StreamingMode.Duplex => "双向流",
            _ => ""
        };
    }

    /// <summary>
    ///     根据流式模式获取 C# 返回类型声明
    /// </summary>
    private static string GetCSharpReturnType(SchemaType? returnType, StreamingMode streamingMode)
    {
        var baseType = MapCSharpType(returnType);
        var isVoid = returnType == null || returnType.TypeName == "unit";

        return streamingMode switch
        {
            StreamingMode.Unary => isVoid ? "Task" : $"Task<{baseType}>",
            StreamingMode.Server => $"IAsyncEnumerable<{baseType}>",
            StreamingMode.Client => isVoid ? "Task" : $"Task<{baseType}>",
            StreamingMode.Duplex => $"IAsyncEnumerable<{baseType}>",
            _ => isVoid ? "Task" : $"Task<{baseType}>"
        };
    }

    #endregion

    #region 服务端骨架生成

    private GeneratedFile GenerateServerInterface(ServiceDefinition service, string ns, string schemaPath,
        string outputPath)
    {
        var sb = new StringBuilder();
        var interfaceName = $"I{service.Name}Service";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {service.Name} 服务接口");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public interface {interfaceName}");
        sb.AppendLine("{");

        foreach (var endpoint in service.Endpoints)
        {
            var methodName = ToPascalCase(endpoint.Name);
            var returnTypeName = MapCSharpType(endpoint.ReturnType);
            var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";

            var returnType = isVoid ? "Task" : "Task<" + returnTypeName + ">";

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {endpoint.Name}");
            sb.AppendLine("    /// </summary>");

            if (endpoint.IsDeprecated)
            {
                var obsoleteMsg = endpoint.DeprecatedMessage ?? (endpoint.ReplaceWith is not null
                    ? $"请使用 {ToPascalCase(endpoint.ReplaceWith)}Async 替代"
                    : $"端点 {endpoint.Name} 已废弃");
                sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");
            }

            sb.Append($"    {returnType} {methodName}Async(");

            for (var i = 0; i < endpoint.Parameters.Count; i++)
            {
                var param = endpoint.Parameters[i];
                var paramType = MapCSharpType(param.ParameterType);
                var comma = i < endpoint.Parameters.Count - 1 ? ", " : "";
                sb.Append($"{paramType} {param.Name}{comma}");
            }

            if (endpoint.Parameters.Count > 0)
                sb.Append(", CancellationToken cancellationToken = default);");
            else
                sb.Append("CancellationToken cancellationToken = default);");

            sb.AppendLine();
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{interfaceName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = interfaceName
        };
    }

    private GeneratedFile GenerateServerBase(ServiceDefinition service, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        var baseName = $"{service.Name}ServiceBase";

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {service.Name} 服务基类（提供默认实现）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public abstract class {baseName} : I{service.Name}Service");
        sb.AppendLine("{");

        foreach (var endpoint in service.Endpoints)
        {
            var methodName = ToPascalCase(endpoint.Name);
            var returnTypeName = MapCSharpType(endpoint.ReturnType);
            var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";

            var returnType = isVoid ? "Task" : "Task<" + returnTypeName + ">";

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {endpoint.Name} 的默认实现");
            sb.AppendLine("    /// </summary>");

            if (endpoint.IsDeprecated)
            {
                var obsoleteMsg = endpoint.DeprecatedMessage ?? (endpoint.ReplaceWith is not null
                    ? $"请使用 {ToPascalCase(endpoint.ReplaceWith)}Async 替代"
                    : $"端点 {endpoint.Name} 已废弃");
                sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");
            }

            sb.Append($"    public virtual {returnType} {methodName}Async(");

            for (var i = 0; i < endpoint.Parameters.Count; i++)
            {
                var param = endpoint.Parameters[i];
                var paramType = MapCSharpType(param.ParameterType);
                var comma = i < endpoint.Parameters.Count - 1 ? ", " : "";
                sb.Append($"{paramType} {param.Name}{comma}");
            }

            if (endpoint.Parameters.Count > 0)
                sb.Append(", CancellationToken cancellationToken = default)");
            else
                sb.Append("CancellationToken cancellationToken = default)");

            sb.AppendLine();
            sb.AppendLine("    {");
            sb.AppendLine("        throw new NotImplementedException($\"{GetType().Name} 未实现 {methodName}\");");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{baseName}.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = baseName
        };
    }

    #endregion

    #region RPC 异常 + 中间件

    private GeneratedFile GenerateRpcException(string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Hermes RPC 异常基类");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class HermesRpcException : Exception");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// HTTP 状态码");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public int StatusCode { get; }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 错误详情");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public string? Detail { get; }");
        sb.AppendLine();
        sb.AppendLine("    public HermesRpcException(int statusCode, string message, string? detail = null)");
        sb.AppendLine("        : base(message)");
        sb.AppendLine("    {");
        sb.AppendLine("        StatusCode = statusCode;");
        sb.AppendLine("        Detail = detail;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine(
            "    public HermesRpcException(int statusCode, string message, Exception innerException, string? detail = null)");
        sb.AppendLine("        : base(message, innerException)");
        sb.AppendLine("    {");
        sb.AppendLine("        StatusCode = statusCode;");
        sb.AppendLine("        Detail = detail;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 参数验证异常");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class HermesRpcValidationException : HermesRpcException");
        sb.AppendLine("{");
        sb.AppendLine("    public IReadOnlyDictionary<string, string[]> Errors { get; }");
        sb.AppendLine();
        sb.AppendLine("    public HermesRpcValidationException(IReadOnlyDictionary<string, string[]> errors)");
        sb.AppendLine("        : base(400, \"参数验证失败\")");
        sb.AppendLine("    {");
        sb.AppendLine("        Errors = errors;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 未授权异常");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class HermesRpcUnauthorizedException : HermesRpcException");
        sb.AppendLine("{");
        sb.AppendLine("    public HermesRpcUnauthorizedException(string message = \"未授权访问\")");
        sb.AppendLine("        : base(401, message) { }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 禁止访问异常");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class HermesRpcForbiddenException : HermesRpcException");
        sb.AppendLine("{");
        sb.AppendLine("    public HermesRpcForbiddenException(string message = \"禁止访问\")");
        sb.AppendLine("        : base(403, message) { }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 资源未找到异常");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class HermesRpcNotFoundException : HermesRpcException");
        sb.AppendLine("{");
        sb.AppendLine("    public HermesRpcNotFoundException(string resource, object key)");
        sb.AppendLine("        : base(404, $\"资源 {resource}({key}) 不存在\") { }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, "HermesRpcException.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = "HermesRpcException"
        };
    }

    private GeneratedFile GenerateMiddlewareInterface(string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 中间件接口");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public interface IHermesRpcMiddleware");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 处理 RPC 请求");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    Task HandleAsync(HttpContext context, Func<Task> next);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// RPC 中间件管道构建器");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed class HermesRpcMiddlewareBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly List<Func<RequestDelegate, RequestDelegate>> _components = new();");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 添加中间件");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine(
            "    public HermesRpcMiddlewareBuilder Use<TMiddleware>() where TMiddleware : IHermesRpcMiddleware, new()");
        sb.AppendLine("    {");
        sb.AppendLine("        _components.Add(next =>");
        sb.AppendLine("        {");
        sb.AppendLine("            var middleware = new TMiddleware();");
        sb.AppendLine("            return async context =>");
        sb.AppendLine("            {");
        sb.AppendLine("                await middleware.HandleAsync(context, () => next(context));");
        sb.AppendLine("            };");
        sb.AppendLine("        });");
        sb.AppendLine("        return this;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 构建请求委托");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public RequestDelegate Build(RequestDelegate terminal)");
        sb.AppendLine("    {");
        sb.AppendLine("        var app = terminal;");
        sb.AppendLine("        for (var i = _components.Count - 1; i >= 0; i--)");
        sb.AppendLine("        {");
        sb.AppendLine("            app = _components[i](app);");
        sb.AppendLine("        }");
        sb.AppendLine("        return app;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, "HermesRpcMiddleware.cs"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = "HermesRpcMiddleware"
        };
    }

    #endregion

    #region 类型映射

    private static string MapCSharpType(SchemaType? type)
    {
        if (type == null) return "void";

        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => "sbyte",
                "i16" => "short",
                "i32" => "int",
                "i64" => "long",
                "u8" => "byte",
                "u16" => "ushort",
                "u32" => "uint",
                "u64" => "ulong",
                "f32" => "float",
                "f64" => "double",
                "bool" => "bool",
                "utf8" => "string",
                "utf16" => "string",
                "uuid" => "Guid",
                "unit" => "void",
                "object" => "object",
                "datetime" => "DateTime",
                "decimal" => "decimal",
                _ => p.TypeName
            },
            ListType l => "List<" + MapCSharpType(l.ElementType) + ">",
            ArrayType a => MapCSharpType(a.ElementType) + "[]",
            OptionType o => MapCSharpType(o.InnerType) + "?",
            DictType d => "Dictionary<" + MapCSharpType(d.KeyType) + ", " + MapCSharpType(d.ValueType) + ">",
            ResultType r => "Result<" + MapCSharpType(r.OkType) + ", " +
                            MapCSharpType(r.ErrorType ?? PrimitiveType.Utf8) + ">",
            StreamType s => "IAsyncEnumerable<" + MapCSharpType(s.InnerType) + ">",
            NamedType n => n.Name,
            ReferenceType r => "Ref<" + MapCSharpType(r.ReferencedType) + ">",
            _ => type.TypeName
        };
    }

    private static string MapCSharpType(ParameterDefinition? param)
    {
        return param == null ? "object" : MapCSharpType(param.ParameterType);
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     为废弃端点生成转发方法——旧方法签名调用新端点方法
    /// </summary>
    private static void GenerateDeprecatedForwardingMethod(StringBuilder sb, EndpointDefinition endpoint,
        string oldMethodName)
    {
        var newMethodName = ToPascalCase(endpoint.ReplaceWith!);
        var returnTypeName = MapCSharpType(endpoint.ReturnType);
        var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";
        var returnType = isVoid ? "Task" : "Task<" + returnTypeName + ">";

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// {endpoint.Name}（已废弃，转发到 {endpoint.ReplaceWith}）");
        sb.AppendLine("    /// </summary>");

        var obsoleteMsg = endpoint.DeprecatedMessage ?? $"请使用 {newMethodName}Async 替代";
        sb.AppendLine($"    [System.Obsolete(\"{obsoleteMsg}\")]");

        sb.Append($"    public async {returnType} {oldMethodName}Async(");

        for (var i = 0; i < endpoint.Parameters.Count; i++)
        {
            var param = endpoint.Parameters[i];
            var paramType = MapCSharpType(param.ParameterType);
            var comma = i < endpoint.Parameters.Count - 1 ? ", " : "";
            sb.Append($"{paramType} {param.Name}{comma}");
        }

        if (endpoint.Parameters.Count > 0)
            sb.Append(", CancellationToken cancellationToken = default)");
        else
            sb.Append("CancellationToken cancellationToken = default)");

        sb.AppendLine();
        sb.AppendLine("    {");

        var args = string.Join(", ", endpoint.Parameters.Select(p => p.Name));
        if (isVoid)
            sb.AppendLine($"        await {newMethodName}Async({args}, cancellationToken);");
        else
            sb.AppendLine($"        return await {newMethodName}Async({args}, cancellationToken);");

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string GenerateFileHeader(string schemaPath, string outputPath)
    {
        return $"// <auto-generated> hermes rpc 命令自动生成 — 请勿手动编辑 </auto-generated>\n" +
               $"// 来源: {schemaPath}\n" +
               $"// 时间: {DateTime.UtcNow:O}\n";
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var parts = name.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p =>
            p.Length > 0
                ? char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..] : "")
                : ""));
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('-');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string GetHttpMethodName(string method)
    {
        return method switch
        {
            "GET" => "Get",
            "POST" => "Post",
            "PUT" => "Put",
            "DELETE" => "Delete",
            "PATCH" => "Patch",
            _ => "Get"
        };
    }

    private static string GetNamespace(IReadOnlyDictionary<string, object> options)
    {
        return options.TryGetValue("namespace", out var ns) ? ns.ToString()! : "Hermes.Generated";
    }

    private static string GetTarget(IReadOnlyDictionary<string, object> options)
    {
        return options.TryGetValue("targets", out var t) ? t.ToString()! : "rpc-client";
    }

    private static string GetSerialization(IReadOnlyDictionary<string, object> options)
    {
        return options.TryGetValue("serialization", out var s) ? s.ToString()! : "json";
    }

    #endregion
}