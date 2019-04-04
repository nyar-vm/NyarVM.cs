using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[SignalR]</c> 特性的接口自动生成 SignalR Hub 客户端代理类，
///     根据方法上的 <c>[Push]</c>、<c>[Stream]</c> 特性生成 Hub 方法调用代码。
/// </summary>
[Generator]
public sealed class SignalGenerator : IIncrementalGenerator
{
    private const string _signalr_attribute_full_name = "Sonic.Standard.Net.Signal.SignalRAttribute";
    private const string _push_attribute_full_name = "Sonic.Standard.Net.Signal.PushAttribute";
    private const string _stream_attribute_full_name = "Sonic.Standard.Net.Signal.StreamAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetInterfaces = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _signalr_attribute_full_name,
                static (node, _) => node is InterfaceDeclarationSyntax,
                static (ctx, ct) => transform_signalr_hub(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetInterfaces, generate_source);
    }

    /// <summary>
    ///     提取标记了 <c>[SignalR]</c> 特性的接口信息。
    /// </summary>
    private static SignalRTypeInfo? transform_signalr_hub(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var signalrAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_signalr_attribute_full_name}");

        if (signalrAttr is null) return null;

        string? hubName = null;

        foreach (var named in signalrAttr.NamedArguments)
            if (named is { Key: "hub_name", Value.Value: string hn })
                hubName = hn;

        if (hubName is null) hubName = typeSymbol.Name;

        var methods = extract_methods(typeSymbol);

        if (methods.Count == 0) return null;

        return new SignalRTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            hubName,
            methods);
    }

    /// <summary>
    ///     提取接口中的方法信息列表，根据 <c>[Push]</c>、<c>[Stream]</c> 特性确定调用方式。
    /// </summary>
    private static List<SignalRMethodInfo> extract_methods(INamedTypeSymbol typeSymbol)
    {
        var methods = new List<SignalRMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol methodSymbol) continue;

            var invokeKind = SignalRInvokeKind.invoke;
            string? topic = null;
            var found = false;

            foreach (var attr in methodSymbol.GetAttributes())
            {
                var fullName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (fullName == $"global::{_push_attribute_full_name}")
                {
                    invokeKind = SignalRInvokeKind.send;
                    found = true;

                    foreach (var named in attr.NamedArguments)
                        if (named is { Key: "topic", Value.Value: string t })
                            topic = t;
                }

                if (fullName == $"global::{_stream_attribute_full_name}")
                {
                    invokeKind = SignalRInvokeKind.stream;
                    found = true;
                }
            }

            if (!found) invokeKind = SignalRInvokeKind.invoke;

            if (topic is null) topic = methodSymbol.Name;

            var returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isAsync = is_type(methodSymbol.ReturnType);
            var actualReturnType = extract_actual_return_type(methodSymbol.ReturnType, isAsync);
            var parameters = extract_parameters(methodSymbol);

            methods.Add(new SignalRMethodInfo(
                methodSymbol.Name,
                returnType,
                actualReturnType,
                isAsync,
                invokeKind,
                topic,
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
    private static List<SignalRParameterInfo> extract_parameters(IMethodSymbol methodSymbol)
    {
        var parameters = new List<SignalRParameterInfo>();

        foreach (var param in methodSymbol.Parameters)
            parameters.Add(new SignalRParameterInfo(
                param.Name,
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        return parameters;
    }

    /// <summary>
    ///     生成所有 SignalR Hub 接口的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<SignalRTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_signalr_client_source(info);
            var hintName = $"{info.interface_name}SignalRClient.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个 SignalR Hub 客户端代理类的源代码。
    /// </summary>
    private static string generate_signalr_client_source(SignalRTypeInfo info)
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
        sb.append_line($"/// <c>{info.interface_name}</c> 的 SignalR Hub 客户端代理。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public sealed class {implName} : global::Sonic.Standard.Net.Signal.SignalRClient, {info.fully_qualified_name}");
        using (sb.block())
        {
            generate_constructor(sb, info, implName);
            sb.append_line();

            for (var i = 0; i < info.methods.Count; i++)
            {
                if (i > 0) sb.append_line();

                generate_signalr_method(sb, info.methods[i], info);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 SignalR 客户端的构造函数。
    /// </summary>
    private static void generate_constructor(SourceTextBuilder sb, SignalRTypeInfo info, string implName)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 初始化 <c>{implName}</c> 的新实例。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"hubUrl\">Hub 连接地址。</param>");
        sb.append_line(
            $"public {implName}(string hubUrl) : base(hubUrl, \"{StringEscapeHelper.escape_for_string(info.hub_name)}\")");
        using (sb.block())
        {
        }
    }

    /// <summary>
    ///     生成单个 SignalR 方法的实现代码。
    /// </summary>
    private static void generate_signalr_method(SourceTextBuilder sb, SignalRMethodInfo method, SignalRTypeInfo info)
    {
        var paramList = build_parameter_list(method.parameters);
        var returnType = method.return_type;

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{method.method_name}</c> 的 SignalR 代理实现。");
        sb.append_line("/// </summary>");

        foreach (var param in method.parameters)
            sb.append_line($"/// <param name=\"{param.name}\">{param.name} 参数。</param>");

        sb.append_line($"public {returnType} {method.method_name}({paramList})");
        using (sb.block())
        {
            var argList = build_argument_list(method.parameters);

            switch (method.invoke_kind)
            {
                case SignalRInvokeKind.send:
                    generate_send_call(sb, method, argList);
                    break;

                case SignalRInvokeKind.invoke:
                    generate_invoke_call(sb, method, argList);
                    break;

                case SignalRInvokeKind.stream:
                    generate_stream_call(sb, method, argList);
                    break;
            }
        }
    }

    /// <summary>
    ///     生成 SignalR Send 调用代码（不等待返回值）。
    /// </summary>
    private static void generate_send_call(SourceTextBuilder sb, SignalRMethodInfo method, string argList)
    {
        if (method.is_async)
            sb.append_line($"await send(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList});");
        else
            sb.append_line(
                $"send(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList}).GetAwaiter().GetResult();");
    }

    /// <summary>
    ///     生成 SignalR Invoke 调用代码（等待返回值）。
    /// </summary>
    private static void generate_invoke_call(SourceTextBuilder sb, SignalRMethodInfo method, string argList)
    {
        if (method.is_async)
        {
            if (method.actual_return_type != "global::System.Threading.Tasks.Task")
                sb.append_line(
                    $"return await invoke<{method.actual_return_type}>(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList});");
            else
                sb.append_line($"await invoke(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList});");
        }
        else
        {
            if (method.actual_return_type != "global::System.Void")
                sb.append_line(
                    $"return invoke<{method.actual_return_type}>(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList}).GetAwaiter().GetResult();");
            else
                sb.append_line(
                    $"invoke(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList}).GetAwaiter().GetResult();");
        }
    }

    /// <summary>
    ///     生成 SignalR Stream 调用代码。
    /// </summary>
    private static void generate_stream_call(SourceTextBuilder sb, SignalRMethodInfo method, string argList)
    {
        if (method.is_async)
            sb.append_line(
                $"return await stream<{method.actual_return_type}>(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList});");
        else
            sb.append_line(
                $"return stream<{method.actual_return_type}>(\"{StringEscapeHelper.escape_for_string(method.topic)}\", {argList}).GetAwaiter().GetResult();");
    }

    /// <summary>
    ///     构建方法参数列表字符串。
    /// </summary>
    private static string build_parameter_list(List<SignalRParameterInfo> parameters)
    {
        var parts = new List<string>();

        foreach (var param in parameters) parts.Add($"{param.Type} {param.name}");

        return string.Join(", ", parts);
    }

    /// <summary>
    ///     构建方法调用参数列表字符串。
    /// </summary>
    private static string build_argument_list(List<SignalRParameterInfo> parameters)
    {
        var parts = new List<string>();

        foreach (var param in parameters) parts.Add(param.name);

        return string.Join(", ", parts);
    }

    #region 数据模型

    /// <summary>
    ///     需要生成 SignalR 客户端的接口信息。
    /// </summary>
    internal readonly struct SignalRTypeInfo
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
        ///     Hub 名称。
        /// </summary>
        public readonly string hub_name;

        /// <summary>
        ///     方法列表。
        /// </summary>
        public readonly List<SignalRMethodInfo> methods;

        /// <summary>
        ///     初始化 <see cref="SignalRTypeInfo" /> 的新实例。
        /// </summary>
        public SignalRTypeInfo(
            string interfaceName,
            string fullyQualifiedName,
            string? namespaceName,
            string hubName,
            List<SignalRMethodInfo> methods)
        {
            interface_name = interfaceName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            hub_name = hubName;
            this.methods = methods;
        }
    }

    /// <summary>
    ///     SignalR 方法信息，包含调用方式和主题。
    /// </summary>
    internal readonly struct SignalRMethodInfo
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
        ///     SignalR 调用方式。
        /// </summary>
        public readonly SignalRInvokeKind invoke_kind;

        /// <summary>
        ///     Hub 方法主题名称。
        /// </summary>
        public readonly string topic;

        /// <summary>
        ///     方法参数列表。
        /// </summary>
        public readonly List<SignalRParameterInfo> parameters;

        /// <summary>
        ///     初始化 <see cref="SignalRMethodInfo" /> 的新实例。
        /// </summary>
        public SignalRMethodInfo(
            string methodName,
            string returnType,
            string actualReturnType,
            bool isAsync,
            SignalRInvokeKind invokeKind,
            string topic,
            List<SignalRParameterInfo> parameters)
        {
            method_name = methodName;
            return_type = returnType;
            actual_return_type = actualReturnType;
            is_async = isAsync;
            invoke_kind = invokeKind;
            this.topic = topic;
            this.parameters = parameters;
        }
    }

    /// <summary>
    ///     SignalR 方法参数信息。
    /// </summary>
    internal readonly struct SignalRParameterInfo
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
        ///     初始化 <see cref="SignalRParameterInfo" /> 的新实例。
        /// </summary>
        public SignalRParameterInfo(string name, string type)
        {
            this.name = name;
            Type = type;
        }
    }

    /// <summary>
    ///     SignalR 调用方式枚举。
    /// </summary>
    internal enum SignalRInvokeKind
    {
        /// <summary>
        ///     调用并等待返回值。
        /// </summary>
        invoke,

        /// <summary>
        ///     发送不等待返回值。
        /// </summary>
        send,

        /// <summary>
        ///     流式调用。
        /// </summary>
        stream
    }

    #endregion
}