namespace Std.DL.Data;

/// <summary>
///     单个字段的定义
/// </summary>
public sealed class FieldDef
{
    /// <summary>字段名</summary>
    public string Name { get; init; } = "";

    /// <summary>字段类型编码</summary>
    public byte TypeCode { get; init; }

    /// <summary>字段形状（非压缩维度，不包含 batch 维）</summary>
    public int[] Shape { get; init; } = [];

    /// <summary>该字段单条记录的字节数</summary>
    public int ByteSize
    {
        get
        {
            var elementSize = TypeCode switch
            {
                1 or 2 => 1, 3 or 4 => 2, 5 or 6 => 4, 7 or 8 => 8, 9 => 2, 10 => 4, 11 => 8, 12 => 2, _ => 4
            };
            var elements = Shape.Length == 0 ? 1 : Shape.Aggregate(1, (a, b) => a * b);
            return elementSize * elements;
        }
    }
}