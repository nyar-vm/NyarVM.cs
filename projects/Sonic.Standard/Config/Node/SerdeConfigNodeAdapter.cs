using Std.DataProcess.Serialize;

namespace Std.Config.Node;

/// <summary>
///     在 `SerdeValue` 与 Sonic 配置树之间做桥接。
///     配置模型归属 Sonic 层，语言与业务层只消费 `ConfigNode`。
/// </summary>
public static class SerdeConfigNodeAdapter
{
    public static ConfigNode from_serde(SerdeValue value)
    {
        return value.type switch
        {
            SerdeValueType.@null => NullConfigNode.instance,
            SerdeValueType.boolean => new ScalarConfigNode(value.get_boolean() ? "true" : "false"),
            SerdeValueType.integer => new ScalarConfigNode(value.get_integer_string() ?? "0"),
            SerdeValueType.@decimal => new ScalarConfigNode(value.get_decimal_string() ?? "0.0"),
            SerdeValueType.@string => new ScalarConfigNode(value.get_string() ?? string.Empty),
            SerdeValueType.array => new ArrayConfigNode([.. (value.elements ?? []).Select(from_serde)]),
            SerdeValueType.@object => new ObjectConfigNode(
                (value.fields ?? new Dictionary<string, SerdeValue>(StringComparer.Ordinal))
                .ToDictionary(kv => kv.Key, kv => from_serde(kv.Value), StringComparer.Ordinal)),
            _ => NullConfigNode.instance
        };
    }
}
