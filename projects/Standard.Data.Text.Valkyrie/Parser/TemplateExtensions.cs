using System.Text;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Template;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Lexer;
using DiagnosticTextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Valkyrie.Parser;

/// <summary>
///     元语言扩展解析：遇到 `<% ... %>` 指令时接管 TokenSource，
///     在模板体内复用 Statement/Declaration/Expression 解析链。
/// </summary>
internal static class TemplateExtensions
{
    extension(TokenStream tokens)
    {
        internal ValkyrieNode? parse_meta_template_node()
        {
            if (!tokens.check(ValkyrieTokenKind.template_l)) return null;

            if (!try_read_directive_tokens(tokens, out var directiveTokens) || directiveTokens.Count == 0) return null;

            var firstToken = directiveTokens[0];
            var head = kind_of(firstToken);
            return head switch
            {
                ValkyrieTokenKind.match => parse_match(tokens, directiveTokens),
                ValkyrieTokenKind.loop => parse_loop(tokens, directiveTokens),
                ValkyrieTokenKind.@if => parse_if(tokens, directiveTokens),
                _ => null
            };
        }
    }

    private static ValkyrieNode? parse_match(TokenStream tokens, IReadOnlyList<GreenLeafNode> openDirective)
    {
        var (expressionTokens, guardTokens) = split_guard_tokens(openDirective.Skip(1));
        var beginExpression = parse_term_value_node(tokens, expressionTokens, "模板 `match` 起始表达式必须是普通表达式。");
        var beginGuard = guardTokens.Count == 0
            ? null
            : parse_term_value_node(tokens, guardTokens, "模板 `match` 守卫必须是普通表达式。");
        var armParts = new List<FragmentArmPart>();
        FragmentArmNode? currentArm = null;
        var bodyTokens = new List<GreenLeafNode>();
        var nestingDepth = 0;

        while (!tokens.is_at_end())
        {
            if (!tokens.check(ValkyrieTokenKind.template_l))
            {
                bodyTokens.Add(tokens.advance());
                continue;
            }

            if (!try_read_directive_tokens(tokens, out var directiveTokens) || directiveTokens.Count == 0) break;

            var firstToken = directiveTokens[0];
            var firstKind = kind_of(firstToken);
            if (is_opening_directive(firstToken))
            {
                nestingDepth++;
                append_directive_as_tokens(bodyTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.end)
            {
                if (nestingDepth > 0)
                {
                    nestingDepth--;
                    append_directive_as_tokens(bodyTokens, directiveTokens);
                    continue;
                }

                if (is_end_directive(directiveTokens, ValkyrieTokenKind.match))
                {
                    commit_match_arm_part(armParts, currentArm, bodyTokens);
                    return new TemplateMatchNode(
                        new FragmentMatchNode(beginExpression)
                        {
                            guard = beginGuard
                        },
                        armParts)
                    {
                        span = default
                    };
                }

                append_directive_as_tokens(bodyTokens, directiveTokens);
                continue;
            }

            if (nestingDepth > 0)
            {
                append_directive_as_tokens(bodyTokens, directiveTokens);
                continue;
            }

            if (is_match_arm_directive(firstKind))
            {
                commit_match_arm_part(armParts, currentArm, bodyTokens);
                currentArm = parse_match_arm_directive(tokens, directiveTokens);
                bodyTokens.Clear();
                continue;
            }

            append_directive_as_tokens(bodyTokens, directiveTokens);
        }

        return new TemplateMatchNode(
            new FragmentMatchNode(beginExpression)
            {
                guard = beginGuard
            },
            armParts)
        {
            span = default
        };
    }

    private static TemplateIfNode parse_if(TokenStream tokens, IReadOnlyList<GreenLeafNode> openDirective)
    {
        var condition = parse_expression_node(openDirective.Skip(1));
        var thenTokens = new List<GreenLeafNode>();
        var elseTokens = new List<GreenLeafNode>();
        var inElse = false;
        var nestingDepth = 0;

        while (!tokens.is_at_end())
        {
            if (!tokens.check(ValkyrieTokenKind.template_l))
            {
                (inElse ? elseTokens : thenTokens).Add(tokens.advance());
                continue;
            }

            if (!try_read_directive_tokens(tokens, out var directiveTokens) || directiveTokens.Count == 0) break;

            var firstToken = directiveTokens[0];
            var firstKind = kind_of(firstToken);
            if (is_opening_directive(firstToken))
            {
                nestingDepth++;
                append_directive_as_tokens(inElse ? elseTokens : thenTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.end)
            {
                if (nestingDepth > 0)
                {
                    nestingDepth--;
                    append_directive_as_tokens(inElse ? elseTokens : thenTokens, directiveTokens);
                    continue;
                }

                if (is_end_directive(directiveTokens, ValkyrieTokenKind.@if)) break;

                append_directive_as_tokens(inElse ? elseTokens : thenTokens, directiveTokens);
                continue;
            }

            if (nestingDepth > 0)
            {
                append_directive_as_tokens(inElse ? elseTokens : thenTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.@else)
            {
                inElse = true;
                continue;
            }

            append_directive_as_tokens(inElse ? elseTokens : thenTokens, directiveTokens);
        }

        var thenBody = parse_body_nodes(thenTokens);
        var elseBody = parse_body_nodes(elseTokens);
        return new TemplateIfNode(condition, thenBody, elseBody.Count == 0 ? null : elseBody);
    }

    private static ValkyrieNode parse_loop(TokenStream tokens, IReadOnlyList<GreenLeafNode> openDirective)
    {
        var bodyTokens = new List<GreenLeafNode>();
        var nestingDepth = 0;

        while (!tokens.is_at_end())
        {
            if (!tokens.check(ValkyrieTokenKind.template_l))
            {
                bodyTokens.Add(tokens.advance());
                continue;
            }

            if (!try_read_directive_tokens(tokens, out var directiveTokens) || directiveTokens.Count == 0) break;

            var firstToken = directiveTokens[0];
            var firstKind = kind_of(firstToken);
            if (is_opening_directive(firstToken))
            {
                nestingDepth++;
                append_directive_as_tokens(bodyTokens, directiveTokens);
                continue;
            }

            if (firstKind == ValkyrieTokenKind.end)
            {
                if (nestingDepth > 0)
                {
                    nestingDepth--;
                    append_directive_as_tokens(bodyTokens, directiveTokens);
                    continue;
                }

                if (is_end_directive(directiveTokens, ValkyrieTokenKind.loop)) break;
            }

            append_directive_as_tokens(bodyTokens, directiveTokens);
        }

        var bodyNodes = parse_body_nodes(bodyTokens);
        var descriptor = openDirective.Skip(1).ToList();
        var variableName = descriptor.Count > 0 ? descriptor[0].text?.Trim() ?? string.Empty : string.Empty;
        var rangeStart = string.Empty;
        var rangeEnd = string.Empty;
        var inTokenIndex = descriptor.FindIndex(t => kind_of(t) == ValkyrieTokenKind.@in);
        if (inTokenIndex >= 0 && inTokenIndex + 3 < descriptor.Count)
        {
            rangeStart = descriptor[inTokenIndex + 3].text ?? string.Empty;
            if (inTokenIndex + 5 < descriptor.Count) rangeEnd = descriptor[inTokenIndex + 5].text ?? string.Empty;
        }

        return new TemplateLoopNode(variableName, rangeStart, rangeEnd, bodyNodes);
    }

    private static IReadOnlyList<ValkyrieNode> parse_body_nodes(IReadOnlyList<GreenLeafNode> bodyTokens)
    {
        if (bodyTokens.Count == 0) return [];

        var parserTokens = new List<GreenLeafNode>(bodyTokens.Count + 1);
        parserTokens.AddRange(bodyTokens);
        parserTokens.Add(new GreenLeafNode(ValkyrieTokenKind.eos.to_node_kind(), 0, string.Empty));
        var source = new TokenStream(parserTokens, ValkyrieNodeKindClassifier.instance);
        var statements = new List<ValkyrieNode>();
        while (!source.is_at_end() && !source.check(ValkyrieTokenKind.eos))
            statements.Add(source.parse_statement_node(ValkyrieLanguage.standard));

        return statements;
    }

    private static ValkyrieNode parse_expression_node(IEnumerable<GreenLeafNode> expressionTokens)
    {
        var tokenList = expressionTokens.ToList();
        if (tokenList.Count == 0) return new IdentifierNode("_");

        var parserTokens = new List<GreenLeafNode>(tokenList.Count + 1);
        parserTokens.AddRange(tokenList);
        parserTokens.Add(new GreenLeafNode(ValkyrieTokenKind.eos.to_node_kind(), 0, string.Empty));

        var source = new TokenStream(parserTokens, ValkyrieNodeKindClassifier.instance);
        return source.parse_term_node();
    }

    private static TermNode parse_term_value_node(TokenStream? tokens, IEnumerable<GreenLeafNode> expressionTokens,
        string message)
    {
        return require_term_node(tokens, parse_expression_node(expressionTokens), message);
    }

    private static void report_unsupported_template_directive(TokenStream tokens, string message)
    {
        tokens.diagnostics?.report_error(new DiagnosticTextSpan(tokens.position, 1), message);
    }

    private static bool is_match_arm_directive(ValkyrieTokenKind kind)
    {
        return kind is ValkyrieTokenKind.@case or ValkyrieTokenKind.@else or ValkyrieTokenKind.type
            or ValkyrieTokenKind.when;
    }

    private static FragmentArmNode? parse_match_arm_directive(TokenStream tokens,
        IReadOnlyList<GreenLeafNode> directiveTokens)
    {
        var head = kind_of(directiveTokens[0]);
        var tail = directiveTokens.Skip(1).ToList();
        return head switch
        {
            ValkyrieTokenKind.@case => parse_case_arm(tokens, tail),
            ValkyrieTokenKind.@else => new FragmentArmElseNode(),
            ValkyrieTokenKind.type => parse_type_arm(tokens, tail),
            ValkyrieTokenKind.when => parse_when_arm(tokens, tail),
            _ => null
        };
    }

    private static FragmentArmNode parse_case_arm(TokenStream tokens, IReadOnlyList<GreenLeafNode> tokensAfterHead)
    {
        var (patternTokens, guardTokens) = split_guard_tokens(tokensAfterHead);
        return new FragmentArmCaseNode
        {
            pattern = parse_pattern_node(patternTokens),
            guard = guardTokens.Count == 0
                ? null
                : parse_term_value_node(tokens, guardTokens, "模板 `case` 守卫必须是普通表达式。")
        };
    }

    private static FragmentArmNode parse_type_arm(TokenStream tokens, IReadOnlyList<GreenLeafNode> tokensAfterHead)
    {
        var (typeTokens, guardTokens) = split_guard_tokens(tokensAfterHead);
        return new FragmentArmTypeNode
        {
            type = parse_type_node(typeTokens),
            guard = guardTokens.Count == 0
                ? null
                : parse_term_value_node(tokens, guardTokens, "模板 `type` 守卫必须是普通表达式。")
        };
    }

    private static FragmentArmNode parse_when_arm(TokenStream tokens, IReadOnlyList<GreenLeafNode> tokensAfterHead)
    {
        var (termTokens, guardTokens) = split_guard_tokens(tokensAfterHead);
        return new FragmentArmWhenNode
        {
            term = parse_term_value_node(tokens, termTokens, "模板 `when` 条件必须是普通表达式。"),
            guard = guardTokens.Count == 0
                ? null
                : parse_term_value_node(tokens, guardTokens, "模板 `when` 守卫必须是普通表达式。")
        };
    }

    private static void commit_match_arm_part(
        ICollection<FragmentArmPart> armParts,
        FragmentArmNode? currentArm,
        List<GreenLeafNode> bodyTokens)
    {
        if (currentArm is null)
        {
            bodyTokens.Clear();
            return;
        }

        armParts.Add(new FragmentArmPart
        {
            arm = currentArm,
            parts = parse_body_nodes(bodyTokens)
        });
        bodyTokens.Clear();
    }

    private static (List<GreenLeafNode> MainTokens, List<GreenLeafNode> GuardTokens) split_guard_tokens(
        IEnumerable<GreenLeafNode> tokens)
    {
        var tokenList = tokens.ToList();
        var ifIndex = tokenList.FindIndex(token => kind_of(token) == ValkyrieTokenKind.@if);
        if (ifIndex < 0) return (tokenList, []);

        return ([.. tokenList.Take(ifIndex)], [.. tokenList.Skip(ifIndex + 1)]);
    }

    private static PatternNode parse_pattern_node(IEnumerable<GreenLeafNode> patternTokens)
    {
        var parserTokens = create_parser_tokens(patternTokens);
        var source = new TokenStream(parserTokens, ValkyrieNodeKindClassifier.instance);
        return source.parse_pattern_expression_node();
    }

    private static TypeNode parse_type_node(IEnumerable<GreenLeafNode> typeTokens)
    {
        var parserTokens = create_parser_tokens(typeTokens);
        var source = new TokenStream(parserTokens, ValkyrieNodeKindClassifier.instance);
        return source.parse_type_expression_node();
    }

    private static List<GreenLeafNode> create_parser_tokens(IEnumerable<GreenLeafNode> tokens)
    {
        var parserTokens = tokens.ToList();
        parserTokens.Add(new GreenLeafNode(ValkyrieTokenKind.eos.to_node_kind(), 0, string.Empty));
        return parserTokens;
    }

    private static TermNode require_term_node(TokenStream? tokens, ValkyrieNode node, string message)
    {
        if (node is TermNode termNode) return termNode;

        tokens?.diagnostics?.report_error(new DiagnosticTextSpan(tokens.position, 1), message);
        return ParserNodeFactory.create_term_literal_symbol("<invalid-term>");
    }

    private static void append_directive_as_tokens(List<GreenLeafNode> target,
        IReadOnlyList<GreenLeafNode> directiveTokens)
    {
        target.Add(new GreenLeafNode(ValkyrieTokenKind.template_l.to_node_kind(), 2, "<%"));
        foreach (var token in directiveTokens) target.Add(token);

        target.Add(new GreenLeafNode(ValkyrieTokenKind.template_r.to_node_kind(), 2, "%>"));
    }

    private static bool try_read_directive_tokens(TokenStream tokens, out List<GreenLeafNode> directiveTokens)
    {
        directiveTokens = [];
        if (!tokens.check(ValkyrieTokenKind.template_l)) return false;

        tokens.advance();
        while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.template_r))
            directiveTokens.Add(tokens.advance());

        if (tokens.check(ValkyrieTokenKind.template_r)) tokens.advance();

        return true;
    }

    private static bool is_opening_directive(GreenLeafNode firstToken)
    {
        var kind = kind_of(firstToken);
        return kind is ValkyrieTokenKind.match or ValkyrieTokenKind.@if or ValkyrieTokenKind.loop;
    }

    private static bool is_end_directive(IReadOnlyList<GreenLeafNode> directiveTokens, ValkyrieTokenKind endKind)
    {
        if (directiveTokens.Count < 2) return false;

        var firstToken = directiveTokens[0];
        var firstKind = kind_of(firstToken);
        if (firstKind != ValkyrieTokenKind.end) return false;

        var secondToken = directiveTokens[1];
        return kind_of(secondToken) == endKind;
    }

    private static string build_text(IEnumerable<GreenLeafNode> tokens)
    {
        var sb = new StringBuilder();
        foreach (var token in tokens)
        {
            var text = token.text ?? string.Empty;
            if (text.Length == 0) continue;

            if (sb.Length > 0 && need_space(sb[^1], text[0])) sb.Append(' ');

            sb.Append(text);
        }

        return sb.ToString();
    }

    private static bool need_space(char prev, char current)
    {
        var prevWord = char.IsLetterOrDigit(prev) || prev == '_';
        var currWord = char.IsLetterOrDigit(current) || current == '_';
        return prevWord && currWord;
    }

    private static ValkyrieTokenKind kind_of(GreenLeafNode token)
    {
        return (ValkyrieTokenKind)token.kind.value;
    }
}
