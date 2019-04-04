using System.Runtime.CompilerServices;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Lexer;
using static Std.Data.Text.Valkyrie.Parser.StatementExtensions;
using DiagnosticTextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Valkyrie.Parser;

internal static class TermParser
{
    private static readonly ConditionalWeakTable<TokenStream, TrailingBlockBindingState>
        _s_trailing_block_binding_states = new();

    extension(TokenStream tokens)
    {
        internal TermNode parse_term_expression_node()
        {
            return parse_term_expression(tokens, 0);
        }

        internal TermNode parse_term_node()
        {
            return parse_term_expression(tokens, 0);
        }

        internal TermNode parse_term_value_node()
        {
            var position = tokens.position;
            return require_term_node(tokens, parse_term_expression(tokens, 0), position, "当前位置需要普通表达式值。");
        }

        internal TermNode parse_block_introducer_term_node()
        {
            return parse_without_trailing_block_binding(tokens, () => parse_term_expression(tokens, 0));
        }

        internal TermNode parse_block_introducer_term_value_node()
        {
            return parse_without_trailing_block_binding(
                tokens,
                () => require_term_node(tokens, parse_term_expression(tokens, 0), tokens.position, "当前位置需要普通表达式值。"));
        }
    }

    private static TermNode parse_term_expression(TokenStream tokens, int minBindingPower)
    {
        var left = parse_prefix(tokens);

        while (!tokens.is_at_end())
        {
            if (try_parse_postfix(tokens, ref left, minBindingPower)) continue;

            if (try_parse_special_infix(tokens, left, minBindingPower, out var special))
            {
                left = special;
                continue;
            }

            var op = tokens.peek_valkyrie_kind();
            if (!TermNodeExtensions.try_get_binary_binding_power(op, out var leftBindingPower,
                    out var rightBindingPower)) break;

            if (leftBindingPower < minBindingPower) break;

            tokens.advance();
            var right = parse_term_expression(tokens, rightBindingPower);
            left = require_term_node(tokens,
                TermNodeExtensions.create_binary_node(op, left, right),
                tokens.position,
                "二元运算必须产生普通表达式。");
        }

        return left;
    }

    private static TermNode parse_prefix(TokenStream tokens)
    {
        if (tokens.is_at_end())
        {
            tokens.diagnostics?.report_warning(new DiagnosticTextSpan(tokens.position, 1), "表达式意外结束");
            return ParserNodeFactory.create_term_literal_symbol("<eof>");
        }

        var kind = tokens.peek_valkyrie_kind();
        if (TermUnaryOperator.try_parse_prefix_operator(kind, out var prefixOperator))
        {
            tokens.advance();
            return prefixOperator.create_node(
                require_term_node(tokens,
                    parse_term_expression(tokens, prefixOperator.prefix_binding_power()),
                    tokens.position,
                    "一元运算的操作数必须是普通表达式。"));
        }

        switch (kind)
        {
            case ValkyrieTokenKind.number:
                return ParserNodeFactory.create_term_literal_number(tokens.advance_text());
            case ValkyrieTokenKind.@string:
            {
                var rawText = tokens.advance_text();
                return parse_string_literal(tokens, rawText);
            }
            case ValkyrieTokenKind.@true:
                tokens.advance();
                return ParserNodeFactory.create_term_literal_boolean(true);
            case ValkyrieTokenKind.@false:
                tokens.advance();
                return ParserNodeFactory.create_term_literal_boolean(false);
            case ValkyrieTokenKind.@null:
                _ = tokens.advance_text();
                return ParserNodeFactory.create_term_literal_null();
            case ValkyrieTokenKind.micro:
                return parse_anonymous_micro(tokens);
            case ValkyrieTokenKind.parenthesis_l:
                return parse_parenthesized_expression(tokens);
            case ValkyrieTokenKind.bracket_l:
                return parse_array_literal(tokens);
            case ValkyrieTokenKind.dot_dot:
            case ValkyrieTokenKind.dot_dot_dot:
            {
                var isDoubleDot = kind == ValkyrieTokenKind.dot_dot;
                var dotSpan = new TextSpan(tokens.position, isDoubleDot ? 2 : 3);
                tokens.advance();
                var target = require_term_node(tokens,
                    parse_term_expression(tokens, 170),
                    tokens.position,
                    "展开运算符的操作数必须是普通表达式。");
                return new TermSpreadExpression(target, isDoubleDot, dotSpan);
            }
            case ValkyrieTokenKind.@if:
                return parse_if_expression(tokens);
            default:
                if (can_start_name(kind, tokens)) return parse_name_or_constructor(tokens);

                var unexpectedText = tokens.advance_text();
                tokens.diagnostics?.report_warning(new DiagnosticTextSpan(tokens.position - 1, 1),
                    $"无法识别的表达式元素：\"{unexpectedText}\"");
                return ParserNodeFactory.create_term_literal_symbol("<unknown>");
        }
    }

    private static bool try_parse_postfix(TokenStream tokens, ref TermNode left, int minBindingPower)
    {
        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
        {
            var bindingPower = TermNodeExtensions.structural_postfix_binding_power();
            if (bindingPower < minBindingPower) return false;

            left = parse_call(tokens, left, null);
            return true;
        }

        if (tokens.check(ValkyrieTokenKind.bracket_l))
        {
            var bindingPower = TermNodeExtensions.structural_postfix_binding_power();
            if (bindingPower < minBindingPower) return false;

            left = parse_ordinal_index(tokens, left);
            return true;
        }

        if (tokens.check(ValkyrieTokenKind.offset_l))
        {
            var bindingPower = TermNodeExtensions.structural_postfix_binding_power();
            if (bindingPower < minBindingPower) return false;

            left = parse_offset_index(tokens, left);
            return true;
        }

        if (tokens.check(ValkyrieTokenKind.dot))
        {
            var bindingPower = TermNodeExtensions.structural_postfix_binding_power();
            if (bindingPower < minBindingPower) return false;

            // 处理 .catch 后缀表达式
            if (tokens.check(ValkyrieTokenKind.@catch, 1))
            {
                tokens.advance(); // 消费 .
                tokens.advance(); // 消费 catch
                left = parse_catch_expression_body(tokens, left);
                return true;
            }

            // 处理 .match 后缀表达式
            if (tokens.check(ValkyrieTokenKind.match, 1))
            {
                tokens.advance(); // 消费 .
                tokens.advance(); // 消费 match
                left = parse_match_expression_body(tokens, left);
                return true;
            }

            left = parse_member_access(tokens, left, ValkyrieTokenKind.dot);
            return true;
        }

        if (tokens.check(ValkyrieTokenKind.double_colon))
        {
            var bindingPower = TermNodeExtensions.structural_postfix_binding_power();
            if (bindingPower < minBindingPower) return false;

            if (tokens.check(ValkyrieTokenKind.bracket_l, 1))
            {
                left = parse_offset_alias_index(tokens, left);
                return true;
            }

            if (tokens.check(ValkyrieTokenKind.less, 1))
            {
                var typeArguments = parse_explicit_type_arguments(tokens);
                if (!is_trailing_block_binding_suppressed(tokens) &&
                    tokens.check(ValkyrieTokenKind.brace_l) &&
                    try_create_object_initializer_constructor(left, typeArguments, out var constructor))
                {
                    left = parse_object_initializer(tokens, constructor);
                    return true;
                }

                if (tokens.check(ValkyrieTokenKind.parenthesis_l))
                {
                    left = parse_call(tokens, left, typeArguments);
                    return true;
                }

                left = TermNodeExtensions.attach_type_arguments(left, typeArguments);
                return true;
            }

            if (can_start_name(tokens.peek_valkyrie_kind(1), tokens, 1))
            {
                left = parse_member_access(tokens, left, ValkyrieTokenKind.double_colon);
                return true;
            }
        }

        if (TermUnaryOperator.try_parse_postfix_operator(tokens.peek_valkyrie_kind(), out var postfixOperator))
        {
            var bindingPower = postfixOperator.postfix_binding_power();
            if (bindingPower < minBindingPower) return false;

            tokens.advance();
            left = postfixOperator.create_node(
                require_term_node(tokens, left, tokens.position, "后缀运算的操作数必须是普通表达式。"));
            return true;
        }

        return false;
    }

    private static bool try_parse_special_infix(TokenStream tokens, TermNode left, int minBindingPower,
        out TermNode result)
    {
        result = left;

        if (tokens.check(ValkyrieTokenKind.@is))
        {
            _ = TermNodeExtensions.try_get_special_infix_binding_power(ValkyrieTokenKind.@is, out var bindingPower);
            if (bindingPower < minBindingPower) return false;

            var span = new TextSpan(tokens.position, 1);
            tokens.advance();
            var isNullable = false;
            if (tokens.check(ValkyrieTokenKind.question))
            {
                tokens.advance();
                isNullable = true;
            }

            result = new TermIsExpression(
                require_term_node(tokens, left, tokens.position, "`is` 左侧必须是普通表达式。"),
                PatternParser.parse_type_pattern_for_is(tokens),
                span,
                isNullable);
            return true;
        }

        if (tokens.check(ValkyrieTokenKind.@as))
        {
            _ = TermNodeExtensions.try_get_special_infix_binding_power(ValkyrieTokenKind.@as, out var bindingPower);
            if (bindingPower < minBindingPower) return false;

            var span = new TextSpan(tokens.position, 1);
            tokens.advance();
            var targetType = tokens.parse_type_expression_node();
            var isNullable = false;
            if (tokens.check(ValkyrieTokenKind.question))
            {
                tokens.advance();
                isNullable = true;
            }

            result = new TermAsExpression(
                require_term_node(tokens, left, tokens.position, "`as` 左侧必须是普通表达式。"),
                targetType,
                isNullable,
                span);
            return true;
        }

        if (tokens.check(ValkyrieTokenKind.@in))
        {
            _ = TermNodeExtensions.try_get_special_infix_binding_power(ValkyrieTokenKind.@in, out var bindingPower);
            if (bindingPower < minBindingPower) return false;

            var span = new TextSpan(tokens.position, 1);
            tokens.advance();
            var target = tokens.parse_term_value_node();
            result = new TermInExpression(
                require_term_node(tokens, left, tokens.position, "`in` 左侧必须是普通表达式。"),
                target,
                span);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     解析 .catch { ... } 后缀表达式体
    /// </summary>
    private static TermCatchExpression parse_catch_expression_body(TokenStream tokens, TermNode operand)
    {
        tokens.expect(ValkyrieTokenKind.brace_l);
        var arms = new List<ArmNode>();
        while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
        {
            arms.Add(parse_catch_arm(tokens, ValkyrieLanguage.standard));
        }

        tokens.expect(ValkyrieTokenKind.brace_r);
        return new TermCatchExpression { operand = operand, arms = arms };
    }

    /// <summary>
    ///     解析 .match { ... } 后缀表达式体
    /// </summary>
    private static TermMatchExpression parse_match_expression_body(TokenStream tokens, TermNode operand)
    {
        tokens.expect(ValkyrieTokenKind.brace_l);
        var arms = new List<ArmNode>();
        while (!tokens.check(ValkyrieTokenKind.brace_r) && !tokens.is_at_end())
        {
            arms.Add(parse_match_arm(tokens, ValkyrieLanguage.standard));
        }

        tokens.expect(ValkyrieTokenKind.brace_r);
        return new TermMatchExpression { operand = operand, arms = arms };
    }

    private static TermNode parse_name_or_constructor(TokenStream tokens)
    {
        var path = parse_qualified_term_path(tokens);
        var symbol = ParserNodeFactory.create_term_literal_symbol(path.segments, path.is_global);

        if (!is_trailing_block_binding_suppressed(tokens) && tokens.check(ValkyrieTokenKind.brace_l))
            return parse_object_initializer(tokens, symbol);

        return symbol;
    }

    private static QualifiedPathNode parse_qualified_term_path(TokenStream tokens)
    {
        var isGlobal = tokens.match(ValkyrieTokenKind.double_colon);
        var segments = new List<IdentifierNode> { ParserNodeFactory.create_identifier(tokens.advance_text()) };

        while (tokens.check(ValkyrieTokenKind.double_colon) && can_start_name(tokens.peek_valkyrie_kind(1), tokens, 1))
        {
            if (tokens.check(ValkyrieTokenKind.less, 1) || tokens.check(ValkyrieTokenKind.bracket_l, 1)) break;

            tokens.advance();
            segments.Add(ParserNodeFactory.create_identifier(tokens.advance_text()));
        }

        return ParserNodeFactory.create_qualified_path(segments, isGlobal);
    }

    private static bool can_start_name(ValkyrieTokenKind kind, TokenStream tokens, int offset = 0)
    {
        if (kind == ValkyrieTokenKind.identifier) return true;

        if (!tokens.peek(offset).kind.is_keyword()) return false;

        return kind is not (ValkyrieTokenKind.@true or ValkyrieTokenKind.@false or ValkyrieTokenKind.@null or
            ValkyrieTokenKind.micro or ValkyrieTokenKind.@if or ValkyrieTokenKind.match or
            ValkyrieTokenKind.@case or ValkyrieTokenKind.@else or ValkyrieTokenKind.@catch or
            ValkyrieTokenKind.@return or ValkyrieTokenKind.@while or ValkyrieTokenKind.loop or
            ValkyrieTokenKind.@in or ValkyrieTokenKind.@is or ValkyrieTokenKind.@as);
    }

    /// <summary>
///     解析括号表达式，支持：
///     <list type="bullet">
///         <item><c>(expr)</c> — 普通括号分组表达式</item>
///         <item><c>()</c> — 空元组 / unit 值</item>
///         <item><c>(expr, expr, ...)</c> — 元组字面量</item>
///     </list>
/// </summary>
private static TermNode parse_parenthesized_expression(TokenStream tokens)
{
    tokens.expect(ValkyrieTokenKind.parenthesis_l);

    // 空元组 / unit 值
    if (tokens.check(ValkyrieTokenKind.parenthesis_r))
    {
        tokens.advance();
        return TermNodeExtensions.create_tuple_literal([]);
    }

    var firstExpression = tokens.parse_term_node();

    // 如果后面是逗号，则是元组字面量
    if (tokens.match(ValkyrieTokenKind.comma))
    {
        var elements = new List<TermNode> { firstExpression };
        while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
        {
            elements.Add(tokens.parse_term_value_node());
            if (!tokens.match(ValkyrieTokenKind.comma))
            {
                break;
            }
        }

        tokens.expect(ValkyrieTokenKind.parenthesis_r);
        return TermNodeExtensions.create_tuple_literal(elements);
    }

    // 普通括号分组表达式
    tokens.expect(ValkyrieTokenKind.parenthesis_r);
    return firstExpression;
}

    private static AnonymousMicro parse_anonymous_micro(TokenStream tokens)
    {
        tokens.expect(ValkyrieTokenKind.micro);
        tokens.expect(ValkyrieTokenKind.parenthesis_l);

        var parameters = new List<TermParameterList>();
        while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
        {
            if (tokens.check(ValkyrieTokenKind.identifier)
                && string.Equals(tokens.peek_text(), "mut", StringComparison.OrdinalIgnoreCase))
            {
                tokens.advance();
            }

            var name = ParserNodeFactory.create_identifier(tokens.advance_text());
            TypeNode? type = null;
            if (tokens.match(ValkyrieTokenKind.colon)) type = tokens.parse_type_expression_node();

            parameters.Add(DeclarationNodeExtensions.create_single_term_parameter_list(
                name,
                type ?? TypeNodeExtensions.create_any_type()));

            if (!tokens.match(ValkyrieTokenKind.comma)) break;
        }

        tokens.expect(ValkyrieTokenKind.parenthesis_r);

        TypeNode? returnType = null;
        if (tokens.match(ValkyrieTokenKind.arrow)) returnType = tokens.parse_type_expression_node();

        FunctionBody? body = null;
        if (tokens.check(ValkyrieTokenKind.brace_l))
            body = tokens.parse_block_node(ValkyrieLanguage.standard);
        else if (!tokens.is_at_end()) body = TermNodeExtensions.create_expression_body(tokens.parse_term_value_node());

        return TermNodeExtensions.create_anonymous_micro(parameters, returnType, body);
    }

    private static TermNode parse_call(TokenStream tokens, TermNode caller, TypeArgumentList? explicitTypeArguments)
    {
        tokens.expect(ValkyrieTokenKind.parenthesis_l);
        var arguments = parse_term_arguments(tokens);
        tokens.expect(ValkyrieTokenKind.parenthesis_r);

        var body = TermNodeExtensions.create_call_body(
            explicitTypeArguments,
            arguments);

        if (!is_trailing_block_binding_suppressed(tokens) && tokens.check(ValkyrieTokenKind.brace_l))
            body = TermNodeExtensions.create_call_body(
                body.type_arguments,
                body.term_arguments,
                tokens.parse_block_node(ValkyrieLanguage.standard));

        return TermNodeExtensions.attach_call(caller, body);
    }

    private static TermArgumentList parse_term_arguments(TokenStream tokens)
    {
        var items = new List<TermArgumentItem>();
        while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
        {
            items.Add(TermNodeExtensions.create_argument_item(tokens.parse_term_value_node()));

            if (!tokens.match(ValkyrieTokenKind.comma)) break;
        }

        return TermNodeExtensions.create_argument_list(items);
    }

    private static TypeArgumentList parse_explicit_type_arguments(TokenStream tokens)
    {
        tokens.expect(ValkyrieTokenKind.double_colon);
        tokens.expect(ValkyrieTokenKind.less);

        var items = new List<TypeArgumentItem>();
        while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.greater) &&
               !tokens.check(ValkyrieTokenKind.generic_r))
        {
            IdentifierNode? slot = null;
            if (tokens.check(ValkyrieTokenKind.identifier) && tokens.check(ValkyrieTokenKind.equal, 1))
            {
                slot = ParserNodeFactory.create_identifier(tokens.advance_text());
                tokens.expect(ValkyrieTokenKind.equal);
            }

            items.Add(TermNodeExtensions.create_type_argument(
                tokens.parse_type_expression_node(),
                slot));

            if (!tokens.match(ValkyrieTokenKind.comma)) break;
        }

        if (tokens.check(ValkyrieTokenKind.greater) || tokens.check(ValkyrieTokenKind.generic_r))
            tokens.advance();
        else
            tokens.expect(ValkyrieTokenKind.greater);

        return TermNodeExtensions.create_type_argument_list(items);
    }

    private static bool try_create_object_initializer_constructor(
        TermNode target,
        TypeArgumentList typeArguments,
        out ValkyrieNode constructor)
    {
        switch (target)
        {
            case TermLiteralNamePathNode namePath:
                constructor = new TypeLiteralNamePathNode
                {
                    path = namePath.path,
                    type_arguments = typeArguments
                };
                return true;
            default:
                constructor = target;
                return false;
        }
    }

    private static TermOrdinalExpression parse_ordinal_index(TokenStream tokens, TermNode target)
    {
        tokens.expect(ValkyrieTokenKind.bracket_l);
        var indices = parse_index_arguments(tokens, ValkyrieTokenKind.bracket_r);
        return TermNodeExtensions.create_ordinal_index(target, indices);
    }

    private static TermOffsetExpression parse_offset_index(TokenStream tokens, TermNode target)
    {
        tokens.expect(ValkyrieTokenKind.offset_l);
        var indices = parse_index_arguments(tokens, ValkyrieTokenKind.offset_r);
        return TermNodeExtensions.create_offset_index(target, indices);
    }

    private static TermOffsetExpression parse_offset_alias_index(TokenStream tokens, TermNode target)
    {
        tokens.expect(ValkyrieTokenKind.double_colon);
        tokens.expect(ValkyrieTokenKind.bracket_l);
        var indices = parse_index_arguments(tokens, ValkyrieTokenKind.bracket_r);
        return TermNodeExtensions.create_offset_index(target, indices);
    }

    private static IReadOnlyList<TermNode> parse_index_arguments(TokenStream tokens, ValkyrieTokenKind closeToken)
    {
        var indices = new List<TermNode>();
        while (!tokens.check(closeToken) && !tokens.is_at_end())
        {
            indices.Add(tokens.parse_term_value_node());
            if (!tokens.match(ValkyrieTokenKind.comma)) break;
        }

        tokens.expect(closeToken);
        return indices;
    }

    private static TermNode parse_member_access(TokenStream tokens, TermNode target, ValkyrieTokenKind separator)
    {
        tokens.expect(separator);
        var memberPath = parse_member_path(tokens);
        var callBody = parse_trailing_member_call_body(tokens);
        var separatorKind = separator == ValkyrieTokenKind.double_colon
            ? MemberAccessSeparatorKind.double_colon
            : MemberAccessSeparatorKind.dot;

        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
            return parse_call(tokens,
                TermNodeExtensions.create_member_access(target, memberPath, separatorKind: separatorKind),
                callBody?.type_arguments);

        return TermNodeExtensions.create_member_access(target, memberPath, callBody, separatorKind);
    }

    private static string unquote_string_literal(string text)
    {
        return try_parse_string_literal(text, out var prefix, out _, out var content)
            ? has_raw_prefix(prefix)
                ? content
                : unescape_string_literal(content)
            : text;
    }

    private static TermNode parse_string_literal(TokenStream tokens, string rawText)
    {
        if (!try_parse_string_literal(rawText, out var prefix, out _, out var content))
        {
            return ParserNodeFactory.create_term_literal_text(
                unquote_string_literal(rawText),
                get_text_literal_kind(rawText));
        }

        if (has_raw_prefix(prefix) && contains_interpolation(content))
        {
            return parse_raw_string_literal(tokens, prefix, content);
        }

        return ParserNodeFactory.create_term_literal_text(
            has_raw_prefix(prefix) ? content : unescape_string_literal(content),
            get_text_literal_kind(rawText),
            prefix);
    }

    private static TermNode parse_raw_string_literal(TokenStream tokens, string prefix, string content)
    {
        var parts = new List<TermNode>();
        var textBuilder = new System.Text.StringBuilder();
        var hasInterpolation = false;
        var index = 0;

        while (index < content.Length)
        {
            var current = content[index];

            if (current == '{' && index + 1 < content.Length && content[index + 1] == '{')
            {
                textBuilder.Append('{');
                index += 2;
                continue;
            }

            if (current == '}' && index + 1 < content.Length && content[index + 1] == '}')
            {
                textBuilder.Append('}');
                index += 2;
                continue;
            }

            if (current != '{')
            {
                textBuilder.Append(current);
                index++;
                continue;
            }

            hasInterpolation = true;

            if (textBuilder.Length > 0)
            {
                parts.Add(ParserNodeFactory.create_term_literal_text(textBuilder.ToString(),
                    TextLiteralKind.literal_text));
                textBuilder.Clear();
            }

            if (!try_find_interpolation_end(content, index, out var closeIndex))
            {
                tokens.diagnostics?.report_error(new DiagnosticTextSpan(tokens.position, 1), "原始字符串插值缺少闭合 `}`。");
                return ParserNodeFactory.create_term_literal_text(content, TextLiteralKind.literal_text);
            }

            var expressionText = content[(index + 1)..closeIndex].Trim();
            if (expressionText.Length == 0)
            {
                tokens.diagnostics?.report_error(new DiagnosticTextSpan(tokens.position, 1), "原始字符串插值不能为空表达式。");
                parts.Add(ParserNodeFactory.create_term_literal_text(string.Empty, TextLiteralKind.literal_text));
            }
            else
            {
                parts.Add(wrap_interpolation_expression(parse_interpolation_expression_node(tokens.diagnostics,
                    expressionText)));
            }

            index = closeIndex + 1;
        }

        if (textBuilder.Length > 0 || !hasInterpolation)
        {
            parts.Add(ParserNodeFactory.create_term_literal_text(textBuilder.ToString(), TextLiteralKind.literal_text));
        }

        return combine_interpolated_text_parts(parts);
    }

    private static bool try_find_interpolation_end(string content, int openBraceIndex, out int closeBraceIndex)
    {
        closeBraceIndex = -1;
        var braceDepth = 0;
        var inString = false;
        var currentQuote = '\0';
        var escaped = false;

        for (var index = openBraceIndex; index < content.Length; index++)
        {
            var current = content[index];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == currentQuote)
                {
                    inString = false;
                }

                continue;
            }

            if (current is '"' or '\'')
            {
                inString = true;
                currentQuote = current;
                continue;
            }

            if (current == '{')
            {
                braceDepth++;
                continue;
            }

            if (current != '}')
            {
                continue;
            }

            braceDepth--;
            if (braceDepth == 0)
            {
                closeBraceIndex = index;
                return true;
            }
        }

        return false;
    }

    private static TermNode combine_interpolated_text_parts(IReadOnlyList<TermNode> parts)
    {
        if (parts.Count == 0)
        {
            return ParserNodeFactory.create_term_literal_text(string.Empty, TextLiteralKind.literal_text);
        }

        TermNode current = parts[0];
        if (current is not TermLiteralTextNode)
        {
            current = new TermBinaryExpression(
                TermBinaryOperator.addition,
                ParserNodeFactory.create_term_literal_text(string.Empty, TextLiteralKind.literal_text),
                current);
        }

        for (var index = 1; index < parts.Count; index++)
        {
            current = new TermBinaryExpression(TermBinaryOperator.addition, current, parts[index]);
        }

        return current;
    }

    private static TermNode parse_interpolation_expression_node(DiagnosticSink? diagnostics, string expressionText)
    {
        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var expressionTokens = lexer.tokenize(expressionText);
        var source = new TokenStream(expressionTokens, ValkyrieNodeKindClassifier.instance, diagnostics);
        return source.parse_term_node();
    }

    private static TermNode wrap_interpolation_expression(TermNode expression)
    {
        var toStringPath = ParserNodeFactory.create_qualified_path("to_string");
        var dot = TermNodeExtensions.create_member_access(expression, toStringPath);
        return TermNodeExtensions.attach_call(dot, TermNodeExtensions.create_call_body());
    }

    private static bool contains_interpolation(string content)
    {
        for (var index = 0; index < content.Length; index++)
        {
            if (content[index] != '{')
            {
                continue;
            }

            if (index + 1 < content.Length && content[index + 1] == '{')
            {
                index++;
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool has_raw_prefix(string prefix)
    {
        return string.Equals(prefix, "r", StringComparison.Ordinal);
    }

    private static bool try_parse_string_literal(string text, out string prefix, out char quote, out string content)
    {
        prefix = string.Empty;
        quote = '\0';
        content = text;

        var quoteIndex = get_string_quote_index(text);
        if (quoteIndex < 0)
        {
            return false;
        }

        prefix = text[..quoteIndex];
        quote = text[quoteIndex];
        content = text[(quoteIndex + 1)..^1];
        return true;
    }

    private static int get_string_quote_index(string text)
    {
        if (text.Length < 2)
        {
            return -1;
        }

        for (var index = 0; index < text.Length - 1; index++)
        {
            var current = text[index];
            if ((current == '"' || current == '\'') && text[^1] == current)
            {
                return index;
            }
        }

        return -1;
    }

    private static string unescape_string_literal(string text)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text;
        }

        var builder = new System.Text.StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (current != '\\' || index + 1 >= text.Length)
            {
                builder.Append(current);
                continue;
            }

            index++;
            builder.Append(text[index] switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '\\' => '\\',
                '"' => '"',
                '\'' => '\'',
                '0' => '\0',
                _ => text[index]
            });
        }

        return builder.ToString();
    }

    private static TextLiteralKind get_text_literal_kind(string text)
    {
        var quoteIndex = get_string_quote_index(text);
        return quoteIndex >= 0 && text[quoteIndex] == '\''
            ? TextLiteralKind.literal_char
            : TextLiteralKind.literal_text;
    }

    private static CallBody? parse_trailing_member_call_body(TokenStream tokens)
    {
        if (tokens.check(ValkyrieTokenKind.double_colon) && tokens.check(ValkyrieTokenKind.less, 1))
            return TermNodeExtensions.create_call_body(parse_explicit_type_arguments(tokens));

        return null;
    }

    private static QualifiedPathNode parse_member_path(TokenStream tokens)
    {
        var segments = new List<IdentifierNode> { ParserNodeFactory.create_identifier(tokens.advance_text()) };
        while (tokens.check(ValkyrieTokenKind.double_colon) && can_start_name(tokens.peek_valkyrie_kind(1), tokens, 1))
        {
            if (tokens.check(ValkyrieTokenKind.less, 1) || tokens.check(ValkyrieTokenKind.bracket_l, 1)) break;

            tokens.advance();
            segments.Add(ParserNodeFactory.create_identifier(tokens.advance_text()));
        }

        return ParserNodeFactory.create_qualified_path(segments);
    }

    private static TermLiteralObjectNode parse_object_initializer(TokenStream tokens, ValkyrieNode constructor)
    {
        tokens.expect(ValkyrieTokenKind.brace_l);
        var fields = new List<TermObjectField>();
        var hasSpread = false;

        while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.brace_r))
        {
            if (tokens.check(ValkyrieTokenKind.dot) && tokens.check(ValkyrieTokenKind.dot, 1))
            {
                tokens.advance();
                tokens.advance();
                hasSpread = true;
                tokens.match(ValkyrieTokenKind.comma);
                continue;
            }

            var name = tokens.advance_text();
            TermNode? value = null;
            if (tokens.match(ValkyrieTokenKind.colon)) value = tokens.parse_term_value_node();

            fields.Add(TermNodeExtensions.create_object_field(name, value));

            tokens.match(ValkyrieTokenKind.comma);
        }

        tokens.expect(ValkyrieTokenKind.brace_r);
        return TermNodeExtensions.create_object_literal(constructor, fields, hasSpread);
    }

    private static TermLiteralArrayNode parse_array_literal(TokenStream tokens)
    {
        tokens.expect(ValkyrieTokenKind.bracket_l);
        var elements = new List<TermNode>();
        while (!tokens.check(ValkyrieTokenKind.bracket_r) && !tokens.is_at_end())
        {
            elements.Add(tokens.parse_term_value_node());
            if (!tokens.match(ValkyrieTokenKind.comma)) break;
        }

        tokens.expect(ValkyrieTokenKind.bracket_r);
        return TermNodeExtensions.create_array_literal(elements);
    }

    private static TermNode require_term_node(TokenStream tokens, ValkyrieNode node, int position, string message)
    {
        if (node is TermNode termNode) return termNode;

        tokens.diagnostics?.report_error(new DiagnosticTextSpan(position, 1), message);
        return ParserNodeFactory.create_term_literal_symbol("<invalid-term>");
    }

    /// <summary>
    ///     解析 if 表达式，如 <c>if condition { then_expr } else { else_expr }</c>
    /// </summary>
    private static TermIfExpression parse_if_expression(TokenStream tokens)
    {
        var span = new TextSpan(tokens.position, 1);
        tokens.advance(); // 消费 if

        // 解析条件表达式
        TermNode cond;
        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
        {
            tokens.advance();
            cond = tokens.parse_term_node();
            tokens.expect(ValkyrieTokenKind.parenthesis_r);
        }
        else
        {
            cond = tokens.parse_block_introducer_term_node();
        }

        // 解析 then 分支
        tokens.expect(ValkyrieTokenKind.brace_l);
        var thenBranch = tokens.parse_term_node();
        tokens.expect(ValkyrieTokenKind.brace_r);

        // 解析 else 分支
        tokens.expect(ValkyrieTokenKind.@else);
        tokens.expect(ValkyrieTokenKind.brace_l);
        var elseBranch = tokens.parse_term_node();
        tokens.expect(ValkyrieTokenKind.brace_r);

        return new TermIfExpression(
            require_term_node(tokens, cond, span.start, "if 表达式条件必须是普通表达式。"),
            require_term_node(tokens, thenBranch, span.start, "if 表达式 then 分支必须是普通表达式。"),
            require_term_node(tokens, elseBranch, span.start, "if 表达式 else 分支必须是普通表达式。"),
            span);
    }

    private static T parse_without_trailing_block_binding<T>(TokenStream tokens, Func<T> parse)
    {
        var state = _s_trailing_block_binding_states.GetOrCreateValue(tokens);
        state.suppress_depth++;

        try
        {
            return parse();
        }
        finally
        {
            state.suppress_depth--;
        }
    }

    private static bool is_trailing_block_binding_suppressed(TokenStream tokens)
    {
        return _s_trailing_block_binding_states.GetOrCreateValue(tokens).suppress_depth > 0;
    }

    private sealed class TrailingBlockBindingState
    {
        public int suppress_depth { get; set; }
    }
}
