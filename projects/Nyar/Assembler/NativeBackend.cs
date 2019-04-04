using System.Text;
using Nyar.Assembler.Format;
using Nyar.Assembler.ISA;
using Nyar.Types.Targets;
using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler;

/// <summary>
///     Native AOT 后端的
///     当前优先打的Windows x64 最小可执行闭环的
///     1. 返回整型退出码的
///     2. 支持将字符串字面量输出到标准输出的
///     3. 产出可被 Windows 装载的合的PE的
/// </summary>
public sealed class NativeBackend : ICoreBackend<NativeCodeInfo>
{
    private const uint _pe_text_rva = 0x1000;
    private const uint _pe_data_rva = 0x2000;
    private const uint _pe_idata_rva = 0x3000;
    private const uint _pe_xdata_rva = 0x4000;
    private const uint _pe_pdata_rva = 0x5000;

    /// <inheritdoc />
    public string name => "Native";

    /// <inheritdoc />
    public IReadOnlyList<TargetArch> supported_archs =>
    [
        TargetArch.x86, TargetArch.x86_64, TargetArch.arm, TargetArch.a_arch64,
        TargetArch.risc_v32, TargetArch.risc_v64
    ];

    /// <inheritdoc />
    public OutputSpec<NativeCodeInfo> compile(GenerateModule module, CompilationOptions options)
    {
        var info = collect_native_info(module);
        var executableBytes = build_windows_pe(module, info);

        return new OutputSpec<NativeCodeInfo>
        {
            data = info,
            file_extension = ".exe",
            media_type = "application/octet-stream",
            generate_text_output = false,
            assets =
            [
                new AssemblerAsset
                {
                    name = $"{module.name}.exe",
                    content = executableBytes,
                    media_type = "application/octet-stream"
                }
            ]
        };
    }

    /// <inheritdoc />
    OutputSpec ICodeGenBackend.compile(GenerateModule module, CompilationOptions options)
    {
        return compile(module, options);
    }


    /// <inheritdoc />
    public bool validate(GenerateModule module, out List<Diagnostic> diagnostics)
    {
        diagnostics = [];

        if (module.has_witness_dispatch)
        {
            diagnostics.Add(new Diagnostic(
                default,
                "Native 后端当前不支持 `trait/imply` 的 witness 分派；请改用 `NyarVM` 目标，或先完成静态单态化。",
                DiagnosticSeverity.error));
            return false;
        }

        foreach (var function in module.functions)
        foreach (var instruction in function.instructions)
            switch (instruction.head_code)
            {
                case NyarHeadCode.@const:
                case NyarHeadCode.@return:
                case NyarHeadCode.call:
                case NyarHeadCode.call_static:
                    break;
                default:
                    diagnostics.Add(new Diagnostic(
                        default,
                        $"NativeBackend 当前仅完整支的Const/Call(CallStatic)/Return，忽略指的{instruction.head_code}",
                        DiagnosticSeverity.warning));
                    break;
            }

        return true;
    }

    private static byte[] build_windows_pe(GenerateModule module, NativeCodeInfo info)
    {
        var (importTableBytes, iatVAs) = PeFormat.build_pe_import_table_static(_pe_idata_rva);
        var dataBytes = build_data_section(info);
        var textLayout = emit_windows_x64_text(module, info, iatVAs);
        var (xdataBytes, pdataBytes) = build_windows_x64_exception_data(textLayout);

        var context = new NativeBuildContext
        {
            module_name = module.name,
            arch = TargetArch.x86_64,
            text_bytes = textLayout.text_bytes,
            data_bytes = dataBytes,
            entry_rva = textLayout.entry_begin_rva,
            import_table_bytes = importTableBytes,
            iat_v_as = iatVAs,
            xdata_bytes = xdataBytes,
            pdata_bytes = pdataBytes
        };

        return new PeFormat().build_and_encode(context);
    }

    private static TextLayout emit_windows_x64_text(
        GenerateModule module,
        NativeCodeInfo info,
        Dictionary<string, uint> iatVAs)
    {
        var emitter = new X64Emitter();
        var writer = new ByteBufferWriter(512);
        var entryFunction = resolve_entry_function(module);

        var entryRva = _pe_text_rva + (uint)writer.position;
        emit_entry_stub(ref writer, emitter, iatVAs);
        var entryEndRva = _pe_text_rva + (uint)writer.position;
        var bodyBeginRva = _pe_text_rva + (uint)writer.position;
        emit_function_body(ref writer, emitter, entryFunction, info, iatVAs);
        var bodyEndRva = _pe_text_rva + (uint)writer.position;

        return new TextLayout(writer.to_array(), entryRva, entryEndRva, bodyBeginRva, bodyEndRva);
    }

    private static (byte[] XdataBytes, byte[] PdataBytes) build_windows_x64_exception_data(TextLayout layout)
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

    private static GenerateFunction resolve_entry_function(GenerateModule module)
    {
        foreach (var export in module.exports)
            if (export is { kind: GenerateExportKind.function, function_index: >= 0 }
                && export.function_index < module.functions.Count)
                return module.functions[export.function_index];

        return module.functions.FirstOrDefault(f => f.name == "main")
               ?? module.functions.FirstOrDefault()
               ?? new GenerateFunction("main", "void");
    }

    private static void emit_entry_stub(
        ref ByteBufferWriter writer,
        IX64Emitter emitter,
        IReadOnlyDictionary<string, uint> iatVAs)
    {
        emitter.push(ref writer, X64Reg.rbp);
        emitter.mov_reg_reg(ref writer, X64Reg.rbp, X64Reg.rsp);
        emitter.sub_rsp_imm8(ref writer, 0x20);

        const uint pushRbpSize = 1;
        const uint movRspRbpSize = 3;
        const uint subRspSize = 4;
        const uint callSize = 5;
        const uint movRspRbpEpilogueSize = 3;
        const uint popRbpSize = 1;
        const uint movEcxEaxSize = 2;
        const uint callExitSize = 6;
        var stubSize = pushRbpSize + movRspRbpSize + subRspSize + callSize
                       + movRspRbpEpilogueSize + popRbpSize + movEcxEaxSize + callExitSize;

        var callMainPos = writer.position;
        emitter.call_rel32(ref writer, _pe_text_rva + (uint)callMainPos, _pe_text_rva + stubSize);

        emitter.mov_reg_reg(ref writer, X64Reg.rsp, X64Reg.rbp);
        emitter.pop(ref writer, X64Reg.rbp);
        emitter.mov_reg_reg(ref writer, X64Reg.ecx, X64Reg.eax);

        var callExitPos = writer.position;
        emitter.call_rip_rel(ref writer, _pe_text_rva + (uint)callExitPos, iatVAs["ExitProcess"]);
    }

    private static void emit_function_body(
        ref ByteBufferWriter writer,
        IX64Emitter emitter,
        GenerateFunction function,
        NativeCodeInfo info,
        IReadOnlyDictionary<string, uint> iatVAs)
    {
        emitter.push(ref writer, X64Reg.rbp);
        emitter.mov_reg_reg(ref writer, X64Reg.rbp, X64Reg.rsp);
        emitter.sub_rsp_imm8(ref writer, 0x30);

        var valueStack = new Stack<PendingValue>();
        var emittedReturn = false;

        foreach (var instruction in function.instructions)
            switch (instruction.head_code)
            {
                case NyarHeadCode.@const:
                    if (instruction.operands.Count > 0)
                        valueStack.Push(PendingValue.from_operand(instruction.operands[0]));

                    break;

                case NyarHeadCode.call:
                case NyarHeadCode.call_static:
                    if (instruction.operands.Count > 0
                        && instruction.operands[0] is GenerateOperand.FuncRef funcRef
                        && is_print_call(funcRef.name)
                        && valueStack.TryPop(out var pendingValue)
                        && pendingValue.kind == PendingValueKind.@string)
                        emit_write_literal(ref writer, emitter, pendingValue.string_value!, info, iatVAs);

                    break;

                case NyarHeadCode.@return:
                    emit_return_value(ref writer, emitter,
                        valueStack.TryPop(out var returnValue) ? returnValue : PendingValue.none);
                    emittedReturn = true;
                    break;
            }

        if (!emittedReturn) emit_return_value(ref writer, emitter, PendingValue.none);

        emitter.mov_reg_reg(ref writer, X64Reg.rsp, X64Reg.rbp);
        emitter.pop(ref writer, X64Reg.rbp);
        emitter.ret(ref writer);
    }

    private static bool is_print_call(string functionName)
    {
        return functionName == "std.io.print"
               || functionName.EndsWith(".print", StringComparison.Ordinal)
               || functionName == "print";
    }

    private static void emit_write_literal(
        ref ByteBufferWriter writer,
        IX64Emitter emitter,
        string literal,
        NativeCodeInfo info,
        IReadOnlyDictionary<string, uint> iatVAs)
    {
        if (!info.string_to_offset.TryGetValue(literal, out var dataOffset)) return;

        var byteLength = info.string_layout.FirstOrDefault(layout => layout.Offset == dataOffset).ByteLength;
        var stringVa = _pe_data_rva + dataOffset;

        emitter.mov_reg_imm32_short(ref writer, X64Reg.ecx, -11);

        var callGetStdHandlePos = writer.position;
        emitter.call_rip_rel(ref writer, _pe_text_rva + (uint)callGetStdHandlePos, iatVAs["GetStdHandle"]);

        emitter.mov_reg_reg(ref writer, X64Reg.rcx, X64Reg.rax);

        var leaStringPos = writer.position;
        emitter.lea_reg_rip_rel(ref writer, X64Reg.rdx, _pe_text_rva + (uint)leaStringPos, stringVa);

        emitter.mov_reg_imm32_short(ref writer, X64Reg.r8_d, byteLength);
        emitter.mov_reg_reg(ref writer, X64Reg.r9, X64Reg.rsp);
        emitter.xor_reg_reg(ref writer, X64Reg.r10, X64Reg.r10);
        emitter.mov_reg_to_mem_disp8(ref writer, X64Reg.r10, X64Reg.rsp, 0x20);

        var callWriteFilePos = writer.position;
        emitter.call_rip_rel(ref writer, _pe_text_rva + (uint)callWriteFilePos, iatVAs["WriteFile"]);
    }

    private static void emit_return_value(
        ref ByteBufferWriter writer,
        IX64Emitter emitter,
        PendingValue value)
    {
        switch (value.kind)
        {
            case PendingValueKind.int64:
                emitter.mov_reg_imm32_short(ref writer, X64Reg.eax, unchecked((int)value.int64_value));
                break;
            case PendingValueKind.int32:
                emitter.mov_reg_imm32_short(ref writer, X64Reg.eax, value.int32_value);
                break;
            default:
                emitter.xor_reg_reg(ref writer, X64Reg.eax, X64Reg.eax);
                break;
        }
    }

    private static byte[] build_data_section(NativeCodeInfo info)
    {
        var writer = new ByteBufferWriter(256);

        foreach (var (_, value) in info.const_idx_to_string.OrderBy(kvp => kvp.Key))
            writer.write(Encoding.UTF8.GetBytes(value));

        return writer.to_array();
    }

    private static NativeCodeInfo collect_native_info(GenerateModule module)
    {
        var info = new NativeCodeInfo();

        foreach (var function in module.functions)
        foreach (var instruction in function.instructions)
            switch (instruction.head_code)
            {
                case NyarHeadCode.@const:
                    if (instruction.operands.Count > 0 && instruction.operands[0] is GenerateOperand.Str str)
                        add_string_literal(info, str.value);

                    break;

                case NyarHeadCode.call:
                case NyarHeadCode.call_static:
                    if (instruction.operands.Count > 0
                        && instruction.operands[0] is GenerateOperand.FuncRef funcRef
                        && is_print_call(funcRef.name))
                    {
                        info.has_prints = true;
                        info.required_win32_apis.Add("GetStdHandle");
                        info.required_win32_apis.Add("WriteFile");
                    }

                    break;
            }

        info.required_win32_apis.Add("ExitProcess");
        return info;
    }

    private static void add_string_literal(NativeCodeInfo info, string value)
    {
        if (info.string_to_offset.ContainsKey(value)) return;

        var offset = info.next_data_offset;
        var byteLength = Encoding.UTF8.GetByteCount(value);
        var constIndex = info.const_idx_to_string.Count;

        info.const_idx_to_string[constIndex] = value;
        info.string_to_offset[value] = offset;
        info.string_layout.Add(((int)offset, byteLength));
        info.next_data_offset += (uint)byteLength;
    }

    private enum PendingValueKind
    {
        none,
        int32,
        int64,
        @string
    }

    private readonly record struct PendingValue(
        PendingValueKind kind,
        int int32_value,
        long int64_value,
        string? string_value)
    {
        public static PendingValue none => new(PendingValueKind.none, 0, 0, null);

        public static PendingValue from_operand(GenerateOperand operand)
        {
            return operand switch
            {
                GenerateOperand.I32 i32 => new PendingValue(PendingValueKind.int32, i32.value, i32.value, null),
                GenerateOperand.I64 i64 => new PendingValue(PendingValueKind.int64, unchecked((int)i64.value),
                    i64.value, null),
                GenerateOperand.Str str => new PendingValue(PendingValueKind.@string, 0, 0, str.value),
                _ => none
            };
        }
    }

    private readonly record struct TextLayout(
        byte[] text_bytes,
        uint entry_begin_rva,
        uint entry_end_rva,
        uint body_begin_rva,
        uint body_end_rva);
}