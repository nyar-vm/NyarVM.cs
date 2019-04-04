using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[SearchDocument]</c> 特性的类自动生成搜索索引定义类，
///     根据属性上的 <c>[FullText]</c> 和 <c>[Keyword]</c> 特性生成索引字段配置。
/// </summary>
[Generator]
public sealed class SearchIndexGenerator : IIncrementalGenerator
{
    private const string _search_document_attribute_full_name = "Sonic.Standard.Data.Search.SearchDocumentAttribute";
    private const string _full_text_attribute_full_name = "Sonic.Standard.Data.Search.FullTextAttribute";
    private const string _keyword_attribute_full_name = "Sonic.Standard.Data.Search.KeywordAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _search_document_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_search_document(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(targetTypes, generate_source);
    }

    /// <summary>
    ///     提取需要生成搜索索引的类型信息。
    /// </summary>
    private static SearchDocumentTypeInfo? transform_search_document(GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var searchDocAttr = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
            $"global::{_search_document_attribute_full_name}");

        if (searchDocAttr is null) return null;

        string? indexName = null;

        foreach (var named in searchDocAttr.NamedArguments)
            if (named is { Key: "index_name", Value.Value: string n })
                indexName = n;

        indexName ??= typeSymbol.Name.ToLowerInvariant();

        var fields = extract_search_fields(typeSymbol);

        return new SearchDocumentTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            indexName,
            fields);
    }

    /// <summary>
    ///     提取类型的搜索字段信息。
    /// </summary>
    private static List<SearchFieldInfo> extract_search_fields(INamedTypeSymbol typeSymbol)
    {
        var fields = new List<SearchFieldInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.IsStatic) continue;

            if (member.DeclaredAccessibility != Accessibility.Public) continue;

            if (member is not IPropertySymbol prop) continue;

            if (prop.IsWriteOnly) continue;

            var isFullText = false;
            string? analyzer = null;
            var isKeyword = false;

            var fullTextAttr = prop.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_full_text_attribute_full_name}");

            if (fullTextAttr is not null)
            {
                isFullText = true;

                foreach (var named in fullTextAttr.NamedArguments)
                    if (named is { Key: "analyzer", Value.Value: string an })
                        analyzer = an;
            }

            var keywordAttr = prop.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_keyword_attribute_full_name}");

            if (keywordAttr is not null) isKeyword = true;

            if (!isFullText && !isKeyword) continue;

            fields.Add(new SearchFieldInfo(
                prop.Name,
                prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                isFullText,
                isKeyword,
                analyzer));
        }

        return fields;
    }

    /// <summary>
    ///     生成所有类型的搜索索引源代码。
    /// </summary>
    private static void generate_source(SourceProductionContext context,
        ImmutableArray<SearchDocumentTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_search_index_source(info);
            var hintName = $"{info.type_name}.SearchIndex.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个类型的搜索索引定义源代码。
    /// </summary>
    private static string generate_search_index_source(SearchDocumentTypeInfo info)
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

        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{info.type_name}</c> 的搜索索引定义。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"public sealed class {info.type_name}SearchIndex : global::Sonic.Standard.Data.Search.ISearchIndex<{info.fully_qualified_name}>");
        using (sb.block())
        {
            generate_index_name_property(sb, info);
            sb.append_line();
            generate_fields_property(sb, info);
            sb.append_line();
            generate_inner_index_field(sb, info);
            sb.append_line();
            generate_constructor(sb, info);
            sb.append_line();
            generate_index_method(sb, info);
            sb.append_line();
            generate_search_method(sb, info);
            sb.append_line();
            generate_remove_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>index_name</c> 属性。
    /// </summary>
    private static void generate_index_name_property(SourceTextBuilder sb, SearchDocumentTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取索引名称。");
        sb.append_line("/// </summary>");
        sb.append_line($"public string index_name => \"{StringEscapeHelper.escape_for_string(info.index_name)}\";");
    }

    /// <summary>
    ///     生成 <c>fields</c> 属性。
    /// </summary>
    private static void generate_fields_property(SourceTextBuilder sb, SearchDocumentTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 获取索引字段定义列表。");
        sb.append_line("/// </summary>");
        sb.append_line(
            "public global::System.Collections.Generic.IReadOnlyList<global::Sonic.Standard.Data.Search.SearchFieldDefinition> fields => new global::System.Collections.Generic.List<global::Sonic.Standard.Data.Search.SearchFieldDefinition>");
        using (sb.block())
        {
            foreach (var field in info.fields)
            {
                var fieldType = field.is_full_text ? "FullText" : "Keyword";
                var analyzerArg = field.analyzer is not null
                    ? $"\"{StringEscapeHelper.escape_for_string(field.analyzer)}\""
                    : "null";
                sb.append_line(
                    $"new global::Sonic.Standard.Data.Search.SearchFieldDefinition(\"{field.name}\", global::Sonic.Standard.Data.Search.SearchFieldType.{fieldType}, {analyzerArg}),");
            }
        }

        sb.append_line(";");
    }

    /// <summary>
    ///     生成内部索引字段。
    /// </summary>
    private static void generate_inner_index_field(SourceTextBuilder sb, SearchDocumentTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 内部内存索引实例。");
        sb.append_line("/// </summary>");
        sb.append_line(
            $"private readonly global::Sonic.Standard.Data.Search.InMemorySearchIndex<{info.fully_qualified_name}> _inner;");
    }

    /// <summary>
    ///     生成构造函数。
    /// </summary>
    private static void generate_constructor(SourceTextBuilder sb, SearchDocumentTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// 初始化 <c>{info.type_name}SearchIndex</c> 的新实例。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"analyzer\">字段分词器，为 null 时使用默认分词器。</param>");
        sb.append_line(
            $"public {info.type_name}SearchIndex(global::Sonic.Standard.Data.Search.IFieldAnalyzer? analyzer = null)");
        using (sb.block())
        {
            sb.append_line("_inner = new global::Sonic.Standard.Data.Search.InMemorySearchIndex<" +
                           info.fully_qualified_name +
                           ">(analyzer ?? new global::Sonic.Standard.Data.Search.DefaultFieldAnalyzer());");
            sb.append_line();

            foreach (var field in info.fields)
                if (field.is_full_text)
                {
                    var analyzerArg = field.analyzer is not null
                        ? $"\"{StringEscapeHelper.escape_for_string(field.analyzer)}\""
                        : "null";
                    sb.append_line($"_inner.register_full_text_field(\"{field.name}\", {analyzerArg});");
                }
        }
    }

    /// <summary>
    ///     生成 <c>index</c> 方法。
    /// </summary>
    private static void generate_index_method(SourceTextBuilder sb, SearchDocumentTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 异步索引文档。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"document\">要索引的文档。</param>");
        sb.append_line($"public global::System.Threading.Tasks.Task index({info.fully_qualified_name} document)");
        using (sb.block())
        {
            sb.append_line("return _inner.index(document);");
        }
    }

    /// <summary>
    ///     生成 <c>search</c> 方法。
    /// </summary>
    private static void generate_search_method(SourceTextBuilder sb, SearchDocumentTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 异步搜索文档。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"query\">搜索查询。</param>");
        sb.append_line("/// <returns>搜索结果列表。</returns>");
        sb.append_line(
            $"public global::System.Threading.Tasks.Task<global::System.Collections.Generic.IReadOnlyList<{info.fully_qualified_name}>> search(string query)");
        using (sb.block())
        {
            sb.append_line("return _inner.search(query);");
        }
    }

    /// <summary>
    ///     生成 <c>remove</c> 方法。
    /// </summary>
    private static void generate_remove_method(SourceTextBuilder sb, SearchDocumentTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 异步移除文档索引。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"document\">要移除的文档。</param>");
        sb.append_line($"public global::System.Threading.Tasks.Task remove({info.fully_qualified_name} document)");
        using (sb.block())
        {
            sb.append_line("return _inner.remove(document);");
        }
    }

    #region 数据模型

    /// <summary>
    ///     需要生成搜索索引的类型信息。
    /// </summary>
    internal readonly struct SearchDocumentTypeInfo
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
        ///     索引名称。
        /// </summary>
        public readonly string index_name;

        /// <summary>
        ///     搜索字段列表。
        /// </summary>
        public readonly List<SearchFieldInfo> fields;

        /// <summary>
        ///     初始化 <see cref="SearchDocumentTypeInfo" /> 的新实例。
        /// </summary>
        public SearchDocumentTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string indexName,
            List<SearchFieldInfo> fields)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            index_name = indexName;
            this.fields = fields;
        }
    }

    /// <summary>
    ///     搜索字段信息。
    /// </summary>
    internal readonly struct SearchFieldInfo
    {
        /// <summary>
        ///     字段名称。
        /// </summary>
        public readonly string name;

        /// <summary>
        ///     字段类型完全限定名称。
        /// </summary>
        public readonly string Type;

        /// <summary>
        ///     是否为全文索引字段。
        /// </summary>
        public readonly bool is_full_text;

        /// <summary>
        ///     是否为关键词字段。
        /// </summary>
        public readonly bool is_keyword;

        /// <summary>
        ///     分词器名称。
        /// </summary>
        public readonly string? analyzer;

        /// <summary>
        ///     初始化 <see cref="SearchFieldInfo" /> 的新实例。
        /// </summary>
        public SearchFieldInfo(
            string name,
            string type,
            bool isFullText,
            bool isKeyword,
            string? analyzer)
        {
            this.name = name;
            Type = type;
            is_full_text = isFullText;
            is_keyword = isKeyword;
            this.analyzer = analyzer;
        }
    }

    #endregion
}