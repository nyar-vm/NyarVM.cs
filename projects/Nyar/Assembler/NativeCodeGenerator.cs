using System.Text;
using Nyar.Assembler.ISA;
using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Assembler;

/// <summary>
///     Native 代码生成器，负责将元编译模块发射为 x86-64 机器码。
///     该类型同时负责函数序言与尾声、指令发射、常量解析和数据段生成。
/// </summary>
public sealed class NativeCodeGenerator
{
    private readonly IX64Emitter _emitter;

    /// <summary>
    ///     创建 Native 代码生成器实例。
    /// </summary>
    /// <param name="emitter">x86-64 指令发射器。</param>
    public NativeCodeGenerator(IX64Emitter emitter)
    {
        _emitter = emitter;
    }

    /// <summary>
    ///     发射 ELF 目标代码：入口存根加所有函数体。
    /// </summary>
    public byte[] emit_elf_code(GenerateModule module, NativeCodeInfo info)
    {
        var writer = new ByteBufferWriter(4096);

        emit_elf_start_entry(ref writer);

        foreach (var function in module.functions) emit_elf_function(ref writer, function, info, module.constants);

        return writer.to_array();
    }

    /// <summary>
    ///     发射 PE 目标代码：入口存根加所有函数体。
    /// </summary>
    public (byte[] Code, uint EntryRva) emit_pe_code_with_linker(GenerateModule module,
        uint textVa, uint dataVa, NativeCodeInfo info,
        Dictionary<string, uint> iatVAs)
    {
        var writer = new ByteBufferWriter(4096);

        emit_pe_entry_stub(ref writer, textVa, iatVAs);
        var entryRva = textVa;

        foreach (var function in module.functions)
            emit_pe_function(ref writer, function, textVa, dataVa, info, iatVAs, module.constants);

        return (writer.to_array(), entryRva);
    }

    /// <summary>
    ///     将模块中的所有函数发射为 x86-64 机器码字节。
    /// </summary>
    public byte[] emit_code(GenerateModule module)
    {
        var writer = new ByteBufferWriter(4096);

        foreach (var function in module.functions) emit_function(ref writer, function, module.constants);

        return writer.to_array();
    }

    /// <summary>
    ///     将模块的常量池发射为 ELF 数据段字节。
    /// </summary>
    public byte[] emit_elf_data(GenerateModule module, NativeCodeInfo info)
    {
        var writer = new ByteBufferWriter(256);

        foreach (var val in module.constants.int64_s) writer.write_i64_le(val);

        foreach (var val in module.constants.float64_s) writer.write_f64_le(val);

        foreach (var val in module.constants.strings)
        {
            var bytes = Encoding.UTF8.GetBytes(val);
            writer.write_i32_le(bytes.Length);
            writer.write(bytes);
            writer.write_u8(0);
        }

        return writer.to_array();
    }

    /// <summary>
    ///     构建数据段并追踪每个字符串常量的偏移与字节长度。
    /// </summary>
    public (byte[] Data, List<(int Offset, int Length)> StringInfo) emit_data_with_layout(GenerateModule module)
    {
        var writer = new ByteBufferWriter(256);
        var stringInfo = new List<(int Offset, int Length)>();

        foreach (var val in module.constants.int64_s) writer.write_i64_le(val);

        foreach (var val in module.constants.float64_s) writer.write_f64_le(val);

        foreach (var val in module.constants.strings)
        {
            var offset = writer.position;
            var bytes = Encoding.UTF8.GetBytes(val);
            writer.write_i32_le(bytes.Length);
            writer.write(bytes);
            writer.write_u8(0);
            stringInfo.Add((offset, bytes.Length));
        }

        return (writer.to_array(), stringInfo);
    }

    /// <summary>
    ///     将模块的常量池发射为数据段字节。
    /// </summary>
    public byte[] emit_data(GenerateModule module)
    {
        var writer = new ByteBufferWriter(256);

        foreach (var val in module.constants.int64_s) writer.write_i64_le(val);

        foreach (var val in module.constants.float64_s) writer.write_f64_le(val);

        foreach (var val in module.constants.strings)
        {
            var bytes = Encoding.UTF8.GetBytes(val);
            writer.write_i32_le(bytes.Length);
            writer.write(bytes);
            writer.write_u8(0);
        }

        return writer.to_array();
    }

    #region ELF 入口

    /// <summary>
    ///     发射 ELF _start 入口：call main; mov edi, eax; mov eax, 60; syscall
    /// </summary>
    private void emit_elf_start_entry(ref ByteBufferWriter writer)
    {
        var callMainPos = writer.position;
        _emitter.call_rel32(ref writer, (uint)callMainPos, (uint)callMainPos + 14u);

        _emitter.mov_reg_reg(ref writer, X64Reg.edi, X64Reg.eax);
        _emitter.mov_reg_imm32(ref writer, X64Reg.eax, 60);
        _emitter.syscall(ref writer);
    }

    /// <summary>
    ///     发射单个函数的 ELF 目标代码。
    /// </summary>
    private void emit_elf_function(ref ByteBufferWriter writer, GenerateFunction function, NativeCodeInfo info,
        GenerateConstantPool constants)
    {
        emit_prologue(ref writer, function);

        var instructions = function.instructions;
        foreach (var instruction in instructions) emit_elf_instruction(ref writer, instruction, constants);

        emit_epilogue(ref writer);
    }

    /// <summary>
    ///     发射 ELF 指令。
    /// </summary>
    private void emit_elf_instruction(ref ByteBufferWriter writer, GenerateInstruction instruction,
        GenerateConstantPool constants)
    {
        switch (instruction.head_code)
        {
            default:
                emit_instruction(ref writer, instruction, constants);
                break;
        }
    }

    #endregion

    #region PE 入口

    /// <summary>
    ///     发射 PE 入口存根
    /// </summary>
    private void emit_pe_entry_stub(ref ByteBufferWriter writer, uint textVa, Dictionary<string, uint> iatVAs)
    {
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
        var exitIatVa = iatVAs["ExitProcess"];
        _emitter.call_rip_rel(ref writer, textVa + (uint)callExitPos, exitIatVa);
    }

    /// <summary>
    ///     发射单个函数的 PE 目标代码。
    /// </summary>
    private void emit_pe_function(ref ByteBufferWriter writer, GenerateFunction function,
        uint textVa, uint dataVa, NativeCodeInfo info,
        Dictionary<string, uint> iatVAs, GenerateConstantPool constants)
    {
        emit_prologue(ref writer, function);

        var instructions = function.instructions;
        foreach (var instruction in instructions)
            emit_pe_instruction(ref writer, instruction, textVa, dataVa, info, iatVAs, constants);

        emit_epilogue(ref writer);
    }

    /// <summary>
    ///     发射 PE 目标代码指令。
    /// </summary>
    private void emit_pe_instruction(ref ByteBufferWriter writer, GenerateInstruction instruction,
        uint textVa, uint dataVa, NativeCodeInfo info,
        Dictionary<string, uint> iatVAs, GenerateConstantPool constants)
    {
        switch (instruction.head_code)
        {
            default:
                emit_instruction(ref writer, instruction, constants);
                break;
        }
    }

    #endregion

    #region 函数发射

    /// <summary>
    ///     发射单个函数的机器码，包含函数序言和尾声。
    /// </summary>
    private void emit_function(ref ByteBufferWriter writer, GenerateFunction function, GenerateConstantPool constants)
    {
        emit_prologue(ref writer, function);

        foreach (var instruction in function.instructions) emit_instruction(ref writer, instruction, constants);

        emit_epilogue(ref writer);
    }

    /// <summary>
    ///     发射函数序言：push rbp; mov rbp, rsp; sub rsp, localsSize。
    /// </summary>
    private void emit_prologue(ref ByteBufferWriter writer, GenerateFunction function)
    {
        _emitter.push(ref writer, X64Reg.rbp);
        _emitter.mov_reg_reg(ref writer, X64Reg.rbp, X64Reg.rsp);

        var localsSize = function.local_variables.Count * 8;
        if (localsSize > 0) _emitter.sub_rsp_imm32(ref writer, localsSize);
    }

    /// <summary>
    ///     发射函数尾声：mov rsp, rbp; pop rbp; ret。
    /// </summary>
    private void emit_epilogue(ref ByteBufferWriter writer)
    {
        _emitter.mov_reg_reg(ref writer, X64Reg.rsp, X64Reg.rbp);
        _emitter.pop(ref writer, X64Reg.rbp);
        _emitter.ret(ref writer);
    }

    /// <summary>
    ///     将单条元编译指令转换为 x86-64 机器码。
    /// </summary>
    private void emit_instruction(ref ByteBufferWriter writer, GenerateInstruction instruction,
        GenerateConstantPool constants)
    {
        switch (instruction.head_code)
        {
            #region 控制流

            case NyarHeadCode.nop:
                writer.write_u8(0x90);
                break;

            case NyarHeadCode.@return:
                break;

            #endregion

            #region 栈操作

            case NyarHeadCode.@const:
                emit_load_const(ref writer, instruction, constants);
                break;

            case NyarHeadCode.pop:
                break;

            case NyarHeadCode.dup:
                break;

            #endregion

            #region 局部变量与参数

            case NyarHeadCode.load_local:
                emit_load_local(ref writer, instruction);
                break;

            case NyarHeadCode.store_local:
                emit_store_local(ref writer, instruction);
                break;

            #endregion

            #region i32 算术

            case NyarHeadCode.i32_add:
                _emitter.add_reg_reg(ref writer, X64Reg.eax, X64Reg.ebx);
                break;

            case NyarHeadCode.i32_sub:
                _emitter.sub_reg_reg(ref writer, X64Reg.eax, X64Reg.ebx);
                break;

            case NyarHeadCode.i32_mul:
                _emitter.i_mul_reg_reg(ref writer, X64Reg.ebx, X64Reg.eax);
                break;

            case NyarHeadCode.i32_and:
                _emitter.add_reg_reg(ref writer, X64Reg.eax, X64Reg.ebx);
                break;

            case NyarHeadCode.i32_or:
                _emitter.or_reg_reg(ref writer, X64Reg.eax, X64Reg.ebx);
                break;

            case NyarHeadCode.i32_xor:
                _emitter.xor_reg_reg(ref writer, X64Reg.eax, X64Reg.ebx);
                break;

            case NyarHeadCode.i32_neg:
                _emitter.neg_reg(ref writer, X64Reg.eax);
                break;

            case NyarHeadCode.i32_not:
                _emitter.not_reg(ref writer, X64Reg.eax);
                break;

            case NyarHeadCode.i32_shl:
                _emitter.shl_reg_cl(ref writer, X64Reg.eax);
                break;

            case NyarHeadCode.i32_shr_s:
                _emitter.sar_reg_cl(ref writer, X64Reg.eax);
                break;

            case NyarHeadCode.i32_shr_u:
                _emitter.shr_reg_cl(ref writer, X64Reg.eax);
                break;

            #endregion

            #region i64 算术

            case NyarHeadCode.i64_add:
                _emitter.add_reg_reg(ref writer, X64Reg.rax, X64Reg.rbx);
                break;

            case NyarHeadCode.i64_sub:
                _emitter.sub_reg_reg(ref writer, X64Reg.rax, X64Reg.rbx);
                break;

            case NyarHeadCode.i64_mul:
                _emitter.i_mul_reg_reg(ref writer, X64Reg.rbx, X64Reg.rax);
                break;

            case NyarHeadCode.i64_neg:
                _emitter.neg_reg(ref writer, X64Reg.rax);
                break;

            #endregion

            #region 比较与控制流

            case NyarHeadCode.i32_eq:
                _emitter.xor_reg_reg(ref writer, X64Reg.ecx, X64Reg.ecx);
                writer.write_u8(0x39);
                writer.write_u8(0xD8);
                writer.write_u8(0x0F);
                writer.write_u8(0x94);
                writer.write_u8(0xC1);
                break;

            case NyarHeadCode.i32_ne:
                _emitter.xor_reg_reg(ref writer, X64Reg.ecx, X64Reg.ecx);
                writer.write_u8(0x39);
                writer.write_u8(0xD8);
                writer.write_u8(0x0F);
                writer.write_u8(0x95);
                writer.write_u8(0xC1);
                break;

            case NyarHeadCode.i32_lt_s:
                _emitter.xor_reg_reg(ref writer, X64Reg.ecx, X64Reg.ecx);
                writer.write_u8(0x39);
                writer.write_u8(0xD8);
                writer.write_u8(0x0F);
                writer.write_u8(0x9C);
                writer.write_u8(0xC1);
                break;

            case NyarHeadCode.i32_gt_s:
                _emitter.xor_reg_reg(ref writer, X64Reg.ecx, X64Reg.ecx);
                writer.write_u8(0x39);
                writer.write_u8(0xD8);
                writer.write_u8(0x0F);
                writer.write_u8(0x9F);
                writer.write_u8(0xC1);
                break;

            case NyarHeadCode.i32_le_s:
                _emitter.xor_reg_reg(ref writer, X64Reg.ecx, X64Reg.ecx);
                writer.write_u8(0x39);
                writer.write_u8(0xD8);
                writer.write_u8(0x0F);
                writer.write_u8(0x9E);
                writer.write_u8(0xC1);
                break;

            case NyarHeadCode.i32_ge_s:
                _emitter.xor_reg_reg(ref writer, X64Reg.ecx, X64Reg.ecx);
                writer.write_u8(0x39);
                writer.write_u8(0xD8);
                writer.write_u8(0x0F);
                writer.write_u8(0x9D);
                writer.write_u8(0xC1);
                break;

            #endregion

            #region 类型转换

            case NyarHeadCode.i32_extend_i64_s:
                writer.write_u8(0x48);
                writer.write_u8(0x63);
                writer.write_u8(0xC0);
                break;

            case NyarHeadCode.i64_trunc_i32_s:
                break;

            #endregion

            #region 调用

            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
                _emitter.call_rel32(ref writer, 0, 0);
                break;

            case NyarHeadCode.call_dynamic:
            case NyarHeadCode.call_witness:
                writer.write_u8(0xFF);
                writer.write_u8(0xD0);
                break;

            case NyarHeadCode.new_closure:
                _emitter.call_rel32(ref writer, 0, 0);
                break;

            #endregion

            default:
                writer.write_u8(0x90);
                break;
        }
    }

    /// <summary>
    ///     发射加载常量指令：mov rax, imm32。
    /// </summary>
    private void emit_load_const(ref ByteBufferWriter writer, GenerateInstruction instruction,
        GenerateConstantPool constants)
    {
        if (instruction.operands.Count == 0) return;

        var operand = instruction.operands[0];

        switch (operand)
        {
            case GenerateOperand.Const:
            {
                var actualValue = resolve_constant(operand, constants);
                _emitter.mov_reg_imm32(ref writer, X64Reg.rax, (int)actualValue);
                break;
            }
            case GenerateOperand.I32 i32:
                _emitter.mov_reg_imm32(ref writer, X64Reg.rax, i32.value);
                break;
            case GenerateOperand.I64 i64:
                _emitter.mov_reg_imm32(ref writer, X64Reg.rax, (int)i64.value);
                break;
        }
    }

    /// <summary>
    ///     从常量池解析实际值。
    /// </summary>
    private static long resolve_constant(GenerateOperand operand, GenerateConstantPool constants)
    {
        if (operand is not GenerateOperand.Const constant) return 0;

        var index = constant.pool_index;

        if (constant.type == GenerateValueType.utf8)
        {
            if (index >= 0 && index < constants.strings.Count) return index;

            return 0;
        }

        if (constant.type == GenerateValueType.f64)
        {
            if (index >= 0 && index < constants.float64_s.Count) return (long)constants.float64_s[index];

            return 0;
        }

        if (index >= 0 && index < constants.int64_s.Count) return constants.int64_s[index];

        return 0;
    }

    /// <summary>
    ///     发射加载局部变量指令：mov eax, [rbp+offset]。
    /// </summary>
    private void emit_load_local(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.Count == 0) return;

        var operand = instruction.operands[0];
        if (operand is not GenerateOperand.Local local) return;

        var offset = local.index * 8;
        _emitter.mov_mem_disp8_to_reg(ref writer, X64Reg.eax, X64Reg.rbp, (sbyte)(-offset - 8));
    }

    /// <summary>
    ///     发射存储局部变量指令：mov [rbp+offset], eax。
    /// </summary>
    private void emit_store_local(ref ByteBufferWriter writer, GenerateInstruction instruction)
    {
        if (instruction.operands.Count == 0) return;

        var operand = instruction.operands[0];
        if (operand is not GenerateOperand.Local local) return;

        var offset = local.index * 8;
        _emitter.mov_reg_to_mem_disp8(ref writer, X64Reg.eax, X64Reg.rbp, (sbyte)(-offset - 8));
    }

    #endregion
}