using Nyar.Analyzer.Semantic;
using Std.DataProcess.Serialize;

namespace Nyar.Language.Von.Semantic;

/// <summary>
///     将中性的 `SerdeValue` 字面量树解释为 `VON` 自身的 literal 语义。
/// </summary>
public static class VonLiteralSemantics
{
    public static IType from_serde_literal(SerdeValue value)
    {
        return value.type switch
        {
            SerdeValueType.boolean => VonLiteralType.boolean,
            SerdeValueType.integer => VonLiteralType.number,
            SerdeValueType.@decimal => VonLiteralType.number,
            SerdeValueType.@string => VonLiteralType.text,
            SerdeValueType.array => create_array_literal_type(value),
            SerdeValueType.@object => create_object_literal_type(value),
            SerdeValueType.@null => VonLiteralType.@null,
            _ => UnknownType.instance
        };
    }

    /// <summary>
    ///     为数组字面量推导独立的 `VON` 字面量数组类型。
    /// </summary>
    private static IType create_array_literal_type(SerdeValue value)
    {
        var elementType = merge_child_literal_types(value.elements?.Select(from_serde_literal) ?? []);
        return VonLiteralType.create_array(elementType);
    }

    /// <summary>
    ///     为对象字面量推导独立的 `VON` 字面量对象类型。
    ///     `VON` 对象键固定为文本字面量，值类型按字段值做保守合并。
    /// </summary>
    private static IType create_object_literal_type(SerdeValue value)
    {
        var mergedValueType = merge_child_literal_types(value.fields?.Values.Select(from_serde_literal) ?? []);
        return VonLiteralType.create_object(VonLiteralType.text, mergedValueType);
    }

    /// <summary>
    ///     合并同层字面量子类型；不一致时保守回退为未知类型。
    /// </summary>
    private static IType merge_child_literal_types(IEnumerable<IType> types)
    {
        IType mergedType = UnknownType.instance;
        foreach (var type in types)
        {
            if (mergedType is UnknownType)
            {
                mergedType = type;
                continue;
            }

            if (!mergedType.equals(type))
            {
                return UnknownType.instance;
            }
        }

        return mergedType;
    }
}
