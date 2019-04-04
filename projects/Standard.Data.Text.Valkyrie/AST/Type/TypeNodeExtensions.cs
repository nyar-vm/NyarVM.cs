using Std.Data.Text.Valkyrie.Lexer;

namespace Std.Data.Text.Valkyrie.AST.Type;

public static class TypeNodeExtensions
{
    extension(TypeUnaryOperator unary)
    {
        public int precedence()
        {
            return unary switch
            {
                TypeUnaryOperator.not => 80,
                TypeUnaryOperator.contravariance => 80,
                TypeUnaryOperator.covariance => 80,
                TypeUnaryOperator.nullable => 90,
                _ => 0
            };
        }

        public int prefix_binding_power()
        {
            return unary switch
            {
                TypeUnaryOperator.not => unary.precedence(),
                TypeUnaryOperator.contravariance => unary.precedence(),
                TypeUnaryOperator.covariance => unary.precedence(),
                _ => 0
            };
        }

        public int postfix_binding_power()
        {
            return unary switch
            {
                TypeUnaryOperator.nullable => unary.precedence(),
                _ => 0
            };
        }

        public TypeNode create_node(TypeNode operand, bool isPrefix = false)
        {
            return new TypeExpressionUnaryNode(unary, operand, isPrefix);
        }

        public static bool try_parse_prefix_operator(ValkyrieTokenKind token, out TypeUnaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.plus:
                    found = TypeUnaryOperator.covariance;
                    return true;
                case ValkyrieTokenKind.minus:
                    found = TypeUnaryOperator.contravariance;
                    return true;
                case ValkyrieTokenKind.bang:
                    found = TypeUnaryOperator.not;
                    return true;
                default:
                    found = default;
                    return false;
            }
        }

        public static bool try_parse_postfix_operator(ValkyrieTokenKind token, out TypeUnaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.question:
                    found = TypeUnaryOperator.nullable;
                    return true;
                default:
                    found = default;
                    return false;
            }
        }
    }

    extension(TypeBinaryOperator binary)
    {
        public int precedence()
        {
            return binary switch
            {
                TypeBinaryOperator.or => 20,
                TypeBinaryOperator.and => 30,
                TypeBinaryOperator.difference => 40,
                TypeBinaryOperator.product => 50,
                _ => 0
            };
        }

        public bool is_right_associative()
        {
            return false;
        }

        public TypeNode create_node(TypeNode left, TypeNode right)
        {
            return new TypeExpressionBinaryNode(binary, left, right);
        }

        public static bool try_parse_operator(ValkyrieTokenKind token, out TypeBinaryOperator found)
        {
            switch (token)
            {
                case ValkyrieTokenKind.pipe:
                    found = TypeBinaryOperator.or;
                    return true;
                case ValkyrieTokenKind.amp:
                    found = TypeBinaryOperator.and;
                    return true;
                case ValkyrieTokenKind.minus:
                    found = TypeBinaryOperator.difference;
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
        if (token == ValkyrieTokenKind.arrow)
        {
            leftBindingPower = 10;
            rightBindingPower = 10;
            return true;
        }

        if (TypeBinaryOperator.try_parse_operator(token, out var binary))
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

    public static TypeNode create_binary_node(ValkyrieTokenKind token, TypeNode left, TypeNode right)
    {
        if (token == ValkyrieTokenKind.arrow) return new TypeMicroNode(left, right);

        if (TypeBinaryOperator.try_parse_operator(token, out var binary)) return binary.create_node(left, right);

        throw new ArgumentOutOfRangeException(nameof(token), token, "不支持的类型中缀运算符。");
    }

    public static TypeLiteralNamePathNode create_named_type(string text, bool isGlobal = false,
        TypeArgumentList? typeArguments = null)
    {
        var segments = text
            .Split(["::", "."], StringSplitOptions.RemoveEmptyEntries)
            .Select(static part => new IdentifierNode(part))
            .ToList();

        return create_named_type(segments, isGlobal, typeArguments);
    }

    public static TypeLiteralNamePathNode create_named_type(IReadOnlyList<IdentifierNode> segments,
        bool isGlobal = false,
        TypeArgumentList? typeArguments = null)
    {
        return new TypeLiteralNamePathNode
        {
            path = new QualifiedPathNode
            {
                is_global = isGlobal,
                segments = segments
            },
            type_arguments = typeArguments
        };
    }

    public static TypeNode create_unit_type()
    {
        return create_named_type("Unit");
    }

    public static TypeNode create_any_type()
    {
        return create_named_type("any");
    }

    public static TypeNode apply_type_arguments(TypeNode targetType, IReadOnlyList<TypeNode> arguments)
    {
        if (arguments.Count == 0) return targetType;

        return apply_type_arguments(
            targetType,
            create_type_argument_list(arguments
                .Select(static argument => create_type_argument(argument))
                .ToArray()));
    }

    public static TypeNode apply_type_arguments(TypeNode targetType, TypeArgumentList arguments)
    {
        if (arguments.items.Count == 0) return targetType;

        if (targetType is TypeLiteralNamePathNode namedType)
        {
            return namedType with { type_arguments = arguments };
        }

        var items = new List<TypeNode>(arguments.items.Count + 1)
        {
            targetType
        };

        items.AddRange(arguments.items.Select(static item => item.argument));
        return create_product_type(items);
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

    public static TypeTupleElementNode create_tuple_element(TypeNode type, IdentifierNode? label = null)
    {
        return new TypeTupleElementNode
        {
            label = label,
            type = type
        };
    }

    public static TypeLiteralTupleNode create_tuple_type(IReadOnlyList<TypeTupleElementNode> elements)
    {
        return new TypeLiteralTupleNode
        {
            elements = elements
        };
    }

    public static TypeNode create_product_type(IReadOnlyList<TypeNode> items)
    {
        if (items.Count == 0) return create_unit_type();

        var current = items[0];
        for (var i = 1; i < items.Count; i++) current = TypeBinaryOperator.product.create_node(current, items[i]);

        return current;
    }

    public static TypeNode collapse_grouped_types(IReadOnlyList<TypeNode> items)
    {
        return items.Count switch
        {
            0 => create_unit_type(),
            1 => items[0],
            _ => create_product_type(items)
        };
    }

    public static TypeNode collapse_parenthesized_types(IReadOnlyList<TypeTupleElementNode> items, bool hasTrailingComma)
    {
        return items.Count switch
        {
            0 => create_tuple_type(items),
            1 when !hasTrailingComma && items[0].label is null => items[0].type,
            _ => create_tuple_type(items)
        };
    }

    public static TypeNode create_function_type(TypeNode parameterType, TypeNode? returnType = null)
    {
        return new TypeMicroNode(parameterType, returnType ?? create_unit_type());
    }

    public static TypeNode create_function_type(IReadOnlyList<TypeNode> parameterTypes, TypeNode? returnType = null)
    {
        return create_function_type(collapse_grouped_types(parameterTypes), returnType);
    }

    /// <summary>
    ///     创建数组类型 <c>Array&lt;T&gt;</c>，对应语法 <c>[T]</c>
    /// </summary>
    public static TypeNode create_array_type(TypeNode elementType)
    {
        return apply_type_arguments(create_named_type("Array"), [elementType]);
    }

    /// <summary>
    ///     创建定长数组类型 <c>FixedArray&lt;T, N&gt;</c>，对应语法 <c>[T; N]</c>
    /// </summary>
    public static TypeNode create_fixed_array_type(TypeNode elementType, int size)
    {
        return apply_type_arguments(create_named_type("FixedArray"), [elementType, create_literal_number(size)]);
    }

    /// <summary>
    ///     创建字面量数值类型节点
    /// </summary>
    private static TypeNode create_literal_number(int value)
    {
        return new TypeLiteralNumberNode(value);
    }

    public static TypeNode create_reference_type(TypeNode elementType)
    {
        return apply_type_arguments(create_named_type("&"), [elementType]);
    }
}
