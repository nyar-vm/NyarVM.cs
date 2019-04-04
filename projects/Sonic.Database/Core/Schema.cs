namespace Olympus.Athena.Core;

#region DataType 枚举

/// <summary>
///     Athena 支持的数据类型
/// </summary>
public enum DataType
{
    /// <summary>
    ///     64 位有符号整数
    /// </summary>
    Int64,

    /// <summary>
    ///     64 位浮点数
    /// </summary>
    Float64,

    /// <summary>
    ///     UTF-8 字符串
    /// </summary>
    String,

    /// <summary>
    ///     字节数组
    /// </summary>
    Bytes,

    /// <summary>
    ///     GUID 标识符
    /// </summary>
    Guid,

    /// <summary>
    ///     布尔值
    /// </summary>
    Bool,

    /// <summary>
    ///     空值
    /// </summary>
    Null
}

#endregion

#region ColumnDescriptor 列描述符

/// <summary>
///     列描述符，定义列的名称、数据类型和可空性
/// </summary>
public readonly struct ColumnDescriptor : IEquatable<ColumnDescriptor>
{
    /// <summary>
    ///     创建列描述符
    /// </summary>
    /// <param name="name">列名</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="isNullable">是否可空</param>
    public ColumnDescriptor(string name, DataType dataType, bool isNullable = false)
    {
        Name = name;
        DataType = dataType;
        IsNullable = isNullable;
    }

    /// <summary>
    ///     列名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     数据类型
    /// </summary>
    public DataType DataType { get; }

    /// <summary>
    ///     是否可空
    /// </summary>
    public bool IsNullable { get; }

    /// <inheritdoc />
    public bool Equals(ColumnDescriptor other)
    {
        return string.Equals(Name, other.Name, StringComparison.Ordinal)
               && DataType == other.DataType
               && IsNullable == other.IsNullable;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ColumnDescriptor other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, DataType, IsNullable);
    }

    /// <summary>
    ///     比较两个 <see cref="ColumnDescriptor" /> 是否相等
    /// </summary>
    public static bool operator ==(ColumnDescriptor left, ColumnDescriptor right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     比较两个 <see cref="ColumnDescriptor" /> 是否不相等
    /// </summary>
    public static bool operator !=(ColumnDescriptor left, ColumnDescriptor right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    ///     返回列描述符的字符串表示
    /// </summary>
    public override string ToString()
    {
        var nullable = IsNullable ? "?" : string.Empty;
        return $"{Name}: {DataType}{nullable}";
    }
}

#endregion

#region Schema 模式定义

/// <summary>
///     不可变的列模式定义，描述表或结果的列结构
/// </summary>
public sealed class Schema
{
    private readonly ColumnDescriptor[] _columns;

    /// <summary>
    ///     使用列描述符集合创建 Schema
    /// </summary>
    /// <param name="columns">列描述符集合</param>
    public Schema(IEnumerable<ColumnDescriptor> columns)
    {
        _columns = [.. columns];
    }

    /// <summary>
    ///     使用列描述符数组创建 Schema
    /// </summary>
    /// <param name="columns">列描述符数组</param>
    public Schema(params ColumnDescriptor[] columns)
    {
        _columns = (ColumnDescriptor[])columns.Clone();
    }

    /// <summary>
    ///     列描述符的只读集合
    /// </summary>
    public IReadOnlyList<ColumnDescriptor> Columns => _columns;

    /// <summary>
    ///     列数量
    /// </summary>
    public int Count => _columns.Length;

    /// <summary>
    ///     获取指定索引的列描述符
    /// </summary>
    /// <param name="index">列索引</param>
    public ColumnDescriptor this[int index] => _columns[index];

    /// <summary>
    ///     返回 Schema 的字符串表示
    /// </summary>
    public override string ToString()
    {
        return $"Schema({string.Join(", ", _columns)})";
    }
}

#endregion

#region TableDefinition 表定义

/// <summary>
///     表定义，包含表名和列模式
/// </summary>
public sealed class TableDefinition
{
    /// <summary>
    ///     创建表定义
    /// </summary>
    /// <param name="name">表名</param>
    /// <param name="schema">列模式</param>
    public TableDefinition(string name, Schema schema)
    {
        Name = name;
        Schema = schema;
    }

    /// <summary>
    ///     表名
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     列模式
    /// </summary>
    public Schema Schema { get; }

    /// <summary>
    ///     返回表定义的字符串表示
    /// </summary>
    public override string ToString()
    {
        return $"Table({Name}, {Schema})";
    }
}

#endregion