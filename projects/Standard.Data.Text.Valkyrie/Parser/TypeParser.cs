using Std.Data.Text.Parsing;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.Parser;

internal static class TypeParser
{
    extension(TokenStream tokens)
    {
        internal TypeNode parse_type_expression_node()
        {
            return parse_type_expression(tokens, 0);
        }
    }

    internal static bool can_start_type_expression(ValkyrieTokenKind kind)
    {
        return kind is ValkyrieTokenKind.identifier
            or ValkyrieTokenKind.micro
            or ValkyrieTokenKind.parenthesis_l
            or ValkyrieTokenKind.bracket_l
            or ValkyrieTokenKind.amp
            or ValkyrieTokenKind.plus
            or ValkyrieTokenKind.minus
            or ValkyrieTokenKind.bang;
    }

    private static TypeNode parse_type_expression(TokenStream tokens, int minBindingPower)
    {
        var left = parse_type_prefix(tokens);

        while (!tokens.is_at_end())
        {
            if (TypeUnaryOperator.try_parse_postfix_operator(tokens.peek_valkyrie_kind(), out var postfixOperator))
            {
                var postfixBindingPower = postfixOperator.postfix_binding_power();
                if (postfixBindingPower < minBindingPower) break;

                tokens.advance();
                left = postfixOperator.create_node(left);
                continue;
            }

            var op = tokens.peek_valkyrie_kind();
            if (!TypeNodeExtensions.try_get_infix_binding_power(op, out var leftBindingPower,
                    out var rightBindingPower)) break;

            if (leftBindingPower < minBindingPower) break;

            tokens.advance();
            var right = parse_type_expression(tokens, rightBindingPower);
            left = TypeNodeExtensions.create_binary_node(op, left, right);
        }

        return left;
    }

    private static TypeNode parse_type_prefix(TokenStream tokens)
    {
        if (tokens.check(ValkyrieTokenKind.amp))
        {
            tokens.advance();
            var elementType = parse_type_expression(tokens, TypeUnaryOperator.covariance.prefix_binding_power());
            return TypeNodeExtensions.create_reference_type(elementType);
        }

        if (TypeUnaryOperator.try_parse_prefix_operator(tokens.peek_valkyrie_kind(), out var prefixOperator))
        {
            tokens.advance();
            return prefixOperator.create_node(parse_type_expression(tokens, prefixOperator.prefix_binding_power()),
                true);
        }

        if (tokens.check(ValkyrieTokenKind.parenthesis_l)) return parse_parenthesized_type(tokens);

        if (tokens.check(ValkyrieTokenKind.bracket_l))
        {
            tokens.advance();
            var elementType = tokens.parse_type_expression_node();

            // 检查是否为定长数组语法 [T; N]
            if (tokens.match(ValkyrieTokenKind.semicolon))
            {
                var sizeToken = tokens.expect(ValkyrieTokenKind.number);
                tokens.expect(ValkyrieTokenKind.bracket_r);
                var size = int.Parse(sizeToken.text);
                return TypeNodeExtensions.create_fixed_array_type(elementType, size);
            }

            tokens.expect(ValkyrieTokenKind.bracket_r);
            return TypeNodeExtensions.create_array_type(elementType);
        }

        if (tokens.check(ValkyrieTokenKind.micro)) return parse_function_type(tokens);

        return parse_named_type(tokens);
    }

    private static TypeNode parse_parenthesized_type(TokenStream tokens)
    {
        tokens.expect(ValkyrieTokenKind.parenthesis_l);
        var elements = new List<TypeTupleElementNode>();
        var hasTrailingComma = false;

        while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
        {
            elements.Add(parse_tuple_element(tokens));
            hasTrailingComma = tokens.match(ValkyrieTokenKind.comma);
        }

        tokens.expect(ValkyrieTokenKind.parenthesis_r);
        return TypeNodeExtensions.collapse_parenthesized_types(elements, hasTrailingComma);
    }

    private static TypeTupleElementNode parse_tuple_element(TokenStream tokens)
    {
        if (tokens.check(ValkyrieTokenKind.identifier) && tokens.check(ValkyrieTokenKind.colon, 1))
        {
            var label = ParserNodeFactory.create_identifier(tokens.advance_text());
            tokens.expect(ValkyrieTokenKind.colon);
            return TypeNodeExtensions.create_tuple_element(tokens.parse_type_expression_node(), label);
        }

        return TypeNodeExtensions.create_tuple_element(tokens.parse_type_expression_node());
    }

    private static TypeNode parse_function_type(TokenStream tokens)
    {
        tokens.expect(ValkyrieTokenKind.micro);
        tokens.expect(ValkyrieTokenKind.parenthesis_l);

        var parameterTypes = new List<TypeNode>();
        while (!tokens.check(ValkyrieTokenKind.parenthesis_r) && !tokens.is_at_end())
        {
            parameterTypes.Add(tokens.parse_type_expression_node());
            tokens.match(ValkyrieTokenKind.comma);
        }

        tokens.expect(ValkyrieTokenKind.parenthesis_r);

        if (tokens.check(ValkyrieTokenKind.arrow))
        {
            tokens.advance();
            return TypeNodeExtensions.create_function_type(parameterTypes, tokens.parse_type_expression_node());
        }

        return TypeNodeExtensions.create_function_type(parameterTypes);
    }

    private static TypeNode parse_named_type(TokenStream tokens)
    {
        var path = parse_qualified_type_path(tokens);
        var baseType = ParserNodeFactory.create_type_literal_symbol(path.segments, path.is_global);

        if (!tokens.check(ValkyrieTokenKind.less)) return baseType;

        var arguments = parse_type_arguments(tokens);
        return TypeNodeExtensions.apply_type_arguments(baseType, arguments);
    }

    private static QualifiedPathNode parse_qualified_type_path(TokenStream tokens)
    {
        var isGlobal = tokens.match(ValkyrieTokenKind.double_colon);
        var segments = new List<IdentifierNode> { ParserNodeFactory.create_identifier(tokens.advance_text()) };

        while (tokens.check(ValkyrieTokenKind.double_colon) || tokens.check(ValkyrieTokenKind.dot))
        {
            var separator = tokens.peek_valkyrie_kind();
            if (separator == ValkyrieTokenKind.double_colon &&
                (tokens.check(ValkyrieTokenKind.bracket_l, 1) || tokens.check(ValkyrieTokenKind.less, 1)))
                break;

            tokens.advance();
            segments.Add(ParserNodeFactory.create_identifier(tokens.advance_text()));
        }

        return ParserNodeFactory.create_qualified_path(segments, isGlobal);
    }

    private static TypeArgumentList parse_type_arguments(TokenStream tokens)
    {
        tokens.expect(ValkyrieTokenKind.less);
        var arguments = new List<TypeArgumentItem>();

        while (!tokens.is_at_end() && !is_generic_type_close(tokens))
        {
            IdentifierNode? slot = null;
            if (tokens.check(ValkyrieTokenKind.identifier) && tokens.check(ValkyrieTokenKind.equal, 1))
            {
                slot = ParserNodeFactory.create_identifier(tokens.advance_text());
                tokens.expect(ValkyrieTokenKind.equal);
            }

            arguments.Add(TypeNodeExtensions.create_type_argument(tokens.parse_type_expression_node(), slot));
            tokens.match(ValkyrieTokenKind.comma);
        }

        consume_generic_type_close(tokens);
        return TypeNodeExtensions.create_type_argument_list(arguments);
    }

    private static bool is_generic_type_close(TokenStream tokens)
    {
        var state = ValkyrieParser._s_generic_close_states.GetOrCreateValue(tokens);
        return state.pending_greater_closers > 0
               || tokens.check(ValkyrieTokenKind.greater)
               || tokens.check(ValkyrieTokenKind.generic_r)
               || tokens.check(ValkyrieTokenKind.greater_greater);
    }

    private static void consume_generic_type_close(TokenStream tokens)
    {
        var state = ValkyrieParser._s_generic_close_states.GetOrCreateValue(tokens);
        if (state.pending_greater_closers > 0)
        {
            state.pending_greater_closers--;
            return;
        }

        if (tokens.check(ValkyrieTokenKind.greater) || tokens.check(ValkyrieTokenKind.generic_r))
        {
            tokens.advance();
            return;
        }

        if (tokens.check(ValkyrieTokenKind.greater_greater))
        {
            tokens.advance();
            state.pending_greater_closers++;
            return;
        }

        tokens.expect(ValkyrieTokenKind.greater);
    }
}
