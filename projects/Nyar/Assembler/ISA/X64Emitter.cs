using System.Runtime.CompilerServices;
using Std.Data.Binary.Frame;

namespace Nyar.Assembler.ISA;

/// <summary>
///     x86-64 指令发射器，将语义化的方法调用转换为 x86-64 机器码字节序列的
///     封装 REX 前缀、ModR/M 字节的SIB 字节的计算的
/// </summary>
public sealed class X64Emitter : IX64Emitter
{
    #region 数据移动

    /// <inheritdoc />
    public void mov_reg_imm32(ref ByteBufferWriter writer, X64Reg reg, int imm32)
    {
        var is64 = is64_bit_reg(reg);
        var rex = rex_prefix(is64, reg, X64Reg.rax);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0xC7);
        writer.write_u8(mod_rm(Mod.register, 0, reg_code(reg)));
        writer.write_i32_le(imm32);
    }

    /// <inheritdoc />
    public void mov_reg_imm64(ref ByteBufferWriter writer, X64Reg reg, long imm64)
    {
        var rex = rex_prefix(true, X64Reg.rax, reg);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8((byte)(0xB8 + reg_code_low(reg)));
        writer.write_i64_le(imm64);
    }

    /// <inheritdoc />
    public void mov_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(src);
        var rex = rex_prefix(is64, src, dst);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x89);
        writer.write_u8(mod_rm(Mod.register, reg_code(src), reg_code(dst)));
    }

    /// <inheritdoc />
    public void mov_reg_to_mem_disp8(ref ByteBufferWriter writer, X64Reg src, X64Reg @base, sbyte disp8)
    {
        var is64 = is64_bit_reg(src) || is64_bit_reg(@base);
        var rex = rex_prefix(is64, src, @base);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x89);
        writer.write_u8(mod_rm(Mod.disp8, reg_code(src), reg_code(@base)));

        if (requires_sib(@base)) writer.write_u8(sib_encode(0, 4, reg_code(@base)));

        writer.write_u8((byte)disp8);
    }

    /// <inheritdoc />
    public void mov_mem_disp8_to_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg @base, sbyte disp8)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(@base);
        var rex = rex_prefix(is64, dst, @base);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x8B);
        writer.write_u8(mod_rm(Mod.disp8, reg_code(dst), reg_code(@base)));

        if (requires_sib(@base)) writer.write_u8(sib_encode(0, 4, reg_code(@base)));

        writer.write_u8((byte)disp8);
    }

    /// <inheritdoc />
    public void mov_reg_imm32_short(ref ByteBufferWriter writer, X64Reg dst, int imm32)
    {
        var is64 = is64_bit_reg(dst);
        var rex = rex_prefix(is64, X64Reg.rax, dst);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8((byte)(0xB8 + reg_code_low(dst)));
        writer.write_i32_le(imm32);
    }

    #endregion

    #region 栈操的

    /// <inheritdoc />
    public void push(ref ByteBufferWriter writer, X64Reg reg)
    {
        if (needs_rex(reg)) writer.write_u8(0x40 | _rex_b);

        writer.write_u8((byte)(0x50 + reg_code_low(reg)));
    }

    /// <inheritdoc />
    public void pop(ref ByteBufferWriter writer, X64Reg reg)
    {
        if (needs_rex(reg)) writer.write_u8(0x40 | _rex_b);

        writer.write_u8((byte)(0x58 + reg_code_low(reg)));
    }

    /// <inheritdoc />
    public void ret(ref ByteBufferWriter writer)
    {
        writer.write_u8(0xC3);
    }

    #endregion

    #region 栈指针调的

    /// <inheritdoc />
    public void sub_rsp_imm8(ref ByteBufferWriter writer, byte imm8)
    {
        writer.write_u8(0x48);
        writer.write_u8(0x83);
        writer.write_u8(0xEC);
        writer.write_u8(imm8);
    }

    /// <inheritdoc />
    public void sub_rsp_imm32(ref ByteBufferWriter writer, int imm32)
    {
        writer.write_u8(0x48);
        writer.write_u8(0x81);
        writer.write_u8(0xEC);
        writer.write_i32_le(imm32);
    }

    /// <inheritdoc />
    public void add_rsp_imm8(ref ByteBufferWriter writer, byte imm8)
    {
        writer.write_u8(0x48);
        writer.write_u8(0x83);
        writer.write_u8(0xC4);
        writer.write_u8(imm8);
    }

    /// <inheritdoc />
    public void add_rsp_imm32(ref ByteBufferWriter writer, int imm32)
    {
        writer.write_u8(0x48);
        writer.write_u8(0x81);
        writer.write_u8(0xC4);
        writer.write_i32_le(imm32);
    }

    #endregion

    #region RIP-relative 寻址

    /// <inheritdoc />
    public void lea_reg_rip_rel(ref ByteBufferWriter writer, X64Reg reg, uint instrVa, uint targetVa)
    {
        var disp = (int)(targetVa - (instrVa + 7u));
        var is64 = is64_bit_reg(reg);
        var rex = rex_prefix(is64, reg, X64Reg.rax);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x8D);
        writer.write_u8(mod_rm(Mod.disp0, reg_code(reg), 5));
        writer.write_i32_le(disp);
    }

    /// <inheritdoc />
    public void call_rip_rel(ref ByteBufferWriter writer, uint instrVa, uint targetVa)
    {
        var disp = (int)(targetVa - (instrVa + 6u));
        writer.write_u8(0xFF);
        writer.write_u8(mod_rm(Mod.disp0, 2, 5));
        writer.write_i32_le(disp);
    }

    /// <inheritdoc />
    public void jmp_rip_rel(ref ByteBufferWriter writer, uint instrVa, uint targetVa)
    {
        var disp = (int)(targetVa - (instrVa + 6u));
        writer.write_u8(0xFF);
        writer.write_u8(mod_rm(Mod.disp0, 4, 5));
        writer.write_i32_le(disp);
    }

    /// <inheritdoc />
    public void call_rel32(ref ByteBufferWriter writer, uint instrVa, uint targetVa)
    {
        var disp = (int)(targetVa - (instrVa + 5u));
        writer.write_u8(0xE8);
        writer.write_i32_le(disp);
    }

    #endregion

    #region 算术运算

    /// <inheritdoc />
    public void add_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(src);
        var rex = rex_prefix(is64, src, dst);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x01);
        writer.write_u8(mod_rm(Mod.register, reg_code(src), reg_code(dst)));
    }

    /// <inheritdoc />
    public void sub_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(src);
        var rex = rex_prefix(is64, src, dst);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x29);
        writer.write_u8(mod_rm(Mod.register, reg_code(src), reg_code(dst)));
    }

    /// <inheritdoc />
    public void i_mul_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(src);
        var rex = rex_prefix(is64, dst, src);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x0F);
        writer.write_u8(0xAF);
        writer.write_u8(mod_rm(Mod.register, reg_code(dst), reg_code(src)));
    }

    /// <inheritdoc />
    public void and_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(src);
        var rex = rex_prefix(is64, src, dst);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x21);
        writer.write_u8(mod_rm(Mod.register, reg_code(src), reg_code(dst)));
    }

    /// <inheritdoc />
    public void or_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(src);
        var rex = rex_prefix(is64, src, dst);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x09);
        writer.write_u8(mod_rm(Mod.register, reg_code(src), reg_code(dst)));
    }

    /// <inheritdoc />
    public void xor_reg_reg(ref ByteBufferWriter writer, X64Reg dst, X64Reg src)
    {
        var is64 = is64_bit_reg(dst) || is64_bit_reg(src);
        var rex = rex_prefix(is64, src, dst);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0x31);
        writer.write_u8(mod_rm(Mod.register, reg_code(src), reg_code(dst)));
    }

    /// <inheritdoc />
    public void neg_reg(ref ByteBufferWriter writer, X64Reg reg)
    {
        var is64 = is64_bit_reg(reg);
        var rex = rex_prefix(is64, X64Reg.rax, reg);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0xF7);
        writer.write_u8(mod_rm(Mod.register, 3, reg_code(reg)));
    }

    /// <inheritdoc />
    public void not_reg(ref ByteBufferWriter writer, X64Reg reg)
    {
        var is64 = is64_bit_reg(reg);
        var rex = rex_prefix(is64, X64Reg.rax, reg);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0xF7);
        writer.write_u8(mod_rm(Mod.register, 2, reg_code(reg)));
    }

    /// <inheritdoc />
    public void shl_reg_cl(ref ByteBufferWriter writer, X64Reg reg)
    {
        var is64 = is64_bit_reg(reg);
        var rex = rex_prefix(is64, X64Reg.rax, reg);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0xD3);
        writer.write_u8(mod_rm(Mod.register, 4, reg_code(reg)));
    }

    /// <inheritdoc />
    public void sar_reg_cl(ref ByteBufferWriter writer, X64Reg reg)
    {
        var is64 = is64_bit_reg(reg);
        var rex = rex_prefix(is64, X64Reg.rax, reg);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0xD3);
        writer.write_u8(mod_rm(Mod.register, 7, reg_code(reg)));
    }

    /// <inheritdoc />
    public void shr_reg_cl(ref ByteBufferWriter writer, X64Reg reg)
    {
        var is64 = is64_bit_reg(reg);
        var rex = rex_prefix(is64, X64Reg.rax, reg);

        if (rex != 0) writer.write_u8(rex);

        writer.write_u8(0xD3);
        writer.write_u8(mod_rm(Mod.register, 5, reg_code(reg)));
    }

    /// <inheritdoc />
    public void syscall(ref ByteBufferWriter writer)
    {
        writer.write_u8(0x0F);
        writer.write_u8(0x05);
    }

    #endregion

    #region 内部辅助方法

    private const byte _rex_w = 0x08;
    private const byte _rex_r = 0x04;
    private const byte _rex_x = 0x02;
    private const byte _rex_b = 0x01;

    /// <summary>
    ///     ModR/M 字节的mod 字段的常的
    /// </summary>
    private static class Mod
    {
        public const byte disp0 = 0x00;
        public const byte disp8 = 0x40;
        public const byte disp32 = 0x80;
        public const byte register = 0xC0;
    }

    /// <summary>
    ///     获取寄存器的 3 位编码（0-7的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte reg_code(X64Reg reg)
    {
        return reg switch
        {
            X64Reg.rax or X64Reg.eax => 0,
            X64Reg.rcx or X64Reg.ecx => 1,
            X64Reg.rdx or X64Reg.edx => 2,
            X64Reg.rbx or X64Reg.ebx => 3,
            X64Reg.rsp or X64Reg.esp => 4,
            X64Reg.rbp or X64Reg.ebp => 5,
            X64Reg.rsi or X64Reg.esi => 6,
            X64Reg.rdi or X64Reg.edi => 7,
            X64Reg.r8 or X64Reg.r8_d => 0,
            X64Reg.r9 or X64Reg.r9_d => 1,
            X64Reg.r10 or X64Reg.r10_d => 2,
            X64Reg.r11 or X64Reg.r11_d => 3,
            X64Reg.r12 or X64Reg.r12_d => 4,
            X64Reg.r13 or X64Reg.r13_d => 5,
            X64Reg.r14 or X64Reg.r14_d => 6,
            X64Reg.r15 or X64Reg.r15_d => 7,
            _ => 0
        };
    }

    /// <summary>
    ///     获取寄存器低 3 位编码（用于 B8+rd / 50+rd 等将寄存器编码在 opcode 中的指令的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte reg_code_low(X64Reg reg)
    {
        return reg_code(reg);
    }

    /// <summary>
    ///     判断寄存器是否为 64 位变的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool is64_bit_reg(X64Reg reg)
    {
        return reg <= X64Reg.r15;
    }

    /// <summary>
    ///     判断寄存器是否需的REX 前缀扩展（R8-R15 的R8d-R15d的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool needs_rex(X64Reg reg)
    {
        return reg is >= X64Reg.r8 and <= X64Reg.r15 or >= X64Reg.r8_d;
    }

    /// <summary>
    ///     构建 ModR/M 字节
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte mod_rm(byte mod, byte reg, byte rm)
    {
        return (byte)(mod | (reg << 3) | rm);
    }

    /// <summary>
    ///     判断基址寄存器是否需的SIB 字节（rm=4 的x64 编码要求 SIB 跟随 ModR/M的
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool requires_sib(X64Reg @base)
    {
        return reg_code(@base) == 4;
    }

    /// <summary>
    ///     构建 SIB 字节（Scale-Index-Base的
    /// </summary>
    /// <param name="scale">
    ///     比例因子的=1, 1=2, 2=4, 3=8的/param>
    ///     <param name="index">索引寄存器编码（4 表示无索引）。</param>
    ///     <param name="baseReg">基址寄存器编的/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte sib_encode(byte scale, byte index, byte baseReg)
    {
        return (byte)((scale << 6) | (index << 3) | baseReg);
    }

    /// <summary>
    ///     生成 REX 前缀字节
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte rex_prefix(bool w, X64Reg regField, X64Reg rmField)
    {
        byte rex = 0x40;

        if (w) rex |= _rex_w;

        if (needs_rex(regField)) rex |= _rex_r;

        if (needs_rex(rmField)) rex |= _rex_b;

        if (rex == 0x40) return 0;

        return rex;
    }

    #endregion
}