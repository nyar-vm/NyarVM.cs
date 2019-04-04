using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Encode;
using Nyar.Assembler;

namespace Nyar.VM.LegacyVM.Compiler;

/// <summary>
///     NyarVM 字节码编译器，将 <see cref="GenerateModule" /> 编译为 .nyar 二进制字节码。
/// </summary>
public sealed class NyarBytecodeCompiler
{
    /// <summary>
    ///     将元编译模块编译为 .nyar 字节码
    /// </summary>
    /// <param name="module">元编译模块</param>
    /// <returns>.nyar 二进制数据</returns>
    public byte[] compile(GenerateModule module)
    {
        var data = convert_to_nyar_data(module);
        var encoder = new NyarEncoder();
        return encoder.encode(data);
    }

    /// <summary>
    ///     将 <see cref="GenerateModule" /> 转换为 <see cref="NyarModuleData" />
    /// </summary>
    private static NyarModuleData convert_to_nyar_data(GenerateModule module)
    {
        var codeWriter = new ByteBufferWriter(1024);
        var functions = new List<NyarFunction>();

        foreach (var func in module.functions)
        {
            var codeOffset = codeWriter.position;
            emit_function_body(codeWriter, func, module);
            var codeLength = codeWriter.position - codeOffset;
            functions.Add(new NyarFunction(
                func.name,
                func.parameters.Count,
                func.local_variables.Count,
                codeOffset,
                codeLength));
        }

        var constants = convert_constants(module.constants);
        var imports = convert_imports(module.imports);
        var exports = convert_exports(module.exports);

        return new NyarModuleData
        {
            name = module.name,
            constants = constants,
            functions = functions,
            imports = imports,
            exports = exports,
            code_bytes = codeWriter.to_array()
        };
    }

    /// <summary>
    ///     发射函数体内的所有指令
    /// </summary>
    private static void emit_function_body(ByteBufferWriter writer, GenerateFunction func, GenerateModule module)
    {
        foreach (var instr in func.instructions)
        {
            emit_instruction(writer, instr, module);
        }
    }

    /// <summary>
    ///     发射单条指令及其操作数
    /// </summary>
    private static void emit_instruction(ByteBufferWriter writer, GenerateInstruction instr, GenerateModule module)
    {
        writer.write_u8((byte)instr.opcode);

        foreach (var operand in instr.operands)
        {
            emit_operand(writer, operand, module);
        }
    }

    /// <summary>
    ///     发射操作数
    /// </summary>
    private static void emit_operand(ByteBufferWriter writer, GenerateOperand operand, GenerateModule module)
    {
        switch (operand)
        {
            case GenerateOperand.I32 i32:
                writer.write_i32_le(i32.value);
                break;
            case GenerateOperand.I64 i64:
                writer.write_i64_le(i64.value);
                break;
            case GenerateOperand.F32 f32:
                writer.write_f32_le(f32.value);
                break;
            case GenerateOperand.F64 f64:
                writer.write_f64_le(f64.value);
                break;
            case GenerateOperand.Str str:
                emit_string(writer, str.value);
                break;
            case GenerateOperand.Local local:
                writer.write_i32_le(local.index);
                break;
            case GenerateOperand.Param param:
                writer.write_i32_le(param.index);
                break;
            case GenerateOperand.Label label:
                emit_string(writer, label.name);
                break;
            case GenerateOperand.FuncRef funcRef:
                writer.write_i32_le(find_function_index(module, funcRef.name));
                break;
            case GenerateOperand.Const constRef:
                writer.write_i32_le(constRef.pool_index);
                break;
            case GenerateOperand.Null:
                writer.write_i32_le(0);
                break;
        }
    }

    /// <summary>
    ///     查找函数在模块函数列表中的索引
    /// </summary>
    private static int find_function_index(GenerateModule module, string name)
    {
        for (var i = 0; i < module.functions.Count; i++)
        {
            if (string.Equals(module.functions[i].name, name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    ///     写入带长度前缀的字符串
    /// </summary>
    private static void emit_string(ByteBufferWriter writer, string value)
    {
        writer.write_i32_le(value.Length);
        foreach (var ch in value)
        {
            writer.write_u8((byte)ch);
        }
    }

    /// <summary>
    ///     转换常量池：将 GenerateConstantPool 的三组常量合并为 NyarConstant 列表
    /// </summary>
    private static IReadOnlyList<NyarConstant> convert_constants(GenerateConstantPool pool)
    {
        var constants = new List<NyarConstant>();

        foreach (var value in pool.int64_s)
        {
            constants.Add(new NyarConstant(NyarConstantKind.integer32, (int)value));
        }

        foreach (var value in pool.float64_s)
        {
            constants.Add(new NyarConstant(NyarConstantKind.float64, value));
        }

        foreach (var value in pool.strings)
        {
            constants.Add(new NyarConstant(NyarConstantKind.@string, value));
        }

        return constants;
    }

    /// <summary>
    ///     转换导入表
    /// </summary>
    private static IReadOnlyList<NyarImport> convert_imports(List<GenerateModuleImport> imports)
    {
        var result = new List<NyarImport>();
        foreach (var imp in imports)
        {
            var kind = imp.kind switch
            {
                GenerateImportKind.function => NyarImportKind.function,
                GenerateImportKind.global => NyarImportKind.global,
                GenerateImportKind.module => NyarImportKind.module,
                _ => NyarImportKind.function
            };
            result.Add(new NyarImport(kind, imp.module_name, imp.symbol_name));
        }

        return result;
    }

    /// <summary>
    ///     转换导出表
    /// </summary>
    private static IReadOnlyList<NyarExport> convert_exports(List<GenerateModuleExport> exports)
    {
        var result = new List<NyarExport>();
        foreach (var exp in exports)
        {
            var kind = exp.kind switch
            {
                GenerateExportKind.function => NyarExportKind.function,
                GenerateExportKind.global => NyarExportKind.global,
                _ => NyarExportKind.function
            };
            result.Add(new NyarExport(kind, exp.name, exp.function_index));
        }

        return result;
    }
}