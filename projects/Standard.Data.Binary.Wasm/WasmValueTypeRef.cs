using Std.Data.Binary.Wasm.Data;

namespace Std.Data.Binary.Wasm;

/// <summary>
///     Wasm 值类型引用，表示一个 Wasm 值类型（i32、i64、f32、f64 及各类引用类型）。
/// </summary>
public sealed class WasmValueTypeRef : WasmType
{
    /// <summary>
    ///     创建值类型引用。
    /// </summary>
    /// <param name="valueType">Wasm 值类型枚举值。</param>
    public WasmValueTypeRef(WasmValueType valueType)
    {
        this.value_type = valueType;
    }

    /// <summary>
    ///     Wasm 值类型。
    /// </summary>
    public WasmValueType value_type { get; }
}
