using Std.Data.Binary.Jvm;

namespace Nyar.Types.Externals;

/// <summary>
///     JVM 方法外部导入链接。
/// </summary>
public sealed record ExternalJvmMethodImport : ExternalImport
{
    /// <summary>
    ///     JVM 内部类名。
    /// </summary>
    public string class_name { get; init; }

    /// <summary>
    ///     方法名称。
    /// </summary>
    public string method_name { get; init; }

    /// <summary>
    ///     方法描述符覆盖。
    /// </summary>
    public string? descriptor_override { get; init; }

    /// <summary>
    ///     强类型的 JVM 类型引用，表示包含该方法的类。
    /// </summary>
    public JvmType jvm_type { get; init; }


    /// <summary>
    ///     JVM 方法外部导入链接。
    /// </summary>
    public ExternalJvmMethodImport(string class_name, string method_name, string? descriptor_override = null) : base(CallingConvention.jvm)
    {
        this.class_name = class_name;
        this.method_name = method_name;
        this.descriptor_override = descriptor_override;
        this.jvm_type = new JvmClassType(class_name);
    }

}