using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Language.Valkyrie.TypeSystem;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Ref;

/// <summary>
///     HIR 类型引用。
/// </summary>
public sealed record HirTypeRef(
    ValkyrieNamePath name,
    HirSpecialTypeKind special_kind = HirSpecialTypeKind.none,
    IReadOnlyList<HirTypeRef>? type_arguments = null,
    IReadOnlyList<HirTypeArgumentBinding>? named_type_arguments = null,
    IReadOnlyList<HirTupleElementRef>? tuple_elements = null)
{
    /// <summary>
    ///     类型的 Valkyrie 名称路径。
    /// </summary>
    public ValkyrieNamePath name_path => name;

    /// <summary>
    ///     类型的短名，即名称路径的最后一段。
    /// </summary>
    public string short_name => name.name;

    /// <summary>
    ///     类型所属的 Valkyrie 命名空间。
    /// </summary>
    public ValkyrieNameSpace namespace_path => name.@namespace as ValkyrieNameSpace ?? new ValkyrieNameSpace(name.@namespace.parts);

    /// <summary>
    ///     仅用于打印的名称文本。
    /// </summary>
    public string canonical_name => name.ToString();

    /// <summary>
    ///     判断当前类型是否为数组类型。
    /// </summary>
    public bool is_array_type => ValkyrieBuiltinTypeFacts.is_array_name_path(name_path);

    /// <summary>
    ///     判断当前类型是否为元组类型。
    /// </summary>
    public bool is_tuple_type => ValkyrieBuiltinTypeFacts.is_tuple_name_path(name_path);

    /// <summary>
    ///     判断当前类型是否为 `unknown` 类型。
    /// </summary>
    public bool is_unknown_type => ValkyrieBuiltinTypeFacts.is_unknown_name_path(name_path);

    /// <summary>
    ///     是否为特殊类型。
    /// </summary>
    public bool is_special => special_kind != HirSpecialTypeKind.none;

    /// <summary>
    ///     包含泛型参数的完整显示名称，例如 `Option<i32>`。
    /// </summary>
    public string display_name
    {
        get
        {
            if (tuple_elements is { Count: >= 0 })
            {
                if (tuple_elements.Count == 0)
                {
                    return "()";
                }

                var elements = tuple_elements.Select(element =>
                    string.IsNullOrWhiteSpace(element.label)
                        ? element.type.display_name
                        : $"{element.label}: {element.type.display_name}");
                var rendered = string.Join(", ", elements);
                if (tuple_elements.Count == 1)
                {
                    rendered += ",";
                }

                return $"({rendered})";
            }

            var parts = new List<string>();
            if (type_arguments is not null && type_arguments.Count > 0)
            {
                parts.AddRange(type_arguments.Select(argument => argument.display_name));
            }

            if (named_type_arguments is not null && named_type_arguments.Count > 0)
            {
                parts.AddRange(named_type_arguments.Select(argument => $"{argument.slot_name} = {argument.type.display_name}"));
            }

            if (parts.Count == 0)
            {
                return name.ToString();
            }

            var args = string.Join(", ", parts);
            return $"{name}<{args}>";
        }
    }

    /// <summary>
    ///     使用名称路径拼接限定名。
    /// </summary>
    public static ValkyrieNamePath qualify_name_path(
        ValkyrieNameSpace? currentNamespace,
        ValkyrieNamePath? ownerName,
        string? memberName)
    {
        var parts = new List<string>();
        if (currentNamespace is not null)
        {
            parts.AddRange(currentNamespace.parts);
        }

        if (ownerName is not null)
        {
            parts.AddRange(ownerName.parts);
        }

        var memberPath = ValkyrieNamePath.parse(memberName);
        if (!memberPath.is_empty)
        {
            parts.AddRange(memberPath.parts);
        }

        return new ValkyrieNamePath(parts);
    }

    /// <summary>
    ///     使用名称路径判断候选类型名是否匹配查询名称。
    /// </summary>
    public static bool matches_name_path(
        ValkyrieNamePath simpleName,
        ValkyrieNamePath qualifiedName,
        ValkyrieNamePath query,
        ValkyrieNameSpace? currentNamespace = null)
    {
        return ValkyrieNamePath.matches(simpleName, qualifiedName, query, currentNamespace);
    }

    /// <summary>
    ///     语义相等比较，同时比较类型名称和泛型参数。
    /// </summary>
    public bool semantically_equals(HirTypeRef? other)
    {
        if (other is null)
        {
            return false;
        }

        if (special_kind != HirSpecialTypeKind.none || other.special_kind != HirSpecialTypeKind.none)
        {
            return special_kind == other.special_kind;
        }

        if (tuple_elements is null != other.tuple_elements is null)
        {
            return false;
        }

        if (tuple_elements is not null)
        {
            if (other.tuple_elements is null || tuple_elements.Count != other.tuple_elements.Count)
            {
                return false;
            }

            for (var i = 0; i < tuple_elements.Count; i++)
            {
                var left = tuple_elements[i];
                var right = other.tuple_elements[i];
                if (!string.Equals(left.label, right.label, StringComparison.Ordinal) ||
                    !left.type.semantically_equals(right.type))
                {
                    return false;
                }
            }

            return true;
        }

        if (!name_path.semantically_equals(other.name_path))
        {
            return false;
        }

        if (type_arguments is null && other.type_arguments is null &&
            named_type_arguments is null && other.named_type_arguments is null)
        {
            return true;
        }

        if (type_arguments is null != other.type_arguments is null)
        {
            return false;
        }

        if (named_type_arguments is null != other.named_type_arguments is null)
        {
            return false;
        }

        if (type_arguments is not null)
        {
            if (other.type_arguments is null || type_arguments.Count != other.type_arguments.Count)
            {
                return false;
            }

            for (var i = 0; i < type_arguments.Count; i++)
            {
                if (!type_arguments[i].semantically_equals(other.type_arguments[i]))
                {
                    return false;
                }
            }
        }

        if (named_type_arguments is not null)
        {
            if (other.named_type_arguments is null ||
                named_type_arguments.Count != other.named_type_arguments.Count)
            {
                return false;
            }

            for (var i = 0; i < named_type_arguments.Count; i++)
            {
                var left = named_type_arguments[i];
                var right = other.named_type_arguments[i];
                if (!string.Equals(left.slot_name, right.slot_name, StringComparison.Ordinal) ||
                    !left.type.semantically_equals(right.type))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     创建具名类型引用，可附带泛型参数。
    /// </summary>
    public static HirTypeRef named(ValkyrieNamePath name, IReadOnlyList<HirTypeRef>? typeArguments = null)
    {
        return new HirTypeRef(name, HirSpecialTypeKind.none, typeArguments);
    }

    /// <summary>
    ///     根据 Valkyrie 名称文本创建具名类型引用。
    /// </summary>
    public static HirTypeRef named(string? name, IReadOnlyList<HirTypeRef>? typeArguments = null)
    {
        return named(ValkyrieNamePath.parse(name), typeArguments);
    }

    /// <summary>
    ///     根据结构化名称路径创建具名类型引用。
    /// </summary>
    public static HirTypeRef from_name_path(ValkyrieNamePath namePath, IReadOnlyList<HirTypeRef>? typeArguments = null)
    {
        return named(namePath, typeArguments);
    }

    /// <summary>
    ///     创建 `unknown` 类型引用。
    /// </summary>
    public static HirTypeRef unknown()
    {
        return named("unknown");
    }

    /// <summary>
    ///     创建 `bool` 类型引用。
    /// </summary>
    public static HirTypeRef @bool()
    {
        return named("bool");
    }

    /// <summary>
    ///     创建 `i32` 类型引用。
    /// </summary>
    public static HirTypeRef i32()
    {
        return named("i32");
    }

    /// <summary>
    ///     创建 `null` 类型引用。
    /// </summary>
    public static HirTypeRef @null()
    {
        return named("null");
    }

    /// <summary>
    ///     创建元组类型引用。
    /// </summary>
    public static HirTypeRef tuple(IReadOnlyList<HirTypeRef> elementTypes, IReadOnlyList<string?>? elementLabels = null)
    {
        var tupleElements = new List<HirTupleElementRef>(elementTypes.Count);
        for (var i = 0; i < elementTypes.Count; i++)
        {
            var label = elementLabels is not null && i < elementLabels.Count ? elementLabels[i] : null;
            tupleElements.Add(new HirTupleElementRef(label, elementTypes[i]));
        }

        return new HirTypeRef(ValkyrieNamePath.parse("tuple"), HirSpecialTypeKind.none, elementTypes, null, tupleElements);
    }

    /// <summary>
    ///     尝试将元组标签映射为运行时字段名。
    /// </summary>
    public bool try_get_tuple_runtime_field_name(string fieldName, out string runtimeFieldName)
    {
        if (tuple_elements is not null)
        {
            for (var i = 0; i < tuple_elements.Count; i++)
            {
                if (string.Equals(tuple_elements[i].label, fieldName, StringComparison.Ordinal))
                {
                    runtimeFieldName = $"_{i}";
                    return true;
                }
            }
        }

        runtimeFieldName = fieldName;
        return false;
    }

    /// <summary>
    ///     返回一个带有指定泛型参数的新类型引用，保留当前名称和特殊类型种类。
    /// </summary>
    public HirTypeRef with_type_arguments(IReadOnlyList<HirTypeRef> typeArgs)
    {
        return new HirTypeRef(name, special_kind, typeArgs, named_type_arguments, tuple_elements);
    }

    /// <summary>
    ///     返回一个带有指定命名类型实参的新类型引用。
    /// </summary>
    public HirTypeRef with_named_type_arguments(IReadOnlyList<HirTypeArgumentBinding> namedTypeArgs)
    {
        return new HirTypeRef(name, special_kind, type_arguments, namedTypeArgs, tuple_elements);
    }

    /// <summary>
    ///     返回一个带有指定元组元素信息的新类型引用。
    /// </summary>
    public HirTypeRef with_tuple_elements(IReadOnlyList<HirTupleElementRef> tupleElements)
    {
        return new HirTypeRef(name, special_kind, type_arguments, named_type_arguments, tupleElements);
    }

    /// <summary>
    ///     从语义类型构建 HIR 类型引用。
    /// </summary>
    public static HirTypeRef from_type(IType? type, string? fallbackName = null)
    {
        return type switch
        {
            PrimitiveType { name: "unit" } => unit(),
            PrimitiveType { name: "void" } => @void(),
            AutoType => auto(),
            NamedType { name: "ExitCode" } => exit_code(),
            NamedType namedType when namedType.is_tuple_type => tuple(
                [
                    .. namedType.type_arguments.Select(argument => from_type(argument, argument.name))
                ],
                namedType.members.Count == namedType.type_arguments.Count
                    ? namedType.members.Select(member => (string?)member.name).ToArray()
                    : null),
            NamedType namedType when ValkyrieTextTypeFacts.is_pre_hir_literal_name(namedType.name) =>
                named(ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(fallbackName ?? namedType.name)),
            PrimitiveType primitiveType =>
                named(normalize_hir_type_name(fallbackName ?? primitiveType.name)),
            NamedType namedType =>
                named(
                    normalize_hir_type_name(fallbackName ?? namedType.name),
                    namedType.type_arguments.Count == 0
                        ? null
                        : namedType.type_arguments.Select(argument => from_type(argument, argument.name)).ToArray()),
            _ => named(normalize_hir_type_name(fallbackName ?? type?.name ?? "unknown"))
        };
    }

    /// <summary>
    ///     归一化 HIR 类型名称。
    /// </summary>
    private static string normalize_hir_type_name(string typeName)
    {
        ValkyrieTextTypeFacts.ensure_no_legacy_text_ref_type(typeName, "HIR");
        return ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(typeName);
    }

    /// <summary>
    ///     创建 `unit` 特殊类型引用。
    /// </summary>
    public static HirTypeRef unit()
    {
        return new HirTypeRef(ValkyrieNamePath.parse("unit"), HirSpecialTypeKind.unit);
    }

    /// <summary>
    ///     创建 `void` 特殊类型引用。
    /// </summary>
    public static HirTypeRef @void()
    {
        return new HirTypeRef(ValkyrieNamePath.parse("void"), HirSpecialTypeKind.@void);
    }

    /// <summary>
    ///     创建 `auto` 特殊类型引用。
    /// </summary>
    public static HirTypeRef auto()
    {
        return new HirTypeRef(ValkyrieNamePath.parse("auto"), HirSpecialTypeKind.auto);
    }

    /// <summary>
    ///     创建 `ExitCode` 特殊类型引用。
    /// </summary>
    public static HirTypeRef exit_code()
    {
        return new HirTypeRef(ValkyrieNamePath.parse("ExitCode"), HirSpecialTypeKind.exit_code);
    }
}
