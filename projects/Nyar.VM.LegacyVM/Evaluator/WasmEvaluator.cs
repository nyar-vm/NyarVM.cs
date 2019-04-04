using System.Text;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     WASM 字节码求值器，将 .wasm 二进制格式解析为栈式解释器执行。
///     支持 WASM MVP 的 i32 子集操作。
/// </summary>
public sealed class WasmEvaluator
{
    /// <summary>
    ///     求值 WASM 字节码
    /// </summary>
    /// <param name="source">WASM 二进制文件路径或 Base64 编码的字节码</param>
    /// <param name="env">运行环境</param>
    /// <returns>执行结果</returns>
    public object evaluate(string source, Dictionary<string, object> env)
    {
        var bytes = load_wasm_bytes(source);
        var module = parse_module(bytes);
        return execute_module(module, env);
    }

    #region 字节加载

    private static byte[] load_wasm_bytes(string source)
    {
        if (File.Exists(source))
        {
            return File.ReadAllBytes(source);
        }

        try
        {
            return Convert.FromBase64String(source);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(source);
        }
    }

    #endregion

    #region 模块解析

    private static WasmModule parse_module(byte[] bytes)
    {
        var pos = 0;

        var magic = read_u32_le(bytes, ref pos);
        if (magic != 0x6D736100)
        {
            throw new InvalidOperationException("不是有效的 WASM 二进制文件：魔数不匹配");
        }

        var version = read_u32_le(bytes, ref pos);
        if (version != 1)
        {
            throw new InvalidOperationException($"不支持的 WASM 版本：{version}");
        }

        var module = new WasmModule();

        while (pos < bytes.Length)
        {
            var sectionId = bytes[pos++];
            var sectionSize = (int)read_leb128_u32(bytes, ref pos);
            var sectionEnd = pos + sectionSize;

            switch (sectionId)
            {
                case 1:
                    parse_type_section(bytes, ref pos, module);
                    break;
                case 2:
                    parse_import_section(bytes, ref pos, module);
                    break;
                case 3:
                    parse_function_section(bytes, ref pos, module);
                    break;
                case 5:
                    parse_memory_section(bytes, ref pos, module);
                    break;
                case 7:
                    parse_export_section(bytes, ref pos, module);
                    break;
                case 10:
                    parse_code_section(bytes, ref pos, module);
                    break;
                default:
                    pos = sectionEnd;
                    break;
            }

            pos = sectionEnd;
        }

        return module;
    }

    private static void parse_type_section(byte[] bytes, ref int pos, WasmModule module)
    {
        var count = (int)read_leb128_u32(bytes, ref pos);

        for (var i = 0; i < count; i++)
        {
            var form = bytes[pos++];
            if (form != 0x60)
            {
                throw new InvalidOperationException($"不支持的类型形式：0x{form:X2}");
            }

            var paramCount = (int)read_leb128_u32(bytes, ref pos);
            var paramTypes = new WasmValueType[paramCount];
            for (var j = 0; j < paramCount; j++)
            {
                paramTypes[j] = (WasmValueType)bytes[pos++];
            }

            var resultCount = (int)read_leb128_u32(bytes, ref pos);
            var resultTypes = new WasmValueType[resultCount];
            for (var j = 0; j < resultCount; j++)
            {
                resultTypes[j] = (WasmValueType)bytes[pos++];
            }

            module.types.Add(new WasmFuncType(paramTypes, resultTypes));
        }
    }

    private static void parse_import_section(byte[] bytes, ref int pos, WasmModule module)
    {
        var count = (int)read_leb128_u32(bytes, ref pos);

        for (var i = 0; i < count; i++)
        {
            var moduleNameLen = (int)read_leb128_u32(bytes, ref pos);
            var moduleName = Encoding.UTF8.GetString(bytes, pos, moduleNameLen);
            pos += moduleNameLen;

            var fieldNameLen = (int)read_leb128_u32(bytes, ref pos);
            var fieldName = Encoding.UTF8.GetString(bytes, pos, fieldNameLen);
            pos += fieldNameLen;

            var kind = bytes[pos++];

            switch (kind)
            {
                case 0:
                    var typeIndex = (int)read_leb128_u32(bytes, ref pos);
                    module.imported_functions.Add(new WasmImport(moduleName, fieldName, kind, typeIndex));
                    break;
                case 2:
                    var memType = read_leb128_u32(bytes, ref pos);
                    var memInitial = (int)read_leb128_u32(bytes, ref pos);
                    module.has_memory_import = true;
                    module.memory_pages = memInitial;
                    break;
                default:
                    throw new InvalidOperationException($"不支持的导入种类：{kind}");
            }
        }
    }

    private static void parse_function_section(byte[] bytes, ref int pos, WasmModule module)
    {
        var count = (int)read_leb128_u32(bytes, ref pos);

        for (var i = 0; i < count; i++)
        {
            var typeIndex = (int)read_leb128_u32(bytes, ref pos);
            module.function_types.Add(typeIndex);
        }
    }

    private static void parse_memory_section(byte[] bytes, ref int pos, WasmModule module)
    {
        var count = (int)read_leb128_u32(bytes, ref pos);

        for (var i = 0; i < count; i++)
        {
            var flags = read_leb128_u32(bytes, ref pos);
            var initial = (int)read_leb128_u32(bytes, ref pos);
            module.memory_pages = initial;

            if ((flags & 0x01) != 0)
            {
                var maximum = (int)read_leb128_u32(bytes, ref pos);
                module.max_memory_pages = maximum;
            }
        }
    }

    private static void parse_export_section(byte[] bytes, ref int pos, WasmModule module)
    {
        var count = (int)read_leb128_u32(bytes, ref pos);

        for (var i = 0; i < count; i++)
        {
            var nameLen = (int)read_leb128_u32(bytes, ref pos);
            var name = Encoding.UTF8.GetString(bytes, pos, nameLen);
            pos += nameLen;

            var kind = bytes[pos++];
            var index = (int)read_leb128_u32(bytes, ref pos);

            module.exports.Add(new WasmExport(name, kind, index));
        }
    }

    private static void parse_code_section(byte[] bytes, ref int pos, WasmModule module)
    {
        var count = (int)read_leb128_u32(bytes, ref pos);

        for (var i = 0; i < count; i++)
        {
            var bodySize = (int)read_leb128_u32(bytes, ref pos);
            var bodyStart = pos;

            var func = new WasmFunctionBody();

            var localDeclCount = (int)read_leb128_u32(bytes, ref pos);
            for (var j = 0; j < localDeclCount; j++)
            {
                var localCount = (int)read_leb128_u32(bytes, ref pos);
                var localType = (WasmValueType)bytes[pos++];
                for (var k = 0; k < localCount; k++)
                {
                    func.locals.Add(localType);
                }
            }

            var codeStart = pos;
            var codeEnd = bodyStart + bodySize;
            while (pos < codeEnd)
            {
                func.instructions.Add(bytes[pos++]);
            }

            module.code_bodies.Add(func);
        }
    }

    #endregion

    #region 执行引擎

    private static object execute_module(WasmModule module, Dictionary<string, object> env)
    {
        var entryIndex = find_entry_function(module);
        if (entryIndex < 0)
        {
            throw new InvalidOperationException("WASM 模块中未找到入口函数（main 或 _start）");
        }

        var memory = new byte[module.memory_pages * 65536];
        var globals = new int[module.imported_functions.Count];
        var executor = new WasmExecutor(module, memory, globals);
        return executor.execute(entryIndex);
    }

    private static int find_entry_function(WasmModule module)
    {
        foreach (var export in module.exports)
        {
            if (export is { kind: 0, name: "main" })
            {
                return export.index;
            }
        }

        foreach (var export in module.exports)
        {
            if (export is { kind: 0, name: "_start" })
            {
                return export.index;
            }
        }

        foreach (var export in module.exports)
        {
            if (export.kind == 0)
            {
                return export.index;
            }
        }

        return -1;
    }

    #endregion

    #region LEB128 解码

    private static uint read_leb128_u32(byte[] data, ref int pos)
    {
        uint result = 0;
        var shift = 0;

        while (pos < data.Length)
        {
            var b = data[pos++];
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }

        return result;
    }

    private static int read_leb128_i32(byte[] data, ref int pos)
    {
        var result = 0;
        var shift = 0;

        while (pos < data.Length)
        {
            var b = data[pos++];
            result |= (b & 0x7F) << shift;
            shift += 7;
            if ((b & 0x80) == 0)
            {
                if (shift < 32 && (b & 0x40) != 0)
                {
                    result |= -(1 << shift);
                }

                return result;
            }
        }

        return result;
    }

    private static uint read_u32_le(byte[] data, ref int pos)
    {
        var value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
        pos += 4;
        return value;
    }

    #endregion

    #region 内部类型

    private enum WasmValueType : byte
    {
        i32 = 0x7F,
        i64 = 0x7E,
        f32 = 0x7D,
        f64 = 0x7C
    }

    private sealed class WasmFuncType(WasmValueType[] parameters, WasmValueType[] results)
    {
        public WasmValueType[] parameters { get; } = parameters;
        public WasmValueType[] results { get; } = results;
    }

    private sealed class WasmImport(string moduleName, string fieldName, byte kind, int typeIndex)
    {
        public string module_name { get; } = moduleName;
        public string field_name { get; } = fieldName;
        public byte kind { get; } = kind;
        public int type_index { get; } = typeIndex;
    }

    private sealed class WasmExport(string name, byte kind, int index)
    {
        public string name { get; } = name;
        public byte kind { get; } = kind;
        public int index { get; } = index;
    }

    private sealed class WasmFunctionBody
    {
        public List<WasmValueType> locals { get; } = [];
        public List<byte> instructions { get; } = [];
    }

    private sealed class WasmModule
    {
        public List<WasmFuncType> types { get; } = [];
        public List<WasmImport> imported_functions { get; } = [];
        public List<int> function_types { get; } = [];
        public List<WasmExport> exports { get; } = [];
        public List<WasmFunctionBody> code_bodies { get; } = [];
        public int memory_pages;
        public int max_memory_pages;
        public bool has_memory_import;
    }

    private sealed class WasmExecutor
    {
        private readonly WasmModule _module;
        private readonly byte[] _memory;
        private readonly int[] _globals;

        public WasmExecutor(WasmModule module, byte[] memory, int[] globals)
        {
            _module = module;
            _memory = memory;
            _globals = globals;
        }

        public object execute(int funcIndex)
        {
            var stack = new Stack<int>();
            var callStack = new Stack<WasmFrame>();
            var funcTypeIndex = _module.function_types[funcIndex];
            var funcType = _module.types[funcTypeIndex];
            var body = _module.code_bodies[funcIndex];

            var locals = new int[funcType.parameters.Length + body.locals.Count];
            var ip = 0;
            var instructions = body.instructions;

            while (ip < instructions.Count)
            {
                var opcode = instructions[ip++];

                switch (opcode)
                {
                    case 0x00:
                        throw new InvalidOperationException("WASM 执行遇到 unreachable 指令");

                    case 0x01:
                        break;

                    case 0x02:
                    {
                        var blockType = read_block_type(instructions, ref ip);
                        var blockEnd = find_block_end(instructions, ip);
                        push_block_frame(callStack, ip, blockEnd, stack.Count, 0);
                        break;
                    }

                    case 0x03:
                    {
                        var blockType = read_block_type(instructions, ref ip);
                        var blockEnd = find_block_end(instructions, ip);
                        push_block_frame(callStack, ip, blockEnd, stack.Count, 1);
                        break;
                    }

                    case 0x04:
                    {
                        var blockType = read_block_type(instructions, ref ip);
                        var condition = stack.Pop();
                        var elsePos = find_else_position(instructions, ip);
                        var blockEnd = find_block_end(instructions, ip);

                        if (condition != 0)
                        {
                            push_block_frame(callStack, ip, blockEnd, stack.Count, 0);
                        }
                        else if (elsePos >= 0)
                        {
                            ip = elsePos + 1;
                        }
                        else
                        {
                            ip = blockEnd;
                        }

                        break;
                    }

                    case 0x05:
                        ip = find_block_end(instructions, ip);
                        break;

                    case 0x0B:
                        if (callStack.Count > 0)
                        {
                            var frame = callStack.Pop();
                            while (stack.Count > frame.stack_height)
                            {
                                stack.Pop();
                            }

                            ip = frame.return_address;
                        }

                        break;

                    case 0x0C:
                    {
                        var labelIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        ip = resolve_br_target(callStack, labelIdx);
                        break;
                    }

                    case 0x0D:
                    {
                        var labelIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var cond = stack.Pop();
                        if (cond != 0)
                        {
                            ip = resolve_br_target(callStack, labelIdx);
                        }

                        break;
                    }

                    case 0x0F:
                        if (stack.Count > 0)
                        {
                            return stack.Pop();
                        }

                        return 0;

                    case 0x10:
                    {
                        var callIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var result = execute_call(callIdx, stack);
                        if (result is int intResult)
                        {
                            stack.Push(intResult);
                        }

                        break;
                    }

                    case 0x1A:
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }

                        break;

                    case 0x1B:
                    {
                        var c = stack.Pop();
                        var v2 = stack.Pop();
                        var v1 = stack.Pop();
                        stack.Push(c != 0 ? v1 : v2);
                        break;
                    }

                    case 0x20:
                    {
                        var localIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        stack.Push(locals[localIdx]);
                        break;
                    }

                    case 0x21:
                    {
                        var localIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        locals[localIdx] = stack.Pop();
                        break;
                    }

                    case 0x22:
                    {
                        var localIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var value = stack.Peek();
                        locals[localIdx] = value;
                        break;
                    }

                    case 0x23:
                    {
                        var globalIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        if (globalIdx < _globals.Length)
                        {
                            stack.Push(_globals[globalIdx]);
                        }
                        else
                        {
                            stack.Push(0);
                        }

                        break;
                    }

                    case 0x24:
                    {
                        var globalIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        if (globalIdx < _globals.Length)
                        {
                            _globals[globalIdx] = stack.Pop();
                        }

                        break;
                    }

                    case 0x28:
                    {
                        var align = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var offset = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var addr = stack.Pop() + offset;
                        var value = BitConverter.ToInt32(_memory, addr);
                        stack.Push(value);
                        break;
                    }

                    case 0x36:
                    {
                        var align = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var offset = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var value = stack.Pop();
                        var addr = stack.Pop() + offset;
                        var valueBytes = BitConverter.GetBytes(value);
                        Array.Copy(valueBytes, 0, _memory, addr, 4);
                        break;
                    }

                    case 0x3F:
                        stack.Push(0);
                        stack.Push(_module.memory_pages);
                        break;

                    case 0x40:
                        stack.Push(0);
                        var growPages = stack.Pop();
                        var oldPages = _module.memory_pages;
                        _module.memory_pages += growPages;
                        var newMemory = new byte[_module.memory_pages * 65536];
                        Array.Copy(_memory, newMemory, oldPages * 65536);
                        stack.Push(oldPages);
                        break;

                    case 0x41:
                        var constValue = read_leb128_i32_in_instructions(instructions, ref ip);
                        stack.Push(constValue);
                        break;

                    case 0x45:
                        stack.Push(stack.Pop() == 0 ? 1 : 0);
                        break;

                    case 0x46:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a == b ? 1 : 0);
                        break;
                    }

                    case 0x47:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a != b ? 1 : 0);
                        break;
                    }

                    case 0x48:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a < b ? 1 : 0);
                        break;
                    }

                    case 0x4A:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a > b ? 1 : 0);
                        break;
                    }

                    case 0x4C:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a <= b ? 1 : 0);
                        break;
                    }

                    case 0x4E:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a >= b ? 1 : 0);
                        break;
                    }

                    case 0x6A:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a + b);
                        break;
                    }

                    case 0x6B:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a - b);
                        break;
                    }

                    case 0x6C:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a * b);
                        break;
                    }

                    case 0x6D:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(b != 0 ? a / b : 0);
                        break;
                    }

                    case 0x6F:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(b != 0 ? a % b : 0);
                        break;
                    }

                    case 0x71:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a & b);
                        break;
                    }

                    case 0x72:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a | b);
                        break;
                    }

                    default:
                        throw new InvalidOperationException($"不支持的 WASM 操作码：0x{opcode:X2}（位置 {ip - 1}）");
                }
            }

            if (stack.Count > 0)
            {
                return stack.Pop();
            }

            return 0;
        }

        private object execute_call(int funcIndex, Stack<int> callerStack)
        {
            var funcTypeIndex = _module.function_types[funcIndex];
            var funcType = _module.types[funcTypeIndex];
            var body = _module.code_bodies[funcIndex];

            var locals = new int[funcType.parameters.Length + body.locals.Count];

            for (var i = funcType.parameters.Length - 1; i >= 0; i--)
            {
                locals[i] = callerStack.Pop();
            }

            var innerStack = new Stack<int>();
            var callStack = new Stack<WasmFrame>();
            var ip = 0;
            var instructions = body.instructions;

            while (ip < instructions.Count)
            {
                var opcode = instructions[ip++];

                switch (opcode)
                {
                    case 0x00:
                        throw new InvalidOperationException("WASM 执行遇到 unreachable 指令");

                    case 0x01:
                        break;

                    case 0x02:
                    {
                        var blockType = read_block_type(instructions, ref ip);
                        var blockEnd = find_block_end(instructions, ip);
                        push_block_frame(callStack, ip, blockEnd, innerStack.Count, 0);
                        break;
                    }

                    case 0x03:
                    {
                        var blockType = read_block_type(instructions, ref ip);
                        var blockEnd = find_block_end(instructions, ip);
                        push_block_frame(callStack, ip, blockEnd, innerStack.Count, 1);
                        break;
                    }

                    case 0x04:
                    {
                        var blockType = read_block_type(instructions, ref ip);
                        var condition = innerStack.Pop();
                        var elsePos = find_else_position(instructions, ip);
                        var blockEnd = find_block_end(instructions, ip);

                        if (condition != 0)
                        {
                            push_block_frame(callStack, ip, blockEnd, innerStack.Count, 0);
                        }
                        else if (elsePos >= 0)
                        {
                            ip = elsePos + 1;
                        }
                        else
                        {
                            ip = blockEnd;
                        }

                        break;
                    }

                    case 0x05:
                        ip = find_block_end(instructions, ip);
                        break;

                    case 0x0B:
                        if (callStack.Count > 0)
                        {
                            var frame = callStack.Pop();
                            while (innerStack.Count > frame.stack_height)
                            {
                                innerStack.Pop();
                            }

                            ip = frame.return_address;
                        }

                        break;

                    case 0x0C:
                    {
                        var labelIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        ip = resolve_br_target(callStack, labelIdx);
                        break;
                    }

                    case 0x0D:
                    {
                        var labelIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var cond = innerStack.Pop();
                        if (cond != 0)
                        {
                            ip = resolve_br_target(callStack, labelIdx);
                        }

                        break;
                    }

                    case 0x0F:
                        if (funcType.results.Length > 0 && innerStack.Count > 0)
                        {
                            return innerStack.Pop();
                        }

                        return 0;

                    case 0x10:
                    {
                        var callIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var result = execute_call(callIdx, innerStack);
                        if (result is int intResult)
                        {
                            innerStack.Push(intResult);
                        }

                        break;
                    }

                    case 0x1A:
                        if (innerStack.Count > 0)
                        {
                            innerStack.Pop();
                        }

                        break;

                    case 0x1B:
                    {
                        var c = innerStack.Pop();
                        var v2 = innerStack.Pop();
                        var v1 = innerStack.Pop();
                        innerStack.Push(c != 0 ? v1 : v2);
                        break;
                    }

                    case 0x20:
                    {
                        var localIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        innerStack.Push(locals[localIdx]);
                        break;
                    }

                    case 0x21:
                    {
                        var localIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        locals[localIdx] = innerStack.Pop();
                        break;
                    }

                    case 0x22:
                    {
                        var localIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var value = innerStack.Peek();
                        locals[localIdx] = value;
                        break;
                    }

                    case 0x23:
                    {
                        var globalIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        if (globalIdx < _globals.Length)
                        {
                            innerStack.Push(_globals[globalIdx]);
                        }
                        else
                        {
                            innerStack.Push(0);
                        }

                        break;
                    }

                    case 0x24:
                    {
                        var globalIdx = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        if (globalIdx < _globals.Length)
                        {
                            _globals[globalIdx] = innerStack.Pop();
                        }

                        break;
                    }

                    case 0x28:
                    {
                        var align = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var offset = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var addr = innerStack.Pop() + offset;
                        var value = BitConverter.ToInt32(_memory, addr);
                        innerStack.Push(value);
                        break;
                    }

                    case 0x36:
                    {
                        var align = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var offset = (int)read_leb128_u32_in_instructions(instructions, ref ip);
                        var value = innerStack.Pop();
                        var addr = innerStack.Pop() + offset;
                        var valueBytes = BitConverter.GetBytes(value);
                        Array.Copy(valueBytes, 0, _memory, addr, 4);
                        break;
                    }

                    case 0x3F:
                        innerStack.Push(0);
                        innerStack.Push(_module.memory_pages);
                        break;

                    case 0x40:
                        innerStack.Push(0);
                        var growPages = innerStack.Pop();
                        var oldPages = _module.memory_pages;
                        _module.memory_pages += growPages;
                        var newMemory = new byte[_module.memory_pages * 65536];
                        Array.Copy(_memory, newMemory, oldPages * 65536);
                        innerStack.Push(oldPages);
                        break;

                    case 0x41:
                        var constValue = read_leb128_i32_in_instructions(instructions, ref ip);
                        innerStack.Push(constValue);
                        break;

                    case 0x45:
                        innerStack.Push(innerStack.Pop() == 0 ? 1 : 0);
                        break;

                    case 0x46:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a == b ? 1 : 0);
                        break;
                    }

                    case 0x47:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a != b ? 1 : 0);
                        break;
                    }

                    case 0x48:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a < b ? 1 : 0);
                        break;
                    }

                    case 0x4A:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a > b ? 1 : 0);
                        break;
                    }

                    case 0x4C:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a <= b ? 1 : 0);
                        break;
                    }

                    case 0x4E:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a >= b ? 1 : 0);
                        break;
                    }

                    case 0x6A:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a + b);
                        break;
                    }

                    case 0x6B:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a - b);
                        break;
                    }

                    case 0x6C:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a * b);
                        break;
                    }

                    case 0x6D:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(b != 0 ? a / b : 0);
                        break;
                    }

                    case 0x6F:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(b != 0 ? a % b : 0);
                        break;
                    }

                    case 0x71:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a & b);
                        break;
                    }

                    case 0x72:
                    {
                        var b = innerStack.Pop();
                        var a = innerStack.Pop();
                        innerStack.Push(a | b);
                        break;
                    }

                    default:
                        throw new InvalidOperationException($"不支持的 WASM 操作码：0x{opcode:X2}（位置 {ip - 1}）");
                }
            }

            if (funcType.results.Length > 0 && innerStack.Count > 0)
            {
                return innerStack.Pop();
            }

            return 0;
        }

        private static void push_block_frame(Stack<WasmFrame> frames, int returnAddress, int blockEnd,
            int stackHeight, int kind)
        {
            frames.Push(new WasmFrame(returnAddress, blockEnd, stackHeight, kind));
        }

        private static int resolve_br_target(Stack<WasmFrame> frames, int labelIdx)
        {
            var framesArray = frames.ToArray();
            if (labelIdx < framesArray.Length)
            {
                return framesArray[labelIdx].block_end;
            }

            return 0;
        }

        private static int find_block_end(List<byte> instructions, int startPos)
        {
            var depth = 0;
            for (var i = startPos; i < instructions.Count; i++)
            {
                var op = instructions[i];
                if (op is 0x02 or 0x03 or 0x04)
                {
                    depth++;
                    if (op == 0x04)
                    {
                        skip_if_block_type(instructions, ref i);
                    }
                    else
                    {
                        skip_block_type(instructions, ref i);
                    }
                }
                else if (op == 0x05)
                {
                }
                else if (op == 0x0B)
                {
                    if (depth == 0)
                    {
                        return i;
                    }

                    depth--;
                }
            }

            return instructions.Count;
        }

        private static int find_else_position(List<byte> instructions, int startPos)
        {
            var depth = 0;
            for (var i = startPos; i < instructions.Count; i++)
            {
                var op = instructions[i];
                if (op is 0x02 or 0x03 or 0x04)
                {
                    depth++;
                    if (op == 0x04)
                    {
                        skip_if_block_type(instructions, ref i);
                    }
                    else
                    {
                        skip_block_type(instructions, ref i);
                    }
                }
                else if (op == 0x05 && depth == 0)
                {
                    return i;
                }
                else if (op == 0x0B)
                {
                    depth--;
                    if (depth < 0)
                    {
                        return -1;
                    }
                }
            }

            return -1;
        }

        private static void skip_block_type(List<byte> instructions, ref int pos)
        {
            if (pos >= instructions.Count)
            {
                return;
            }

            var bt = instructions[pos++];
            if (bt == 0x40)
            {
                return;
            }
        }

        private static void skip_if_block_type(List<byte> instructions, ref int pos)
        {
            if (pos >= instructions.Count)
            {
                return;
            }

            var bt = instructions[pos++];
            if (bt == 0x40)
            {
                return;
            }
        }

        private static byte read_block_type(List<byte> instructions, ref int pos)
        {
            if (pos >= instructions.Count)
            {
                return 0x40;
            }

            return instructions[pos++];
        }

        private static uint read_leb128_u32_in_instructions(List<byte> instructions, ref int pos)
        {
            uint result = 0;
            var shift = 0;

            while (pos < instructions.Count)
            {
                var b = instructions[pos++];
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }

                shift += 7;
            }

            return result;
        }

        private static int read_leb128_i32_in_instructions(List<byte> instructions, ref int pos)
        {
            var result = 0;
            var shift = 0;

            while (pos < instructions.Count)
            {
                var b = instructions[pos++];
                result |= (b & 0x7F) << shift;
                shift += 7;
                if ((b & 0x80) == 0)
                {
                    if (shift < 32 && (b & 0x40) != 0)
                    {
                        result |= -(1 << shift);
                    }

                    return result;
                }
            }

            return result;
        }

        private readonly record struct WasmFrame(int return_address, int block_end, int stack_height, int kind);
    }

    #endregion
}