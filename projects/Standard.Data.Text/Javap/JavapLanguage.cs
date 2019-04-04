using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     Javap 字节码反汇编语言定义
///     javap -c 是 JDK 自带的标准 JVM 字节码反汇编格式
///     参考：https://docs.oracle.com/javase/specs/jvms/se17/html/jvms-4.html
/// </summary>
public sealed class JavapLanguage : Language
{
    /// <summary>
    ///     语言名称
    /// </summary>
    public override string name => "Javap";


    /// <summary>
    ///     默认实例
    /// </summary>
    public static JavapLanguage @default => new();
}