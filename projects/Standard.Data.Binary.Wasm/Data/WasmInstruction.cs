namespace Std.Data.Binary.Wasm.Data;

/// <summary>
///     WebAssembly 结构化指令，封装操作码和操作数列表的
///     替代直接的ByteBufferWriter 中写裸字节的
/// </summary>
public sealed class WasmInstruction
{
    /// <summary>
    ///     WASM 操作码的
    /// </summary>
    public WasmOpcode opcode { get; init; }

    /// <summary>
    ///     立即数操作数列表，无立即数时的null的
    /// </summary>
    public IReadOnlyList<WasmImmediate>? operands { get; init; }
}

/// <summary>
///     WASM 指令立即数操作数抽象基类的
/// </summary>
public abstract record WasmImmediate;

/// <summary>
///     32 位有符号整数立即数的
/// </summary>
public sealed record WasmI32Imm(int value) : WasmImmediate;

/// <summary>
///     64 位有符号整数立即数的
/// </summary>
public sealed record WasmI64Imm(long value) : WasmImmediate;

/// <summary>
///     32 位浮点数立即数的
/// </summary>
public sealed record WasmF32Imm(float value) : WasmImmediate;

/// <summary>
///     64 位浮点数立即数的
/// </summary>
public sealed record WasmF64Imm(double value) : WasmImmediate;

/// <summary>
///     类型索引立即数（用于 struct.new, array.get 的GC 指令）的
/// </summary>
public sealed record WasmTypeIndexImm(uint index) : WasmImmediate;

/// <summary>
///     函数索引立即数（用于 call 指令）的
/// </summary>
public sealed record WasmFuncIndexImm(uint index) : WasmImmediate;

/// <summary>
///     字段索引立即数（用于 struct.get/set 指令）的
/// </summary>
public sealed record WasmFieldIndexImm(uint index) : WasmImmediate;

/// <summary>
///     堆类型索引立即数（用的ref.cast, ref.test 等）的
/// </summary>
public sealed record WasmHeapTypeImm(uint index) : WasmImmediate;

/// <summary>
///     标签索引立即数（用于 br/br_if/br_table）的
/// </summary>
public sealed record WasmLabelIndexImm(uint index) : WasmImmediate;

/// <summary>
///     局部变量索引立即数（用的local.get/set/tee）的
/// </summary>
public sealed record WasmLocalIndexImm(uint index) : WasmImmediate;

/// <summary>
///     全局变量索引立即数（用于 global.get/set）的
/// </summary>
public sealed record WasmGlobalIndexImm(uint index) : WasmImmediate;

/// <summary>
///     标签索引立即数（用于 throw 指令，EH 提案）的
/// </summary>
public sealed record WasmTagIndexImm(uint index) : WasmImmediate;

/// <summary>
///     128 位向量常量立即数（用的v128.const 指令，SIMD 提案）的
///     包含 16 字节的原始向量数据的
/// </summary>
public sealed record WasmV128Imm(byte[] value) : WasmImmediate;