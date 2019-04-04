using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[StreamProcessor]</c>、<c>[StreamSource]</c>、<c>[StreamSink]</c> 或 <c>[Window]</c> 特性的类
///     自动生成 <c>IPipeline&lt;T&gt;</c> 实现，连接数据源、处理器和输出端。
/// </summary>
[Generator]
public sealed class PipelineGenerator : IIncrementalGenerator
{
    private const string _stream_processor_attribute_full_name =
        "Sonic.Standard.Flow.Pipeline.StreamProcessorAttribute";

    private const string _stream_source_attribute_full_name = "Sonic.Standard.Flow.Pipeline.StreamSourceAttribute";
    private const string _stream_sink_attribute_full_name = "Sonic.Standard.Flow.Pipeline.StreamSinkAttribute";
    private const string _window_attribute_full_name = "Sonic.Standard.Flow.Pipeline.WindowAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var processorTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _stream_processor_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_pipeline(ctx, PipelineKind.processor, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        var sourceTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _stream_source_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_pipeline(ctx, PipelineKind.source, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        var sinkTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _stream_sink_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_pipeline(ctx, PipelineKind.sink, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        var allTypes = processorTypes.Collect()
            .Combine(sourceTypes.Collect())
            .Combine(sinkTypes.Collect());

        context.RegisterSourceOutput(allTypes, generate_source);
    }

    /// <summary>
    ///     提取管道类型信息。
    /// </summary>
    private static PipelineTypeInfo? transform_pipeline(GeneratorAttributeSyntaxContext context, PipelineKind kind,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var processMethods = new List<ProcessMethodInfo>();
        var windowMethods = new List<WindowMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.IsStatic) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            var windowAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_window_attribute_full_name}");

            if (windowAttr is not null)
            {
                var windowType = "tumbling";
                var windowSize = 1;

                foreach (var named in windowAttr.NamedArguments)
                {
                    if (named is { Key: "type", Value.Value: int wt })
                        windowType = wt switch
                        {
                            0 => "tumbling",
                            1 => "sliding",
                            2 => "session",
                            _ => "tumbling"
                        };

                    if (named is { Key: "size", Value.Value: int s }) windowSize = s;
                }

                var windowElementType = method.Parameters.Length > 0
                    ? method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : "object";

                windowMethods.Add(new WindowMethodInfo(method.Name, windowElementType, windowType, windowSize));
                continue;
            }

            if (method.Name.StartsWith("process") || method.Name.StartsWith("transform") ||
                method.Name.StartsWith("filter"))
            {
                var inputType = method.Parameters.Length > 0
                    ? method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : null;

                processMethods.Add(new ProcessMethodInfo(
                    method.Name,
                    inputType,
                    method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }
        }

        string? elementType = null;

        if (processMethods.Count > 0 && processMethods[0].input_type is not null)
            elementType = processMethods[0].input_type;
        else if (windowMethods.Count > 0) elementType = windowMethods[0].element_type;

        if (elementType is null) return null;

        return new PipelineTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            kind,
            elementType,
            processMethods,
            windowMethods);
    }

    /// <summary>
    ///     生成所有管道类型的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context,
        ((ImmutableArray<PipelineTypeInfo> processors, ImmutableArray<PipelineTypeInfo> sources) pair,
            ImmutableArray<PipelineTypeInfo> sinks) allTypes)
    {
        var all = allTypes.pair.processors
            .Concat(allTypes.pair.sources)
            .Concat(allTypes.sinks);

        foreach (var info in all)
        {
            var sourceText = generate_pipeline_source(info);
            var hintName = $"{info.type_name}.Pipeline.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个管道类型的源代码。
    /// </summary>
    private static string generate_pipeline_source(PipelineTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System;");
        sb.append_line("using System.Collections.Generic;");
        sb.append_line("using System.Runtime.CompilerServices;");
        sb.append_line("using System.Threading;");
        sb.append_line("using System.Threading.Tasks;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的管道实现。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public partial class {info.type_name} : global::Sonic.Standard.Flow.Pipeline.IPipeline<{info.element_type}>");
        using (sb.block())
        {
            generate_process_method(sb, info);
            sb.append_line();
            generate_window_methods(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>process</c> 方法。
    /// </summary>
    private static void generate_process_method(SourceTextBuilder sb, PipelineTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 处理输入数据流并返回输出数据流。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"source\">输入数据流。</param>");
        sb.append_line("/// <param name=\"cancellationToken\">取消令牌。</param>");
        sb.append_line(
            $"public async global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> process(global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> source, global::System.Threading.CancellationToken cancellationToken = default)");
        using (sb.block())
        {
            if (info.process_methods.Count == 0)
            {
                sb.append_line(
                    "await foreach (var __item in source.WithCancellation(cancellationToken).ConfigureAwait(false))");
                using (sb.block())
                {
                    sb.append_line("yield return __item;");
                }

                yield_break(sb);
                return;
            }

            foreach (var method in info.process_methods)
            {
                var isAsync = method.return_type.Contains("IAsyncEnumerable");

                if (isAsync)
                    sb.append_line($"source = __apply_{method.method_name}(source, cancellationToken);");
                else if (method.return_type.Contains("Task"))
                    sb.append_line($"source = __apply_{method.method_name}(source, cancellationToken);");
                else
                    sb.append_line($"source = __apply_sync_{method.method_name}(source, cancellationToken);");
            }

            sb.append_line();
            sb.append_line(
                "await foreach (var __item in source.WithCancellation(cancellationToken).ConfigureAwait(false))");
            using (sb.block())
            {
                sb.append_line("yield return __item;");
            }
        }

        sb.append_line();

        foreach (var method in info.process_methods)
        {
            generate_apply_helper(sb, method, info);
            sb.append_line();
        }
    }

    /// <summary>
    ///     生成 <c>yield break</c> 语句。
    /// </summary>
    private static void yield_break(SourceTextBuilder sb)
    {
        sb.append_line("yield break;");
    }

    /// <summary>
    ///     生成流处理辅助方法。
    /// </summary>
    private static void generate_apply_helper(SourceTextBuilder sb, ProcessMethodInfo method, PipelineTypeInfo info)
    {
        var isAsync = method.return_type.Contains("IAsyncEnumerable");

        if (isAsync)
        {
            sb.append_line(
                $"private async global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> __apply_{method.method_name}(global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> source, global::System.Threading.CancellationToken cancellationToken)");
            using (sb.block())
            {
                sb.append_line(
                    "await foreach (var __item in source.WithCancellation(cancellationToken).ConfigureAwait(false))");
                using (sb.block())
                {
                    sb.append_line(
                        $"await foreach (var __result in {method.method_name}(__item).WithCancellation(cancellationToken).ConfigureAwait(false))");
                    using (sb.block())
                    {
                        sb.append_line("yield return __result;");
                    }
                }
            }
        }
        else if (method.return_type.Contains("Task"))
        {
            sb.append_line(
                $"private async global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> __apply_{method.method_name}(global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> source, global::System.Threading.CancellationToken cancellationToken)");
            using (sb.block())
            {
                sb.append_line(
                    "await foreach (var __item in source.WithCancellation(cancellationToken).ConfigureAwait(false))");
                using (sb.block())
                {
                    sb.append_line($"var __result = await {method.method_name}(__item).ConfigureAwait(false);");
                    sb.append_line("yield return __result;");
                }
            }
        }
        else
        {
            sb.append_line(
                $"private async global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> __apply_sync_{method.method_name}(global::System.Collections.Generic.IAsyncEnumerable<{info.element_type}> source, global::System.Threading.CancellationToken cancellationToken)");
            using (sb.block())
            {
                sb.append_line(
                    "await foreach (var __item in source.WithCancellation(cancellationToken).ConfigureAwait(false))");
                using (sb.block())
                {
                    sb.append_line($"var __result = {method.method_name}(__item);");
                    sb.append_line("yield return __result;");
                }
            }
        }
    }

    /// <summary>
    ///     生成窗口处理方法。
    /// </summary>
    private static void generate_window_methods(SourceTextBuilder sb, PipelineTypeInfo info)
    {
        foreach (var window in info.window_methods)
        {
            sb.append_line("/// <summary>");
            sb.append_line($"/// 对数据流应用 {window.window_type} 窗口，窗口大小为 {window.window_size}。");
            sb.append_line("/// </summary>");
            sb.append_line("/// <param name=\"source\">输入数据流。</param>");
            sb.append_line("/// <param name=\"cancellationToken\">取消令牌。</param>");
            sb.append_line(
                $"public async global::System.Collections.Generic.IAsyncEnumerable<global::System.Collections.Generic.List<{window.element_type}>> {window.method_name}_windowed(global::System.Collections.Generic.IAsyncEnumerable<{window.element_type}> source, global::System.Threading.CancellationToken cancellationToken = default)");
            using (sb.block())
            {
                sb.append_line(
                    $"var __buffer = new global::System.Collections.Generic.List<{window.element_type}>({window.window_size});");
                sb.append_line();
                sb.append_line(
                    "await foreach (var __item in source.WithCancellation(cancellationToken).ConfigureAwait(false))");
                using (sb.block())
                {
                    sb.append_line("__buffer.Add(__item);");
                    sb.append_line();
                    sb.append_line($"if (__buffer.Count >= {window.window_size})");
                    using (sb.block())
                    {
                        sb.append_line(
                            $"var __window = new global::System.Collections.Generic.List<{window.element_type}>(__buffer);");
                        sb.append_line("__buffer.Clear();");
                        sb.append_line("yield return __window;");
                    }
                }

                sb.append_line();
                sb.append_line("if (__buffer.Count > 0)");
                using (sb.block())
                {
                    sb.append_line("yield return __buffer;");
                }
            }

            sb.append_line();
        }
    }

    #region 数据模型

    /// <summary>
    ///     管道类型信息。
    /// </summary>
    internal readonly struct PipelineTypeInfo
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
        ///     管道种类。
        /// </summary>
        public readonly PipelineKind kind;

        /// <summary>
        ///     元素类型的完全限定名称。
        /// </summary>
        public readonly string element_type;

        /// <summary>
        ///     处理方法列表。
        /// </summary>
        public readonly List<ProcessMethodInfo> process_methods;

        /// <summary>
        ///     窗口方法列表。
        /// </summary>
        public readonly List<WindowMethodInfo> window_methods;

        /// <summary>
        ///     初始化 <see cref="PipelineTypeInfo" /> 的新实例。
        /// </summary>
        public PipelineTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            PipelineKind kind,
            string elementType,
            List<ProcessMethodInfo> processMethods,
            List<WindowMethodInfo> windowMethods)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            this.kind = kind;
            element_type = elementType;
            process_methods = processMethods;
            window_methods = windowMethods;
        }
    }

    /// <summary>
    ///     处理方法信息。
    /// </summary>
    internal readonly struct ProcessMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     输入类型的完全限定名称。
        /// </summary>
        public readonly string? input_type;

        /// <summary>
        ///     返回类型的完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     初始化 <see cref="ProcessMethodInfo" /> 的新实例。
        /// </summary>
        public ProcessMethodInfo(string methodName, string? inputType, string returnType)
        {
            method_name = methodName;
            input_type = inputType;
            return_type = returnType;
        }
    }

    /// <summary>
    ///     窗口方法信息。
    /// </summary>
    internal readonly struct WindowMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     元素类型的完全限定名称。
        /// </summary>
        public readonly string element_type;

        /// <summary>
        ///     窗口类型。
        /// </summary>
        public readonly string window_type;

        /// <summary>
        ///     窗口大小。
        /// </summary>
        public readonly int window_size;

        /// <summary>
        ///     初始化 <see cref="WindowMethodInfo" /> 的新实例。
        /// </summary>
        public WindowMethodInfo(string methodName, string elementType, string windowType, int windowSize)
        {
            method_name = methodName;
            element_type = elementType;
            window_type = windowType;
            window_size = windowSize;
        }
    }

    /// <summary>
    ///     管道种类。
    /// </summary>
    internal enum PipelineKind
    {
        /// <summary>
        ///     流处理器。
        /// </summary>
        processor,

        /// <summary>
        ///     流数据源。
        /// </summary>
        source,

        /// <summary>
        ///     流输出端。
        /// </summary>
        sink
    }

    #endregion
}