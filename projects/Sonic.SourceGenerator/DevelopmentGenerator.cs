using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为开发辅助特性自动生成桩代码，包括构建任务、属性测试、文档提取、模板渲染和文档转换。
/// </summary>
[Generator]
public sealed class DevelopmentGenerator : IIncrementalGenerator
{
    private const string _build_task_attribute_full_name = "Sonic.Standard.Compiler.BuildTask.BuildTaskAttribute";

    private const string _property_test_attribute_full_name =
        "Sonic.Standard.Compiler.Verification.PropertyTestAttribute";

    private const string _doc_example_attribute_full_name = "Sonic.Standard.Compiler.Document.DocExampleAttribute";
    private const string _template_attribute_full_name = "Sonic.Standard.Compiler.Document.Template.TemplateAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildTaskTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _build_task_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_build_task(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var propertyTestTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _property_test_attribute_full_name,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, ct) => transform_property_test(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var docExampleTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _doc_example_attribute_full_name,
                static (node, _) =>
                    node is ClassDeclarationSyntax or MethodDeclarationSyntax or PropertyDeclarationSyntax,
                static (ctx, ct) => transform_doc_example(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var templateTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _template_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_template(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(buildTaskTargets, generate_build_task_source);
        context.RegisterSourceOutput(propertyTestTargets, generate_property_test_source);
        context.RegisterSourceOutput(docExampleTargets, generate_doc_example_source);
        context.RegisterSourceOutput(templateTargets, generate_template_source);
    }

    /// <summary>
    ///     提取标记了 <c>[BuildTask]</c> 特性的类信息。
    /// </summary>
    private static BuildTaskTypeInfo? transform_build_task(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var buildTaskAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_build_task_attribute_full_name}");

        string? taskName = null;

        if (buildTaskAttr is not null)
            foreach (var named in buildTaskAttr.NamedArguments)
                if (named is { Key: "name", Value.Value: string n })
                    taskName = n;

        taskName ??= typeSymbol.Name;

        return new BuildTaskTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            taskName);
    }

    /// <summary>
    ///     提取标记了 <c>[PropertyTest]</c> 特性的方法信息。
    /// </summary>
    private static PropertyTestInfo? transform_property_test(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var methodSymbol = (IMethodSymbol)context.TargetSymbol;

        var propTestAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_property_test_attribute_full_name}");

        if (propTestAttr is null) return null;

        var iterations = 100;
        var maxShrinks = 100;

        foreach (var named in propTestAttr.NamedArguments)
        {
            if (named is { Key: "iterations", Value.Value: int it }) iterations = it;

            if (named is { Key: "max_shrinks", Value.Value: int ms }) maxShrinks = ms;
        }

        var containingType = methodSymbol.ContainingType;

        return new PropertyTestInfo(
            containingType.Name,
            containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            containingType.ContainingNamespace?.ToDisplayString(),
            methodSymbol.Name,
            iterations,
            maxShrinks);
    }

    /// <summary>
    ///     提取标记了 <c>[DocExample]</c> 特性的符号信息。
    /// </summary>
    private static DocExampleInfo? transform_doc_example(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var docAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_doc_example_attribute_full_name}");

        if (docAttr is null) return null;

        string? code = null;

        if (docAttr.ConstructorArguments.Length > 0 &&
            docAttr.ConstructorArguments[0].Value is string c)
            code = c;

        if (code is null) return null;

        var symbol = context.TargetSymbol;
        var containingType = symbol as INamedTypeSymbol ?? symbol.ContainingType;

        return new DocExampleInfo(
            containingType.Name,
            containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            containingType.ContainingNamespace?.ToDisplayString(),
            symbol.Name,
            symbol.Kind == SymbolKind.NamedType,
            code);
    }

    /// <summary>
    ///     提取标记了 <c>[Template]</c> 特性的类信息。
    /// </summary>
    private static TemplateTypeInfo? transform_template(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var templateAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_template_attribute_full_name}");

        string? templatePath = null;

        if (templateAttr is not null)
            foreach (var named in templateAttr.NamedArguments)
                if (named is { Key: "path", Value.Value: string p })
                    templatePath = p;

        return new TemplateTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            templatePath);
    }

    /// <summary>
    ///     生成构建任务桩代码。
    /// </summary>
    private static void generate_build_task_source(SourceProductionContext context,
        ImmutableArray<BuildTaskTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_build_task_impl(info);
            var hintName = $"{info.type_name}.BuildTask.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个构建任务的源代码。
    /// </summary>
    private static string generate_build_task_impl(BuildTaskTypeInfo info)
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

        sb.append_line($"partial class {info.type_name} : global::Sonic.Standard.Compiler.BuildTask.IBuildTask");
        using (sb.block())
        {
            sb.append_line("/// <summary>");
            sb.append_line("/// 构建任务名称。");
            sb.append_line("/// </summary>");
            sb.append_line($"public string task_name => \"{StringEscapeHelper.escape_for_string(info.task_name)}\";");
            sb.append_line();
            sb.append_line("/// <summary>");
            sb.append_line("/// 异步执行构建任务。");
            sb.append_line("/// </summary>");
            sb.append_line("/// <param name=\"cancellationToken\">取消令牌。</param>");
            sb.append_line(
                "public global::System.Threading.Tasks.Task execute(global::System.Threading.CancellationToken cancellationToken)");
            using (sb.block())
            {
                sb.append_line("cancellationToken.ThrowIfCancellationRequested();");
                sb.append_line("return execute_core(cancellationToken);");
            }

            sb.append_line();
            sb.append_line("/// <summary>");
            sb.append_line("/// 构建任务的核心执行逻辑，由子类实现。");
            sb.append_line("/// </summary>");
            sb.append_line("/// <param name=\"cancellationToken\">取消令牌。</param>");
            sb.append_line(
                "protected virtual global::System.Threading.Tasks.Task execute_core(global::System.Threading.CancellationToken cancellationToken)");
            using (sb.block())
            {
                sb.append_line("return global::System.Threading.Tasks.Task.CompletedTask;");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成属性测试运行器代码。
    /// </summary>
    private static void generate_property_test_source(SourceProductionContext context,
        ImmutableArray<PropertyTestInfo> testInfos)
    {
        var grouped = testInfos.GroupBy(t => t.containing_type_fqn).ToList();

        foreach (var group in grouped)
        {
            var first = group.First();
            var sourceText = generate_property_test_impl(first.containing_type_name, first.containing_type_fqn,
                first.namespace_name, [.. group]);
            var hintName = $"{first.containing_type_name}.PropertyTest.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成属性测试运行器的源代码。
    /// </summary>
    private static string generate_property_test_impl(string typeName, string fqn, string? ns,
        List<PropertyTestInfo> tests)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        if (!string.IsNullOrEmpty(ns))
        {
            sb.append_line($"namespace {ns};");
            sb.append_line();
        }

        sb.append_line($"partial class {typeName}");
        using (sb.block())
        {
            foreach (var test in tests)
            {
                sb.append_line("/// <summary>");
                sb.append_line($"/// 运行 <c>{test.method_name}</c> 属性测试。");
                sb.append_line("/// </summary>");
                sb.append_line($"public void run_{test.method_name}_property_test()");
                using (sb.block())
                {
                    sb.append_line($"for (var __i = 0; __i < {test.iterations}; __i++)");
                    using (sb.block())
                    {
                        sb.append_line("try");
                        using (sb.block())
                        {
                            sb.append_line($"{test.method_name}();");
                        }

                        sb.append_line("catch (global::System.Exception)");
                        using (sb.block())
                        {
                            sb.append_line($"if (__i >= {test.max_shrinks}) throw;");
                        }
                    }
                }

                sb.append_line();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成文档示例提取代码。
    /// </summary>
    private static void generate_doc_example_source(SourceProductionContext context,
        ImmutableArray<DocExampleInfo> infos)
    {
        var grouped = infos.GroupBy(i => i.containing_type_fqn).ToList();

        foreach (var group in grouped)
        {
            var first = group.First();
            var sourceText = generate_doc_example_impl(first.containing_type_name, first.containing_type_fqn,
                first.namespace_name, [.. group]);
            var hintName = $"{first.containing_type_name}.DocExample.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成文档示例提取的源代码。
    /// </summary>
    private static string generate_doc_example_impl(string typeName, string fqn, string? ns,
        List<DocExampleInfo> examples)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();

        if (!string.IsNullOrEmpty(ns))
        {
            sb.append_line($"namespace {ns};");
            sb.append_line();
        }

        sb.append_line($"partial class {typeName} : global::Sonic.Standard.Compiler.Document.IDocGenerator");
        using (sb.block())
        {
            sb.append_line("/// <summary>");
            sb.append_line("/// 生成文档内容。");
            sb.append_line("/// </summary>");
            sb.append_line("/// <returns>生成的文档字符串。</returns>");
            sb.append_line("public string generate()");
            using (sb.block())
            {
                sb.append_line("var __sb = new global::System.Text.StringBuilder();");

                foreach (var example in examples)
                {
                    sb.append_line(
                        $"__sb.AppendLine(\"{StringEscapeHelper.escape_for_string(example.symbol_name)}:\");");
                    sb.append_line($"__sb.AppendLine(\"  {StringEscapeHelper.escape_for_string(example.code)}\");");
                }

                sb.append_line("return __sb.ToString();");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成模板渲染代码。
    /// </summary>
    private static void generate_template_source(SourceProductionContext context,
        ImmutableArray<TemplateTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_template_impl(info);
            var hintName = $"{info.type_name}.Template.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成模板渲染的源代码。
    /// </summary>
    private static string generate_template_impl(TemplateTypeInfo info)
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

        sb.append_line(
            $"partial class {info.type_name} : global::Sonic.Standard.Compiler.Document.Template.ITemplateEngine");
        using (sb.block())
        {
            sb.append_line("/// <summary>");
            sb.append_line("/// 模板文件路径。");
            sb.append_line("/// </summary>");
            sb.append_line(
                $"public string? template_path => {(info.template_path is not null ? $"\"{StringEscapeHelper.escape_for_string(info.template_path)}\"" : "null")};");
            sb.append_line();
            sb.append_line("/// <summary>");
            sb.append_line("/// 使用指定的模型渲染模板。");
            sb.append_line("/// </summary>");
            sb.append_line("/// <param name=\"template\">模板内容。</param>");
            sb.append_line("/// <param name=\"model\">数据模型。</param>");
            sb.append_line("/// <returns>渲染后的字符串。</returns>");
            sb.append_line("public string render(string template, object model)");
            using (sb.block())
            {
                sb.append_line("return template;");
            }
        }

        return sb.ToString();
    }

    #region 数据模型

    /// <summary>
    ///     构建任务类型信息。
    /// </summary>
    internal readonly struct BuildTaskTypeInfo
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
        ///     构建任务名称。
        /// </summary>
        public readonly string task_name;

        /// <summary>
        ///     初始化 <see cref="BuildTaskTypeInfo" /> 的新实例。
        /// </summary>
        public BuildTaskTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string taskName)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            task_name = taskName;
        }
    }

    /// <summary>
    ///     属性测试信息。
    /// </summary>
    internal readonly struct PropertyTestInfo
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
        ///     方法名称。
        /// </summary>
        public readonly string method_name;

        /// <summary>
        ///     测试迭代次数。
        /// </summary>
        public readonly int iterations;

        /// <summary>
        ///     最大收缩次数。
        /// </summary>
        public readonly int max_shrinks;

        /// <summary>
        ///     初始化 <see cref="PropertyTestInfo" /> 的新实例。
        /// </summary>
        public PropertyTestInfo(
            string containingTypeName,
            string containingTypeFqn,
            string? namespaceName,
            string methodName,
            int iterations,
            int maxShrinks)
        {
            containing_type_name = containingTypeName;
            containing_type_fqn = containingTypeFqn;
            namespace_name = namespaceName;
            method_name = methodName;
            this.iterations = iterations;
            max_shrinks = maxShrinks;
        }
    }

    /// <summary>
    ///     文档示例信息。
    /// </summary>
    internal readonly struct DocExampleInfo
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
        ///     符号名称。
        /// </summary>
        public readonly string symbol_name;

        /// <summary>
        ///     是否为类型级别示例。
        /// </summary>
        public readonly bool is_type_level;

        /// <summary>
        ///     示例代码。
        /// </summary>
        public readonly string code;

        /// <summary>
        ///     初始化 <see cref="DocExampleInfo" /> 的新实例。
        /// </summary>
        public DocExampleInfo(
            string containingTypeName,
            string containingTypeFqn,
            string? namespaceName,
            string symbolName,
            bool isTypeLevel,
            string code)
        {
            containing_type_name = containingTypeName;
            containing_type_fqn = containingTypeFqn;
            namespace_name = namespaceName;
            symbol_name = symbolName;
            is_type_level = isTypeLevel;
            this.code = code;
        }
    }

    /// <summary>
    ///     模板类型信息。
    /// </summary>
    internal readonly struct TemplateTypeInfo
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
        ///     模板文件路径。
        /// </summary>
        public readonly string? template_path;

        /// <summary>
        ///     初始化 <see cref="TemplateTypeInfo" /> 的新实例。
        /// </summary>
        public TemplateTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string? templatePath)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            template_path = templatePath;
        }
    }

    #endregion
}