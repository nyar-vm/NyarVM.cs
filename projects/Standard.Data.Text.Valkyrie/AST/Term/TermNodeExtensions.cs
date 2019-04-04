using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.AST.Term;

public static class TermNodeExtensions
{
    extension(TermUnaryOperator unary)
    {
        public int precedence()
        {
            return unary switch
            {
                TermUnaryOperator.logical_not => 170,
                TermUnaryOperator.bitwise_not => 170,
                TermUnaryOperator.negate => 170,
                TermUnaryOperator.increment => 190,
                TermUnaryOperator.decrement => 190,
                _ => 0
            };
        }

        public int prefix_binding_power()
        {
            return unary switch
            {
                TermUnaryOperator.logical_not => 170,
                TermUnaryOperator.bitwise_not => 170,
                TermUnaryOperator.negate => 170,
                TermUnaryOperator.increment => 170,
                TermUnaryOperator.decrement => 170,
                _ => 0
            };
        }

        public int postfix_binding_power()
        {
            return unary switch
            {
                TermUnaryOperator.increment => 190,
                TermUnaryOperator.decrement => 190,
                TermUnaryOperator.try_operator => 190,
                _ => 0
            };
        }

        public TermNode create_node(TermNode operand)
        {
            return new TermUnaryExpression(unary, operand);
        }

        public static bool try_parse_prefix_operator(ValkyrieTokenKind token, out TermUnaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.bang:
                    found = TermUnaryOperator.logical_not;
                    return true;
                case ValkyrieTokenKind.tilde:
                    found = TermUnaryOperator.bitwise_not;
                    return true;
                case ValkyrieTokenKind.minus:
                    found = TermUnaryOperator.negate;
                    return true;
                case ValkyrieTokenKind.plus_plus:
                    found = TermUnaryOperator.increment;
                    return true;
                case ValkyrieTokenKind.minus_minus:
                    found = TermUnaryOperator.decrement;
                    return true;
                default:
                    found = default;
                    return false;
            }
        }

        public static bool try_parse_postfix_operator(ValkyrieTokenKind token, out TermUnaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.plus_plus:
                    found = TermUnaryOperator.increment;
                    return true;
                case ValkyrieTokenKind.minus_minus:
                    found = TermUnaryOperator.decrement;
                    return true;
                case ValkyrieTokenKind.question:
                    found = TermUnaryOperator.try_operator;
                    return true;
                default:
                    found = default;
                    return false;
            }
        }
    }

    extension(TermBinaryOperator binary)
    {
        public int precedence()
        {
            return binary switch
            {
                TermBinaryOperator.logical_or => 10,
                TermBinaryOperator.logical_and => 20,
                TermBinaryOperator.bitwise_or => 30,
                TermBinaryOperator.bitwise_xor => 40,
                TermBinaryOperator.bitwise_and => 50,
                TermBinaryOperator.equal => 60,
                TermBinaryOperator.not_equal => 60,
                TermBinaryOperator.less_than => 80,
                TermBinaryOperator.greater_than => 80,
                TermBinaryOperator.less_than_or_equal => 80,
                TermBinaryOperator.greater_than_or_equal => 80,
                TermBinaryOperator.left_shift => 90,
                TermBinaryOperator.right_shift => 90,
                TermBinaryOperator.addition => 100,
                TermBinaryOperator.subtraction => 100,
                TermBinaryOperator.multiplication => 110,
                TermBinaryOperator.division => 110,
                TermBinaryOperator.modulus => 110,
                TermBinaryOperator.power => 120,

                TermBinaryOperator.assign => 5,
                TermBinaryOperator.plus_assign => 5,
                TermBinaryOperator.minus_assign => 5,
                TermBinaryOperator.multiply_assign => 5,
                TermBinaryOperator.divide_assign => 5,
                TermBinaryOperator.modulus_assign => 5,
                TermBinaryOperator.and_assign => 5,
                TermBinaryOperator.or_assign => 5,
                TermBinaryOperator.xor_assign => 5,
                TermBinaryOperator.left_shift_assign => 5,
                TermBinaryOperator.right_shift_assign => 5,
                _ => 0
            };
        }

        public bool is_right_associative()
        {
            return binary switch
            {
                TermBinaryOperator.power => true,

                TermBinaryOperator.assign => true,
                TermBinaryOperator.plus_assign => true,
                TermBinaryOperator.minus_assign => true,
                TermBinaryOperator.multiply_assign => true,
                TermBinaryOperator.divide_assign => true,
                TermBinaryOperator.modulus_assign => true,
                TermBinaryOperator.and_assign => true,
                TermBinaryOperator.or_assign => true,
                TermBinaryOperator.xor_assign => true,
                TermBinaryOperator.left_shift_assign => true,
                TermBinaryOperator.right_shift_assign => true,
                _ => false
            };
        }

        public TermNode create_node(TermNode left, TermNode right)
        {
            return new TermBinaryExpression(binary, left, right);
        }

        public static bool try_parse_binary_operator(ValkyrieTokenKind token, out TermBinaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.equal:
                    found = TermBinaryOperator.assign;
                    return true;
                case ValkyrieTokenKind.plus_equal:
                    found = TermBinaryOperator.plus_assign;
                    return true;
                case ValkyrieTokenKind.minus_equal:
                    found = TermBinaryOperator.minus_assign;
                    return true;
                case ValkyrieTokenKind.star_equal:
                    found = TermBinaryOperator.multiply_assign;
                    return true;
                case ValkyrieTokenKind.slash_equal:
                    found = TermBinaryOperator.divide_assign;
                    return true;
                case ValkyrieTokenKind.percent_equal:
                    found = TermBinaryOperator.modulus_assign;
                    return true;
                case ValkyrieTokenKind.amp_equal:
                    found = TermBinaryOperator.and_assign;
                    return true;
                case ValkyrieTokenKind.pipe_equal:
                    found = TermBinaryOperator.or_assign;
                    return true;
                case ValkyrieTokenKind.caret_equal:
                    found = TermBinaryOperator.xor_assign;
                    return true;
                case ValkyrieTokenKind.less_less_equal:
                    found = TermBinaryOperator.left_shift_assign;
                    return true;
                case ValkyrieTokenKind.greater_greater_equal:
                    found = TermBinaryOperator.right_shift_assign;
                    return true;

                case ValkyrieTokenKind.equal_equal:
                    found = TermBinaryOperator.equal;
                    return true;
                case ValkyrieTokenKind.bang_equal:
                    found = TermBinaryOperator.not_equal;
                    return true;
                case ValkyrieTokenKind.less:
                    found = TermBinaryOperator.less_than;
                    return true;
                case ValkyrieTokenKind.greater:
                    found = TermBinaryOperator.greater_than;
                    return true;
                case ValkyrieTokenKind.less_equal:
                    found = TermBinaryOperator.less_than_or_equal;
                    return true;
                case ValkyrieTokenKind.greater_equal:
                    found = TermBinaryOperator.greater_than_or_equal;
                    return true;
                case ValkyrieTokenKind.amp_amp:
                    found = TermBinaryOperator.logical_and;
                    return true;
                case ValkyrieTokenKind.pipe_pipe:
                    found = TermBinaryOperator.logical_or;
                    return true;
                case ValkyrieTokenKind.power:
                    found = TermBinaryOperator.power;
                    return true;
                case ValkyrieTokenKind.plus:
                    found = TermBinaryOperator.addition;
                    return true;
                case ValkyrieTokenKind.minus:
                    found = TermBinaryOperator.subtraction;
                    return true;
                case ValkyrieTokenKind.star:
                    found = TermBinaryOperator.multiplication;
                    return true;
                case ValkyrieTokenKind.slash:
                    found = TermBinaryOperator.division;
                    return true;
                case ValkyrieTokenKind.percent:
                    found = TermBinaryOperator.modulus;
                    return true;
                case ValkyrieTokenKind.amp:
                    found = TermBinaryOperator.bitwise_and;
                    return true;
                case ValkyrieTokenKind.pipe:
                    found = TermBinaryOperator.bitwise_or;
                    return true;
                case ValkyrieTokenKind.less_less:
                    found = TermBinaryOperator.left_shift;
                    return true;
                case ValkyrieTokenKind.greater_greater:
                    found = TermBinaryOperator.right_shift;
                    return true;
                default:
                    found = default;
                    return false;
            }
        }
    }

    public static bool try_get_binary_binding_power(ValkyrieTokenKind token, out int leftBindingPower,
        out int rightBindingPower)
    {
        if (TermBinaryOperator.try_parse_binary_operator(token, out var binary))
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

    public static TermNode create_binary_node(ValkyrieTokenKind token, TermNode left, TermNode right)
    {
        if (TermBinaryOperator.try_parse_binary_operator(token, out var binary)) return binary.create_node(left, right);

        throw new ArgumentOutOfRangeException(nameof(token), token, "不支持的表达式二元运算符。");
    }

    public static bool try_get_special_infix_binding_power(ValkyrieTokenKind token, out int bindingPower)
    {
        switch (token)
        {
            case ValkyrieTokenKind.@is:
            case ValkyrieTokenKind.@as:
            case ValkyrieTokenKind.@in:
                bindingPower = 70;
                return true;
            default:
                bindingPower = 0;
                return false;
        }
    }

    public static int structural_postfix_binding_power()
    {
        return 190;
    }

    public static CallBody create_call_body(
        TypeArgumentList? typeArguments = null,
        TermArgumentList? termArguments = null,
        FunctionBody? functionBody = null)
    {
        return new CallBody
        {
            type_arguments = typeArguments,
            term_arguments = termArguments,
            function_body = functionBody
        };
    }

    public static TermArgumentItem create_argument_item(TermNode value, IDeclarationNode? key = null)
    {
        return new TermArgumentItem
        {
            key = key,
            value = value
        };
    }

    public static TermArgumentList create_argument_list(IReadOnlyList<TermArgumentItem> items)
    {
        return new TermArgumentList
        {
            items = items
        };
    }

    public static TypeArgumentItem create_type_argument(TypeNode argument, IdentifierNode? slot = null)
    {
        return new TypeArgumentItem
        {
            slot = slot,
            argument = argument
        };
    }

    public static TypeArgumentList create_type_argument_list(IReadOnlyList<TypeArgumentItem> items)
    {
        return new TypeArgumentList
        {
            items = items
        };
    }

    public static TermNode attach_call(TermNode caller, CallBody callBody)
    {
        return caller switch
        {
            TermDotExpression member => member with { call_body = callBody },
            _ => new TermCallExpression(caller, callBody)
        };
    }

    public static TermNode attach_type_arguments(TermNode caller, TypeArgumentList typeArguments)
    {
        return attach_call(caller, create_call_body(typeArguments));
    }

    public static TermDotExpression create_member_access(
        TermNode caller,
        QualifiedPathNode callee,
        CallBody? callBody = null,
        MemberAccessSeparatorKind separatorKind = MemberAccessSeparatorKind.dot)
    {
        return new TermDotExpression(caller, callee, callBody, separatorKind);
    }

    public static TermOrdinalExpression create_ordinal_index(TermNode target, IReadOnlyList<TermNode> indices)
    {
        return new TermOrdinalExpression
        {
            target = target,
            indices = indices
        };
    }

    public static TermOffsetExpression create_offset_index(TermNode target, IReadOnlyList<TermNode> indices)
    {
        return new TermOffsetExpression
        {
            target = target,
            indices = indices
        };
    }

    public static TermObjectField create_object_field(string name, ValkyrieNode? value = null)
    {
        return new TermObjectField
        {
            name = name,
            value = value
        };
    }

    public static TermLiteralObjectNode create_object_literal(
        ValkyrieNode constructor,
        IReadOnlyList<TermObjectField> fields,
        bool hasSpread)
    {
        return new TermLiteralObjectNode
        {
            constructor = constructor,
            fields = fields,
            has_spread = hasSpread
        };
    }

    /// <summary>
    ///     创建元组字面量节点
    /// </summary>
    public static TermLiteralTupleNode create_tuple_literal(IReadOnlyList<TermNode> elements)
    {
        return new TermLiteralTupleNode
        {
            elements = elements
        };
    }

    public static TermLiteralArrayNode create_array_literal(IReadOnlyList<TermNode> elements)
    {
        return new TermLiteralArrayNode
        {
            elements = elements
        };
    }

    public static FunctionBody create_expression_body(TermNode expression)
    {
        return new FunctionBody([expression]);
    }

    public static AnonymousMicro create_anonymous_micro(
        IReadOnlyList<TermParameterList> parameters,
        TypeNode? returnType = null,
        FunctionBody? body = null)
    {
        return new AnonymousMicro
        {
            parameters = parameters,
            return_type = returnType,
            body = body
        };
    }
}
