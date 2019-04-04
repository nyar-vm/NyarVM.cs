using Nyar.Assembler.ISA;
using Std.Data.Binary.Frame;

namespace Nyar.Assembler.ABI;

/// <summary>
///     Windows x86-64 ABI 实现，使的Win32 API 调用约定的
///     通过 IAT（导入地址表）间接调用 kernel32.dll 函数的
/// </summary>
public sealed class WindowsX64Abi : IPlatformAbi
{
    private readonly IX64Emitter _emitter;

    /// <summary>
    ///     创建 Windows x86-64 ABI 实例
    /// </summary>
    /// <param name="emitter">x86-64 指令发射的/param>
    public WindowsX64Abi(IX64Emitter emitter)
    {
        _emitter = emitter;
    }

    /// <inheritdoc />
    public string name => "Windows x86-64";

    /// <inheritdoc />
    public void emit_syscall(ref ByteBufferWriter writer)
    {
        writer.write_u8(0x90);
        writer.write_u8(0x90);
    }

    /// <inheritdoc />
    public void emit_call_indirect(ref ByteBufferWriter writer)
    {
        writer.write_u8(0xFF);
        writer.write_u8(0xD0);
    }

    /// <inheritdoc />
    public void emit_exit(ref ByteBufferWriter writer, int exitCode)
    {
        _emitter.mov_reg_reg(ref writer, X64Reg.ecx, X64Reg.eax);
    }
}