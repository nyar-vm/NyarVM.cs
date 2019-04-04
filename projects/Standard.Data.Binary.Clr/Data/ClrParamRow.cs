namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     Param 表行（ECMA-335 §22.33）的
/// </summary>
public sealed class ClrParamRow
{
    public ushort flags { get; init; }
    public ushort sequence { get; init; }
    public string name { get; init; } = string.Empty;
}