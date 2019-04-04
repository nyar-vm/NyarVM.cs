namespace Std.Data.Binary.Wasm;

/// <summary>
///     Wasm 函数类型引用，表示一个 Wasm 函数签名类型。
/// </summary>
public sealed class WasmFuncTypeRef : WasmType
{
    /// <summary>
    ///     创建函数类型引用。
    /// </summary>
    /// <param name="module">导入模块名。</param>
    /// <param name="field">导入字段名。</param>
    public WasmFuncTypeRef(string module, string field)
    {
        this.module = module;
        this.field = field;
    }

    /// <summary>
    ///     导入模块名。
    /// </summary>
    public string module { get; }

    /// <summary>
    ///     导入字段名。
    /// </summary>
    public string field { get; }
}
