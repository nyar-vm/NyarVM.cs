using System.Runtime.CompilerServices;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Schema;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Template;
using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.Parser;

/// <summary>
///     Valkyrie 语法分析器
/// </summary>
public sealed class ValkyrieParser
{
    internal static readonly ConditionalWeakTable<TokenStream, GenericCloseState> _s_generic_close_states = new();
    private readonly DiagnosticSink? _diagnostics;
    private readonly ValkyrieLanguage _language;

    /// <summary>
    ///     使用默认语言创建分析器
    /// </summary>
    public ValkyrieParser()
    {
        _language = ValkyrieLanguage.standard;
    }

    /// <summary>
    ///     使用指定语言配置创建分析器
    /// </summary>
    /// <param name="language">语言配置。</param>
    public ValkyrieParser(ValkyrieLanguage language)
    {
        _language = language;
    }

    /// <summary>
    ///     使用指定语言配置和诊断接收器创建分析器
    /// </summary>
    /// <param name="language">语言配置。</param>
    /// <param name="diagnostics">诊断接收器。</param>
    public ValkyrieParser(ValkyrieLanguage language, DiagnosticSink? diagnostics)
    {
        _language = language;
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     解析词法单元列表并生成 AST
    /// </summary>
    /// <param name="tokens">词法单元列表。</param>
    /// <returns>AST 根节点。</returns>
    public ValkyrieNode parse(IReadOnlyList<GreenLeafNode> tokens)
    {
        var source = new TokenStream(tokens, ValkyrieNodeKindClassifier.instance, _diagnostics);
        var declarations = source.parse_top_level_nodes(_language);
        var normalizedDeclarations = normalize_declarations(declarations);

        return new ProgramRoot
        {
            declarations = normalizedDeclarations,
            file_path = string.Empty
        };
    }

    /// <summary>
    ///     统一按新版 ObjectBody 体系归一化声明节点
    /// </summary>
    private static IReadOnlyList<ValkyrieNode> normalize_declarations(IReadOnlyList<ValkyrieNode> declarations)
    {
        if (declarations.Count == 0) return declarations;

        var normalized = new List<ValkyrieNode>(declarations.Count);
        foreach (var declaration in declarations) normalized.Add(normalize_declaration_node(declaration));

        return normalized;
    }

    /// <summary>
    ///     递归归一化声明节点，确保对象体使用新版结构
    /// </summary>
    private static ValkyrieNode normalize_declaration_node(ValkyrieNode node)
    {
        switch (node)
        {
            case DeclareClass declareClass:
                return declareClass with
                {
                    body = normalize_object_body(declareClass.body)
                };
            case DeclareStructure declareStructure:
                return declareStructure with
                {
                    body = normalize_object_body(declareStructure.body)
                };
            case DeclareTrait declareTrait:
                return declareTrait with
                {
                    body = normalize_object_body(declareTrait.body)
                };
            case DeclareModel declareModel:
                return declareModel with
                {
                    body = normalize_object_body(declareModel.body)
                };
            case DeclareService declareService:
                return declareService with
                {
                    body = normalize_object_body(declareService.body)
                };
            case DeclareObjectDomain declareObjectDomain:
                return normalize_domain(declareObjectDomain);
            case DeclareUniteVariant declareUniteVariant:
                return declareUniteVariant with
                {
                    body = normalize_object_body(declareUniteVariant.body)
                };
            case DeclareUnite declareUnite:
            {
                var normalizedVariants = declareUnite.variants
                    .Select(v => normalize_declaration_node(v) as DeclareUniteVariant).Where(v => v != null)
                    .Cast<DeclareUniteVariant>().ToList();
                var normalizedMethods = declareUnite.methods
                    .Select(m => normalize_declaration_node(m) as DeclareObjectMethod).Where(m => m != null)
                    .Cast<DeclareObjectMethod>().ToList();
                return declareUnite with { variants = normalizedVariants, methods = normalizedMethods };
            }
            case DeclareObjectField declareObjectField:
                return declareObjectField;
            case DeclareMicro declareMicro:
                return declareMicro with
                {
                    body = normalize_function_body(declareMicro.body)
                };
            case DeclareImply declareImply:
            {
                var normalizedMethods = declareImply.methods
                    .Select(m => normalize_declaration_node(m) as DeclareObjectMethod).Where(m => m != null)
                    .Cast<DeclareObjectMethod>().ToList();
                return declareImply with { methods = normalizedMethods };
            }
            case DeclareObjectMethod declareObjectMethod:
                return declareObjectMethod with
                {
                    body = normalize_function_body(declareObjectMethod.body)
                };
            case DeclareNamespace declareNamespace:
            {
                var nestedDeclarations = normalize_declarations(declareNamespace.declarations);
                return declareNamespace with { declarations = nestedDeclarations };
            }
            case TemplateMatchNode matchTemplate:
            {
                return matchTemplate;
            }
            case TemplateIfNode ifTemplate:
            {
                var normalizedThen = normalize_declarations(ifTemplate.then_body);
                var normalizedElse = ifTemplate.else_body != null ? normalize_declarations(ifTemplate.else_body) : null;
                return new TemplateIfNode(ifTemplate.condition, normalizedThen, normalizedElse);
            }
            case TemplateLoopNode loopTemplate:
            {
                var normalizedBody = normalize_declarations(loopTemplate.body);
                return new TemplateLoopNode(loopTemplate.variable_name, loopTemplate.range_start,
                    loopTemplate.range_end,
                    normalizedBody);
            }
            default:
                return node;
        }
    }

    /// <summary>
    ///     归一化子域节点，确保其对象体符合新版结构
    /// </summary>
    private static DeclareObjectDomain normalize_domain(DeclareObjectDomain domain)
    {
        return domain with
        {
            body = normalize_object_body(domain.body)
        };
    }

    /// <summary>
    ///     归一化函数体中的语句列表
    /// </summary>
    private static FunctionBody? normalize_function_body(FunctionBody? body)
    {
        if (body is null) return null;

        var normalizedStatements = normalize_declarations(body.statements);
        return body with { statements = normalizedStatements };
    }

    /// <summary>
    ///     归一化对象体并递归处理子域
    /// </summary>
    private static ObjectBody normalize_object_body(ObjectBody? body)
    {
        var normalizedBody = body ?? new ObjectBody();

        var normalizedMethods = normalizedBody.methods
            .Select(m => normalize_declaration_node(m) as DeclareObjectMethod)
            .Where(m => m != null)
            .Cast<DeclareObjectMethod>()
            .ToList();

        var normalizedAssociatedTypes = normalizedBody.associated_types
            .Select(t => normalize_declaration_node(t) as DeclareAssociatedType)
            .Where(t => t != null)
            .Cast<DeclareAssociatedType>()
            .ToList();

        var normalizedFields = normalizedBody.fields
            .Select(f => normalize_declaration_node(f) as DeclareObjectField)
            .Where(f => f != null)
            .Cast<DeclareObjectField>()
            .ToList();

        if (normalizedBody.domains.Count == 0)
            return normalizedBody with
            {
                methods = normalizedMethods,
                associated_types = normalizedAssociatedTypes,
                fields = normalizedFields
            };

        var normalizedDomains = new List<DeclareObjectDomain>(normalizedBody.domains.Count);
        foreach (var domain in normalizedBody.domains) normalizedDomains.Add(normalize_domain(domain));

        return normalizedBody with
        {
            domains = normalizedDomains,
            methods = normalizedMethods,
            associated_types = normalizedAssociatedTypes,
            fields = normalizedFields
        };
    }
}