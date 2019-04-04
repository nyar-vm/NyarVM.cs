using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[RpcService]</c> 特性的接口自动生成 RPC 客户端代理类，
///     根据方法上的 <c>[RpcMethod]</c> 特性生成 RPC 调用代码。
/// </summary>
[Generator]
public sealed class RpcGenerator : IIncrementalGenerator
{
    private const string _rpc_service_attribute_full_name = "Sonic.Standard.Net.Rpc.RpcServiceAttribute";
    private const string _rpc_method_attribute_full_name = "Sonic.Standard.Net.Rpc.RpcMethodAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetInterfaces = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _rpc_service_attribute_full_name,
                static (node, _) => node is InterfaceDeclarationSyntax,
                static (ctx, ct) => transform_rpc_service(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetInterfaces, generate_source);
    }

    /// <summary>
    ///     提取标记了 <c>[RpcService]</c> 特性的接口信息。
    /// </summary>
    private static RpcServiceTypeInfo? transform_rpc_service(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var rpcAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_rpc_service_attribute_full_name}");

        if (rpcAttr is null) return null;

        string? serviceName = null;

        foreach (var named in rpcAttr.NamedArguments)
            if (named is { Key: "service_name", Value.Value: string sn })
                serviceName = sn;

        if (serviceName is null) serviceName = typeSymbol.Name;

        var methods = extract_methods(typeSymbol);

        if (methods.Count == 0) return null;

        return new RpcServiceTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            serviceName,
            methods);
    }

    /// <summary>
    ///     提取接口中标记了 <c>[RpcMethod]</c> 特性的方法信息列表。
    /// </summary>
    private static List<RpcMethodInfo> extract_methods(INamedTypeSymbol typeSymbol)
    {
        var methods = new List<RpcMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol methodSymbol) continue;

            var rpcMethodAttr = methodSymbol.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_rpc_method_attribute_full_name}");

            if (rpcMethodAttr is null) continue;

            var streamType = RpcStreamType.unary;

            foreach (var named in rpcMethodAttr.NamedArguments)
                if (named is { Key: "stream_type", Value.Value: int st })
                    streamType = (RpcStreamType)st;

            var returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isAsync = is_type(methodSymbol.ReturnType);
            var actualReturnType = extract_actual_return_type(methodSymbol.ReturnType, isAsync);
            var parameters = extract_parameters(methodSymbol);

            methods.Add(new RpcMethodInfo(
                methodSymbol.Name,
                returnType,
                actualReturnType,
                isAsync,
                streamType,
                parameters));
        }

        return methods;
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
    ///     提取方法的参数信息列表。
    /// </summary>
    private static List<RpcParameterInfo> extract_parameters(IMethodSymbol methodSymbol)
    {
        var parameters = new List<RpcParameterInfo>();

        foreach (var param in methodSymbol.Parameters)
            parameters.Add(new RpcParameterInfo(
                param.Name,
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        return parameters;
    }

    /// <summary>
    ///     生成所有 RPC 服务接口的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<RpcServiceTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_rpc_client_source(info);
            var hintName = $"{info.interface_name}RpcClient.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个 RPC 客户端代理类的源代码。
    /// </summary>
    private static string generate_rpc_client_source(RpcServiceTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
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
        sb.append_line($"/// <c>{info.interface_name}</c> 的 RPC 客户端代理。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public sealed class {implName} : global::Sonic.Standard.Net.Rpc.RpcClient, {info.fully_qualified_name}");
        using (sb.block())
        {
            generate_constructor(sb, info, implName);
            sb.append_line();

            for (var i = 0; i < info.methods.Count; i++)
            {
                if (i > 0) sb.append_line();

                generate_rpc_method(sb, info.methods[i], info);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 RPC 客户端的构造函数。
    /// </summary>
    private static void generate_constructor(SourceTextBuilder sb, RpcServiceTypeInfo info, string implName)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 初始化 <c>{implName}</c> 的新实例。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"baseUrl\">RPC 服务基础地址。</param>");
        sb.append_line(
            $"public {implName}(string baseUrl) : base(baseUrl, \"{StringEscapeHelper.escape_for_string(info.service_name)}\")");
        using (sb.block())
        {
        }
    }

    /// <summary>
    ///     生成单个 RPC 方法的实现代码。
    /// </summary>
    private static void generate_rpc_method(SourceTextBuilder sb, RpcMethodInfo method, RpcServiceTypeInfo info)
    {
        var paramList = build_parameter_list(method.parameters);
        var returnType = method.return_type;

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{method.method_name}</c> 的 RPC 代理实现。");
        sb.append_line("/// </summary>");

        foreach (var param in method.parameters)
            sb.append_line($"/// <param name=\"{param.name}\">{param.name} 参数。</param>");

        sb.append_line($"public {returnType} {method.method_name}({paramList})");
        using (sb.block())
        {
            var argList = build_argument_list(method.parameters);

            switch (method.stream_type)
            {
                case RpcStreamType.unary:
                    generate_unary_call(sb, method, argList);
                    break;

                case RpcStreamType.server_stream:
                    generate_server_stream_call(sb, method, argList);
                    break;

                case RpcStreamType.client_stream:
                    generate_client_stream_call(sb, method, argList);
                    break;

                case RpcStreamType.duplex:
                    generate_duplex_stream_call(sb, method, argList);
                    break;
            }
        }
    }

    /// <summary>
    ///     生成单向 RPC 调用代码。
    /// </summary>
    private static void generate_unary_call(SourceTextBuilder sb, RpcMethodInfo method, string argList)
    {
        if (method.is_async)
        {
            if (method.actual_return_type != "global::System.Threading.Tasks.Task")
                sb.append_line(
                    $"return await invoke<{method.actual_return_type}>(\"{method.method_name}\", {argList});");
            else
                sb.append_line($"await invoke(\"{method.method_name}\", {argList});");
        }
        else
        {
            if (method.actual_return_type != "global::System.Void")
                sb.append_line(
                    $"return invoke<{method.actual_return_type}>(\"{method.method_name}\", {argList}).GetAwaiter().GetResult();");
            else
                sb.append_line($"invoke(\"{method.method_name}\", {argList}).GetAwaiter().GetResult();");
        }
    }

    /// <summary>
    ///     生成服务端流式 RPC 调用代码。
    /// </summary>
    private static void generate_server_stream_call(SourceTextBuilder sb, RpcMethodInfo method, string argList)
    {
        if (method.is_async)
            sb.append_line(
                $"return await server_stream<{method.actual_return_type}>(\"{method.method_name}\", {argList});");
        else
            sb.append_line(
                $"return server_stream<{method.actual_return_type}>(\"{method.method_name}\", {argList}).GetAwaiter().GetResult();");
    }

    /// <summary>
    ///     生成客户端流式 RPC 调用代码。
    /// </summary>
    private static void generate_client_stream_call(SourceTextBuilder sb, RpcMethodInfo method, string argList)
    {
        if (method.is_async)
        {
            if (method.actual_return_type != "global::System.Threading.Tasks.Task")
                sb.append_line(
                    $"return await client_stream<{method.actual_return_type}>(\"{method.method_name}\", {argList});");
            else
                sb.append_line($"await client_stream(\"{method.method_name}\", {argList});");
        }
        else
        {
            sb.append_line(
                $"return client_stream<{method.actual_return_type}>(\"{method.method_name}\", {argList}).GetAwaiter().GetResult();");
        }
    }

    /// <summary>
    ///     生成双向流式 RPC 调用代码。
    /// </summary>
    private static void generate_duplex_stream_call(SourceTextBuilder sb, RpcMethodInfo method, string argList)
    {
        if (method.is_async)
            sb.append_line(
                $"return await duplex_stream<{method.actual_return_type}>(\"{method.method_name}\", {argList});");
        else
            sb.append_line(
                $"return duplex_stream<{method.actual_return_type}>(\"{method.method_name}\", {argList}).GetAwaiter().GetResult();");
    }

    /// <summary>
    ///     构建方法参数列表字符串。
    /// </summary>
    private static string build_parameter_list(List<RpcParameterInfo> parameters)
    {
        var parts = new List<string>();

        foreach (var param in parameters) parts.Add($"{param.Type} {param.name}");

        return string.Join(", ", parts);
    }

    /// <summary>
    ///     构建方法调用参数列表字符串。
    /// </summary>
    private static string build_argument_list(List<RpcParameterInfo> parameters)
    {
        var parts = new List<string>();

        foreach (var param in parameters) parts.Add(param.name);

        return string.Join(", ", parts);
    }

    #region 数据模型

    /// <summary>
    ///     需要生成 RPC 客户端的接口信息。
    /// </summary>
    internal readonly struct RpcServiceTypeInfo
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
        ///     RPC 服务名称。
        /// </summary>
        public readonly string service_name;

        /// <summary>
        ///     RPC 方法列表。
        /// </summary>
        public readonly List<RpcMethodInfo> methods;

        /// <summary>
        ///     初始化 <see cref="RpcServiceTypeInfo" /> 的新实例。
        /// </summary>
        public RpcServiceTypeInfo(
            string interfaceName,
            string fullyQualifiedName,
            string? namespaceName,
            string serviceName,
            List<RpcMethodInfo> methods)
        {
            interface_name = interfaceName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            service_name = serviceName;
            this.methods = methods;
        }
    }

    /// <summary>
    ///     RPC 方法信息，包含流类型和参数。
    /// </summary>
    internal readonly struct RpcMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

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
        ///     RPC 流类型。
        /// </summary>
        public readonly RpcStreamType stream_type;

        /// <summary>
        ///     方法参数列表。
        /// </summary>
        public readonly List<RpcParameterInfo> parameters;

        /// <summary>
        ///     初始化 <see cref="RpcMethodInfo" /> 的新实例。
        /// </summary>
        public RpcMethodInfo(
            string methodName,
            string returnType,
            string actualReturnType,
            bool isAsync,
            RpcStreamType streamType,
            List<RpcParameterInfo> parameters)
        {
            method_name = methodName;
            return_type = returnType;
            actual_return_type = actualReturnType;
            is_async = isAsync;
            stream_type = streamType;
            this.parameters = parameters;
        }
    }

    /// <summary>
    ///     RPC 方法参数信息。
    /// </summary>
    internal readonly struct RpcParameterInfo
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
        ///     初始化 <see cref="RpcParameterInfo" /> 的新实例。
        /// </summary>
        public RpcParameterInfo(string name, string type)
        {
            this.name = name;
            Type = type;
        }
    }

    /// <summary>
    ///     RPC 流类型枚举。
    /// </summary>
    internal enum RpcStreamType
    {
        /// <summary>
        ///     单向调用。
        /// </summary>
        unary = 0,

        /// <summary>
        ///     服务端流。
        /// </summary>
        server_stream = 1,

        /// <summary>
        ///     客户端流。
        /// </summary>
        client_stream = 2,

        /// <summary>
        ///     双向流。
        /// </summary>
        duplex = 3
    }

    #endregion
}