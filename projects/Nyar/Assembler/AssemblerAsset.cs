namespace Nyar.Assembler;

/// <summary>
///     元编译附加产物。
///     适用于 .wat、.map、.d.ts、胶水代码等旁路文件。
/// </summary>
public sealed record AssemblerAsset
{
    /// <summary>
    ///     产物名称
    /// </summary>
    public string name { get; init; } = "";

    /// <summary>
    ///     产物内容
    /// </summary>
    public byte[] content { get; init; } = [];

    /// <summary>
    ///     产物媒介类型
    /// </summary>
    public string media_type { get; init; } = "";
}