using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Actor]</c> 特性的类自动生成消息循环代码，
///     为标记了 <c>[Channel]</c> 特性的字段/属性生成通道包装代码。
/// </summary>
[Generator]
public sealed class ConcurrencyGenerator : IIncrementalGenerator
{
    private const string _actor_attribute_full_name = "Sonic.Standard.Concurrency.ActorAttribute";
    private const string _channel_attribute_full_name = "Sonic.Standard.Concurrency.ChannelAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var actorTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _actor_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_actor(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var channelTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _channel_attribute_full_name,
                static (node, _) => node is FieldDeclarationSyntax or PropertyDeclarationSyntax,
                static (ctx, ct) => transform_channel(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(actorTargets, generate_actor_source);
        context.RegisterSourceOutput(channelTargets, generate_channel_source);
    }

    /// <summary>
    ///     提取标记了 <c>[Actor]</c> 特性的类信息。
    /// </summary>
    private static ActorTypeInfo? transform_actor(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var messageHandlers = extract_message_handlers(typeSymbol);

        return new ActorTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            messageHandlers);
    }

    /// <summary>
    ///     提取类中的消息处理方法（公共方法）。
    /// </summary>
    private static List<MessageHandlerInfo> extract_message_handlers(INamedTypeSymbol typeSymbol)
    {
        var handlers = new List<MessageHandlerInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            if (method.IsStatic) continue;

            if (method.MethodKind != MethodKind.Ordinary) continue;

            if (method.Parameters.Length != 1) continue;

            var paramType = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isAsync = is_type(method.ReturnType);

            handlers.Add(new MessageHandlerInfo(
                method.Name,
                paramType,
                returnType,
                isAsync));
        }

        return handlers;
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
    ///     提取标记了 <c>[Channel]</c> 特性的字段/属性信息。
    /// </summary>
    private static ChannelFieldInfo? transform_channel(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var symbol = context.TargetSymbol;

        var channelAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_channel_attribute_full_name}");

        if (channelAttr is null) return null;

        var capacity = 0;

        foreach (var named in channelAttr.NamedArguments)
            if (named is { Key: "capacity", Value.Value: int c })
                capacity = c;

        var containingType = symbol.ContainingType;
        string fieldType;
        string? elementTypeName = null;

        if (symbol is IPropertySymbol prop)
        {
            fieldType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (prop.Type is INamedTypeSymbol { IsGenericType: true } namedType)
                elementTypeName = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        else if (symbol is IFieldSymbol field)
        {
            fieldType = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (field.Type is INamedTypeSymbol { IsGenericType: true } namedType)
                elementTypeName = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        else
        {
            return null;
        }

        if (elementTypeName is null) return null;

        return new ChannelFieldInfo(
            containingType.Name,
            containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            containingType.ContainingNamespace?.ToDisplayString(),
            symbol.Name,
            fieldType,
            elementTypeName,
            capacity);
    }

    /// <summary>
    ///     生成所有 Actor 类型的源代码。
    /// </summary>
    private static void generate_actor_source(SourceProductionContext context, ImmutableArray<ActorTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_actor_impl(info);
            var hintName = $"{info.type_name}.Actor.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个 Actor 类型的源代码。
    /// </summary>
    private static string generate_actor_impl(ActorTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System.Threading;");
        sb.append_line("using System.Threading.Channels;");
        sb.append_line("using System.Threading.Tasks;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        sb.append_line($"partial class {info.type_name}");
        using (sb.block())
        {
            generate_mailbox_field(sb);
            sb.append_line();
            generate_tell_method(sb);
            sb.append_line();
            generate_ask_method(sb);
            sb.append_line();
            generate_message_loop(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成邮箱字段。
    /// </summary>
    private static void generate_mailbox_field(SourceTextBuilder sb)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// Actor 的内部邮箱通道。");
        sb.append_line("/// </summary>");
        sb.append_line("private readonly global::System.Threading.Channels.Channel<object> __mailbox = " +
                       "global::System.Threading.Channels.Channel.CreateUnbounded<object>();");
    }

    /// <summary>
    ///     生成 <c>tell</c> 方法。
    /// </summary>
    private static void generate_tell_method(SourceTextBuilder sb)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 向 Actor 异步发送消息（不等待回复）。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"message\">发送的消息对象。</param>");
        sb.append_line("/// <returns>异步任务。</returns>");
        sb.append_line("public async global::System.Threading.Tasks.Task tell(object message)");
        using (sb.block())
        {
            sb.append_line("await __mailbox.Writer.WriteAsync(message);");
        }
    }

    /// <summary>
    ///     生成 <c>ask</c> 方法。
    /// </summary>
    private static void generate_ask_method(SourceTextBuilder sb)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 向 Actor 异步发送消息并等待回复。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <typeparam name=\"T\">回复类型。</typeparam>");
        sb.append_line("/// <param name=\"message\">发送的消息对象。</param>");
        sb.append_line("/// <returns>回复结果。</returns>");
        sb.append_line("public async global::System.Threading.Tasks.Task<T> ask<T>(object message)");
        using (sb.block())
        {
            sb.append_line("var __tcs = new global::System.Threading.Tasks.TaskCompletionSource<T>();");
            sb.append_line("await __mailbox.Writer.WriteAsync(message);");
            sb.append_line("return await __tcs.Task;");
        }
    }

    /// <summary>
    ///     生成消息循环方法。
    /// </summary>
    private static void generate_message_loop(SourceTextBuilder sb, ActorTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 启动 Actor 的消息循环。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"cancellationToken\">取消令牌。</param>");
        sb.append_line("/// <returns>异步任务。</returns>");
        sb.append_line(
            "public async global::System.Threading.Tasks.Task run(global::System.Threading.CancellationToken cancellationToken = default)");
        using (sb.block())
        {
            sb.append_line("await foreach (var __message in __mailbox.Reader.ReadAllAsync(cancellationToken))");
            using (sb.block())
            {
                if (info.handlers.Count > 0)
                {
                    sb.append_line("switch (__message)");
                    using (sb.block())
                    {
                        foreach (var handler in info.handlers)
                        {
                            sb.append_line($"case {handler.parameter_type} __msg_{handler.method_name}:");
                            sb.indent();
                            if (handler.is_async)
                                sb.append_line($"await {handler.method_name}(__msg_{handler.method_name});");
                            else
                                sb.append_line($"{handler.method_name}(__msg_{handler.method_name});");

                            sb.append_line("break;");
                            sb.outdent();
                        }

                        sb.append_line("default:");
                        sb.indent();
                        sb.append_line("break;");
                        sb.outdent();
                    }
                }
            }
        }
    }

    /// <summary>
    ///     生成所有通道字段的源代码。
    /// </summary>
    private static void generate_channel_source(SourceProductionContext context,
        ImmutableArray<ChannelFieldInfo> fieldInfos)
    {
        var grouped = fieldInfos.GroupBy(f => f.containing_type_fqn).ToList();

        foreach (var group in grouped)
        {
            var first = group.First();
            var sourceText = generate_channel_impl(first.containing_type_name, first.containing_type_fqn,
                first.namespace_name, [.. group]);
            var hintName = $"{first.containing_type_name}.Channel.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成通道包装的源代码。
    /// </summary>
    private static string generate_channel_impl(string typeName, string fqn, string? ns,
        List<ChannelFieldInfo> channels)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System.Threading.Channels;");
        sb.append_line("using System.Threading.Tasks;");
        sb.append_line();

        if (!string.IsNullOrEmpty(ns))
        {
            sb.append_line($"namespace {ns};");
            sb.append_line();
        }

        sb.append_line($"partial class {typeName}");
        using (sb.block())
        {
            foreach (var channel in channels)
            {
                generate_channel_wrapper(sb, channel);
                sb.append_line();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成单个通道字段的包装方法。
    /// </summary>
    private static void generate_channel_wrapper(SourceTextBuilder sb, ChannelFieldInfo channel)
    {
        var fieldName = channel.field_name;
        var elementType = channel.element_type_name;

        sb.append_line("/// <summary>");
        sb.append_line($"/// 向 <c>{fieldName}</c> 通道异步写入元素。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"item\">要写入的元素。</param>");
        sb.append_line($"public async global::System.Threading.Tasks.Task write_{fieldName}({elementType} item)");
        using (sb.block())
        {
            sb.append_line(
                $"await ((global::Sonic.Standard.Concurrency.IChannel<{elementType}>){fieldName}).write(item);");
        }

        sb.append_line();
        sb.append_line("/// <summary>");
        sb.append_line($"/// 从 <c>{fieldName}</c> 通道异步读取元素。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <returns>读取到的元素。</returns>");
        sb.append_line($"public async global::System.Threading.Tasks.Task<{elementType}> read_{fieldName}()");
        using (sb.block())
        {
            sb.append_line(
                $"return await ((global::Sonic.Standard.Concurrency.IChannel<{elementType}>){fieldName}).read();");
        }
    }

    #region 数据模型

    /// <summary>
    ///     Actor 类型信息。
    /// </summary>
    internal readonly struct ActorTypeInfo
    {
        /// <summary>
        ///     类型名称。
        /// </summary>
        public readonly string type_name;

        /// <summary>
        ///     完全限定类型名称。
        /// </summary>
        public readonly string fully_qualified_name;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     消息处理器列表。
        /// </summary>
        public readonly List<MessageHandlerInfo> handlers;

        /// <summary>
        ///     初始化 <see cref="ActorTypeInfo" /> 的新实例。
        /// </summary>
        public ActorTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            List<MessageHandlerInfo> handlers)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            this.handlers = handlers;
        }
    }

    /// <summary>
    ///     消息处理器信息。
    /// </summary>
    internal readonly struct MessageHandlerInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     参数类型完全限定名称。
        /// </summary>
        public readonly string parameter_type;

        /// <summary>
        ///     返回类型完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     是否为异步方法。
        /// </summary>
        public readonly bool is_async;

        /// <summary>
        ///     初始化 <see cref="MessageHandlerInfo" /> 的新实例。
        /// </summary>
        public MessageHandlerInfo(
            string methodName,
            string parameterType,
            string returnType,
            bool isAsync)
        {
            method_name = methodName;
            parameter_type = parameterType;
            return_type = returnType;
            is_async = isAsync;
        }
    }

    /// <summary>
    ///     通道字段信息。
    /// </summary>
    internal readonly struct ChannelFieldInfo
    {
        /// <summary>
        ///     包含类型名称。
        /// </summary>
        public readonly string containing_type_name;

        /// <summary>
        ///     包含类型完全限定名称。
        /// </summary>
        public readonly string containing_type_fqn;

        /// <summary>
        ///     命名空间名称。
        /// </summary>
        public readonly string? namespace_name;

        /// <summary>
        ///     字段/属性名称。
        /// </summary>
        public readonly string field_name;

        /// <summary>
        ///     字段/属性类型完全限定名称。
        /// </summary>
        public readonly string field_type;

        /// <summary>
        ///     元素类型完全限定名称。
        /// </summary>
        public readonly string element_type_name;

        /// <summary>
        ///     通道容量。
        /// </summary>
        public readonly int capacity;

        /// <summary>
        ///     初始化 <see cref="ChannelFieldInfo" /> 的新实例。
        /// </summary>
        public ChannelFieldInfo(
            string containingTypeName,
            string containingTypeFqn,
            string? namespaceName,
            string fieldName,
            string fieldType,
            string elementTypeName,
            int capacity)
        {
            containing_type_name = containingTypeName;
            containing_type_fqn = containingTypeFqn;
            namespace_name = namespaceName;
            field_name = fieldName;
            field_type = fieldType;
            element_type_name = elementTypeName;
            this.capacity = capacity;
        }
    }

    #endregion
}