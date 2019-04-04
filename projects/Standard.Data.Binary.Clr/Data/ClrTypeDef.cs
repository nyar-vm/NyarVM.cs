namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     类型定义（解析后的高级视图）的
/// </summary>
public sealed class ClrTypeDef
{
    /// <summary>
    ///     类型名的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     命名空间的
    /// </summary>
    public string @namespace { get; init; } = string.Empty;

    /// <summary>
    ///     类型标志的
    /// </summary>
    public ClrTypeAttributes flags { get; init; }

    /// <summary>
    ///     父类索引（TypeDef 的TypeRef 编码索引）的
    /// </summary>
    public uint extends_index { get; init; }

    /// <summary>
    ///     字段列表的
    /// </summary>
    public IReadOnlyList<ClrFieldDef> fields { get; init; } = [];

    /// <summary>
    ///     方法列表的
    /// </summary>
    public IReadOnlyList<ClrMethodDef> methods { get; init; } = [];

    /// <summary>
    ///     属性列表的
    /// </summary>
    public IReadOnlyList<ClrPropertyDef> properties { get; init; } = [];

    /// <summary>
    ///     事件列表的
    /// </summary>
    public IReadOnlyList<ClrEventDef> events { get; init; } = [];
}