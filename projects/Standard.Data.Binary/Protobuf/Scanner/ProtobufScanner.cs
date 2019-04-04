using System.Text;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Protobuf.Scanner;

/// <summary>
///     Protobuf 格式扫描器，基于 <see cref="SpanScanner" /> 提供零分配的快的Protobuf 消息结构探查的
/// </summary>
/// <remarks>
///     Protobuf 使用变长编码（Varint）和标签-值对（Tag-Length-Value）格式组织数据的
///     扫描器逐字段解析消息结构，提取字段编号、线型和值摘要的
/// </remarks>
public ref struct ProtobufScanner
{
    private SpanScanner _scanner;

    public ProtobufScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 Protobuf 消息，提取所有顶层字段信息的
    /// </summary>
    /// <returns>字段信息列表的/returns>
    public List<ProtobufFieldInfo> scan_fields()
    {
        var fields = new List<ProtobufFieldInfo>();

        while (!_scanner.is_end)
        {
            if (!try_read_tag(out var fieldNumber, out var wireType)) break;

            var field = new ProtobufFieldInfo
            {
                field_number = fieldNumber,
                wire_type = wireType
            };

            switch (wireType)
            {
                case 0:
                    field = field with
                    {
                        value_type = ProtobufValueType.varint, varint_value = read_varint()
                    };
                    break;
                case 1:
                    field = field with
                    {
                        value_type = ProtobufValueType.fixed64, fixed64_value = _scanner.buffer.read_u64_le()
                    };
                    break;
                case 2:
                    field = field with
                    {
                        value_type = ProtobufValueType.length_delimited,
                        length_delimited_value = [.. _scanner.buffer.read_bytes((int)read_varint())]
                    };
                    break;
                case 5:
                    field = field with
                    {
                        value_type = ProtobufValueType.fixed32, fixed32_value = _scanner.buffer.read_u32_le()
                    };
                    break;
                default:
                    return fields;
            }

            fields.Add(field);
        }

        return fields;
    }

    /// <summary>
    ///     扫描 Protobuf 消息结构，返回文本描述的
    /// </summary>
    /// <returns>消息结构描述的/returns>
    public string scan_structure()
    {
        var result = new StringBuilder();
        result.AppendLine("Protobuf Message Structure:");
        result.AppendLine("==========================");

        var fields = scan_fields();

        foreach (var field in fields)
        {
            var wireTypeName = field.wire_type switch
            {
                0 => "Varint",
                1 => "64-bit",
                2 => "Length-delimited",
                5 => "32-bit",
                _ => $"Unknown({field.wire_type})"
            };

            var valueDesc = field.value_type switch
            {
                ProtobufValueType.varint => field.varint_value.ToString(),
                ProtobufValueType.fixed64 => $"0x{field.fixed64_value:X16}",
                ProtobufValueType.fixed32 => $"0x{field.fixed32_value:X8}",
                ProtobufValueType.length_delimited => try_decode_string(field.length_delimited_value, out var str)
                    ? $"\"{str}\""
                    : $"[{field.length_delimited_value.Length} bytes]",
                _ => "unknown"
            };

            result.AppendLine($"  Field {field.field_number} ({wireTypeName}): {valueDesc}");
        }

        return result.ToString();
    }

    private bool try_read_tag(out int fieldNumber, out int wireType)
    {
        wireType = 0;
        fieldNumber = 0;

        if (_scanner.is_end) return false;

        var tag = read_varint();

        if (tag == 0) return false;

        wireType = (int)(tag & 0x7);
        fieldNumber = (int)(tag >> 3);
        return true;
    }

    private ulong read_varint()
    {
        return _scanner.buffer.read_leb128_u64();
    }

    private static bool try_decode_string(byte[] bytes, out string value)
    {
        value = string.Empty;

        try
        {
            var decoded = Encoding.UTF8.GetString(bytes);

            foreach (var c in decoded)
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                    return false;

            value = decoded;
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
///     Protobuf 字段信息的
/// </summary>
public sealed record ProtobufFieldInfo
{
    /// <summary>
    ///     字段编号的
    /// </summary>
    public int field_number { get; init; }

    /// <summary>
    ///     线型的
    /// </summary>
    public int wire_type { get; init; }

    /// <summary>
    ///     值类型的
    /// </summary>
    public ProtobufValueType value_type { get; init; }

    /// <summary>
    ///     Varint 值（的<see cref="value_type" /> 的<see cref="ProtobufValueType.varint" /> 时有效）的
    /// </summary>
    public ulong varint_value { get; init; }

    /// <summary>
    ///     32 位固定值（的<see cref="value_type" /> 的<see cref="ProtobufValueType.fixed32" /> 时有效）的
    /// </summary>
    public uint fixed32_value { get; init; }

    /// <summary>
    ///     64 位固定值（的<see cref="value_type" /> 的<see cref="ProtobufValueType.fixed64" /> 时有效）的
    /// </summary>
    public ulong fixed64_value { get; init; }

    /// <summary>
    ///     变长值（的<see cref="value_type" /> 的<see cref="ProtobufValueType.length_delimited" /> 时有效）的
    /// </summary>
    public byte[] length_delimited_value { get; init; } = [];
}

/// <summary>
///     Protobuf 值类型的
/// </summary>
public enum ProtobufValueType
{
    /// <summary>
    ///     变长整数的
    /// </summary>
    varint,

    /// <summary>
    ///     32 位固定值的
    /// </summary>
    fixed32,

    /// <summary>
    ///     64 位固定值的
    /// </summary>
    fixed64,

    /// <summary>
    ///     变长数据的
    /// </summary>
    length_delimited
}