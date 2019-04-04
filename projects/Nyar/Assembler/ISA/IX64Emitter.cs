using Std.Data.Binary.Frame;

namespace Nyar.Assembler.ISA;

/// <summary>
///     x86-64 指令发射器接口，定义将语义化操作转换的x86-64 机器码的方法
/// </summary>
public interface IX64Emitter
{
    /// <summary>
    ///     发射 <c>mov reg, imm32</c> 指令（使的C7 /0 编码的
    /// </summary>
    void mov_reg_imm32(ref ByteBufferWriter writer, X64Reg reg, int imm32);

    /// <summary>
    ///     发射 <c>mov reg, imm64</c> 指令（使的B8+rd 编码的
    /// </summary>
    void mov_reg_imm64(ref ByteBufferWriter writer, X64Reg reg, long imm64);

    /// <summary>
    ///     发射 <c>mov dst, src</c> 指令的9 /r 编码：MOV r/m, r的
    /// </summary>
    void mov_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src);

    /// <summary>
    ///     发射 <c>mov [base+disp8], src</c> 指令的9 /r 编码，Mod=01的
    /// </summary>
    void mov_reg_to_mem_disp8(ref ByteBufferWriter writer, X64Reg src, X64Reg @base, sbyte disp8);

    /// <summary>
    ///     发射 <c>mov dst, [base+disp8]</c> 指令的B /r 编码，Mod=01的
    /// </summary>
    void mov_mem_disp8_to_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg @base, sbyte disp8);

    /// <summary>
    ///     发射 <c>mov dst, imm32</c> 指令（使的B8+rd 编码，适用于标量赋值）
    /// </summary>
    void mov_reg_imm32_short(ref ByteBufferWriter writer, X64Reg dst, int imm32);

    /// <summary>
    ///     发射 <c>push reg</c> 指令的0+rd 编码的
    /// </summary>
    void push(ref ByteBufferWriter writer, X64Reg reg);

    /// <summary>
    ///     发射 <c>pop reg</c> 指令的8+rd 编码的
    /// </summary>
    void pop(ref ByteBufferWriter writer, X64Reg reg);

    /// <summary>
    ///     发射 <c>ret</c> 指令（C3的
    /// </summary>
    void ret(ref ByteBufferWriter writer);

    /// <summary>
    ///     发射 <c>sub rsp, imm8</c> 指令的3 /5 ib的
    /// </summary>
    void sub_rsp_imm8(ref ByteBufferWriter writer, byte imm8);

    /// <summary>
    ///     发射 <c>sub rsp, imm32</c> 指令的1 /5 id的
    /// </summary>
    void sub_rsp_imm32(ref ByteBufferWriter writer, int imm32);

    /// <summary>
    ///     发射 <c>add rsp, imm8</c> 指令的3 /0 ib的
    /// </summary>
    void add_rsp_imm8(ref ByteBufferWriter writer, byte imm8);

    /// <summary>
    ///     发射 <c>add rsp, imm32</c> 指令的1 /0 id的
    /// </summary>
    void add_rsp_imm32(ref ByteBufferWriter writer, int imm32);

    /// <summary>
    ///     发射 <c>lea reg, [rip+disp]</c> 指令的D /r，Mod=00，RM=101的
    /// </summary>
    void lea_reg_rip_rel(ref ByteBufferWriter writer, X64Reg reg, uint instrVa, uint targetVa);

    /// <summary>
    ///     发射 <c>call [rip+disp]</c> 间接调用指令（FF /2，Mod=00，RM=101的
    /// </summary>
    void call_rip_rel(ref ByteBufferWriter writer, uint instrVa, uint targetVa);

    /// <summary>
    ///     发射 <c>jmp [rip+disp]</c> 间接跳转指令（FF /4，Mod=00，RM=101的
    /// </summary>
    void jmp_rip_rel(ref ByteBufferWriter writer, uint instrVa, uint targetVa);

    /// <summary>
    ///     发射 <c>call disp32</c> 相对调用指令（E8 cd的
    /// </summary>
    void call_rel32(ref ByteBufferWriter writer, uint instrVa, uint targetVa);

    /// <summary>
    ///     发射 <c>add dst, src</c> 指令的1 /r：ADD r/m, r的
    /// </summary>
    void add_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src);

    /// <summary>
    ///     发射 <c>sub dst, src</c> 指令的9 /r：SUB r/m, r的
    /// </summary>
    void sub_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src);

    /// <summary>
    ///     发射 <c>imul dst, src</c> 指令的F AF /r：IMUL r, r/m的
    /// </summary>
    void i_mul_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src);

    /// <summary>
    ///     发射 <c>and dst, src</c> 指令的1 /r：AND r/m, r的
    /// </summary>
    void and_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src);

    /// <summary>
    ///     发射 <c>or dst, src</c> 指令的9 /r：OR r/m, r的
    /// </summary>
    void or_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src);

    /// <summary>
    ///     发射 <c>xor dst, src</c> 指令的1 /r：XOR r/m, r的
    /// </summary>
    void xor_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src);

    /// <summary>
    ///     发射 <c>neg reg</c> 指令（F7 /3的
    /// </summary>
    void neg_reg(ref ByteBufferWriter writer, X64Reg reg);

    /// <summary>
    ///     发射 <c>not reg</c> 指令（F7 /2的
    /// </summary>
    void not_reg(ref ByteBufferWriter writer, X64Reg reg);

    /// <summary>
    ///     发射 <c>shl reg, cl</c> 指令（D3 /4的
    /// </summary>
    void shl_reg_cl(ref ByteBufferWriter writer, X64Reg reg);

    /// <summary>
    ///     发射 <c>sar reg, cl</c> 指令（D3 /7的
    /// </summary>
    void sar_reg_cl(ref ByteBufferWriter writer, X64Reg reg);

    /// <summary>
    ///     发射 <c>shr reg, cl</c> 指令（D3 /5的
    /// </summary>
    void shr_reg_cl(ref ByteBufferWriter writer, X64Reg reg);

    /// <summary>
    ///     发射 <c>syscall</c> 指令的F 05的
    /// </summary>
    void syscall(ref ByteBufferWriter writer);
}