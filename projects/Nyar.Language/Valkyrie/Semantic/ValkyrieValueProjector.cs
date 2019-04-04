using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.TypeSystem;
using Nyar.Language.Von;
using Std.DataProcess.Serialize;

namespace Nyar.Language.Valkyrie.Semantic;

/// <summary>
///     将 `SerdeValue`/`VonValue` 经 `ValkyrieValueBinder` 绑定后，再投影为宿主配置层常用标量。
///     这样配置消费者不再直接把中性 literal tree 当成最终值模型来取字段。
/// </summary>
public static class ValkyrieValueProjector
{
    public static readonly IType bool_type = ValkyrieBuiltinTypeFacts.bool_type;
    public static readonly IType i32_type = ValkyrieBuiltinTypeFacts.i32_type;
    public static readonly IType utf8_type =
        ValkyrieTextTypeFacts.create_semantic_owned_text_type(ValkyrieTextTypeFacts.utf8_name);
    public static readonly IType utf8_array_type = new NamedType("Array", "Array", typeArguments: [utf8_type]);
    public static readonly IType utf8_map_type = new NamedType("map", "map", typeArguments: [utf8_type, utf8_type]);

    public static ValkyrieValue? bind_field(SerdeValue owner, string fieldName, IType expectedType)
    {
        return owner.get_field(fieldName) is { } field ? bind_literal(field, expectedType) : null;
    }

    public static ValkyrieValue? bind_field(VonValue owner, string fieldName, IType expectedType)
    {
        return owner.get_field(fieldName) is { } field ? bind_literal(field.inner, expectedType) : null;
    }

    public static ValkyrieValue bind_literal(SerdeValue literal, IType expectedType)
    {
        return ValkyrieValueBinder.bind_serde_literal(literal, expectedType);
    }

    public static bool? bind_bool_field(SerdeValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, bool_type) is { } value ? as_bool(value) : null;
    }

    public static bool? bind_bool_field(VonValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, bool_type) is { } value ? as_bool(value) : null;
    }

    public static int? bind_i32_field(SerdeValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, i32_type) is { } value ? as_i32(value) : null;
    }

    public static int? bind_i32_field(VonValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, i32_type) is { } value ? as_i32(value) : null;
    }

    public static string? bind_utf8_field(SerdeValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, utf8_type) is { } value ? as_utf8(value) : null;
    }

    public static string? bind_utf8_field(VonValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, utf8_type) is { } value ? as_utf8(value) : null;
    }

    public static IReadOnlyList<string>? bind_utf8_array_field(SerdeValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, utf8_array_type) is { } value ? as_utf8_array(value) : null;
    }

    public static IReadOnlyList<string>? bind_utf8_array_field(VonValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, utf8_array_type) is { } value ? as_utf8_array(value) : null;
    }

    public static IReadOnlyDictionary<string, string>? bind_utf8_map_field(SerdeValue owner, string fieldName)
    {
        return bind_field(owner, fieldName, utf8_map_type) is { } value ? as_utf8_map(value) : null;
    }

    public static bool? as_bool(ValkyrieValue value)
    {
        return value is ValkyrieBoolValue boolean ? boolean.value : null;
    }

    public static int? as_i32(ValkyrieValue value)
    {
        return value switch
        {
            ValkyrieSignedIntegerValue { value: >= int.MinValue and <= int.MaxValue } signed =>
                (int)signed.value,
            ValkyrieUnsignedIntegerValue { value: <= int.MaxValue } unsigned =>
                (int)unsigned.value,
            _ => null
        };
    }

    public static string? as_utf8(ValkyrieValue value)
    {
        return value is ValkyrieTextValue text ? text.value : null;
    }

    public static IReadOnlyList<string>? as_utf8_array(ValkyrieValue value)
    {
        if (value is not ValkyrieArrayValue arrayValue)
        {
            return null;
        }

        var items = new List<string>(arrayValue.elements.Count);
        foreach (var element in arrayValue.elements)
        {
            if (element is not ValkyrieTextValue text)
            {
                return null;
            }

            items.Add(text.value);
        }

        return items;
    }

    public static IReadOnlyDictionary<string, string>? as_utf8_map(ValkyrieValue value)
    {
        if (value is not ValkyrieMapValue mapValue)
        {
            return null;
        }

        var result = new Dictionary<string, string>(mapValue.entries.Count, StringComparer.Ordinal);
        foreach (var entry in mapValue.entries)
        {
            if (entry.key is not ValkyrieTextValue key || entry.value is not ValkyrieTextValue text)
            {
                return null;
            }

            result[key.value] = text.value;
        }

        return result;
    }
}
