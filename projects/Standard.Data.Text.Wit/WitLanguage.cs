using Std.Data.Text.Syntax;

namespace Oak.Wit;

/// <summary>
///     WIT (Wasm Interface Types) 语言定义。
///     WIT 是 WASM Component Model 的接口定义语言。
///     参考：https://github.com/WebAssembly/component-model/blob/main/design/mvp/WIT.md
/// </summary>
public sealed class WitLanguage : Language
{
    /// <summary>
    ///     WIT 语言名称。
    /// </summary>
    public override string name => "WIT";


    /// <summary>
    ///     默认实例。
    /// </summary>
    public static WitLanguage Default => new();
}