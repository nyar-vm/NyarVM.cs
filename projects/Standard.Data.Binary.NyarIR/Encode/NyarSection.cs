using Std.Data.Binary.NyarIR.Data;

namespace Std.Data.Binary.NyarIR.Encode;

/// <summary>
///     Nyar 段数据（内部使用）的
/// </summary>
internal sealed class NyarSection
{
    public NyarSectionKind kind { get; init; }
    public byte[] data { get; init; } = [];
}