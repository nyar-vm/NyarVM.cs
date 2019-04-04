namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar Witness 分派条目的数据模型。
/// </summary>
public sealed class NyarWitnessDispatchEntry
{
    /// <summary>
    ///     方法 ID。
    /// </summary>
    public int method_id { get; init; }

    /// <summary>
    ///     类型 ID。
    /// </summary>
    public int type_id { get; init; }

    /// <summary>
    ///     方法名称。
    /// </summary>
    public string method_name { get; init; } = string.Empty;

    /// <summary>
    ///     目标函数索引。
    /// </summary>
    public int function_index { get; init; }

    /// <summary>
    ///     接口 ID。
    /// </summary>
    public int interface_id { get; init; }

    /// <summary>
    ///     接口方法槽位。
    /// </summary>
    public int interface_method_index { get; init; }
}