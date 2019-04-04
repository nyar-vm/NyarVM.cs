using Std.Data.Text.Syntax;

namespace Oak.Wat;

/// <summary>
///     WAT 字节码文本语言定义
///     WAT (WebAssembly Text Format) 是 WASM 的标准文本表示格式
///     参考：https://webassembly.github.io/spec/core/text/index.html
/// </summary>
public sealed class WatLanguage : Language
{
    /// <summary>
    ///     语言名称
    /// </summary>
    public override string name => "WAT";


    /// <summary>
    ///     默认实例
    /// </summary>
    public static WatLanguage Default => new();
}