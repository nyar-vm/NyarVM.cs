using Nyar.Assembler.Format;
using Nyar.Assembler.ISA;
using Nyar.Types.Targets;
using Std.Data.Binary.Frame;

namespace Nyar.VM.LegacyVM.Compiler;

/// <summary>
///     PE 可执行文件编译器，将 x64 机器码编译为 Windows PE 可执行文件。
///     使用 <see cref="PeFormat" /> 构建 PE 二进制，包含入口点存根和 kernel32.dll 导入表。
/// </summary>
public sealed class PeCompiler
{
    private const uint _pe_text_rva = 0x1000;
    private const uint _pe_data_rva = 0x2000;
    private const uint _pe_idata_rva = 0x3000;
    private const uint _pe_xdata_rva = 0x4000;
    private const uint _pe_pdata_rva = 0x5000;

    /// <summary>
    ///     将 x64 机器码字节编译为 PE 可执行文件并写入指定路径
    /// </summary>
    /// <param name="entryFunctionName">入口函数名称</param>
    /// <param name="x64Code">x64 机器码字节</param>
    /// <param name="outputPath">输出 .exe 文件路径</param>
    /// <returns>编译后的 PE 字节数组</returns>
    public byte[] compile(string entryFunctionName, byte[] x64Code, string outputPath)
    {
        var peBytes = build_pe(x64Code);
        File.WriteAllBytes(outputPath, peBytes);
        return peBytes;
    }

    /// <summary>
    ///     将 x64 机器码字节编译为 PE 可执行文件字节数组
    /// </summary>
    /// <param name="x64Code">x64 机器码字节</param>
    /// <returns>PE 可执行文件字节数组</returns>
    public byte[] compile_to_bytes(byte[] x64Code)
    {
        return build_pe(x64Code);
    }

    private static byte[] build_pe(byte[] x64Code)
    {
        var (importTableBytes, iatVAs) = PeFormat.build_pe_import_table_static(_pe_idata_rva);
        var textLayout = build_text_section(x64Code, iatVAs);
        var (xdataBytes, pdataBytes) = build_exception_data(textLayout);

        var context = new NativeBuildContext
        {
            module_name = "legacyvm_output",
            arch = TargetArch.x86_64,
            text_bytes = textLayout.text_bytes,
            data_bytes = [],
            entry_rva = textLayout.entry_begin_rva,
            import_table_bytes = importTableBytes,
            iat_v_as = iatVAs,
            xdata_bytes = xdataBytes,
            pdata_bytes = pdataBytes
        };

        return new PeFormat().build_and_encode(context);
    }

    private static TextLayout build_text_section(byte[] x64Code,
        IReadOnlyDictionary<string, uint> iatVAs)
    {
        var emitter = new X64Emitter();
        var writer = new ByteBufferWriter(512);

        var entryRva = _pe_text_rva + (uint)writer.position;
        emit_entry_stub(ref writer, emitter, iatVAs, x64Code.Length);
        var entryEndRva = _pe_text_rva + (uint)writer.position;

        var bodyBeginRva = _pe_text_rva + (uint)writer.position;
        writer.write(x64Code);
        var bodyEndRva = _pe_text_rva + (uint)writer.position;

        return new TextLayout(writer.to_array(), entryRva, entryEndRva, bodyBeginRva, bodyEndRva);
    }

    private static void emit_entry_stub(
        ref ByteBufferWriter writer,
        IX64Emitter emitter,
        IReadOnlyDictionary<string, uint> iatVAs,
        int userCodeLength)
    {
        emitter.push(ref writer, X64Reg.rbp);
        emitter.mov_reg_reg(ref writer, X64Reg.rbp, X64Reg.rsp);
        emitter.sub_rsp_imm8(ref writer, 0x20);

        const uint pushRbpSize = 1;
        const uint movRspRbpSize = 3;
        const uint subRspSize = 4;
        var stubSize = pushRbpSize + movRspRbpSize + subRspSize + 5;

        var callMainPos = writer.position;
        emitter.call_rel32(ref writer, _pe_text_rva + (uint)callMainPos, _pe_text_rva + stubSize);

        emitter.mov_reg_reg(ref writer, X64Reg.rsp, X64Reg.rbp);
        emitter.pop(ref writer, X64Reg.rbp);
        emitter.mov_reg_reg(ref writer, X64Reg.ecx, X64Reg.eax);

        var callExitPos = writer.position;
        emitter.call_rip_rel(ref writer, _pe_text_rva + (uint)callExitPos, iatVAs["ExitProcess"]);
    }

    private static (byte[] XdataBytes, byte[] PdataBytes) build_exception_data(TextLayout layout)
    {
        var xdataWriter = new ByteBufferWriter(32);
        var entryUnwindRva = _pe_xdata_rva + (uint)xdataWriter.position;
        write_unwind_info(ref xdataWriter, 0x20);
        var bodyUnwindRva = _pe_xdata_rva + (uint)xdataWriter.position;
        write_unwind_info(ref xdataWriter, 0x30);

        var pdataWriter = new ByteBufferWriter(24);
        write_runtime_function(ref pdataWriter, layout.entry_begin_rva, layout.entry_end_rva, entryUnwindRva);
        write_runtime_function(ref pdataWriter, layout.body_begin_rva, layout.body_end_rva, bodyUnwindRva);

        return (xdataWriter.to_array(), pdataWriter.to_array());
    }

    private static void write_runtime_function(ref ByteBufferWriter writer, uint beginRva, uint endRva, uint unwindRva)
    {
        writer.write_u32_le(beginRva);
        writer.write_u32_le(endRva);
        writer.write_u32_le(unwindRva);
    }

    private static void write_unwind_info(ref ByteBufferWriter writer, byte stackAllocBytes)
    {
        var allocOpInfo = (byte)((stackAllocBytes - 8) / 8);
        var frameOffset = (byte)(stackAllocBytes / 16);

        writer.write_u8(0x01);
        writer.write_u8(0x08);
        writer.write_u8(0x03);
        writer.write_u8((byte)((frameOffset << 4) | 0x05));

        writer.write_u8(0x08);
        writer.write_u8((byte)((allocOpInfo << 4) | 0x02));
        writer.write_u8(0x04);
        writer.write_u8(0x03);
        writer.write_u8(0x01);
        writer.write_u8(0x50);
        writer.write_u16_le(0);
    }

    private readonly record struct TextLayout(
        byte[] text_bytes,
        uint entry_begin_rva,
        uint entry_end_rva,
        uint body_begin_rva,
        uint body_end_rva);
}