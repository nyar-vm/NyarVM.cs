using Std.Data.Binary.Clr;

namespace Nyar.Types.Externals;

/// <summary>
///     CLR 方法外部导入链接。
/// </summary>
public sealed record ExternalClrMethodImport : ExternalImport
{
    /// <summary>
    ///     程序集名称。
    /// </summary>
    public string assembly_name { get; init; }

    /// <summary>
    ///     类型全名。
    /// </summary>
    public string type_full_name { get; init; }

    /// <summary>
    ///     方法名称。
    /// </summary>
    public string method_name { get; init; }

    /// <summary>
    ///     强类型的 CLR 类型引用，表示包含该方法的类型。
    /// </summary>
    public ClrType clr_type { get; init; }

    /// <summary>
    ///     CLR 方法外部导入链接。
    /// </summary>
    public ExternalClrMethodImport(string type_full_name, string method_name) : base(CallingConvention.clr)
    {
        // TODO: 自动推断
        this.assembly_name = type_full_name;
        this.type_full_name = type_full_name;
        this.method_name = method_name;
        this.clr_type = ClrNamedType.from_full_name(type_full_name);
    }

    /// <summary>
    ///     CLR 方法外部导入链接。
    /// </summary>
    public ExternalClrMethodImport(string assembly_name, string type_full_name, string method_name) : base(CallingConvention.clr)
    {
        this.assembly_name = assembly_name;
        this.type_full_name = type_full_name;
        this.method_name = method_name;
        this.clr_type = ClrNamedType.from_full_name(type_full_name);
    }
}