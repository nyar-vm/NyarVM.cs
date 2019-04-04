using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExitCode = Core.Terminal.ExitCode;

namespace Legion.CLI.Commands;

/// <summary>
///     legion spy wasm 子模式：WASM 二进制反汇编与错误定位工具。
///     支持 --list 列出函数、--func 反汇编函数、--offset 定位绝对偏移三种模式。
/// </summary>
internal static partial class LegionSpyWasm
{
    /// <summary>
    ///     执行 WASM 反汇编。
    /// </summary>
    /// <param name="file">目标 .wasm 文件路径。</param>
    /// <param name="func">函数索引或函数名（--func 模式）。</param>
    /// <param name="offset">绝对文件偏移量（--offset 模式）。</param>
    /// <param name="list">是否列出所有函数。</param>
    /// <param name="context">错误点上下文行数。</param>
    /// <param name="json">是否输出 JSON 格式。</param>
    /// <param name="hex">是否 dump 函数体原始字节（配合 --func 使用）。</param>
    /// <returns>退出码。</returns>
    public static Task<ExitCode> run(string? file, string? func, int? offset, bool list, int context, bool json, bool hex)
    {
        if (string.IsNullOrEmpty(file))
        {
            Console.Error.WriteLine("错误：缺少目标 WASM 文件路径");
            return Task.FromResult(ExitCode.InvalidArgs);
        }

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"错误：文件不存在：{file}");
            return Task.FromResult(ExitCode.FileNotFound);
        }

        byte[] data;
        try
        {
            data = File.ReadAllBytes(file);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"错误：读取文件失败：{ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }

        if (!is_valid_wasm(data))
        {
            Console.Error.WriteLine("错误：不是有效的 WASM 文件（magic/version 校验失败）");
            return Task.FromResult(ExitCode.Error);
        }

        var module = parse_module(data);
        if (module is null)
        {
            Console.Error.WriteLine("错误：解析 WASM 模块失败");
            return Task.FromResult(ExitCode.Error);
        }

        if (list)
        {
            return Task.FromResult(run_list(module, json));
        }

        if (offset.HasValue)
        {
            return Task.FromResult(run_offset(module, offset.Value, context, json));
        }

        if (!string.IsNullOrEmpty(func))
        {
            return Task.FromResult(run_func(module, func, json, hex));
        }

        Console.Error.WriteLine("错误：未指定模式，请使用 --list / --func <idx> / --offset <abs>");
        return Task.FromResult(ExitCode.InvalidArgs);
    }

    #region WASM 校验

    /// <summary>
    ///     校验 WASM 文件头（8字节 magic + version）。
    /// </summary>
    /// <param name="data">文件字节数据。</param>
    /// <returns>是否为合法 WASM。</returns>
    private static bool is_valid_wasm(byte[] data)
    {
        if (data.Length < 8)
        {
            return false;
        }

        // magic: \0asm
        if (data[0] != 0x00 || data[1] != 0x61 || data[2] != 0x73 || data[3] != 0x6d)
        {
            return false;
        }

        // version: 1
        return data[4] == 0x01 && data[5] == 0x00 && data[6] == 0x00 && data[7] == 0x00;
    }

    #endregion

    #region 模块解析

    /// <summary>
    ///     解析 WASM 模块，提取段结构、导入函数与代码段函数体。
    /// </summary>
    /// <param name="data">文件字节数据。</param>
    /// <returns>解析结果；失败返回 null。</returns>
    private static WasmModule? parse_module(byte[] data)
    {
        var module = new WasmModule();
        module.set_raw_data(data);
        // 跳过 magic + version
        int pos = 8;

        while (pos < data.Length)
        {
            if (pos >= data.Length)
            {
                break;
            }

            int sectionId = data[pos++];
            int sectionSize = Leb128.read_unsigned(data, ref pos);
            int sectionStart = pos;
            int sectionEnd = sectionStart + sectionSize;

            switch (sectionId)
            {
                case 2:
                    // Import section
                    parse_imports(data, sectionStart, sectionEnd, module);
                    break;
                case 7:
                    // Export section
                    parse_exports(data, sectionStart, sectionEnd, module);
                    break;
                case 10:
                    // Code section
                    parse_code_section(data, sectionStart, sectionEnd, module);
                    break;
            }

            pos = sectionEnd;
        }

        return module;
    }

    /// <summary>
    ///     解析 Import 段，统计导入的函数数量并记录函数名。
    /// </summary>
    private static void parse_imports(byte[] data, int start, int end, WasmModule module)
    {
        int p = start;
        int count = Leb128.read_unsigned(data, ref p);
        int funcImportIndex = 0;

        for (int i = 0; i < count; i++)
        {
            int modLen = Leb128.read_unsigned(data, ref p);
            string modName = read_utf8(data, ref p, modLen);
            int fieldLen = Leb128.read_unsigned(data, ref p);
            string fieldName = read_utf8(data, ref p, fieldLen);
            int kind = data[p++];
            string kindDesc;

            switch (kind)
            {
                case 0:
                    // function
                    int typeIdx = Leb128.read_unsigned(data, ref p);
                    kindDesc = $"func type={typeIdx}";
                    module.importedFunctions.Add(new WasmImportedFunction
                    {
                        index = funcImportIndex,
                        moduleName = modName,
                        fieldName = fieldName,
                        typeIndex = typeIdx
                    });
                    funcImportIndex++;
                    break;
                case 1:
                    // table
                    int tableElemType = data[p++];
                    int tableFlags = Leb128.read_unsigned(data, ref p);
                    long tableInitial = Leb128.read_unsigned_64(data, ref p);
                    long tableMax = 0;
                    if ((tableFlags & 1) != 0)
                    {
                        tableMax = Leb128.read_unsigned_64(data, ref p);
                    }

                    kindDesc = "table";
                    break;
                case 2:
                    // memory
                    int memFlags = Leb128.read_unsigned(data, ref p);
                    long memInitial = Leb128.read_unsigned_64(data, ref p);
                    long memMax = 0;
                    if ((memFlags & 1) != 0)
                    {
                        memMax = Leb128.read_unsigned_64(data, ref p);
                    }

                    kindDesc = "memory";
                    break;
                case 3:
                    // global
                    int globalType = data[p++];
                    int globalMut = data[p++];
                    kindDesc = "global";
                    break;
                default:
                    kindDesc = $"unknown_kind={kind}";
                    break;
            }

            module.imports.Add(new WasmImport
            {
                moduleName = modName,
                fieldName = fieldName,
                kind = kind,
                description = kindDesc
            });
        }
    }

    /// <summary>
    ///     解析 Export 段，建立函数索引到导出名映射。
    /// </summary>
    private static void parse_exports(byte[] data, int start, int end, WasmModule module)
    {
        int p = start;
        int count = Leb128.read_unsigned(data, ref p);

        for (int i = 0; i < count; i++)
        {
            int nameLen = Leb128.read_unsigned(data, ref p);
            string name = read_utf8(data, ref p, nameLen);
            int kind = data[p++];
            int idx = Leb128.read_unsigned(data, ref p);

            // 仅处理 function 类型的导出
            if (kind == 0)
            {
                module.exportedFunctionNames[idx] = name;
            }
        }
    }

    /// <summary>
    ///     解析 Code 段，记录每个函数体在文件中的绝对偏移。
    /// </summary>
    private static void parse_code_section(byte[] data, int start, int end, WasmModule module)
    {
        int p = start;
        int funcCount = Leb128.read_unsigned(data, ref p);
        module.codeSectionStart = start;

        for (int i = 0; i < funcCount; i++)
        {
            int bodySize = Leb128.read_unsigned(data, ref p);
            int bodyStart = p;
            int bodyEnd = bodyStart + bodySize;

            // 跳过 local 声明以获取指令起始
            int q = bodyStart;
            int localCount = Leb128.read_unsigned(data, ref q);
            for (int j = 0; j < localCount; j++)
            {
                Leb128.read_unsigned(data, ref q); // 局部组计数
                q++; // 局部组类型
            }
            int instrStart = q;

            module.localFunctions.Add(new WasmLocalFunction
            {
                localIndex = i,
                absoluteIndex = module.importedFunctions.Count + i,
                bodyStartOffset = bodyStart,
                bodyEndOffset = bodyEnd,
                bodySize = bodySize,
                instructionStartOffset = instrStart
            });

            p = bodyEnd;
        }
    }

    /// <summary>
    ///     读取 UTF-8 字符串。
    /// </summary>
    private static string read_utf8(byte[] data, ref int p, int len)
    {
        string s = System.Text.Encoding.UTF8.GetString(data, p, len);
        p += len;
        return s;
    }

    #endregion

    #region 模式：--list

    /// <summary>
    ///     列出所有函数（导入 + 本地）。
    /// </summary>
    private static ExitCode run_list(WasmModule module, bool json)
    {
        var funcs = new List<object>();
        int total = module.importedFunctions.Count + module.localFunctions.Count;

        foreach (var imp in module.importedFunctions)
        {
            funcs.Add(new
            {
                index = imp.index,
                kind = "import",
                name = $"{imp.moduleName}.{imp.fieldName}",
                typeIndex = imp.typeIndex
            });
        }

        foreach (var lf in module.localFunctions)
        {
            string? exportName = null;
            if (module.exportedFunctionNames.TryGetValue(lf.absoluteIndex, out var en))
            {
                exportName = en;
            }

            funcs.Add(new
            {
                index = lf.absoluteIndex,
                kind = "local",
                name = exportName ?? $"<func[{lf.absoluteIndex}]>",
                bodyOffset = lf.bodyStartOffset,
                bodySize = lf.bodySize,
                localIndex = lf.localIndex
            });
        }

        if (json)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                total,
                imported = module.importedFunctions.Count,
                local = module.localFunctions.Count,
                functions = funcs
            }, options));
        }
        else
        {
            Console.WriteLine($"函数总数：{total}（导入 {module.importedFunctions.Count}，本地 {module.localFunctions.Count}）");
            Console.WriteLine();
            Console.WriteLine($"{"idx",-6} {"kind",-8} {"name",-40} {"offset",-10} {"size",-8}");
            Console.WriteLine(new string('-', 76));
            foreach (dynamic f in funcs)
            {
                string kind = f.kind;
                string name = f.name;
                string offset = kind == "import" ? "-" : Convert.ToString(f.bodyOffset);
                string size = kind == "import" ? "-" : Convert.ToString(f.bodySize);
                Console.WriteLine($"{f.index,-6} {kind,-8} {name,-40} {offset,-10} {size,-8}");
            }
        }

        return ExitCode.Success;
    }

    #endregion

    #region 模式：--func

    /// <summary>
    ///     反汇编指定函数。
    /// </summary>
    private static ExitCode run_func(WasmModule module, string func, bool json, bool hex)
    {
        if (!int.TryParse(func, out int idx))
        {
            // 按名字查找
            foreach (var kv in module.exportedFunctionNames)
            {
                if (kv.Value == func)
                {
                    idx = kv.Key;
                    break;
                }
            }

            if (idx < 0)
            {
                Console.Error.WriteLine($"错误：未找到名为 '{func}' 的导出函数");
                return ExitCode.InvalidArgs;
            }
        }

        int importCount = module.importedFunctions.Count;
        if (idx < importCount)
        {
            var imp = module.importedFunctions[idx];
            Console.Error.WriteLine($"错误：索引 {idx} 是导入函数（{imp.moduleName}.{imp.fieldName}），无函数体可反汇编");
            return ExitCode.InvalidArgs;
        }

        int localIdx = idx - importCount;
        if (localIdx >= module.localFunctions.Count)
        {
            Console.Error.WriteLine($"错误：函数索引 {idx} 超出范围（本地函数总数 {module.localFunctions.Count}）");
            return ExitCode.InvalidArgs;
        }

        var lf = module.localFunctions[localIdx];
        var instructions = disassemble_function_body(module.data, lf);
        string? exportName = null;
        module.exportedFunctionNames.TryGetValue(idx, out exportName);

        if (json)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                index = idx,
                name = exportName ?? $"<func[{idx}]>",
                bodyOffset = lf.bodyStartOffset,
                bodySize = lf.bodySize,
                instructionCount = instructions.Count,
                instructions
            }, options));
        }
        else
        {
            Console.WriteLine($"=== Function #{idx} ({exportName ?? "<anonymous>"}) ===");
            Console.WriteLine($"  body @+{lf.bodyStartOffset}, size={lf.bodySize}");
            Console.WriteLine($"  指令总数：{instructions.Count}");
            Console.WriteLine();

            if (hex)
            {
                Console.WriteLine("  --- raw bytes ---");
                dump_hex(module.data, lf.bodyStartOffset, lf.bodySize);
                Console.WriteLine();
            }

            foreach (var ins in instructions)
            {
                Console.WriteLine(
                    $"  [{ins.index,3}] @{ins.relativeOffset,4} " +
                    $"d={ins.blockDepth} s={ins.prevStack}→{ins.currStack} " +
                    $"{ins.mnemonic}");
            }
        }

        return ExitCode.Success;
    }

    #endregion

    #region 模式：--offset

    /// <summary>
    ///     定位绝对偏移所在的函数，输出错误点上下文。
    /// </summary>
    private static ExitCode run_offset(WasmModule module, int absOffset, int context, bool json)
    {
        WasmLocalFunction? target = null;
        int targetIdx = -1;

        for (int i = 0; i < module.localFunctions.Count; i++)
        {
            var lf = module.localFunctions[i];
            if (absOffset >= lf.bodyStartOffset && absOffset < lf.bodyEndOffset)
            {
                target = lf;
                targetIdx = i;
                break;
            }
        }

        if (target is null)
        {
            Console.Error.WriteLine($"错误：绝对偏移 {absOffset} 不在任何函数体内");
            return ExitCode.InvalidArgs;
        }

        var instructions = disassemble_function_body(module.data, target);
        int errorRel = absOffset - target.bodyStartOffset;

        int errorInstrIdx = -1;
        for (int i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].relativeOffset == errorRel)
            {
                errorInstrIdx = i;
                break;
            }
        }

        int absFuncIdx = module.importedFunctions.Count + targetIdx;
        string? exportName = null;
        module.exportedFunctionNames.TryGetValue(absFuncIdx, out exportName);

        if (json)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            List<object> ctx;
            if (errorInstrIdx >= 0)
            {
                int lo = Math.Max(0, errorInstrIdx - context);
                int hi = Math.Min(instructions.Count - 1, errorInstrIdx + context);
                ctx = new List<object>();
                for (int i = lo; i <= hi; i++)
                {
                    ctx.Add(new
                    {
                        instructions[i].index,
                        instructions[i].relativeOffset,
                        instructions[i].blockDepth,
                        instructions[i].prevStack,
                        instructions[i].currStack,
                        instructions[i].mnemonic,
                        isError = i == errorInstrIdx
                    });
                }
            }
            else
            {
                ctx = new List<object>();
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                absoluteOffset = absOffset,
                relativeOffset = errorRel,
                functionIndex = absFuncIdx,
                functionName = exportName ?? $"<func[{absFuncIdx}]>",
                bodyOffset = target.bodyStartOffset,
                bodySize = target.bodySize,
                instructionFound = errorInstrIdx >= 0,
                errorInstructionIndex = errorInstrIdx,
                contextInstructions = ctx
            }, options));
        }
        else
        {
            Console.WriteLine($"错误点 @+{absOffset} 位于函数 #{absFuncIdx} ({exportName ?? "<anonymous>"})");
            Console.WriteLine($"  body @+{target.bodyStartOffset}, size={target.bodySize}, 相对偏移 {errorRel}");
            Console.WriteLine();

            if (errorInstrIdx < 0)
            {
                Console.WriteLine("  (无法精确匹配到某条指令的起始位置，可能位于指令操作数中间)");
                return ExitCode.Success;
            }

            int lo = Math.Max(0, errorInstrIdx - context);
            int hi = Math.Min(instructions.Count - 1, errorInstrIdx + context);

            for (int i = lo; i <= hi; i++)
            {
                var ins = instructions[i];
                string marker = i == errorInstrIdx ? " <<< ERROR HERE" : "";
                Console.WriteLine(
                    $"  [{ins.index,3}] @{ins.relativeOffset,4} " +
                    $"d={ins.blockDepth} s={ins.prevStack}→{ins.currStack} " +
                    $"{ins.mnemonic}{marker}");
            }
        }

        return ExitCode.Success;
    }

    #endregion

    #region 函数体反汇编

    /// <summary>
    ///     以 hex dump 格式输出指定偏移开始的字节序列。
    /// </summary>
    /// <param name="data">文件字节数据。</param>
    /// <param name="startOffset">起始绝对偏移。</param>
    /// <param name="length">输出字节数。</param>
    private static void dump_hex(byte[] data, int startOffset, int length)
    {
        var end = Math.Min(startOffset + length, data.Length);
        for (var i = startOffset; i < end; i += 16)
        {
            var lineEnd = Math.Min(i + 16, end);
            var hexPart = new StringBuilder();
            var asciiPart = new StringBuilder();

            for (var j = i; j < lineEnd; j++)
            {
                hexPart.Append($"{data[j]:X2} ");
                var c = (char)data[j];
                asciiPart.Append(c >= 0x20 && c < 0x7F ? c : '.');
            }

            // 补齐 hex 部分对齐
            for (var j = lineEnd; j < i + 16; j++)
            {
                hexPart.Append("   ");
            }

            var relOff = i - startOffset;
            Console.WriteLine($"  +{relOff:X4}  {hexPart} {asciiPart}");
        }
    }

    /// <summary>
    ///     反汇编单个函数体（跳过 local 声明，逐条解码指令并跟踪栈深度）。
    /// </summary>
    private static List<WasmInstruction> disassemble_function_body(byte[] data, WasmLocalFunction lf)
    {
        var result = new List<WasmInstruction>();

        int p = lf.bodyStartOffset;
        int localCount = Leb128.read_unsigned(data, ref p);
        for (int i = 0; i < localCount; i++)
        {
            Leb128.read_unsigned(data, ref p); // 局部组计数
            p++; // 局部组类型
        }
        int bodyEnd = lf.bodyEndOffset;

        int instrIdx = 0;
        int blockDepth = 0;
        int stackDepth = 0;
        bool unreachable = false;

        var blockStack = new Stack<BlockInfo>();

        while (p < bodyEnd)
        {
            int relIp = p - lf.bodyStartOffset;
            var (mnemonic, stackEffect, terminates) = decode_instruction(data, ref p);

            int prevStack = stackDepth;
            if (terminates || unreachable)
            {
                unreachable = terminates || unreachable;

                if (terminates)
                {
                    // br/return/unreachable 之后进入不可达模式
                    stackDepth = 0;
                }
                else
                {
                    stackDepth += stackEffect;
                }
            }
            else
            {
                stackDepth += stackEffect;
            }

            // 特殊处理 block/loop/end 的 blockDepth
            if (mnemonic.StartsWith("block ") || mnemonic.StartsWith("loop "))
            {
                blockDepth++;
                blockStack.Push(new BlockInfo { startInstr = instrIdx });
            }
            else if (mnemonic.StartsWith("end"))
            {
                blockDepth--;
                if (blockDepth < 0)
                {
                    blockDepth = 0;
                }

                unreachable = false;
                if (blockStack.Count > 0)
                {
                    blockStack.Pop();
                }
            }

            result.Add(new WasmInstruction
            {
                index = instrIdx,
                relativeOffset = relIp,
                blockDepth = Math.Max(0, blockDepth),
                prevStack = prevStack,
                currStack = stackDepth,
                mnemonic = mnemonic
            });

            instrIdx++;
        }

        return result;
    }

    /// <summary>
    ///     block 跟踪信息。
    /// </summary>
    private struct BlockInfo
    {
        /// <summary>
        ///     起始指令索引。
        /// </summary>
        public int startInstr;
    }

    #endregion

    #region 数据结构

    /// <summary>
    ///     WASM 模块解析结果。
    /// </summary>
    private sealed class WasmModule
    {
        /// <summary>
        ///     所有导入项。
        /// </summary>
        public List<WasmImport> imports { get; } = new();

        /// <summary>
        ///     导入的函数列表。
        /// </summary>
        public List<WasmImportedFunction> importedFunctions { get; } = new();

        /// <summary>
        ///     本地函数列表。
        /// </summary>
        public List<WasmLocalFunction> localFunctions { get; } = new();

        /// <summary>
        ///     导出函数索引到名字的映射。
        /// </summary>
        public Dictionary<int, string> exportedFunctionNames { get; } = new();

        /// <summary>
        ///     Code 段在文件中的起始偏移。
        /// </summary>
        public int codeSectionStart { get; set; }

        /// <summary>
        ///     原始文件字节数据。
        /// </summary>
        public byte[] data => _data!;

        private byte[]? _data;

        /// <summary>
        ///     设置原始数据。
        /// </summary>
        public void set_raw_data(byte[] d)
        {
            _data = d;
        }
    }

    /// <summary>
    ///     WASM 导入项。
    /// </summary>
    private sealed class WasmImport
    {
        /// <summary>
        ///     模块名。
        /// </summary>
        public string moduleName { get; set; } = string.Empty;

        /// <summary>
        ///     字段名。
        /// </summary>
        public string fieldName { get; set; } = string.Empty;

        /// <summary>
        ///     导入类型（0=func, 1=table, 2=memory, 3=global）。
        /// </summary>
        public int kind { get; set; }

        /// <summary>
        ///     描述文本。
        /// </summary>
        public string description { get; set; } = string.Empty;
    }

    /// <summary>
    ///     导入的函数。
    /// </summary>
    private sealed class WasmImportedFunction
    {
        /// <summary>
        ///     函数索引。
        /// </summary>
        public int index { get; set; }

        /// <summary>
        ///     模块名。
        /// </summary>
        public string moduleName { get; set; } = string.Empty;

        /// <summary>
        ///     字段名。
        /// </summary>
        public string fieldName { get; set; } = string.Empty;

        /// <summary>
        ///     类型索引。
        /// </summary>
        public int typeIndex { get; set; }
    }

    /// <summary>
    ///     本地函数。
    /// </summary>
    private sealed class WasmLocalFunction
    {
        /// <summary>
        ///     在本地函数列表中的索引。
        /// </summary>
        public int localIndex { get; set; }

        /// <summary>
        ///     绝对函数索引（导入数 + localIndex）。
        /// </summary>
        public int absoluteIndex { get; set; }

        /// <summary>
        ///     body 起始绝对偏移。
        /// </summary>
        public int bodyStartOffset { get; set; }

        /// <summary>
        ///     body 结束绝对偏移。
        /// </summary>
        public int bodyEndOffset { get; set; }

        /// <summary>
        ///     body 大小。
        /// </summary>
        public int bodySize { get; set; }

        /// <summary>
        ///     指令起始绝对偏移（跳过 local 声明）。
        /// </summary>
        public int instructionStartOffset { get; set; }
    }

    /// <summary>
    ///     反汇编出的单条指令。
    /// </summary>
    private sealed class WasmInstruction
    {
        /// <summary>
        ///     指令在函数内的序号。
        /// </summary>
        public int index { get; set; }

        /// <summary>
        ///     在函数体内的相对字节偏移。
        /// </summary>
        public int relativeOffset { get; set; }

        /// <summary>
        ///     当前 block 深度。
        /// </summary>
        public int blockDepth { get; set; }

        /// <summary>
        ///     执行前的栈深度。
        /// </summary>
        public int prevStack { get; set; }

        /// <summary>
        ///     执行后的栈深度。
        /// </summary>
        public int currStack { get; set; }

        /// <summary>
        ///     助记符与操作数。
        /// </summary>
        public string mnemonic { get; set; } = string.Empty;
    }

    #endregion
}
