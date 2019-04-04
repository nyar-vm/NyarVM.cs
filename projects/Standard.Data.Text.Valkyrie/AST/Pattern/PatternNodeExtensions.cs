using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.AST.Pattern;

public static class PatternNodeExtensions
{
    extension(PatternUnaryOperator unary)
    {
        public int precedence()
        {
            return unary switch
            {
                PatternUnaryOperator.not => 80,
                PatternUnaryOperator.plus => 80,
                PatternUnaryOperator.minus => 80,
                _ => 0
            };
        }

        public int prefix_binding_power()
        {
            return unary.precedence();
        }

        public PatternNode create_node(PatternNode operand)
        {
            return new PatternExpressionUnaryNode
            {
                @operator = unary,
                operand = operand
            };
        }

        public static bool try_parse_prefix_operator(ValkyrieTokenKind token, out PatternUnaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.bang:
                    found = PatternUnaryOperator.not;
                    return true;
                case ValkyrieTokenKind.plus:
                    found = PatternUnaryOperator.plus;
                    return true;
                case ValkyrieTokenKind.minus:
                    found = PatternUnaryOperator.minus;
                    return true;
                default:
                    found = default;
                    return false;
            }
        }
    }

    extension(PatternBinaryOperator binary)
    {
        public int precedence()
        {
            return binary switch
            {
                PatternBinaryOperator.fake_or => 20,
                PatternBinaryOperator.range_inclusive => 5,
                _ => 0
            };
        }

        public bool is_right_associative()
        {
            return false;
        }

        public PatternNode create_node(PatternNode left, PatternNode right)
        {
            if (binary == PatternBinaryOperator.range_inclusive)
                return new PatternLiteralRangeNode
                {
                    lower = left,
                    upper = right
                };

            return new PatternExpressionBinaryNode
            {
                @operator = binary,
                lhs = left,
                rhs = right
            };
        }

        public static bool try_parse_operator(ValkyrieTokenKind token, out PatternBinaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.pipe:
                    found = PatternBinaryOperator.fake_or;
                    return true;
                case ValkyrieTokenKind.dot_dot_equal:
                    found = PatternBinaryOperator.range_inclusive;
                    return true;
                default:
                    found = default;
                    return false;
            }
        }
    }

    public static bool try_get_infix_binding_power(ValkyrieTokenKind token, out int leftBindingPower,
        out int rightBindingPower)
    {
        if (PatternBinaryOperator.try_parse_operator(token, out var binary))
        {
            leftBindingPower = binary.precedence();
            rightBindingPower = binary.is_right_associative()
                ? binary.precedence()
                : binary.precedence() + 1;
            return true;
        }

        leftBindingPower = 0;
        rightBindingPower = 0;
        return false;
    }

    public static PatternNode create_binary_node(ValkyrieTokenKind token, PatternNode left, PatternNode right)
    {
        if (PatternBinaryOperator.try_parse_operator(token, out var binary)) return binary.create_node(left, right);

        throw new ArgumentOutOfRangeException(nameof(token), token, "不支持的模式中缀运算符。");
    }

    public static PatternLiteralWildcardNode create_wildcard_pattern(TextSpan? span = null)
    {
        return span is { } value
            ? new PatternLiteralWildcardNode(value)
            : new PatternLiteralWildcardNode();
    }

    public static PatternLiteralNumberNode create_number_pattern(string value)
    {
        return new PatternLiteralNumberNode
        {
            value = value
        };
    }

    public static PatternLiteralNullNode create_null_pattern()
    {
        return new PatternLiteralNullNode();
    }

    public static PatternLiteralTextNode create_text_pattern(string value, TextLiteralKind literalKind)
    {
        return new PatternLiteralTextNode
        {
            value = value,
            literal_kind = literalKind
        };
    }

    public static PatternLiteralBooleanNode create_boolean_pattern(bool value)
    {
        return new PatternLiteralBooleanNode
        {
            value = value
        };
    }

    public static PatternLiteralVariableNode create_variable_pattern(string name)
    {
        return new PatternLiteralVariableNode
        {
            name = name
        };
    }

    public static PatternLiteralObjectNode create_object_pattern(QualifiedPathNode path,
        IReadOnlyList<PatternLiteralFieldNode> fields)
    {
        return new PatternLiteralObjectNode
        {
            path = path,
            fields = fields
        };
    }

    public static PatternLiteralTupleNode create_tuple_pattern(QualifiedPathNode path,
        IReadOnlyList<PatternNode> elements)
    {
        return new PatternLiteralTupleNode
        {
            path = path,
            elements = elements
        };
    }

    public static bool try_create_pattern_from_type(TypeNode type, out PatternNode pattern)
    {
        if (type is TypeLiteralNamePathNode namePath)
        {
            pattern = create_object_pattern(namePath.path, []);
            return true;
        }

        pattern = null!;
        return false;
    }
}
