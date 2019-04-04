using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     为标记了 <c>[GenerateLexer]</c> 或 <c>[GenerateParser]</c> 特性的类自动生成
///     词法分析器和语法分析器代码，识别 <c>[Grammar]</c> 标记的语法定义类。
/// </summary>
[Generator]
public sealed class LexerParserGenerator : IIncrementalGenerator
{
    private const string _generate_lexer_attribute_full_name = "Sonic.Standard.Compiler.GenerateLexerAttribute";
    private const string _generate_parser_attribute_full_name = "Sonic.Standard.Compiler.GenerateParserAttribute";
    private const string _grammar_attribute_full_name = "Sonic.Standard.Compiler.GrammarAttribute";

    /// <summary>
    ///     初始化增量源代码生成管道。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lexerTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _generate_lexer_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_lexer(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        var parserTargets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _generate_parser_attribute_full_name,
                static (node, _) => node is ClassDeclarationSyntax classDecl &&
                                    classDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, ct) => transform_parser(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value)
            .Collect();

        context.RegisterSourceOutput(lexerTargets, generate_lexer_source);
        context.RegisterSourceOutput(parserTargets, generate_parser_source);
    }

    /// <summary>
    ///     提取标记了 <c>[GenerateLexer]</c> 特性的类信息。
    /// </summary>
    private static LexerTypeInfo? transform_lexer(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var members = extract_token_definitions(typeSymbol);

        return new LexerTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            members);
    }

    /// <summary>
    ///     提取标记了 <c>[GenerateParser]</c> 特性的类信息。
    /// </summary>
    private static ParserTypeInfo? transform_parser(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        var grammarRef = find_grammar_reference(typeSymbol);
        var rules = extract_parser_rules(typeSymbol);

        return new ParserTypeInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.ContainingNamespace?.ToDisplayString(),
            grammarRef,
            rules);
    }

    /// <summary>
    ///     从类型中提取词法单元定义（公共常量字符串字段）。
    /// </summary>
    private static List<TokenDefinitionInfo> extract_token_definitions(INamedTypeSymbol typeSymbol)
    {
        var tokens = new List<TokenDefinitionInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IFieldSymbol field) continue;

            if (!field.IsConst) continue;

            if (field.DeclaredAccessibility != Accessibility.Public) continue;

            if (field.ConstantValue is not string pattern) continue;

            tokens.Add(new TokenDefinitionInfo(field.Name, pattern));
        }

        return tokens;
    }

    /// <summary>
    ///     查找类型中引用的 <c>[Grammar]</c> 语法定义类。
    /// </summary>
    private static string? find_grammar_reference(INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            var grammarAttr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                $"global::{_grammar_attribute_full_name}");

            if (grammarAttr is not null) return member.Name;
        }

        return null;
    }

    /// <summary>
    ///     从类型中提取语法规则定义（公共方法）。
    /// </summary>
    private static List<ParserRuleInfo> extract_parser_rules(INamedTypeSymbol typeSymbol)
    {
        var rules = new List<ParserRuleInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;

            if (method.DeclaredAccessibility != Accessibility.Public) continue;

            if (method.IsStatic) continue;

            if (method.MethodKind != MethodKind.Ordinary) continue;

            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var parameters = extract_rule_parameters(method);

            rules.Add(new ParserRuleInfo(method.Name, returnType, parameters));
        }

        return rules;
    }

    /// <summary>
    ///     提取语法规则方法的参数信息。
    /// </summary>
    private static List<RuleParameterInfo> extract_rule_parameters(IMethodSymbol method)
    {
        var parameters = new List<RuleParameterInfo>();

        foreach (var param in method.Parameters)
            parameters.Add(new RuleParameterInfo(
                param.Name,
                param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        return parameters;
    }

    /// <summary>
    ///     生成所有词法分析器的源代码。
    /// </summary>
    private static void generate_lexer_source(SourceProductionContext context, ImmutableArray<LexerTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_lexer_impl(info);
            var hintName = $"{info.type_name}.Lexer.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个词法分析器的源代码。
    /// </summary>
    private static string generate_lexer_impl(LexerTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System.Collections.Generic;");
        sb.append_line("using System.Threading;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        sb.append_line($"partial class {info.type_name} : global::Sonic.Standard.Compiler.ILexer");
        using (sb.block())
        {
            generate_tokenize_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>tokenize</c> 方法实现。
    /// </summary>
    private static void generate_tokenize_method(SourceTextBuilder sb, LexerTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 将源代码文本异步转换为词法单元序列。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"source\">源代码文本。</param>");
        sb.append_line("/// <param name=\"cancellationToken\">取消令牌。</param>");
        sb.append_line(
            "public async global::System.Collections.Generic.IAsyncEnumerable<global::Sonic.Standard.Compiler.IToken> tokenize(");
        sb.append_line("    string source,");
        sb.append_line("    global::System.Threading.CancellationToken cancellationToken = default)");
        using (sb.block())
        {
            sb.append_line("var __pos = 0;");
            sb.append_line();
            sb.append_line("while (__pos < source.Length)");
            using (sb.block())
            {
                sb.append_line("cancellationToken.ThrowIfCancellationRequested();");
                sb.append_line("var __matched = false;");
                sb.append_line();

                foreach (var token in info.token_definitions)
                {
                    sb.append_line(
                        $"if (try_match(source, ref __pos, {token.field_name}, out var __token_{token.field_name}))");
                    using (sb.block())
                    {
                        sb.append_line($"yield return __token_{token.field_name};");
                        sb.append_line("__matched = true;");
                        sb.append_line("break;");
                    }

                    sb.append_line();
                }

                sb.append_line("if (!__matched)");
                using (sb.block())
                {
                    sb.append_line("var __ch = source[__pos];");
                    sb.append_line("yield return new global::Sonic.Standard.Compiler.Token.Token(");
                    sb.append_line("    global::Sonic.Standard.Compiler.Token.TokenKind.unknown,");
                    sb.append_line("    __ch.ToString());");
                    sb.append_line("__pos++;");
                }
            }

            sb.append_line();
            sb.append_line("yield break;");
        }

        sb.append_line();
        generate_try_match_helper(sb);
    }

    /// <summary>
    ///     生成 <c>try_match</c> 辅助方法。
    /// </summary>
    private static void generate_try_match_helper(SourceTextBuilder sb)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 尝试在源代码指定位置匹配词法单元模式。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"source\">源代码文本。</param>");
        sb.append_line("/// <param name=\"pos\">当前位置引用。</param>");
        sb.append_line("/// <param name=\"pattern\">匹配模式。</param>");
        sb.append_line("/// <param name=\"token\">匹配到的词法单元。</param>");
        sb.append_line("/// <returns>是否匹配成功。</returns>");
        sb.append_line(
            "private static bool try_match(string source, ref int pos, string pattern, out global::Sonic.Standard.Compiler.IToken token)");
        using (sb.block())
        {
            sb.append_line("token = default!;");
            sb.append_line();
            sb.append_line("if (string.IsNullOrEmpty(pattern)) return false;");
            sb.append_line();
            sb.append_line("if (pos + pattern.Length > source.Length) return false;");
            sb.append_line();
            sb.append_line("var __slice = source.Substring(pos, pattern.Length);");
            sb.append_line();
            sb.append_line("if (__slice == pattern)");
            using (sb.block())
            {
                sb.append_line("token = new global::Sonic.Standard.Compiler.Token.Token(");
                sb.append_line("    global::Sonic.Standard.Compiler.Token.TokenKind.identifier,");
                sb.append_line("    __slice);");
                sb.append_line("pos += pattern.Length;");
                sb.append_line("return true;");
            }

            sb.append_line();
            sb.append_line("return false;");
        }
    }

    /// <summary>
    ///     生成所有语法分析器的源代码。
    /// </summary>
    private static void generate_parser_source(SourceProductionContext context,
        ImmutableArray<ParserTypeInfo> typeInfos)
    {
        foreach (var info in typeInfos)
        {
            var sourceText = generate_parser_impl(info);
            var hintName = $"{info.type_name}.Parser.g.cs";
            context.AddSource(hintName, SourceText.From(sourceText, Encoding.UTF8));
        }
    }

    /// <summary>
    ///     生成单个语法分析器的源代码。
    /// </summary>
    private static string generate_parser_impl(ParserTypeInfo info)
    {
        var sb = new SourceTextBuilder();
        sb.append_line("// <auto-generated />");
        sb.append_line("#nullable enable");
        sb.append_line();
        sb.append_line("using System.Collections.Generic;");
        sb.append_line();

        if (!string.IsNullOrEmpty(info.namespace_name))
        {
            sb.append_line($"namespace {info.namespace_name};");
            sb.append_line();
        }

        sb.append_line($"partial class {info.type_name} : global::Sonic.Standard.Compiler.IParser");
        using (sb.block())
        {
            generate_parse_method(sb, info);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成 <c>parse</c> 方法实现。
    /// </summary>
    private static void generate_parse_method(SourceTextBuilder sb, ParserTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 将词法单元列表解析为语法树。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"tokens\">词法单元只读列表。</param>");
        sb.append_line("/// <returns>解析后的语法树。</returns>");
        sb.append_line(
            "public global::Sonic.Standard.Compiler.ISyntaxTree parse(global::System.Collections.Generic.IReadOnlyList<global::Sonic.Standard.Compiler.IToken> tokens)");
        using (sb.block())
        {
            sb.append_line("var __index = 0;");
            sb.append_line("var __root = parse_entry(tokens, ref __index);");
            sb.append_line("return new global::Sonic.Standard.Compiler.Syntax.SyntaxTree(__root);");
        }

        sb.append_line();
        generate_parse_entry_method(sb, info);
    }

    /// <summary>
    ///     生成 <c>parse_entry</c> 方法，分派到各语法规则。
    /// </summary>
    private static void generate_parse_entry_method(SourceTextBuilder sb, ParserTypeInfo info)
    {
        sb.append_line("/// <summary>");
        sb.append_line("/// 入口解析方法，根据当前词法单元分派到对应的语法规则。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"tokens\">词法单元列表。</param>");
        sb.append_line("/// <param name=\"index\">当前解析位置引用。</param>");
        sb.append_line("/// <returns>解析后的语法节点。</returns>");
        sb.append_line("private global::Sonic.Standard.Compiler.ISyntaxNode parse_entry(");
        sb.append_line(
            "    global::System.Collections.Generic.IReadOnlyList<global::Sonic.Standard.Compiler.IToken> tokens,");
        sb.append_line("    ref int index)");
        using (sb.block())
        {
            if (info.rules.Count > 0)
                sb.append_line("return " + info.rules[0].method_name + "(tokens, ref index);");
            else
                sb.append_line("throw new global::System.NotSupportedException(\"未定义语法规则\");");
        }

        sb.append_line();

        foreach (var rule in info.rules)
        {
            generate_rule_dispatch_method(sb, rule);
            sb.append_line();
        }
    }

    /// <summary>
    ///     生成单个语法规则的分派方法。
    /// </summary>
    private static void generate_rule_dispatch_method(SourceTextBuilder sb, ParserRuleInfo rule)
    {
        sb.append_line("/// <summary>");
        sb.append_line($"/// <c>{rule.method_name}</c> 语法规则的递归下降解析方法。");
        sb.append_line("/// </summary>");
        sb.append_line("/// <param name=\"tokens\">词法单元列表。</param>");
        sb.append_line("/// <param name=\"index\">当前解析位置引用。</param>");
        sb.append_line("/// <returns>解析后的语法节点。</returns>");
        sb.append_line($"private global::Sonic.Standard.Compiler.ISyntaxNode {rule.method_name}(");
        sb.append_line(
            "    global::System.Collections.Generic.IReadOnlyList<global::Sonic.Standard.Compiler.IToken> tokens,");
        sb.append_line("    ref int index)");
        using (sb.block())
        {
            sb.append_line("var __start = index;");
            sb.append_line(
                "var __children = new global::System.Collections.Generic.List<global::Sonic.Standard.Compiler.ISyntaxNode>();");
            sb.append_line();
            sb.append_line(
                $"return new global::Sonic.Standard.Compiler.Syntax.SyntaxNode(\"{StringEscapeHelper.escape_for_string(rule.method_name)}\", __children);");
        }
    }

    #region 数据模型

    /// <summary>
    ///     需要生成词法分析器的类型信息。
    /// </summary>
    internal readonly struct LexerTypeInfo
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
        ///     词法单元定义列表。
        /// </summary>
        public readonly List<TokenDefinitionInfo> token_definitions;

        /// <summary>
        ///     初始化 <see cref="LexerTypeInfo" /> 的新实例。
        /// </summary>
        public LexerTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            List<TokenDefinitionInfo> tokenDefinitions)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            token_definitions = tokenDefinitions;
        }
    }

    /// <summary>
    ///     词法单元定义信息。
    /// </summary>
    internal readonly struct TokenDefinitionInfo
    {
        /// <summary>
        ///     字段名称。
        /// </summary>
        public readonly string field_name;

        /// <summary>
        ///     匹配模式。
        /// </summary>
        public readonly string pattern;

        /// <summary>
        ///     初始化 <see cref="TokenDefinitionInfo" /> 的新实例。
        /// </summary>
        public TokenDefinitionInfo(string fieldName, string pattern)
        {
            field_name = fieldName;
            this.pattern = pattern;
        }
    }

    /// <summary>
    ///     需要生成语法分析器的类型信息。
    /// </summary>
    internal readonly struct ParserTypeInfo
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
        ///     引用的语法定义名称。
        /// </summary>
        public readonly string? grammar_reference;

        /// <summary>
        ///     语法规则列表。
        /// </summary>
        public readonly List<ParserRuleInfo> rules;

        /// <summary>
        ///     初始化 <see cref="ParserTypeInfo" /> 的新实例。
        /// </summary>
        public ParserTypeInfo(
            string typeName,
            string fullyQualifiedName,
            string? namespaceName,
            string? grammarReference,
            List<ParserRuleInfo> rules)
        {
            type_name = typeName;
            fully_qualified_name = fullyQualifiedName;
            namespace_name = namespaceName;
            grammar_reference = grammarReference;
            this.rules = rules;
        }
    }

    /// <summary>
    ///     语法规则信息。
    /// </summary>
    internal readonly struct ParserRuleInfo
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
        ///     方法参数列表。
        /// </summary>
        public readonly List<RuleParameterInfo> parameters;

        /// <summary>
        ///     初始化 <see cref="ParserRuleInfo" /> 的新实例。
        /// </summary>
        public ParserRuleInfo(
            string methodName,
            string returnType,
            List<RuleParameterInfo> parameters)
        {
            method_name = methodName;
            return_type = returnType;
            this.parameters = parameters;
        }
    }

    /// <summary>
    ///     语法规则参数信息。
    /// </summary>
    internal readonly struct RuleParameterInfo
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
        ///     初始化 <see cref="RuleParameterInfo" /> 的新实例。
        /// </summary>
        public RuleParameterInfo(string name, string type)
        {
            this.name = name;
            Type = type;
        }
    }

    #endregion
}