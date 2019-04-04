namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     MemberRef 表行（ECMA-335 §22.25）的
/// </summary>
public sealed class ClrMemberRefRow
{
    public uint class_index { get; init; }
    public string name { get; init; } = string.Empty;
    public uint signature_index { get; init; }
}