using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[HttpClient]</c> 特性的接口自动生成 HTTP 客户端实现类，
///     根据方法上的 <c>[Get]</c>、<c>[Post]</c>、<c>[Put]</c>、<c>[Delete]</c>、<c>[Patch]</c>
///     特性生成对应的 HTTP 请求调用代码。
/// </summary>
[Generator]
public sealed class HttpClientGenerator : IIncrementalGenerator
{
    private const string _http_client_attribute_full_name = "Sonic.Standard.Net.Http.HttpClientAttribute";
    private const string _get_attribute_full_name = "Sonic.Standard.Net.Http.GetAttribute";
    private const string _post_attribute_full_name = "Sonic.Standard.Net.Http.PostAttribute";
    private const string _put_attribute_full_name = "Sonic.Standard.Net.Http.PutAttribute";
    private const string _delete_attribute_full_name = "Sonic.Standard.Net.Http.DeleteAttribute";
    private const string _patch_attribute_full_name = "Sonic.Standard.Net.Http.PatchAttribute";
    private const string _body_attribute_full_name = "Sonic.Standard.Net.Http.BodyAttribute";
    private const string _query_attribute_full_name = "Sonic.Standard.Net.Http.QueryAttribute";
    private const string _header_attribute_full_name = "Sonic.Standard.Net.Http.HeaderAttribute";
    private const string _from_route_attribute_full_name = "Sonic.Standard.Net.Http.FromRouteAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetInterfaces = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _http_client_attribute_full_name,
                static (node, _) => node is InterfaceDeclarationSyntax,
                static (ctx, ct) => transform_http_client(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetInterfaces, generate_source);
    }

    /// <summary>
    ///     提取标记了 <c>[HttpClient]</c> 特性的接口信息。
    /// </summary>
    private static HttpClientTypeInfo? transform_http_client(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var httpAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_http_client_attribute_full_name}");

        if (httpAttr is null) return null;

        string? baseUrl = null;
        string? configurationSection = null;
        var timeout = 30;

        foreach (var named in httpAttr.NamedArguments)
        {
            if (named is { Key: "base_url", Value.Value: string bu }) baseUrl = bu;

            if (named is { Key: "configuration_section", Value.Value: string cs }) configurationSection = cs;

            if (named is { Key: "timeout", Value.Value: int t }) timeout = t;
        }

        var methods = extract_methods(typeSymbol);

        if (methods.Count == 0) return null;

        return new HttpClientTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            baseUrl,
            configurationSection,
            timeout,
            methods);
    }

    /// <summary>
    ///     提取接口中标记了 HTTP 方法特性的方法信息列表。
    /// </summary>
    private static List<HttpMethodInfo> extract_methods(INamedTypeSymbol typeSymbol)
    {
        var methods = new List<HttpMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol methodSymbol) continue;

            var httpMethod = detect_http_method(methodSymbol);
            if (httpMethod is null) continue;

            var template = extract_template(methodSymbol, httpMethod);
            var returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isAsync = is_type(methodSymbol.ReturnType);
            var actualReturnType = extract_actual_return_type(methodSymbol.ReturnType, isAsync);
            var parameters = extract_method_parameters(methodSymbol);
            var headers = extract_method_headers(methodSymbol);

            methods.Add(new HttpMethodInfo(
                methodSymbol.Name,
                httpMethod,
                template,
                returnType,
                actualReturnType,
                isAsync,
                parameters,
                headers));
        }

        return methods;
    }

    /// <summary>
    ///     检测方法上的 HTTP 方法特性类型。
    /// </summary>
    private static string? detect_http_method(IMethodSymbol methodSymbol)
    {
        foreach (var attr in methodSymbol.GetAttributes())
        {
            var fullName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == $"global::{_get_attribute_full_name}") return "GET";

            if (fullName == $"global::{_post_attribute_full_name}") return "POST";

            if (fullName == $"global::{_put_attribute_full_name}") return "PUT";

            if (fullName == $"global::{_delete_attribute_full_name}") return "DELETE";

            if (fullName == $"global::{_patch_attribute_full_name}") return "PATCH";
        }

        return null;
    }

    /// <summary>
    ///     从 HTTP 方法特性中提取路由模板。
    /// </summary>
    private static string extract_template(IMethodSymbol methodSymbol, string httpMethod)
    {
        var attrName = httpMethod switch
        {
            "GET" => _get_attribute_full_name,
            "POST" => _post_attribute_full_name,
            "PUT" => _put_attribute_full_name,
            "DELETE" => _delete_attribute_full_name,
            "PATCH" => _patch_attribute_full_name,
            _ => ""
        };

        foreach (var attr in methodSymbol.GetAttributes())
        {
            var fullName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == $"global::{attrName}" && attr.ConstructorArguments.Length > 0)
                if (attr.ConstructorArguments[0].Value is string template)
                    return template;
        }

        return "/";
    }

    /// <summary>
    ///     判断返回类型是否为异步类型。
    /// </summary>
    private static bool is_type(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType) return namedType.Name is "Task" or "ValueTask";

        return false;
    }

    /// <summary>
    ///     提取异步返回类型中的实际返回类型。
    /// </summary>
    private static string extract_actual_return_type(ITypeSymbol type, bool isAsync)
    {
        if (!isAsync) return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
            return namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return "global::System.Threading.Tasks.Task";
    }

    /// <summary>
    ///     提取方法的参数信息列表，包含参数绑定方式。
    /// </summary>
    private static List<HttpParameterInfo> extract_method_parameters(IMethodSymbol methodSymbol)
    {
        var parameters = new List<HttpParameterInfo>();

        foreach (var param in methodSymbol.Parameters)
        {
            var binding = HttpParameterBinding.none;
            string? queryName = null;
            string? routeName = null;

            foreach (var attr in param.GetAttributes())
            {
                var fullName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (fullName == $"global::{_body_attribute_full_name}") binding = HttpParameterBinding.body;

                if (fullName == $"global::{_query_attribute_full_name}")
                {
                    binding = HttpParameterBinding.query;

                    foreach (var named in attr.NamedArguments)
                        if (named is { Key: "name", Value.Value: string n })
                            queryName = n;

                    if (queryName is null) queryName = param.Name;
                }

                if (fullName == $"global::{_header_attribute_full_name}")
                {
                    binding = HttpParameterBinding.header;

                    if (attr.ConstructorArguments.Length >= 2) routeName = attr.ConstructorArguments[0].Value as string;
                }

                if (fullName == $"global::{_from_route_attribute_full_name}")
                {
                    binding = HttpParameterBinding.route;

                    foreach (var named in attr.NamedArguments)
                        if (named is { Key: "Name", Value.Value: string n })
                            routeName = n;

                    if (routeName is null) routeName = param.Name;
                }
            }

            if (binding == HttpParameterBinding.none) binding = HttpParameterBinding.query;

            if (binding == HttpParameterBinding.query && queryName is null) queryName = param.Name;

            if (binding == HttpParameterBinding.route && routeName is null) routeName = param.Name;

            parameters.Add(new HttpParameterInfo(
                param.Name,
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                binding,
                queryName,
                routeName));
        }

        return parameters;
    }

    /// <summary>
    ///     提取方法级别的 Header 特性列表。
    /// </summary>
    private static List<HttpHeaderInfo> extract_method_headers(IMethodSymbol methodSymbol)
    {
        var headers = new List<HttpHeaderInfo>();

        foreach (var attr in methodSymbol.GetAttributes())
        {
            var fullName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == $"global::{_header_attribute_full_name}" && attr.ConstructorArguments.Length >= 2)
            {
                var name = attr.ConstructorArguments[0].Value as string;
                var value = attr.ConstructorArguments[1].Value as string;

                if (name is not null && value is not null) headers.Add(new HttpHeaderInfo(name, value));
            }
        }

        return headers;
    }

    /// <summary>
    ///     生成所有 HTTP 客户端接口的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<HttpClientTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_http_client_source(info);
            var hintName = $"{info.interface_name}HttpClient.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个 HTTP 客户端实现类的源代码。
    /// </summary>
    private static string generate_http_client_source(HttpClientTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System.Net.Http;");
        sb.append_line("using System.Threading;");
        sb.append_line("using System.Threading.Tasks;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        var implName = $"{info.interface_name}Client";

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.interface_name}</c> 的 HTTP 客户端实现。");
        sb.append_line("/// </summary>");
        sb.append_line($"public sealed class {implName} : {info.fully_qualified_name}");
        using (sb.block())
        {
            generate_constructor(sb, info, implName);
            sb.append_line();

            for (var i = 0; i < info.methods.Count; i++)
            {
                if (i > 0) sb.append_line();

                generate_http_method(sb, info.methods[i], info);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 HTTP 客户端的构造函数。
    /// </summary>
    private static void generate_constructor(SourceTextBuilder sb, HttpClientTypeInfo info, string implName)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 初始化 <c>{implName}</c> 的新实例。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"httpClient\">HTTP 客户端实例。</param>");
        sb.append_line($"public {implName}(global::System.Net.Http.HttpClient httpClient)");
        using (sb.block())
        {
            sb.append_line("_httpClient = httpClient;");

            if (info.base_url is not null)
                sb.append_line(
                    $"_httpClient.BaseAddress = new global::System.Uri(\"{StringEscapeHelper.escape_for_string(info.base_url)}\");");

            sb.append_line($"_httpClient.Timeout = global::System.TimeSpan.FromSeconds({info.timeout});");
        }
    }

    /// <summary>
    ///     生成单个 HTTP 方法的实现代码。
    /// </summary>
    private static void generate_http_method(SourceTextBuilder sb, HttpMethodInfo method, HttpClientTypeInfo info)
    {
        var paramList = build_parameter_list(method.parameters);
        var returnType = method.return_type;

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{method.method_name}</c> 的 HTTP {method.http_method} 实现。");
        sb.append_line("/// </summary>");

        foreach (var param in method.parameters)
            sb.append_line($"/// <param name=\"{param.name}\">{param.name} 参数。</param>");

        sb.append_line($"public {returnType} {method.method_name}({paramList})");
        using (sb.block())
        {
            generate_url_building(sb, method);
            sb.append_line();

            var hasBody = method.http_method is "POST" or "PUT" or "PATCH";
            var bodyParamIndex = method.parameters.FindIndex(p => p.binding == HttpParameterBinding.body);
            var hasBodyParam = hasBody && bodyParamIndex >= 0;

            if (hasBodyParam)
            {
                var bodyParamName = method.parameters[bodyParamIndex].name;
                sb.append_line($"var __json = global::System.Text.Json.JsonSerializer.Serialize({bodyParamName});");
                sb.append_line(
                    "var __content = new global::System.Net.Http.StringContent(__json, global::System.Text.Encoding.UTF8, \"application/json\");");
                sb.append_line(
                    $"var __request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.{method.http_method}, __url) {{ Content = __content }};");
            }
            else
            {
                sb.append_line(
                    $"var __request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.{method.http_method}, __url);");
            }

            sb.append_line();

            foreach (var header in method.headers)
                sb.append_line(
                    $"__request.Headers.Add(\"{StringEscapeHelper.escape_for_string(header.name)}\", \"{StringEscapeHelper.escape_for_string(header.value)}\");");

            foreach (var param in method.parameters)
                if (param is { binding: HttpParameterBinding.header, header_name: not null })
                    sb.append_line(
                        $"__request.Headers.Add(\"{StringEscapeHelper.escape_for_string(param.header_name)}\", {param.name}?.ToString());");

            sb.append_line();

            if (method.is_async)
            {
                sb.append_line("using var __response = await _httpClient.SendAsync(__request);");
                sb.append_line("__response.EnsureSuccessStatusCode();");
                sb.append_line();

                if (method.actual_return_type != "global::System.Threading.Tasks.Task")
                {
                    sb.append_line("var __responseBody = await __response.Content.ReadAsStringAsync();");
                    sb.append_line(
                        $"return global::System.Text.Json.JsonSerializer.Deserialize<{method.actual_return_type}>(__responseBody)!;");
                }
                else
                {
                    sb.append_line("await __response.Content.ReadAsStringAsync();");
                }
            }
            else
            {
                sb.append_line("using var __response = _httpClient.SendAsync(__request).GetAwaiter().GetResult();");
                sb.append_line("__response.EnsureSuccessStatusCode();");
                sb.append_line();

                if (method.actual_return_type != "global::System.Void")
                {
                    sb.append_line(
                        "var __responseBody = __response.Content.ReadAsStringAsync().GetAwaiter().GetResult();");
                    sb.append_line(
                        $"return global::System.Text.Json.JsonSerializer.Deserialize<{method.actual_return_type}>(__responseBody)!;");
                }
            }
        }
    }

    /// <summary>
    ///     生成 URL 构建代码，包含路由参数替换和查询字符串拼接。
    /// </summary>
    private static void generate_url_building(SourceTextBuilder sb, HttpMethodInfo method)
    {
        var template = method.template;
        var routeParams = method.parameters.Where(p => p.binding == HttpParameterBinding.route).ToList();
        var queryParams = method.parameters.Where(p => p.binding == HttpParameterBinding.query).ToList();

        if (routeParams.Count > 0)
            sb.append_line($"var __url = $\"{StringEscapeHelper.escape_for_string(template)}\";");
        else
            sb.append_line($"var __url = \"{StringEscapeHelper.escape_for_string(template)}\";");

        if (queryParams.Count > 0)
        {
            sb.append_line("var __queryBuilder = new global::System.Text.StringBuilder();");

            foreach (var param in queryParams)
            {
                var queryName = param.query_name ?? param.name;
                sb.append_line($"if ({param.name} is not null)");
                using (sb.block())
                {
                    sb.append_line("if (__queryBuilder.Length > 0) __queryBuilder.Append('&');");
                    sb.append_line(
                        $"__queryBuilder.Append(\"{StringEscapeHelper.escape_for_string(queryName)}=\").Append(global::System.Uri.EscapeDataString({param.name}?.ToString() ?? \"\"));");
                }
            }

            sb.append_line("if (__queryBuilder.Length > 0)");
            using (sb.block())
            {
                sb.append_line("__url += \"?\" + __queryBuilder.ToString();");
            }
        }
    }

    /// <summary>
    ///     构建方法参数列表字符串。
    /// </summary>
    private static string build_parameter_list(List<HttpParameterInfo> parameters)
    {
        var parts = new List<string>();

        foreach (var param in parameters) parts.Add($"{param.Type} {param.name}");

        return string.Join(", ", parts);
    }

    #region 数据模型

    /// <summary>
    ///     需要生成 HTTP 客户端的接口信息。
    /// </summary>
    internal readonly struct HttpClientTypeInfo
    {
        /// <summary>
        ///     接口名称。
        /// </summary>
        public readonly string interface_name;

        /// <summary>
        ///     接口完全限定名称。
        /// </summary>
        public readonly string fully_qualified_name;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     基础 URL 地址。
        /// </summary>
        public readonly string? base_url;

        /// <summary>
        ///     配置节名称。
        /// </summary>
        public readonly string? configuration_section;

        /// <summary>
        ///     请求超时时间（秒）。
        /// </summary>
        public readonly int timeout;

        /// <summary>
        ///     HTTP 方法列表。
        /// </summary>
        public readonly List<HttpMethodInfo> methods;

        /// <summary>
        ///     初始化 <see cref="HttpClientTypeInfo" /> 的新实例。
        /// </summary>
        public HttpClientTypeInfo(
            string interfaceName,
            string fullyQualifiedName,
            string? namespaceName,
            string? baseUrl,
            string? configurationSection,
            int timeout,
            List<HttpMethodInfo> methods)
        {
            interface_name = interfaceName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            base_url = baseUrl;
            configuration_section = configurationSection;
            this.timeout = timeout;
            this.methods = methods;
        }
    }

    /// <summary>
    ///     HTTP 方法信息，包含路由模板和参数绑定。
    /// </summary>
    internal readonly struct HttpMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     HTTP 方法（GET/POST/PUT/DELETE/PATCH）。
        /// </summary>
        public readonly string http_method;

        /// <summary>
        ///     路由模板。
        /// </summary>
        public readonly string template;

        /// <summary>
        ///     返回类型完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     实际返回类型（去除 Task 包装）。
        /// </summary>
        public readonly string actual_return_type;

        /// <summary>
        ///     是否为异步方法。
        /// </summary>
        public readonly bool is_async;

        /// <summary>
        ///     方法参数列表。
        /// </summary>
        public readonly List<HttpParameterInfo> parameters;

        /// <summary>
        ///     方法级 Header 列表。
        /// </summary>
        public readonly List<HttpHeaderInfo> headers;

        /// <summary>
        ///     初始化 <see cref="HttpMethodInfo" /> 的新实例。
        /// </summary>
        public HttpMethodInfo(
            string methodName,
            string httpMethod,
            string template,
            string returnType,
            string actualReturnType,
            bool isAsync,
            List<HttpParameterInfo> parameters,
            List<HttpHeaderInfo> headers)
        {
            method_name = methodName;
            http_method = httpMethod;
            this.template = template;
            return_type = returnType;
            actual_return_type = actualReturnType;
            is_async = isAsync;
            this.parameters = parameters;
            this.headers = headers;
        }
    }

    /// <summary>
    ///     HTTP 方法参数信息，包含参数绑定方式。
    /// </summary>
    internal readonly struct HttpParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     参数绑定方式。
        /// </summary>
        public readonly HttpParameterBinding binding;

        /// <summary>
        ///     查询参数名称，为 null 时使用参数名。
        /// </summary>
        public readonly string? query_name;

        /// <summary>
        ///     Header 名称，用于 header 绑定。
        /// </summary>
        public readonly string? header_name;

        /// <summary>
        ///     初始化 <see cref="HttpParameterInfo" /> 的新实例。
        /// </summary>
        public HttpParameterInfo(
            string name,
            string type,
            HttpParameterBinding binding,
            string? queryName,
            string? headerName)
        {
            this.name = name;
            Type = type;
            this.binding = binding;
            query_name = queryName;
            header_name = headerName;
        }
    }

    /// <summary>
    ///     HTTP Header 信息。
    /// </summary>
    internal readonly struct HttpHeaderInfo
    {
        /// <summary>
        ///     Header 名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     Header 值。
        /// </summary>
        public readonly string value;

        /// <summary>
        ///     初始化 <see cref="HttpHeaderInfo" /> 的新实例。
        /// </summary>
        public HttpHeaderInfo(string name, string value)
        {
            this.name = name;
            this.value = value;
        }
    }

    /// <summary>
    ///     HTTP 参数绑定方式枚举。
    /// </summary>
    internal enum HttpParameterBinding
    {
        /// <summary>
        ///     未指定绑定方式，默认为查询参数。
        /// </summary>
        none,

        /// <summary>
        ///     请求体绑定。
        /// </summary>
        body,

        /// <summary>
        ///     查询字符串绑定。
        /// </summary>
        query,

        /// <summary>
        ///     路由参数绑定。
        /// </summary>
        route,

        /// <summary>
        ///     请求头绑定。
        /// </summary>
        header
    }

    #endregion
}