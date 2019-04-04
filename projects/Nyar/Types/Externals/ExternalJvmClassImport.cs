using Std.Data.Binary.Jvm;

namespace Nyar.Types.Externals;

/// <summary>
///     JVM 类型外部导入链接。
/// </summary>
public sealed record ExternalJvmClassImport : ExternalImport
{
    /// <summary>
    ///     JVM 内部类名。
    /// </summary>
    public string class_name { get; init; }

    /// <summary>
    ///     强类型的 JVM 类型引用。
    /// </summary>
    public JvmType jvm_type { get; init; }

    /// <summary>
    ///     JVM 类型外部导入链接。
    /// </summary>
    public ExternalJvmClassImport(string class_name) : base(CallingConvention.jvm)
    {
        this.class_name = class_name;
        this.jvm_type = new JvmClassType(class_name);
    }
}