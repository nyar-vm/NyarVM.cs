using Nyar.Assembler.ISA;
using Std.Data.Binary.Frame;

namespace Nyar.Assembler.ABI;

/// <summary>
///     Linux x86-64 ABI 实现，使的syscall 指令进行系统调用的
///     系统调用号存的eax，参数依次通过 edi/esi/edx 传递的
/// </summary>
public sealed class LinuxX64Abi : IPlatformAbi
{
    private readonly IX64Emitter _emitter;

    /// <summary>
    ///     创建 Linux x86-64 ABI 实例
    /// </summary>
    /// <param name="emitter">x86-64 指令发射的/param>
    public LinuxX64Abi(IX64Emitter emitter)
    {
        _emitter = emitter;
    }

    /// <inheritdoc />
    public string name => "Linux x86-64";

    /// <inheritdoc />
    public void emit_syscall(ref ByteBufferWriter writer)
    {
        _emitter.syscall(ref writer);
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
        _emitter.mov_reg_imm32(ref writer, X64Reg.edi, exitCode);
        _emitter.mov_reg_imm32(ref writer, X64Reg.eax, 60);
        _emitter.syscall(ref writer);
    }
}