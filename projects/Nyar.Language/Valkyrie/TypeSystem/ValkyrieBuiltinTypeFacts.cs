using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     Valkyrie 内建类型与关键字类型的统一构造入口。
/// </summary>
public static class ValkyrieBuiltinTypeFacts
{
    public const string any_name = "any";
    public const string auto_name = "auto";
    public const string self_type_name = "Self";
    public const string self_value_name = "self";
    public const string tuple_kind_tag = "tuple";
    public const string union_kind_tag = "union";
    public const string intersection_kind_tag = "intersection";
    public const string any_kind_tag = "any";

    public static readonly IType bool_type = new PrimitiveType("bool");
    public static readonly IType i32_type = new PrimitiveType("i32");
    public static readonly IType i64_type = new PrimitiveType("i64");
    public static readonly IType f32_type = new PrimitiveType("f32");
    public static readonly IType f64_type = new PrimitiveType("f64");
    public static readonly IType isize_type = new PrimitiveType("isize");
    public static readonly IType usize_type = new PrimitiveType("usize");
    public static readonly IType unit_type = new PrimitiveType("unit");
    public static readonly IType void_type = new PrimitiveType("void");
    public static readonly IType any_type = new NamedType(any_name, any_kind_tag);
    public static readonly IType null_type = new NullableType(UnknownType.instance);
    public static readonly SemanticNamePath array_name = new(["Array"]);
    public static readonly SemanticNamePath std_array_name = new(["std", "collection", "Array"]);
    public static readonly SemanticNamePath tuple_name = new(["tuple"]);
    public static readonly SemanticNamePath unknown_name_path = new(["unknown"]);

    /// <summary>
    ///     根据名称获取常用内建类型缓存。
    /// </summary>
    public static IType? try_get_cached_type(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return normalizedTypeName switch
        {
            "bool" => bool_type,
            "i32" => i32_type,
            "i64" => i64_type,
            "f32" => f32_type,
            "f64" => f64_type,
            "isize" => isize_type,
            "usize" => usize_type,
            "Unit" or "unit" => unit_type,
            "Void" or "void" or "Never" or "never" => void_type,
            "any" => any_type,
            "null" => null_type,
            _ => null
        };
    }

    /// <summary>
    ///     判断语义类型是否匹配指定类型名。
    /// </summary>
    public static bool is_type(IType type, string typeName)
    {
        return string.Equals(
            ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(type.name),
            ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName),
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断名称是否为 `any` 特殊类型。
    /// </summary>
    public static bool is_any_type_name(string? typeName)
    {
        return string.Equals(
            ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName),
            any_name,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断名称是否为 `auto` 特殊类型。
    /// </summary>
    public static bool is_auto_type_name(string? typeName)
    {
        return string.Equals(
            ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName),
            auto_name,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断名称是否为 `Self` 特殊类型。
    /// </summary>
    public static bool is_self_type_name(string? typeName)
    {
        return string.Equals(typeName, self_type_name, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断名称是否为 `self` 特殊值。
    /// </summary>
    public static bool is_self_value_name(string? valueName)
    {
        return string.Equals(valueName, self_value_name, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断 kind tag 是否表示 tuple。
    /// </summary>
    public static bool is_tuple_kind_tag(string? kindTag)
    {
        return string.Equals(kindTag, tuple_kind_tag, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断 kind tag 是否表示 union。
    /// </summary>
    public static bool is_union_kind_tag(string? kindTag)
    {
        return string.Equals(kindTag, union_kind_tag, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断 kind tag 是否表示 intersection。
    /// </summary>
    public static bool is_intersection_kind_tag(string? kindTag)
    {
        return string.Equals(kindTag, intersection_kind_tag, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断 kind tag 是否表示 any。
    /// </summary>
    public static bool is_any_kind_tag(string? kindTag)
    {
        return string.Equals(kindTag, any_kind_tag, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断名称路径是否为 Valkyrie 数组类型。
    /// </summary>
    public static bool is_array_name_path(SemanticNamePath? namePath)
    {
        return namePath is not null &&
               (namePath.semantically_equals(array_name) || namePath.semantically_equals(std_array_name));
    }

    /// <summary>
    ///     判断名称路径是否为 Valkyrie 元组类型。
    /// </summary>
    public static bool is_tuple_name_path(SemanticNamePath? namePath)
    {
        return namePath is not null && namePath.semantically_equals(tuple_name);
    }

    /// <summary>
    ///     判断名称路径是否为 Valkyrie `unknown` 类型。
    /// </summary>
    public static bool is_unknown_name_path(SemanticNamePath? namePath)
    {
        return namePath is not null && namePath.semantically_equals(unknown_name_path);
    }

    /// <summary>
    ///     判断类型名是否为数值类型。
    /// </summary>
    public static bool is_numeric_type_name(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return normalizedTypeName is "i8" or "i16" or "i32" or "i64" or "isize" or
            "u8" or "u16" or "u32" or "u64" or "usize" or
            "f32" or "f64";
    }

    /// <summary>
    ///     判断类型名是否可参与显式数值转换。
    /// </summary>
    public static bool is_numeric_or_castable_type_name(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return is_numeric_type_name(normalizedTypeName)
               || normalizedTypeName is "bool" or "utf8" or "char";
    }

    /// <summary>
    ///     获取数值类型的优先级，非数值类型返回 -1。
    /// </summary>
    public static int get_numeric_rank(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return normalizedTypeName switch
        {
            "i8" => 0,
            "u8" => 1,
            "i16" => 2,
            "u16" => 3,
            "i32" => 4,
            "u32" => 5,
            "f32" => 6,
            "i64" => 7,
            "isize" => 8,
            "u64" => 9,
            "usize" => 10,
            "f64" => 11,
            _ => -1
        };
    }

    /// <summary>
    ///     判断类型名是否为需要阻止无符号到有符号隐式收窄的目标类型。
    /// </summary>
    public static bool is_signed_numeric_target_name(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return normalizedTypeName.StartsWith('i') || normalizedTypeName == "isize";
    }

    /// <summary>
    ///     判断类型名是否为无符号数值类型。
    /// </summary>
    public static bool is_unsigned_numeric_name(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return normalizedTypeName.StartsWith('u');
    }

    /// <summary>
    ///     判断类型名是否为匿名 row 形状。
    /// </summary>
    public static bool is_anonymous_row_name(string? typeName)
    {
        return !string.IsNullOrEmpty(typeName) && typeName.StartsWith('{');
    }

    /// <summary>
    ///     判断类型名是否为泛型占位符。
    /// </summary>
    public static bool is_generic_placeholder_name(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        return typeName.All(ch => char.IsUpper(ch) || char.IsDigit(ch) || ch == '_');
    }

    /// <summary>
    ///     判断类型名是否属于语言内建或保留类型。
    /// </summary>
    public static bool is_intrinsic_type_name(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return try_get_cached_type(normalizedTypeName) is not null
               || normalizedTypeName is "i8" or "i16" or "i128"
               or "u8" or "u16" or "u32" or "u64" or "u128"
               or "f16" or "f128"
               or "ascii"
               or "type" or "Type"
               || is_auto_type_name(normalizedTypeName)
               || ValkyrieTextTypeFacts.is_char_name(normalizedTypeName)
               || ValkyrieTextTypeFacts.is_owned_text_name(normalizedTypeName)
               || ValkyrieTextTypeFacts.is_owned_text_class_name(normalizedTypeName);
    }

    /// <summary>
    ///     请勿继续使用类型表示字符串判断数组。
    /// </summary>
    [Obsolete("请勿继续使用 `typeName string` 判断数组；请改用 `IType` / `HirTypeRef` 上的 `is_array_type`。", false)]
    public static bool is_array_type_name(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        return normalizedTypeName is "Array" or "std∷collection∷Array";
    }

    /// <summary>
    ///     判断语义类型是否为语言内建数组。
    /// </summary>
    public static bool is_array_type(IType? type)
    {
        if (type is null)
        {
            return false;
        }

        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(type.name);
        return normalizedTypeName is "Array" or "std∷collection∷Array";
    }

    /// <summary>
    ///     根据算术参与类型推断结果类型。
    /// </summary>
    public static IType infer_arithmetic_result_type(IType left, IType right, bool requireBothI32 = false)
    {
        if (is_type(left, "f64") || is_type(right, "f64"))
        {
            return f64_type;
        }

        if (is_type(left, "f32") || is_type(right, "f32"))
        {
            return f32_type;
        }

        if (is_type(left, "isize") || is_type(right, "isize"))
        {
            return isize_type;
        }

        if (is_type(left, "usize") || is_type(right, "usize"))
        {
            return usize_type;
        }

        if (is_type(left, "i64") || is_type(right, "i64"))
        {
            return i64_type;
        }

        if (requireBothI32)
        {
            return is_type(left, "i32") && is_type(right, "i32")
                ? i32_type
                : UnknownType.instance;
        }

        return is_type(left, "i32") || is_type(right, "i32")
            ? i32_type
            : UnknownType.instance;
    }

    /// <summary>
    ///     根据 literal 节点构造默认语义类型。
    /// </summary>
    public static IType create_literal_type(TermNode literal)
    {
        return literal switch
        {
            TermLiteralNumberNode numberLiteral =>
                ValkyrieNumberLiteralFacts.try_get_explicit_type_name(numberLiteral.value, out var typeName)
                    ? try_create_non_text_annotation_type(typeName) ??
                      (ValkyrieNumberLiteralFacts.is_floating_literal(numberLiteral.value) ? f32_type : i32_type)
                    : ValkyrieNumberLiteralFacts.is_floating_literal(numberLiteral.value) ? f32_type : i32_type,
            TermLiteralTextNode textLiteral => textLiteral.literal_kind switch
            {
                Std.Data.Text.Valkyrie.AST.TextLiteralKind.literal_char =>
                    ValkyrieTextTypeFacts.create_semantic_literal_char_type(),
                _ => ValkyrieTextTypeFacts.create_semantic_literal_text_type()
            },
            TermLiteralBooleanNode => bool_type,
            LiteralNullNode => null_type,
            _ => UnknownType.instance
        };
    }

    /// <summary>
    ///     根据类型标注名称创建非文本内建类型对象。
    /// </summary>
    public static IType? try_create_non_text_annotation_type(string? typeName)
    {
        var normalizedTypeName = ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
        if (try_get_cached_type(normalizedTypeName) is { } cachedType && cachedType != null_type)
        {
            return cachedType;
        }

        return normalizedTypeName switch
        {
            "i8" or "i16" or "i32" or "i64" or "i128" or "isize" or
                "u8" or "u16" or "u32" or "u64" or "u128" or "usize" or
                "f32" or "f64" or "f128" or "bool" => new PrimitiveType(normalizedTypeName),
            "char" => ValkyrieTextTypeFacts.create_semantic_char_type(),
            _ when is_auto_type_name(normalizedTypeName) => AutoType.instance,
            "ExitCode" => new NamedType("ExitCode", "ExitCode"),
            _ => null
        };
    }

    /// <summary>
    ///     根据类型标注名称创建内建或文本类型对象。
    /// </summary>
    public static IType? try_create_annotation_type(string? typeName)
    {
        return try_create_non_text_annotation_type(typeName)
               ?? ValkyrieTextTypeFacts.try_create_semantic_text_type(typeName);
    }

    /// <summary>
    ///     根据标识符名称创建表达式层可直接识别的类型对象。
    /// </summary>
    public static IType? try_create_identifier_type(string? typeName)
    {
        return try_create_annotation_type(typeName) ?? typeName switch
        {
            "true" or "false" => bool_type,
            "null" => null_type,
            _ => null
        };
    }

    /// <summary>
    ///     构造 `any` 类型节点。
    /// </summary>
    public static TypeLiteralNamePathNode create_any_type_node()
    {
        return new TypeLiteralNamePathNode
        {
            path = new QualifiedPathNode
            {
                segments = [new IdentifierNode(any_name)]
            }
        };
    }

    /// <summary>
    ///     判断返回类型标注是否为 `auto`。
    /// </summary>
    public static bool is_auto_return_type(TypeNode? typeNode)
    {
        return typeNode is TypeLiteralNamePathNode literal && is_auto_type_name(literal.path.full_name);
    }

    /// <summary>
    ///     判断函数类型参数列表是否表示零参数。
    /// </summary>
    public static bool is_empty_function_parameter_list(TypeNode node)
    {
        return node is TypeLiteralNamePathNode literal
               && ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(literal.path.full_name) is "unit" or "void";
    }
}
