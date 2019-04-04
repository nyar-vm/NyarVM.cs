using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     Jasmin 字节码汇编语言定义
///     Jasmin 是 JVM 字节码的标准文本汇编格式
///     参考：https://jasmin.sourceforge.net/
/// </summary>
public sealed class JasminLanguage : Language
{
    /// <summary>
    ///     语言名称
    /// </summary>
    public override string name => "Jasmin";


    /// <summary>
    ///     默认实例
    /// </summary>
    public static JasminLanguage @default => new();
}