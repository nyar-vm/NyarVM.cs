using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 语法树构建器 —— 创建和管理 Valkyrie 强类型语法树
/// </summary>
public static class ValkyrieSyntaxTreeBuilder
{
    /// <summary>
    ///     Valkyrie 语法节点类型的 NodeKind 起始值，避免与 Oak 内置类型冲突
    /// </summary>
    public const int kind_base = 10000;

    private static bool _registered;

    private static readonly Dictionary<string, ValkyrieTokenKind> _keyword_map = new()
    {
        { "let", ValkyrieTokenKind.let },
        { "micro", ValkyrieTokenKind.micro },
        { "mezzo", ValkyrieTokenKind.mezzo },
        { "macro", ValkyrieTokenKind.macro },
        { "component", ValkyrieTokenKind.component },
        { "system", ValkyrieTokenKind.system },
        { "widget", ValkyrieTokenKind.widget },
        { "return", ValkyrieTokenKind.@return },
        { "if", ValkyrieTokenKind.@if },
        { "else", ValkyrieTokenKind.@else },
        { "loop", ValkyrieTokenKind.loop },
        { "while", ValkyrieTokenKind.@while },
        { "until", ValkyrieTokenKind.until },
        { "structure", ValkyrieTokenKind.structure },
        { "match", ValkyrieTokenKind.match },
        { "case", ValkyrieTokenKind.@case },
        { "end", ValkyrieTokenKind.end },
        { "namespace", ValkyrieTokenKind.@namespace },
        { "using", ValkyrieTokenKind.@using },
        { "class", ValkyrieTokenKind.@class },
        { "enums", ValkyrieTokenKind.enums },
        { "flags", ValkyrieTokenKind.flags },
        { "union", ValkyrieTokenKind.union },
        { "in", ValkyrieTokenKind.@in },
        { "break", ValkyrieTokenKind.@break },
        { "continue", ValkyrieTokenKind.@continue },
        { "resume", ValkyrieTokenKind.resume },
        { "catch", ValkyrieTokenKind.@catch },
        { "unite", ValkyrieTokenKind.unite },
        { "type", ValkyrieTokenKind.type },
        { "where", ValkyrieTokenKind.where },
        { "model", ValkyrieTokenKind.model },
        { "service", ValkyrieTokenKind.service },
        { "message", ValkyrieTokenKind.message },
        { "trait", ValkyrieTokenKind.trait },
        { "neural", ValkyrieTokenKind.neural },
        { "shader", ValkyrieTokenKind.shader },
        { "vertex", ValkyrieTokenKind.vertex },
        { "fragment", ValkyrieTokenKind.fragment },
        { "compute", ValkyrieTokenKind.compute },
        { "uniform", ValkyrieTokenKind.uniform },
        { "varying", ValkyrieTokenKind.varying },
        { "cbuffer", ValkyrieTokenKind.c_buffer },
        { "texture", ValkyrieTokenKind.texture },
        { "sampler", ValkyrieTokenKind.sampler },
        { "discard", ValkyrieTokenKind.discard },
        { "raygen", ValkyrieTokenKind.raygen },
        { "closesthit", ValkyrieTokenKind.closesthit },
        { "anyhit", ValkyrieTokenKind.anyhit },
        { "miss", ValkyrieTokenKind.miss },
        { "constant", ValkyrieTokenKind.constant },
        { "binding", ValkyrieTokenKind.binding },
        { "is", ValkyrieTokenKind.@is },
        { "as", ValkyrieTokenKind.@as }
    };

    /// <summary>
    ///     获取 CompilationUnit 的 NodeKind
    /// </summary>
    public static NodeKind compilation_unit_kind => new(kind_base + 99);

    /// <summary>
    ///     获取 ComponentDecl 的 NodeKind
    /// </summary>
    public static NodeKind component_kind => new(kind_base + 0);

    /// <summary>
    ///     获取 SystemDecl 的 NodeKind
    /// </summary>
    public static NodeKind system_kind => new(kind_base + 1);

    /// <summary>
    ///     获取 WidgetDecl 的 NodeKind
    /// </summary>
    public static NodeKind widget_kind => new(kind_base + 2);

    /// <summary>
    ///     获取 FunctionDecl 的 NodeKind
    /// </summary>
    public static NodeKind function_kind => new(kind_base + 3);

    /// <summary>
    ///     获取 EnumDecl 的 NodeKind
    /// </summary>
    public static NodeKind enum_kind => new(kind_base + 4);

    /// <summary>
    ///     获取 ImportDecl 的 NodeKind
    /// </summary>
    public static NodeKind import_kind => new(kind_base + 5);

    /// <summary>
    ///     获取 VariableDecl 的 NodeKind
    /// </summary>
    public static NodeKind variable_kind => new(kind_base + 6);

    /// <summary>
    ///     获取 FieldDecl 的 NodeKind
    /// </summary>
    public static NodeKind field_kind => new(kind_base + 7);

    /// <summary>
    ///     获取 ShaderDecl 的 NodeKind
    /// </summary>
    public static NodeKind shader_kind => new(kind_base + 8);

    /// <summary>
    ///     获取 StorageDecl 的 NodeKind
    /// </summary>
    public static NodeKind storage_kind => new(kind_base + 9);

    /// <summary>
    ///     获取 ServiceDecl 的 NodeKind
    /// </summary>
    public static NodeKind service_kind => new(kind_base + 10);

    /// <summary>
    ///     获取 NamespaceDecl 的 NodeKind
    /// </summary>
    public static NodeKind namespace_kind => new(kind_base + 11);

    /// <summary>
    ///     获取 ParameterDecl 的 NodeKind
    /// </summary>
    public static NodeKind parameter_kind => new(kind_base + 12);

    /// <summary>
    ///     获取 QueryDecl 的 NodeKind
    /// </summary>
    public static NodeKind query_kind => new(kind_base + 13);

    /// <summary>
    ///     向 NodeFactory 注册所有 Valkyrie 语法节点类型的构造委托
    /// </summary>
    public static void register_node_kinds()
    {
        if (_registered) return;

        _registered = true;

        NodeFactory.register(kind_base + 0, (g, t, o) => new ComponentSyntax(g, t, o));
        NodeFactory.register(kind_base + 1, (g, t, o) => new SystemSyntax(g, t, o));
        NodeFactory.register(kind_base + 2, (g, t, o) => new WidgetSyntax(g, t, o));
        NodeFactory.register(kind_base + 3, (g, t, o) => new FunctionSyntax(g, t, o));
        NodeFactory.register(kind_base + 4, (g, t, o) => new EnumSyntax(g, t, o));
        NodeFactory.register(kind_base + 5, (g, t, o) => new ImportSyntax(g, t, o));
        NodeFactory.register(kind_base + 6, (g, t, o) => new VariableSyntax(g, t, o));
        NodeFactory.register(kind_base + 7, (g, t, o) => new FieldSyntax(g, t, o));
        NodeFactory.register(kind_base + 8, (g, t, o) => new ShaderSyntax(g, t, o));
        NodeFactory.register(kind_base + 9, (g, t, o) => new StorageSyntax(g, t, o));
        NodeFactory.register(kind_base + 10, (g, t, o) => new ServiceSyntax(g, t, o));
        NodeFactory.register(kind_base + 11, (g, t, o) => new NamespaceSyntax(g, t, o));
        NodeFactory.register(kind_base + 12, (g, t, o) => new ParameterSyntax(g, t, o));
        NodeFactory.register(kind_base + 13, (g, t, o) => new QuerySyntax(g, t, o));

        NodeFactory.register(kind_base + 99, (g, t, o) => new ValkyrieSyntaxRoot(g, t, o));
    }

    /// <summary>
    ///     从 ISource 和 CstBuilder 构建操作创建 SyntaxTree
    /// </summary>
    public static SyntaxTree build(ISource source, Action<CstBuilder> buildAction)
    {
        register_node_kinds();

        var builder = new CstBuilder();
        buildAction(builder);
        var green = builder.build();

        return new SyntaxTree(source, green, true);
    }

    /// <summary>
    ///     从字符串源码和 CstBuilder 构建操作创建 SyntaxTree
    /// </summary>
    public static SyntaxTree build(string source, Action<CstBuilder> buildAction)
    {
        return build(new StringSource(source), buildAction);
    }

    /// <summary>
    ///     从源文本和绿树根节点创建 SyntaxTree（启用父节点缓存）
    /// </summary>
    public static SyntaxTree build_from_green(ISource source, GreenNode greenRoot)
    {
        register_node_kinds();
        return new SyntaxTree(source, greenRoot, true);
    }

    /// <summary>
    ///     构建 CompilationUnit 的绿树节点
    /// </summary>
    public static GreenNode build_compilation_unit_green(params GreenNode[] declarations)
    {
        var b = new CstBuilder(256);
        b.begin_node(compilation_unit_kind);
        foreach (var decl in declarations) b.add_node(decl);

        b.end_node();
        return b.build();
    }

    /// <summary>
    ///     构建 Component 的绿树节点
    /// </summary>
    public static GreenNode build_component_green(string name, params GreenNode[] fields)
    {
        var b = new CstBuilder(256);
        b.begin_node(component_kind);
        b.add_token(ValkyrieTokenKind.component.to_node_kind(), "component");
        b.add_token(ValkyrieTokenKind.identifier.to_node_kind(), name);
        b.add_token(ValkyrieTokenKind.brace_l.to_node_kind(), "{");

        foreach (var field in fields) b.add_node(field);

        b.add_token(ValkyrieTokenKind.brace_r.to_node_kind(), "}");
        b.end_node();
        return b.build();
    }

    /// <summary>
    ///     构建 Field 的绿树节点
    /// </summary>
    public static GreenNode build_field_green(string name, string type, string? defaultValue = null)
    {
        var b = new CstBuilder(256);
        b.begin_node(field_kind);
        b.add_token(ValkyrieTokenKind.identifier.to_node_kind(), name);
        b.add_token(ValkyrieTokenKind.colon.to_node_kind(), ":");
        b.add_token(ValkyrieTokenKind.identifier.to_node_kind(), type);

        if (defaultValue != null)
        {
            b.add_token(ValkyrieTokenKind.equal.to_node_kind(), "=");
            b.add_token(ValkyrieTokenKind.number.to_node_kind(), defaultValue);
        }

        b.end_node();
        return b.build();
    }

    /// <summary>
    ///     构建 Identifier Token 绿树叶节点
    /// </summary>
    public static GreenNode build_identifier_token(string name)
    {
        return new GreenLeafNode(ValkyrieTokenKind.identifier.to_node_kind(), name.Length, name);
    }

    /// <summary>
    ///     构建关键字 Token 绿树叶节点
    /// </summary>
    public static GreenNode build_keyword_token(string keyword)
    {
        if (_keyword_map.TryGetValue(keyword, out var kind))
            return new GreenLeafNode(kind.to_node_kind(), keyword.Length, keyword);

        return new GreenLeafNode(ValkyrieTokenKind.identifier.to_node_kind(), keyword.Length, keyword);
    }

    /// <summary>
    ///     尝试从 SyntaxTree 获取 Valkyrie 语法根
    /// </summary>
    public static ValkyrieSyntaxRoot? get_valkyrie_root(SyntaxTree tree)
    {
        if (!_registered) register_node_kinds();

        var node = NodeFactory.create(compilation_unit_kind, tree.root, tree, 0);
        return node as ValkyrieSyntaxRoot;
    }
}