using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Observable]</c> 特性的方法自动生成 <c>Activity</c> 追踪代码，
///     为标记了 <c>[Metric]</c> 特性的方法/属性生成 <c>Counter</c>/<c>Histogram</c> 指标记录代码。
/// </summary>
[Generator]
public sealed class ObservabilityGenerator : IIncrementalGenerator
{
    private const string _observable_attribute_full_name = "Sonic.Standard.Observability.ObservableAttribute";
    private const string _metric_attribute_full_name = "Sonic.Standard.Observability.MetricAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var observableTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _observable_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_observable(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var metricTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _metric_attribute_full_name,
                static (node, _) => node is MethodDeclarationSyntax or PropertyDeclarationSyntax,
                static (ctx, ct) => transform_metric(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(observableTargets, generate_observable_source);
        context.RegisterSourceOutput(metricTargets, generate_metric_source);
    }

    /// <summary>
    ///     提取可观测类型信息。
    /// </summary>
    private static ObservableTypeInfo? transform_observable(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var observableMethods = new List<ObservableMethodInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.IsStatic) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            var observableAttr = method.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_observable_attribute_full_name}");

            if (observableAttr is not null)
            {
                var parameters = new List<ParameterInfo>();

                foreach (var param in method.Parameters)
                    parameters.Add(new ParameterInfo(
                        param.Name,
                        param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

                observableMethods.Add(new ObservableMethodInfo(
                    method.Name,
                    method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    method.ReturnType.SpecialType == SpecialType.System_Void,
                    parameters));
            }
        }

        if (observableMethods.Count == 0) return null;

        return new ObservableTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            observableMethods);
    }

    /// <summary>
    ///     提取指标信息。
    /// </summary>
    private static MetricInfo? transform_metric(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var metricAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_metric_attribute_full_name}");

        if (metricAttr is null) return null;

        string? metricName = null;
        var metricType = "counter";

        foreach (var named in metricAttr.NamedArguments)
        {
            if (named is { Key: "name", Value.Value: string n }) metricName = n;

            if (named is { Key: "type", Value.Value: int t })
                metricType = t switch
                {
                    0 => "counter",
                    1 => "gauge",
                    2 => "histogram",
                    _ => "counter"
                };
        }

        var targetSymbol = context.TargetSymbol;
        var typeSymbol = targetSymbol.ContainingType;

        if (typeSymbol is null) return null;

        var memberName = targetSymbol.Name;
        var memberKind = targetSymbol is IPropertySymbol ? MetricMemberKind.property : MetricMemberKind.method;

        string? returnType = null;

        if (targetSymbol is IMethodSymbol method)
            returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        else if (targetSymbol is IPropertySymbol prop)
            returnType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        metricName ??= memberName;

        return new MetricInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            memberName,
            metricName,
            metricType,
            memberKind,
            returnType);
    }

    /// <summary>
    ///     生成可观测类型的源代码。
    /// </summary>
    private static void generate_observable_source(SourceProductionContext context,
        ImmutableArray<ObservableTypeInfo> typeInfos)
    {
        var grouped = new Dictionary<string, ObservableTypeInfo>();

        foreach (var info in typeInfos)
            if (!grouped.ContainsKey(info.fully_qualified_name))
                grouped[info.fully_qualified_name] = info;

        foreach (var info in grouped.Values)
        {
            var sourceText = generate_observable_type_source(info);
            var hintName = $"{info.type_name}.Observable.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成指标类型的源代码。
    /// </summary>
    private static void generate_metric_source(SourceProductionContext context, ImmutableArray<MetricInfo> metricInfos)
    {
        var grouped = new Dictionary<string, List<MetricInfo>>();

        foreach (var metric in metricInfos)
        {
            if (!grouped.TryGetValue(metric.fully_qualified_name, out var list))
            {
                list = [];
                grouped[metric.fully_qualified_name] = list;
            }

            list.Add(metric);
        }

        foreach (var __kvp in grouped)
        {
            var metrics = __kvp.Value;
            var first = metrics[0];
            var sourceText = generate_metric_type_source(first, metrics);
            var hintName = $"{first.type_name}.Metric.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成可观测类型的源代码。
    /// </summary>
    private static string generate_observable_type_source(ObservableTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System;");
        sb.append_line("using System.Diagnostics;");
        sb.append_line("using System.Threading.Tasks;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的可观测追踪实现。");
        sb.append_line("/// </summary>");
        sb.append_line($"public partial class {info.type_name}");
        using (sb.block())
        {
            foreach (var method in info.observable_methods)
            {
                generate_traced_method(sb, method, info);
                sb.append_line();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成带追踪的方法。
    /// </summary>
    private static void generate_traced_method(SourceTextBuilder sb, ObservableMethodInfo method,
        ObservableTypeInfo info)
    {
        var paramList = string.Join(", ", method.parameters.Select(p => $"{p.type_full_name} {p.name}"));
        var returnType = method.is_void ? "void" : method.return_type;
        var asyncKeyword = method.return_type.Contains("Task") && !method.is_void ? "async " : "";

        sb.append_line("/// <summary>");
        sb.append_line($"/// 带追踪的 <c>{method.method_name}</c> 方法，自动创建 <c>Activity</c> 跨度。");
        sb.append_line("/// </summary>");

        foreach (var param in method.parameters) sb.append_line($"/// <param name=\"{param.name}\">方法参数。</param>");

        sb.append_line($"public {asyncKeyword}{returnType} {method.method_name}_traced({paramList})");
        using (sb.block())
        {
            sb.append_line(
                $"using var __activity = global::Sonic.Standard.Observability.SonicActivitySource.start_activity(\"{info.type_name}.{method.method_name}\");");
            sb.append_line();

            if (method.is_void)
            {
                sb.append_line("try");
                using (sb.block())
                {
                    sb.append_line(
                        $"{method.method_name}({string.Join(", ", method.parameters.Select(p => p.name))});");
                }

                sb.append_line("catch (global::System.Exception __ex)");
                using (sb.block())
                {
                    sb.append_line("__activity?.SetTag(\"error\", true);");
                    sb.append_line("__activity?.SetTag(\"error.message\", __ex.Message);");
                    sb.append_line("throw;");
                }
            }
            else
            {
                sb.append_line("try");
                using (sb.block())
                {
                    if (method.return_type.Contains("Task"))
                        sb.append_line(
                            $"var __result = await {method.method_name}({string.Join(", ", method.parameters.Select(p => p.name))});");
                    else
                        sb.append_line(
                            $"var __result = {method.method_name}({string.Join(", ", method.parameters.Select(p => p.name))});");

                    sb.append_line("return __result;");
                }

                sb.append_line("catch (global::System.Exception __ex)");
                using (sb.block())
                {
                    sb.append_line("__activity?.SetTag(\"error\", true);");
                    sb.append_line("__activity?.SetTag(\"error.message\", __ex.Message);");
                    sb.append_line("throw;");
                }
            }
        }
    }

    /// <summary>
    ///     生成指标类型的源代码。
    /// </summary>
    private static string generate_metric_type_source(MetricInfo first, List<MetricInfo> metrics)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System;");
        sb.append_line();

        if (!string.IsNullOrEmpty(first.namespace_name))
        {
            sb.append_line($"namespace {first.namespace_name};");
            sb.append_line();
        }

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{first.type_name}</c> 的指标采集实现。");
        sb.append_line("/// </summary>");
        sb.append_line($"public partial class {first.type_name}");
        using (sb.block())
        {
            generate_metric_fields(sb, metrics);
            sb.append_line();
            generate_record_methods(sb, metrics);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成指标字段。
    /// </summary>
    private static void generate_metric_fields(SourceTextBuilder sb, List<MetricInfo> metrics)
    {
        sb.append_line("#region 指标字段");
        sb.append_line();

        foreach (var metric in metrics)
            if (metric.metric_type == "counter")
                sb.append_line($"private static int __{metric.member_name}_counter;");
            else if (metric.metric_type == "gauge")
                sb.append_line($"private static double __{metric.member_name}_gauge;");
            else if (metric.metric_type == "histogram")
                sb.append_line(
                    $"private static readonly global::System.Collections.Generic.List<double> __{metric.member_name}_histogram = new();");

        sb.append_line();
        sb.append_line("#endregion");
    }

    /// <summary>
    ///     生成指标记录方法。
    /// </summary>
    private static void generate_record_methods(SourceTextBuilder sb, List<MetricInfo> metrics)
    {
        sb.append_line("#region 指标记录方法");
        sb.append_line();

        foreach (var metric in metrics)
        {
            if (metric.metric_type == "counter")
            {
                sb.append_line("/// <summary>");
                sb.append_line($"/// 递增 <c>{metric.metric_name}</c> 计数器。");
                sb.append_line("/// </summary>");
                sb.append_line("/// <param name=\"value\">递增量，默认为 1。</param>");
                sb.append_line($"public static void record_{metric.member_name}(int value = 1)");
                using (sb.block())
                {
                    sb.append_line(
                        $"global::System.Threading.Interlocked.Add(ref __{metric.member_name}_counter, value);");
                }
            }
            else if (metric.metric_type == "gauge")
            {
                sb.append_line("/// <summary>");
                sb.append_line($"/// 设置 <c>{metric.metric_name}</c> 仪表盘值。");
                sb.append_line("/// </summary>");
                sb.append_line("/// <param name=\"value\">仪表盘值。</param>");
                sb.append_line($"public static void record_{metric.member_name}(double value)");
                using (sb.block())
                {
                    sb.append_line($"__{metric.member_name}_gauge = value;");
                }
            }
            else if (metric.metric_type == "histogram")
            {
                sb.append_line("/// <summary>");
                sb.append_line($"/// 记录 <c>{metric.metric_name}</c> 直方图观测值。");
                sb.append_line("/// </summary>");
                sb.append_line("/// <param name=\"value\">观测值。</param>");
                sb.append_line($"public static void record_{metric.member_name}(double value)");
                using (sb.block())
                {
                    sb.append_line($"lock (__{metric.member_name}_histogram)");
                    using (sb.block())
                    {
                        sb.append_line($"__{metric.member_name}_histogram.Add(value);");
                    }
                }
            }

            sb.append_line();
        }

        sb.append_line("#endregion");
    }

    #region 数据模型

    /// <summary>
    ///     可观测类型信息。
    /// </summary>
    internal readonly struct ObservableTypeInfo
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
        ///     可观测方法列表。
        /// </summary>
        public readonly List<ObservableMethodInfo> observable_methods;

        /// <summary>
        ///     初始化 <see cref="ObservableTypeInfo" /> 的新实例。
        /// </summary>
        public ObservableTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            List<ObservableMethodInfo> observableMethods)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            observable_methods = observableMethods;
        }
    }

    /// <summary>
    ///     可观测方法信息。
    /// </summary>
    internal readonly struct ObservableMethodInfo
    {
        /// <summary>
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     返回类型的完全限定名称。
        /// </summary>
        public readonly string return_type;

        /// <summary>
        ///     是否为 void 返回类型。
        /// </summary>
        public readonly bool is_void;

        /// <summary>
        ///     参数列表。
        /// </summary>
        public readonly List<ParameterInfo> parameters;

        /// <summary>
        ///     初始化 <see cref="ObservableMethodInfo" /> 的新实例。
        /// </summary>
        public ObservableMethodInfo(string methodName, string returnType, bool isVoid, List<ParameterInfo> parameters)
        {
            method_name = methodName;
            return_type = returnType;
            is_void = isVoid;
            this.parameters = parameters;
        }
    }

    /// <summary>
    ///     参数信息。
    /// </summary>
    internal readonly struct ParameterInfo
    {
        /// <summary>
        ///     参数名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     参数类型的完全限定名称。
        /// </summary>
        public readonly string type_full_name;

        /// <summary>
        ///     初始化 <see cref="ParameterInfo" /> 的新实例。
        /// </summary>
        public ParameterInfo(string name, string typeFullName)
        {
            this.name = name;
            type_full_name = typeFullName;
        }
    }

    /// <summary>
    ///     指标信息。
    /// </summary>
    internal readonly struct MetricInfo
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
        ///     成员名称。
        /// </summary>
        public readonly string member_name;

        /// <summary>
        ///     指标名称。
        /// </summary>
        public readonly string metric_name;

        /// <summary>
        ///     指标类型。
        /// </summary>
        public readonly string metric_type;

        /// <summary>
        ///     成员种类。
        /// </summary>
        public readonly MetricMemberKind member_kind;

        /// <summary>
        ///     返回类型的完全限定名称。
        /// </summary>
        public readonly string? return_type;

        /// <summary>
        ///     初始化 <see cref="MetricInfo" /> 的新实例。
        /// </summary>
        public MetricInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string memberName,
            string metricName,
            string metricType,
            MetricMemberKind memberKind,
            string? returnType)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            member_name = memberName;
            metric_name = metricName;
            metric_type = metricType;
            member_kind = memberKind;
            return_type = returnType;
        }
    }

    /// <summary>
    ///     指标成员种类。
    /// </summary>
    internal enum MetricMemberKind
    {
        /// <summary>
        ///     方法。
        /// </summary>
        method,

        /// <summary>
        ///     属性。
        /// </summary>
        property
    }

    #endregion
}