using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     表示一条已经从代码字节流中解出的预解码指令。
/// </summary>
/// <remarks>
///     <para>
///         这里存放的是执行期使用的“指令视图”，不是原始代码字节本身。
///         <see cref="code" /> 只是指令头码字段，用来标识指令种类；完整指令还包含 <see cref="size" /> 与各个立即数槽位。
///     </para>
///     <para>
///         <see cref="operand1" />、<see cref="operand2" />、<see cref="operand3" /> 是统一的立即数缓存槽位，
///         目的是避免执行热路径重复回读字节流，并不表示 Nyar ISA 被语义上固定为“三地址指令”。
///     </para>
///     <para>
///         只有能够明确证明对解释器 `run loop` 或 `JIT` 扫描带来显著收益的派生信息，才允许常驻缓存到该结构中。
///         仅用于解码分层、验证便利或语义表达的元数据，应保持为函数级推导，避免为热路径长期占用字段空间。
///     </para>
///     <para>
///         默认值即无效指令：当 <see cref="size" /> 为 <c>0</c> 时，表示该槽位尚未填充、越界，或只是预解码数组中用于占位的空项。
///     </para>
///     <para>
///         内存布局如下：
///     </para>
///     <code>
///     | 偏移 | 大小 | 字段     |
///     |------|------|----------|
///     | 0    | 1    | code     |
///     | 1    | 1    | size     |
///     | 2    | 2    | padding  |
///     | 4    | 4    | operand1 |
///     | 8    | 4    | operand2 |
///     | 12   | 4    | operand3 |
///     | 总计 | 16   |          |
///     </code>
///     <para>
///         固定为 `16` 字节后，一条 `64` 字节缓存行通常可容纳 `4` 条指令；同时各操作数保持稳定偏移，
///         便于 JIT 消除额外边界计算与二次解码逻辑。
///     </para>
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct NyarInstruction
{
    /// <summary>
    ///     指令头码字段。
    ///     它描述的是编码头部，不等同于完整指令语义。
    /// </summary>
    [FieldOffset(0)] public readonly NyarHeadCode code;

    /// <summary>
    ///     指令在原始代码字节流中的编码长度。
    ///     值为 <c>0</c> 表示当前实例为无效指令或默认占位。
    /// </summary>
    [FieldOffset(1)] public readonly byte size;

    /// <summary>
    ///     第一立即数缓存槽位。
    /// </summary>
    [FieldOffset(4)] public readonly int operand1;

    /// <summary>
    ///     第二立即数缓存槽位。
    /// </summary>
    [FieldOffset(8)] public readonly int operand2;

    /// <summary>
    ///     第三立即数缓存槽位。
    /// </summary>
    [FieldOffset(12)] public readonly int operand3;

    /// <summary>
    ///     构造一条预解码指令。
    /// </summary>
    /// <remarks>
    ///     指令长度由 <paramref name="code" /> 推导，不由调用方手工传入，
    ///     以避免编码头与长度信息分离后出现不一致。
    /// </remarks>
    /// <param name="code">指令头码。</param>
    /// <param name="operand1">第一立即数缓存槽位。</param>
    /// <param name="operand2">第二立即数缓存槽位。</param>
    /// <param name="operand3">第三立即数缓存槽位。</param>
    public NyarInstruction(NyarHeadCode code, int operand1 = 0, int operand2 = 0, int operand3 = 0)
    {
        this.code = code;
        this.operand1 = operand1;
        this.operand2 = operand2;
        this.operand3 = operand3;
        size = code_size(code);
    }

    /// <summary>
    ///     构造一条已经完成字节级解码的指令。
    ///     该入口用于需要显式指定实际编码长度的场景。
    /// </summary>
    /// <param name="code">指令头码。</param>
    /// <param name="size">指令真实编码长度。</param>
    /// <param name="operand1">第一立即数缓存槽位。</param>
    /// <param name="operand2">第二立即数缓存槽位。</param>
    /// <param name="operand3">第三立即数缓存槽位。</param>
    public NyarInstruction(NyarHeadCode code, int operand1, int operand2, int operand3, byte size)
    {
        this.code = code;
        this.operand1 = operand1;
        this.operand2 = operand2;
        this.operand3 = operand3;
        this.size = size;
    }

    /// <summary>
    ///     获取当前实例是否为有效指令。
    ///     约定 <c>size == 0</c> 时即为无效态，因此 <c>default</c> 可直接作为统一占位值。
    /// </summary>
    public bool is_valid => size != 0;

    /// <summary>
    ///     根据头码获取指令编码形态。
    /// </summary>
    /// <param name="head">指令头码。</param>
    /// <returns>编码形态；无法识别时返回 <see cref="NyarInstructionForm.invalid" />。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NyarInstructionForm get_form(NyarHeadCode head)
    {
        return head switch
        {
            NyarHeadCode.call_witness => NyarInstructionForm.imm2,
            NyarHeadCode.call_dynamic => NyarInstructionForm.imm3,
            NyarHeadCode.access_dynamic => NyarInstructionForm.imm2,
            NyarHeadCode.simd => NyarInstructionForm.prefixed,
            NyarHeadCode.jump => NyarInstructionForm.imm1,
            NyarHeadCode.jump_if_true => NyarInstructionForm.imm1,
            NyarHeadCode.jump_if_false => NyarInstructionForm.imm1,
            NyarHeadCode.call => NyarInstructionForm.imm1,
            NyarHeadCode.call_static => NyarInstructionForm.imm1,
            NyarHeadCode.tail_call => NyarInstructionForm.imm1,
            NyarHeadCode.@catch => NyarInstructionForm.imm1,
            NyarHeadCode.resume => NyarInstructionForm.imm1,
            NyarHeadCode.effect_handle => NyarInstructionForm.imm1,
            NyarHeadCode.@const => NyarInstructionForm.imm1,
            NyarHeadCode.builtin_call => NyarInstructionForm.imm1,
            NyarHeadCode.load_local => NyarInstructionForm.imm1,
            NyarHeadCode.store_local => NyarInstructionForm.imm1,
            NyarHeadCode.load_arg => NyarInstructionForm.imm1,
            NyarHeadCode.store_arg => NyarInstructionForm.imm1,
            NyarHeadCode.load_global => NyarInstructionForm.imm1,
            NyarHeadCode.store_global => NyarInstructionForm.imm1,
            NyarHeadCode.alloc => NyarInstructionForm.imm1,
            NyarHeadCode.i32_load => NyarInstructionForm.imm1,
            NyarHeadCode.i32_store => NyarInstructionForm.imm1,
            NyarHeadCode.i64_load => NyarInstructionForm.imm1,
            NyarHeadCode.i64_store => NyarInstructionForm.imm1,
            NyarHeadCode.new_object => NyarInstructionForm.imm1,
            NyarHeadCode.field_store => NyarInstructionForm.imm1,
            NyarHeadCode.new_closure => NyarInstructionForm.imm1,
            NyarHeadCode.get_upvalue => NyarInstructionForm.imm1,
            NyarHeadCode.set_upvalue => NyarInstructionForm.imm1,
            NyarHeadCode.access_static => NyarInstructionForm.imm1,
            NyarHeadCode.access_witness => NyarInstructionForm.imm1,
            NyarHeadCode.inline_cache_update => NyarInstructionForm.imm1,
            NyarHeadCode.call_intrinsic => NyarInstructionForm.imm2,
            NyarHeadCode.call_native => NyarInstructionForm.imm1,
            NyarHeadCode.load_native_lib => NyarInstructionForm.imm1,
            NyarHeadCode.get_native_func => NyarInstructionForm.imm1,
            _ when Enum.IsDefined(head) => NyarInstructionForm.plain,
            _ => NyarInstructionForm.invalid
        };
    }

    /// <summary>
    ///     根据头码获取对应指令的编码长度。
    /// </summary>
    /// <param name="head">指令头码。</param>
    /// <returns>固定编码长度；返回 <c>0</c> 表示无法识别的头码。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte code_size(NyarHeadCode head)
    {
        return get_form(head) switch
        {
            NyarInstructionForm.plain => 1,
            NyarInstructionForm.imm1 => 5,
            NyarInstructionForm.imm2 => 9,
            NyarInstructionForm.imm3 => 13,
            NyarInstructionForm.prefixed => 5,
            _ => 0
        };
    }
}