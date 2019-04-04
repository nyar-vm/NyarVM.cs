using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[Chart]</c> 特性的类型自动生成 <c>IChart</c> 实现，
///     包含 <c>render</c> 方法以及系列和轴的渲染胶水代码。
/// </summary>
[Generator]
public sealed class ChartGenerator : IIncrementalGenerator
{
    private const string _chart_attribute_full_name = "Sonic.Standard.Graphic.Chart.ChartAttribute";
    private const string _series_attribute_full_name = "Sonic.Standard.Graphic.Chart.SeriesAttribute";
    private const string _axis_attribute_full_name = "Sonic.Standard.Graphic.Chart.AxisAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _chart_attribute_full_name,
                static (node, _) => node is TypeDeclarationSyntax typeDecl &&
                                    typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_chart(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成图表代码的类型信息。
    /// </summary>
    private static ChartTypeInfo? transform_chart(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var chartAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_chart_attribute_full_name}");

        var chartWidth = 800;
        var chartHeight = 600;

        if (chartAttr is not null)
            foreach (var named in chartAttr.NamedArguments)
            {
                if (named is { Key: "width", Value.Value: int w }) chartWidth = w;

                if (named is { Key: "height", Value.Value: int h }) chartHeight = h;
            }

        var series = new List<SeriesInfo>();
        var axes = new List<AxisInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            if (member.DeclaredAccessibility != Accessibility.Public) continue;

            if (member is IPropertySymbol prop)
            {
                var seriesAttr = prop.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                    $"global::{_series_attribute_full_name}");

                if (seriesAttr is not null)
                {
                    string? seriesName = null;
                    string? seriesColor = null;

                    foreach (var named in seriesAttr.NamedArguments)
                    {
                        if (named is { Key: "name", Value.Value: string sn }) seriesName = sn;

                        if (named is { Key: "color", Value.Value: string sc }) seriesColor = sc;
                    }

                    series.Add(new SeriesInfo(
                        prop.Name,
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        seriesName ?? prop.Name,
                        seriesColor));
                    continue;
                }

                var axisAttr = prop.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                    $"global::{_axis_attribute_full_name}");

                if (axisAttr is not null)
                {
                    string? axisType = null;

                    foreach (var named in axisAttr.NamedArguments)
                        if (named is { Key: "type", Value.Value: string at })
                            axisType = at;

                    axes.Add(new AxisInfo(
                        prop.Name,
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        axisType));
                }
            }
        }

        return new ChartTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            typeSymbol.TypeKind == TypeKind.Struct,
            chartWidth,
            chartHeight,
            series,
            axes);
    }

    /// <summary>
    ///     生成所有图表类型的源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context, ImmutableArray<ChartTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_chart_source(info);
            var hintName = $"{info.type_name}.Chart.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的图表实现源代码。
    /// </summary>
    private static string generate_chart_source(ChartTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        var typeKeyword = info.is_struct ? "partial struct" : "partial class";
        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的 <c>IChart</c> 实现。");
        sb.append_line("/// </summary>");
        sb.append_line($"{typeKeyword} {info.type_name} : global::Sonic.Standard.Graphic.Chart.IChart");
        using (sb.block())
        {
            generate_chart_properties(sb, info);
            sb.append_line();
            generate_render_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成图表的配置属性。
    /// </summary>
    private static void generate_chart_properties(SourceTextBuilder sb, ChartTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取图表宽度。");
        sb.append_line("/// </summary>");
        sb.append_line($"public int chart_width => {info.chart_width};");
        sb.append_line();
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取图表高度。");
        sb.append_line("/// </summary>");
        sb.append_line($"public int chart_height => {info.chart_height};");
    }

    /// <summary>
    ///     生成 <c>render</c> 方法体。
    /// </summary>
    private static void generate_render_method(SourceTextBuilder sb, ChartTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 在画布上渲染 <c>{info.type_name}</c> 图表。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"canvas\">目标画布。</param>");
        sb.append_line("public void render(global::Sonic.Standard.Graphic.Canvas.ICanvas canvas)");
        using (sb.block())
        {
            sb.append_line("canvas.clear();");
            sb.append_line();

            if (info.axes.Count > 0)
            {
                sb.append_line("#region 渲染坐标轴");
                sb.append_line();

                foreach (var axis in info.axes)
                    sb.append_line($"render_axis(canvas, \"{axis.property_name}\", {axis.property_name});");

                sb.append_line();
                sb.append_line("#endregion");
                sb.append_line();
            }

            if (info.series.Count > 0)
            {
                sb.append_line("#region 渲染数据系列");
                sb.append_line();

                foreach (var series in info.series)
                    sb.append_line($"render_series(canvas, \"{series.display_name}\", {series.property_name});");

                sb.append_line();
                sb.append_line("#endregion");
            }
        }
    }

    #region 数据模型

    /// <summary>
    ///     需要生成图表实现的目标类型信息。
    /// </summary>
    internal readonly struct ChartTypeInfo
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
        ///     是否为结构体。
        /// </summary>
        public readonly bool is_struct;

        /// <summary>
        ///     图表宽度。
        /// </summary>
        public readonly int chart_width;

        /// <summary>
        ///     图表高度。
        /// </summary>
        public readonly int chart_height;

        /// <summary>
        ///     数据系列列表。
        /// </summary>
        public readonly List<SeriesInfo> series;

        /// <summary>
        ///     坐标轴列表。
        /// </summary>
        public readonly List<AxisInfo> axes;

        /// <summary>
        ///     初始化 <see cref="ChartTypeInfo" /> 的新实例。
        /// </summary>
        public ChartTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            bool isStruct,
            int chartWidth,
            int chartHeight,
            List<SeriesInfo> series,
            List<AxisInfo> axes)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            is_struct = isStruct;
            chart_width = chartWidth;
            chart_height = chartHeight;
            this.series = series;
            this.axes = axes;
        }
    }

    /// <summary>
    ///     数据系列信息。
    /// </summary>
    internal readonly struct SeriesInfo
    {
        /// <summary>
        ///     属性名称。
        /// </summary>
        public readonly string property_name;

        /// <summary>
        ///     属性类型的完全限定名称。
        /// </summary>
        public readonly string type_full_name;

        /// <summary>
        ///     显示名称。
        /// </summary>
        public readonly string display_name;

        /// <summary>
        ///     系列颜色，为 null 时使用默认颜色。
        /// </summary>
        public readonly string? color;

        /// <summary>
        ///     初始化 <see cref="SeriesInfo" /> 的新实例。
        /// </summary>
        public SeriesInfo(string propertyName, string typeFullName, string displayName, string? color)
        {
            property_name = propertyName;
            type_full_name = typeFullName;
            display_name = displayName;
            this.color = color;
        }
    }

    /// <summary>
    ///     坐标轴信息。
    /// </summary>
    internal readonly struct AxisInfo
    {
        /// <summary>
        ///     属性名称。
        /// </summary>
        public readonly string property_name;

        /// <summary>
        ///     属性类型的完全限定名称。
        /// </summary>
        public readonly string type_full_name;

        /// <summary>
        ///     轴类型，为 null 时使用默认类型。
        /// </summary>
        public readonly string? axis_type;

        /// <summary>
        ///     初始化 <see cref="AxisInfo" /> 的新实例。
        /// </summary>
        public AxisInfo(string propertyName, string typeFullName, string? axisType)
        {
            property_name = propertyName;
            type_full_name = typeFullName;
            axis_type = axisType;
        }
    }

    #endregion
}