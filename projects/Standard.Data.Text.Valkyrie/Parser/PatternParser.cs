using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.Parser;

internal static class PatternParser
{
    extension(TokenStream tokens)
    {
        internal PatternNode parse_pattern_expression_node()
        {
            return parse_pattern(tokens, 0);
        }
    }

    internal static PatternNode parse_type_pattern_for_is(TokenStream tokens)
    {
        var type = tokens.parse_type_expression_node();
        if (tokens.check(ValkyrieTokenKind.@as))
        {
            tokens.advance();
            _ = tokens.advance_text();
            tokens.diagnostics?.report_error(default, "当前 AST 尚未提供 `is T as name` 的绑定模式节点，已忽略绑定名。");
        }

        return create_pattern_from_type(tokens, type);
    }

    private static PatternNode parse_pattern(TokenStream tokens, int minBindingPower)
    {
        var pattern = parse_primary_pattern(tokens);

        while (!tokens.is_at_end())
        {
            var op = tokens.peek_valkyrie_kind();
            if (!PatternNodeExtensions.try_get_infix_binding_power(op, out var leftBindingPower, out var rightBindingPower)) break;

            if (leftBindingPower < minBindingPower) break;

            tokens.advance();
            var right = parse_pattern(tokens, rightBindingPower);
            pattern = PatternNodeExtensions.create_binary_node(op, pattern, right);
        }

        return pattern;
    }

    private static PatternNode parse_primary_pattern(TokenStream tokens)
    {
        if (tokens.check(ValkyrieTokenKind.identifier) && tokens.peek_text() == "_")
        {
            tokens.advance();
            return PatternNodeExtensions.create_wildcard_pattern();
        }

        if (PatternUnaryOperator.try_parse_prefix_operator(tokens.peek_valkyrie_kind(), out var prefixOperator))
        {
            tokens.advance();
            return prefixOperator.create_node(parse_pattern(tokens, prefixOperator.prefix_binding_power()));
        }

        if (tokens.check(ValkyrieTokenKind.number)) return PatternNodeExtensions.create_number_pattern(tokens.advance_text());

        if (tokens.check(ValkyrieTokenKind.@null))
        {
            _ = tokens.advance_text();
            return PatternNodeExtensions.create_null_pattern();
        }

        if (tokens.check(ValkyrieTokenKind.@string)
            || tokens.check(ValkyrieTokenKind.@true)
            || tokens.check(ValkyrieTokenKind.@false))
        {
            if (tokens.check(ValkyrieTokenKind.@string))
            {
                var rawText = tokens.advance_text();
                return PatternNodeExtensions.create_text_pattern(
                    unquote_string_literal(rawText),
                    get_text_literal_kind(rawText));
            }

            var tokenText = tokens.advance_text();
            return PatternNodeExtensions.create_boolean_pattern(
                string.Equals(tokenText, "true", StringComparison.OrdinalIgnoreCase));
        }

        if (tokens.check(ValkyrieTokenKind.parenthesis_l))
        {
            tokens.advance();
            var inner = parse_pattern(tokens, 0);
            if (tokens.match(ValkyrieTokenKind.comma))
            {
                var elements = new List<PatternNode> { inner };
                while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.parenthesis_r))
                {
                    elements.Add(parse_pattern(tokens, 0));
                    if (!tokens.match(ValkyrieTokenKind.comma)) break;
                }

                tokens.expect(ValkyrieTokenKind.parenthesis_r);
                return new PatternLiteralTupleNode
                {
                    elements = elements
                };
            }

            tokens.expect(ValkyrieTokenKind.parenthesis_r);
            return inner;
        }

        // Support array patterns: [] or [head, ..tail] or [head, tail, ...]
        if (tokens.check(ValkyrieTokenKind.bracket_l))
        {
            tokens.advance();
            var elements = new List<PatternNode>();

            // Check for empty array pattern
            if (tokens.check(ValkyrieTokenKind.bracket_r))
            {
                tokens.advance();
                return PatternNodeExtensions.create_wildcard_pattern(); // TODO: Return actual empty array pattern
            }

            while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.bracket_r))
            {
                elements.Add(parse_pattern(tokens, 0));

                if (tokens.check(ValkyrieTokenKind.dot_dot) || tokens.check(ValkyrieTokenKind.dot_dot_dot))
                {
                    // Spread pattern: ..tail or ...rest
                    var spreadKind = tokens.check(ValkyrieTokenKind.dot_dot) 
                        ? ValkyrieTokenKind.dot_dot 
                        : ValkyrieTokenKind.dot_dot_dot;
                    tokens.advance();
                    
                    // Variable pattern for the spread
                    if (tokens.check(ValkyrieTokenKind.identifier))
                    {
                        var varName = tokens.advance_text();
                        elements.Add(PatternNodeExtensions.create_variable_pattern(varName));
                    }
                }

                if (!tokens.match(ValkyrieTokenKind.comma))
                {
                    break;
                }
            }

            tokens.expect(ValkyrieTokenKind.bracket_r);
            return PatternNodeExtensions.create_wildcard_pattern(); // TODO: Return actual array pattern
        }

        if (tokens.check(ValkyrieTokenKind.identifier) || tokens.check(ValkyrieTokenKind.double_colon))
        {
            var path = parse_qualified_pattern_path(tokens);
            if (tokens.check(ValkyrieTokenKind.less))
            {
                consume_type_arguments(tokens);
                tokens.diagnostics?.report_error(default, "当前 AST 尚未保存模式上的泛型参数，仅保留限定路径。");
            }

            if (tokens.check(ValkyrieTokenKind.@as))
            {
                tokens.advance();
                _ = tokens.advance_text();
                tokens.diagnostics?.report_error(default, "当前 AST 尚未提供模式绑定名节点，已忽略 `as` 绑定。");
            }

            if (tokens.check(ValkyrieTokenKind.brace_l))
            {
                return parse_object_pattern(tokens, path);
            }

            if (tokens.check(ValkyrieTokenKind.parenthesis_l))
            {
                return parse_tuple_pattern(tokens, path);
            }

            if (path is { is_global: false, segments.Count: 1 })
            {
                return PatternNodeExtensions.create_variable_pattern(path.segments[0].name);
            }

            return PatternNodeExtensions.create_object_pattern(path, []);
        }

        return create_recovery_pattern(tokens, "无法将当前模式映射到现有 AST。");
    }

    private static PatternLiteralObjectNode parse_object_pattern(TokenStream tokens, QualifiedPathNode path)
    {
        tokens.expect(ValkyrieTokenKind.brace_l);
        var fields = new List<PatternLiteralFieldNode>();

        while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.brace_r))
        {
            if (tokens.check(ValkyrieTokenKind.dot_dot))
            {
                tokens.advance();
                PatternNode? restPattern = null;
                if (tokens.check(ValkyrieTokenKind.identifier))
                {
                    var restToken = tokens.advance();
                    restPattern = PatternNodeExtensions.create_variable_pattern(restToken.text);
                }
                fields.Add(new PatternLiteralFieldNode
                {
                    name = "...",
                    pattern = restPattern,
                    span = new TextSpan(tokens.position - 1, 1)
                });
                if (!tokens.match(ValkyrieTokenKind.comma))
                {
                    break;
                }
                continue;
            }

            var nameToken = tokens.expect(ValkyrieTokenKind.identifier);
            var name = nameToken.text;
            var nameSpan = new TextSpan(tokens.position - 1, 1);

            PatternNode innerPattern;
            if (tokens.match(ValkyrieTokenKind.colon))
            {
                innerPattern = parse_pattern(tokens, 0);
            }
            else
            {
                innerPattern = PatternNodeExtensions.create_variable_pattern(name);
            }

            fields.Add(new PatternLiteralFieldNode
            {
                name = name,
                pattern = innerPattern,
                span = nameSpan
            });

            if (!tokens.match(ValkyrieTokenKind.comma))
            {
                break;
            }
        }

        tokens.expect(ValkyrieTokenKind.brace_r);
        return PatternNodeExtensions.create_object_pattern(path, fields);
    }

    private static PatternLiteralTupleNode parse_tuple_pattern(TokenStream tokens, QualifiedPathNode path)
    {
        tokens.expect(ValkyrieTokenKind.parenthesis_l);
        var elements = new List<PatternNode>();

        while (!tokens.is_at_end() && !tokens.check(ValkyrieTokenKind.parenthesis_r))
        {
            elements.Add(parse_pattern(tokens, 0));

            if (!tokens.match(ValkyrieTokenKind.comma))
            {
                break;
            }
        }

        tokens.expect(ValkyrieTokenKind.parenthesis_r);
        return PatternNodeExtensions.create_tuple_pattern(path, elements);
    }

    private static QualifiedPathNode parse_qualified_pattern_path(TokenStream tokens)
    {
        var isGlobal = tokens.match(ValkyrieTokenKind.double_colon);
        var segments = new List<IdentifierNode>();

        if (!tokens.check(ValkyrieTokenKind.identifier))
        {
            return ParserNodeFactory.create_qualified_path("<invalid-pattern>", isGlobal);
        }

        segments.Add(ParserNodeFactory.create_identifier(tokens.advance_text()));
        while (tokens.check(ValkyrieTokenKind.double_colon) || tokens.check(ValkyrieTokenKind.dot))
        {
            tokens.advance();
            if (!tokens.check(ValkyrieTokenKind.identifier))
            {
                break;
            }

            segments.Add(ParserNodeFactory.create_identifier(tokens.advance_text()));
        }

        return ParserNodeFactory.create_qualified_path(segments, isGlobal);
    }

    private static void consume_type_arguments(TokenStream tokens)
    {
        tokens.expect(ValkyrieTokenKind.less);
        var depth = 1;

        while (!tokens.is_at_end() && depth > 0)
        {
            if (tokens.check(ValkyrieTokenKind.less) || tokens.check(ValkyrieTokenKind.generic_l))
            {
                tokens.advance();
                depth++;
                continue;
            }

            if (tokens.check(ValkyrieTokenKind.greater) || tokens.check(ValkyrieTokenKind.generic_r))
            {
                tokens.advance();
                depth--;
                continue;
            }

            if (tokens.check(ValkyrieTokenKind.greater_greater))
            {
                tokens.advance();
                depth -= 2;
                continue;
            }

            tokens.advance();
        }
    }

    private static PatternNode create_pattern_from_type(TokenStream tokens, TypeNode type)
    {
        if (PatternNodeExtensions.try_create_pattern_from_type(type, out var pattern))
        {
            return pattern;
        }

        return create_recovery_pattern(tokens, "当前 AST 尚未提供复杂类型模式节点。");
    }

    private static PatternNode create_recovery_pattern(TokenStream tokens, string message)
    {
        tokens.diagnostics?.report_error(default, message);
        return PatternNodeExtensions.create_wildcard_pattern();
    }

    private static string unquote_string_literal(string text)
    {
        var quoteIndex = get_string_quote_index(text);
        if (quoteIndex >= 0)
        {
            return text[(quoteIndex + 1)..^1];
        }

        return text;
    }

    private static TextLiteralKind get_text_literal_kind(string text)
    {
        var quoteIndex = get_string_quote_index(text);
        return quoteIndex >= 0 && text[quoteIndex] == '\''
            ? TextLiteralKind.literal_char
            : TextLiteralKind.literal_text;
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
}
