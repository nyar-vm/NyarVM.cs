using Std.Data.Binary.NyarIR.Data;

namespace Std.Data.Binary.NyarIR.Scanner;

/// <summary>
///     Nyar 段扫描概要信息的
/// </summary>
public sealed class NyarSectionScanInfo
{
    /// <summary>
    ///     段类型的
    /// </summary>
    public NyarSectionKind kind { get; set; }

    /// <summary>
    ///     段数据偏移量的
    /// </summary>
    public int offset { get; set; }

    /// <summary>
    ///     段数据大小的
    /// </summary>
    public int size { get; set; }
}