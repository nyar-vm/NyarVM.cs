using System.Globalization;
using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.TypeSystem;
using Std.DataProcess.Serialize;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     将中性的 `SerdeValue` 字面量与 `Valkyrie` 期望类型绑定为 `ValkyrieValue`。
///     绑定规则遵循 `literal + type info => value`，不让 `Serde` 自己决定最终值语义。
///     这里处理的是 `data model` 绑定；配置层的 merge/overlay 语义应由独立的 config model 承载。
/// </summary>
public static class ValkyrieValueBinder
{
    public static ValkyrieValue bind_serde_literal(SerdeValue literal, IType expectedType)
    {
        if (expectedType is NullableType nullableType)
        {
            if (literal.type == SerdeValueType.@null)
            {
                return new ValkyrieNullValue(expectedType);
            }

            return bind_serde_literal(literal, nullableType.inner_type);
        }

        ensure_supported_expected_type(expectedType);

        if (literal.type == SerdeValueType.@null)
        {
            return new ValkyrieNullValue(expectedType);
        }

        return literal.type switch
        {
            SerdeValueType.boolean => bind_boolean(literal, expectedType),
            SerdeValueType.integer => bind_integer(literal, expectedType),
            SerdeValueType.@decimal => bind_decimal(literal, expectedType),
            SerdeValueType.@string => bind_text(literal, expectedType),
            SerdeValueType.array => bind_array(literal, expectedType),
            SerdeValueType.@object => bind_object(literal, expectedType),
            _ => throw unsupported_binding(literal.type, expectedType)
        };
    }

    private static void ensure_supported_expected_type(IType expectedType)
    {
        if (expectedType is UnknownType || expectedType is AutoType)
        {
            throw new InvalidOperationException(
                "Valkyrie value 绑定要求显式期望类型；请先完成类型绑定，再把 literal 收敛为 value。");
        }

        if (expectedType.name is "any")
        {
            throw new InvalidOperationException(
                "Valkyrie value 绑定不接受宽泛的 `any`；请先将 literal 约束到明确的 `Valkyrie` 类型。");
        }
    }

    private static ValkyrieValue bind_boolean(SerdeValue literal, IType expectedType)
    {
        if (expectedType.name != "bool")
        {
            throw unsupported_binding(literal.type, expectedType);
        }

        return new ValkyrieBoolValue(expectedType, literal.get_boolean());
    }

    private static ValkyrieValue bind_integer(SerdeValue literal, IType expectedType)
    {
        var raw = literal.get_integer_string()
                  ?? throw new InvalidOperationException("Serde integer literal 缺少原始整数字符串。");

        return normalize_expected_type_name(expectedType) switch
        {
            "i8" or "i16" or "i32" or "i64" or "isize" =>
                new ValkyrieSignedIntegerValue(expectedType, long.Parse(raw, CultureInfo.InvariantCulture)),
            "u8" or "u16" or "u32" or "u64" or "usize" =>
                new ValkyrieUnsignedIntegerValue(expectedType, ulong.Parse(raw, CultureInfo.InvariantCulture)),
            "f32" or "f64" =>
                new ValkyrieFloatValue(expectedType, double.Parse(raw, CultureInfo.InvariantCulture)),
            _ => throw unsupported_binding(literal.type, expectedType)
        };
    }

    private static ValkyrieValue bind_decimal(SerdeValue literal, IType expectedType)
    {
        var raw = literal.get_decimal_string()
                  ?? throw new InvalidOperationException("Serde decimal literal 缺少原始小数字符串。");

        return normalize_expected_type_name(expectedType) switch
        {
            "f32" or "f64" =>
                new ValkyrieFloatValue(expectedType, double.Parse(raw, CultureInfo.InvariantCulture)),
            _ => throw unsupported_binding(literal.type, expectedType)
        };
    }

    private static ValkyrieValue bind_text(SerdeValue literal, IType expectedType)
    {
        var raw = literal.get_string()
                  ?? throw new InvalidOperationException("Serde string literal 缺少原始文本。");
        var normalizedTypeName = normalize_expected_type_name(expectedType);

        if (ValkyrieTextTypeFacts.is_char_name(normalizedTypeName))
        {
            if (raw.Length != 1)
            {
                throw new InvalidOperationException(
                    $"无法将长度为 {raw.Length} 的文本 literal 绑定为 `char`。");
            }

            return new ValkyrieCharValue(expectedType, raw[0]);
        }

        if (ValkyrieTextTypeFacts.is_owned_text_name(normalizedTypeName))
        {
            return new ValkyrieTextValue(expectedType, raw);
        }

        throw unsupported_binding(literal.type, expectedType);
    }

    private static ValkyrieValue bind_array(SerdeValue literal, IType expectedType)
    {
        if (expectedType is not NamedType namedType
            || namedType.name is not "Array" and not "FixedArray"
            || namedType.type_arguments.Count == 0)
        {
            throw unsupported_binding(literal.type, expectedType);
        }

        var elementType = namedType.type_arguments[0];
        var elements = (literal.elements ?? [])
            .Select(element => bind_serde_literal(element, elementType))
            .ToArray();
        return new ValkyrieArrayValue(expectedType, elements);
    }

    private static ValkyrieValue bind_object(SerdeValue literal, IType expectedType)
    {
        if (try_bind_map_object(literal, expectedType) is { } mapValue)
        {
            return mapValue;
        }

        if (try_bind_structured_object(literal, expectedType) is { } objectValue)
        {
            return objectValue;
        }

        throw new InvalidOperationException(
            $"当前只支持将对象 literal 绑定为 `map<K, V>` 或带显式成员的结构化对象；实际期望类型为 `{expectedType.name}`。");
    }

    private static ValkyrieMapValue? try_bind_map_object(SerdeValue literal, IType expectedType)
    {
        if (expectedType is not NamedType namedType
            || namedType.name != "map"
            || namedType.type_arguments.Count != 2)
        {
            return null;
        }

        var keyType = namedType.type_arguments[0];
        var valueType = namedType.type_arguments[1];
        var entries = (literal.fields ?? [])
            .Select(field => new ValkyrieMapEntry(
                bind_text(SerdeValue.@string(field.Key), keyType),
                bind_serde_literal(field.Value, valueType)))
            .ToArray();
        return new ValkyrieMapValue(expectedType, entries);
    }

    private static ValkyrieObjectValue? try_bind_structured_object(SerdeValue literal, IType expectedType)
    {
        var bindableMembers = get_bindable_object_members(expectedType);
        if (bindableMembers.Count == 0)
        {
            return null;
        }

        var fields = literal.fields ?? [];
        var boundFields = new List<ValkyrieObjectField>(bindableMembers.Count);
        foreach (var member in bindableMembers)
        {
            if (!fields.TryGetValue(member.name, out var fieldLiteral))
            {
                throw new InvalidOperationException(
                    $"对象 literal 缺少结构化绑定所需字段 `{member.name}`，目标类型为 `{expectedType.name}`。");
            }

            var fieldType = member.type
                            ?? throw new InvalidOperationException(
                                $"目标类型 `{expectedType.name}` 的成员 `{member.name}` 缺少字段类型，无法绑定对象 literal。");
            boundFields.Add(new ValkyrieObjectField(
                member.name,
                fieldType,
                bind_serde_literal(fieldLiteral, fieldType),
                member));
        }

        var knownMembers = new HashSet<string>(bindableMembers.Select(member => member.name), StringComparer.Ordinal);
        var unknownFields = fields.Keys.Where(fieldName => !knownMembers.Contains(fieldName)).ToArray();
        if (unknownFields.Length > 0)
        {
            throw new InvalidOperationException(
                $"对象 literal 包含目标类型 `{expectedType.name}` 未声明的字段：{string.Join(", ", unknownFields)}。");
        }

        return new ValkyrieObjectValue(expectedType, boundFields);
    }

    private static IReadOnlyList<ISymbol> get_bindable_object_members(IType expectedType)
    {
        var members = new Dictionary<string, ISymbol>(StringComparer.Ordinal);
        collect_bindable_object_members(expectedType, members);
        return [.. members.Values];
    }

    private static void collect_bindable_object_members(IType currentType, Dictionary<string, ISymbol> target)
    {
        if (currentType.base_type is { } baseType)
        {
            collect_bindable_object_members(baseType, target);
        }

        foreach (var member in currentType.members)
        {
            if (member.type is null)
            {
                continue;
            }

            if (member.kind is not SymbolKind.property and not SymbolKind.variable)
            {
                continue;
            }

            target[member.name] = member;
        }
    }

    private static string normalize_expected_type_name(IType expectedType)
    {
        return ValkyrieTextTypeFacts.canonicalize_post_literal_type_name(expectedType.name);
    }

    private static InvalidOperationException unsupported_binding(SerdeValueType literalType, IType expectedType)
    {
        return new InvalidOperationException(
            $"无法将 `{literalType}` literal 绑定为 `Valkyrie` 值类型 `{expectedType.name}`。");
    }
}
