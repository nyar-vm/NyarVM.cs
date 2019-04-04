namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     Module 表行（ECMA-335 §22.30）的
/// </summary>
public sealed class ClrModuleRow
{
    public ushort generation { get; init; }
    public string name { get; init; } = string.Empty;
    public Guid mvid { get; init; }
    public Guid enc_id { get; init; }
    public Guid enc_base_id { get; init; }
}