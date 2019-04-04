using Std.Data.Binary.Wasm;

namespace Nyar.Types.Externals;

/// <summary>
///     WASM 外部导入链接。
/// </summary>
public sealed record ExternalWasmFunctionImport : ExternalImport
{
    /// <summary>
    ///     导入模块名。
    /// </summary>
    public string module_name { get; init; }

    /// <summary>
    ///     导入函数名。
    /// </summary>
    public string function_name { get; init; }

    /// <summary>
    ///     强类型的 Wasm 类型引用。
    /// </summary>
    public WasmType wasm_type { get; init; }


    /// <summary>
    ///     WASM 外部导入链接。
    /// </summary>
    public ExternalWasmFunctionImport(string module_name, string function_name) : base(CallingConvention.wasm)
    {
        this.module_name = module_name;
        this.function_name = function_name;
        this.wasm_type = new WasmFuncTypeRef(module_name, function_name);
    }
}