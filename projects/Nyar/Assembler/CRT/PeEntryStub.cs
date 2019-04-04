using Nyar.Assembler.ISA;
using Std.Data.Binary.Frame;

namespace Nyar.Assembler.CRT;

/// <summary>
///     PE 入口存根，实的push rbp 的mov rbp, rsp 的sub rsp, 0x20 的call main 的
///     mov rsp, rbp 的pop rbp 的mov ecx, eax 的call [ExitProcess]的
///     入口存根大小 = 1 + 3 + 4 + 5 + 3 + 1 + 2 + 6 = 25 字节
/// </summary>
public sealed class PeEntryStub : IEntryPointStub
{
    private readonly IX64Emitter _emitter;

    /// <summary>
    ///     ExitProcess 的IAT 虚拟地址
    /// </summary>
    private readonly uint _exit_process_iat_va;

    /// <summary>
    ///     创建 PE 入口存根实例
    /// </summary>
    /// <param name="emitter">
    ///     x86-64 指令发射的/param>
    ///     <param name="exitProcessIatVa">ExitProcess 的IAT 虚拟地址。</param>
    public PeEntryStub(IX64Emitter emitter, uint exitProcessIatVa)
    {
        _emitter = emitter;
        _exit_process_iat_va = exitProcessIatVa;
    }

    /// <inheritdoc />
    public string name => "PE WinMain";

    /// <inheritdoc />
    public uint emit_stub(ref ByteBufferWriter writer, uint textVa)
    {
        var entryRva = textVa + (uint)writer.position;

        _emitter.push(ref writer, X64Reg.rbp);
        _emitter.mov_reg_reg(ref writer, X64Reg.rbp, X64Reg.rsp);
        _emitter.sub_rsp_imm8(ref writer, 0x20);

        var pushRbpSize = 1u;
        var movRspRbpSize = 3u;
        var subRspSize = 4u;
        var callSize = 5u;
        var movRspRbpEpilogueSize = 3u;
        var popRbpSize = 1u;
        var movEcxEaxSize = 2u;
        var callExitSize = 6u;
        var entryStubSize = pushRbpSize + movRspRbpSize + subRspSize + callSize
                            + movRspRbpEpilogueSize + popRbpSize + movEcxEaxSize + callExitSize;

        var callMainPos = writer.position;
        var mainRva = textVa + entryStubSize;
        _emitter.call_rel32(ref writer, textVa + (uint)callMainPos, mainRva);

        _emitter.mov_reg_reg(ref writer, X64Reg.rsp, X64Reg.rbp);
        _emitter.pop(ref writer, X64Reg.rbp);

        _emitter.mov_reg_reg(ref writer, X64Reg.ecx, X64Reg.eax);

        var callExitPos = writer.position;
        _emitter.call_rip_rel(ref writer, textVa + (uint)callExitPos, _exit_process_iat_va);

        return entryRva;
    }
}