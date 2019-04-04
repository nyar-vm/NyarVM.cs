using Std.Data.Binary.NyarIR.Data;

namespace Std.Data.Binary.NyarIR.Validate;

/// <summary>
///     Nyar 代码字节流验证器。
///     它验证模块元数据与编码后的代码字节流是否匹配，不依赖运行时中的解码指令对象。
/// </summary>
public sealed class NyarValidator
{
    /// <summary>
    ///     验证模块对应的代码字节流是否安全且格式正确。
    /// </summary>
    /// <param name="module">
    ///     要验证的模块数据�?/param>
    ///     <param name="codeBytes">
    ///         编码后的模块代码字节流�?/param>
    ///         <param name="diagnostics">
    ///             验证诊断信息�?/param>
    ///             <returns>验证是否通过�?/returns>
    public bool validate(NyarModuleData module, byte[] codeBytes, out List<string> diagnostics)
    {
        diagnostics = [];
        var valid = true;

        // 验证模块名称不为空�?        if (string.IsNullOrEmpty(module.name))
        {
            diagnostics.Add("Module name is empty or null.");
            valid = false;
        }

        // 验证函数
        foreach (var func in module.functions)
            if (!validate_function(func, codeBytes, diagnostics))
                valid = false;

        // 验证函数之间没有重叠
        if (!validate_function_overlap(module.functions, diagnostics)) valid = false;

        // 验证导入
        foreach (var import in module.imports)
            if (string.IsNullOrEmpty(import.module_name) || string.IsNullOrEmpty(import.symbol_name))
            {
                diagnostics.Add("Invalid import: module or symbol name is empty.");
                valid = false;
            }

        // 验证导出
        foreach (var export in module.exports)
        {
            if (string.IsNullOrEmpty(export.symbol_name))
            {
                diagnostics.Add("Invalid export: symbol name is empty.");
                valid = false;
            }

            if (export.kind == NyarExportKind.function)
                if (export.function_index < 0 || export.function_index >= module.functions.Count)
                {
                    diagnostics.Add(
                        $"Invalid export: function index {export.function_index} out of range [0, {module.functions.Count}).");
                    valid = false;
                }
        }

        foreach (var witnessEntry in module.witness_entries)
        {
            if (string.IsNullOrEmpty(witnessEntry.method_name))
            {
                diagnostics.Add("Invalid witness entry: method name is empty.");
                valid = false;
            }

            if (witnessEntry.function_index < 0 || witnessEntry.function_index >= module.functions.Count)
            {
                diagnostics.Add(
                    $"Invalid witness entry: function index {witnessEntry.function_index} out of range [0, {module.functions.Count}).");
                valid = false;
            }
        }

        return valid;
    }

    #region 跳转目标验证

    /// <summary>
    ///     验证跳转目标是否在函数代码范围内且正好在指令起始位置
    /// </summary>
    private static bool validate_jump_targets(NyarFunction func, byte[] codeBytes, List<string> diagnostics)
    {
        var valid = true;
        var end = func.code_offset + func.code_length;
        var instructionOffsets = new HashSet<int>();

        for (var pc = func.code_offset; pc < end;)
        {
            if (pc < 0 || pc >= codeBytes.Length) break;

            var op = codeBytes[pc];
            var opcode = (NyarHeadCode)op;
            if (!Enum.IsDefined(typeof(NyarHeadCode), opcode)) break;

            var instructionSize = get_instruction_size(opcode);
            if (instructionSize == 0) break;

            instructionOffsets.Add(pc);
            pc += instructionSize;
        }

        for (var pc = func.code_offset; pc < end;)
        {
            var op = codeBytes[pc];
            var opcode = (NyarHeadCode)op;

            if (!Enum.IsDefined(typeof(NyarHeadCode), opcode))
            {
                diagnostics.Add($"Function '{func.name}': undefined opcode 0x{op:X2} at offset {pc}.");
                valid = false;
                pc++;
                continue;
            }

            var instructionSize = get_instruction_size(opcode);
            if (instructionSize == 0)
            {
                diagnostics.Add($"Function '{func.name}': invalid instruction size at offset {pc}.");
                valid = false;
                pc++;
                continue;
            }

            if (opcode is NyarHeadCode.jump or NyarHeadCode.jump_if_true or NyarHeadCode.jump_if_false)
                if (pc + 1 + 4 <= end)
                {
                    var target = BitConverter.ToInt32(codeBytes, pc + 1);
                    if (target < func.code_offset || target > end)
                    {
                        diagnostics.Add(
                            $"Function '{func.name}': jump target {target} out of range [{func.code_offset}, {end}] at offset {pc}.");
                        valid = false;
                    }
                    else if (target != end && !instructionOffsets.Contains(target))
                    {
                        diagnostics.Add(
                            $"Function '{func.name}': jump target {target} not aligned with instruction start at offset {pc}.");
                        valid = false;
                    }
                }

            pc += instructionSize;
        }

        return valid;
    }

    #endregion

    #region 指令大小

    /// <summary>
    ///     获取指令大小。
    ///     统一复用 IR 侧的编码长度定义，`0` 表示无效头码。
    /// </summary>
    private static int get_instruction_size(NyarHeadCode headCode)
    {
        return NyarInstruction.code_size(headCode);
    }

    #endregion

    #region 函数验证

    /// <summary>
    ///     验证单个函数的字节码
    /// </summary>
    private static bool validate_function(NyarFunction func, byte[] codeBytes, List<string> diagnostics)
    {
        var valid = true;

        // 验证函数名称不为空�?        if (string.IsNullOrEmpty(func.name))
        {
            diagnostics.Add("Function name is empty or null.");
            valid = false;
        }

        if (func.code_offset < 0 || func.code_offset >= codeBytes.Length)
        {
            diagnostics.Add($"Function '{func.name}': invalid code offset {func.code_offset}.");
            return false;
        }

        if (func.code_offset + func.code_length > codeBytes.Length)
        {
            diagnostics.Add(
                $"Function '{func.name}': code range [{func.code_offset}, {func.code_offset + func.code_length}) exceeds code bytes length {codeBytes.Length}.");
            return false;
        }

        if (func.arity < 0)
        {
            diagnostics.Add($"Function '{func.name}': negative arity {func.arity}.");
            valid = false;
        }

        if (func.local_count < 0)
        {
            diagnostics.Add($"Function '{func.name}': negative local count {func.local_count}.");
            valid = false;
        }

        valid = validate_jump_targets(func, codeBytes, diagnostics) && valid;

        return valid;
    }

    /// <summary>
    ///     验证函数之间没有重叠
    /// </summary>
    private static bool validate_function_overlap(IReadOnlyList<NyarFunction> functions, List<string> diagnostics)
    {
        var valid = true;

        for (var i = 0; i < functions.Count; i++)
        for (var j = i + 1; j < functions.Count; j++)
        {
            var f1 = functions[i];
            var f2 = functions[j];

            var f1End = f1.code_offset + f1.code_length;
            var f2End = f2.code_offset + f2.code_length;

            // 检查是否有重叠
            if (!(f1End <= f2.code_offset || f2End <= f1.code_offset))
            {
                diagnostics.Add(
                    $"Function overlap: '{f1.name}' [{f1.code_offset}, {f1End}) and '{f2.name}' [{f2.code_offset}, {f2End}).");
                valid = false;
            }
        }

        return valid;
    }

    #endregion
}