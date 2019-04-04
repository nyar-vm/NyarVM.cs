namespace Olympus.Athena.Storage;

/// <summary>
///     列数据编码类型
/// </summary>
public enum EncodingType
{
    /// <summary>
    ///     直接存储，按 <c>T</c> 类型大小逐值排列
    /// </summary>
    Plain,

    /// <summary>
    ///     差值编码，存储首个值后接后续值的差值序列
    /// </summary>
    Delta,

    /// <summary>
    ///     变长整数编码，对整数类型使用可变长度编码
    /// </summary>
    VarInt,

    /// <summary>
    ///     字典编码，使用字典映射去重后的值并存储索引数组
    /// </summary>
    Dictionary
}