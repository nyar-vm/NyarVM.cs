using Nyar.Assembler.ISA;
using Std.Data.Binary.Frame;

namespace Nyar.Assembler.CRT;

/// <summary>
///     ELF _start 入口存根，实的call main 的mov edi, eax 的mov eax, 60 的syscall的
///     入口存根大小 = 5 + 2 + 5 + 2 = 14 字节
/// </summary>
public sealed class ElfStartStub : IEntryPointStub
{
    private readonly IX64Emitter _emitter;

    /// <summary>
    ///     创建 ELF 入口存根实例
    /// </summary>
    /// <param name="emitter">x86-64 指令发射的/param>
    public ElfStartStub(IX64Emitter emitter)
    {
        _emitter = emitter;
    }

    /// <inheritdoc />
    public string name => "ELF _start";

    /// <inheritdoc />
    public uint emit_stub(ref ByteBufferWriter writer, uint textVa)
    {
        var entryRva = textVa + (uint)writer.position;

        var callMainPos = writer.position;
        _emitter.call_rel32(ref writer, (uint)callMainPos, (uint)callMainPos + 14u);

        _emitter.mov_reg_reg(ref writer, X64Reg.edi, X64Reg.eax);
        _emitter.mov_reg_imm32(ref writer, X64Reg.eax, 60);
        _emitter.syscall(ref writer);

        return entryRva;
    }
}