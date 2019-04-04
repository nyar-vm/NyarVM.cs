using Std.Data.Binary.Jvm.Data;

namespace Std.Data.Binary.Jvm.Scanner;

/// <summary>
///     JVM 字节码分析器，计算精确最大栈深度并构建控制流的
/// </summary>
public static class JvmCodeAnalyzer
{
    /// <summary>
    ///     分析 JVM Code 属性的字节码，返回栈深度计算和 CFG 分析结果
    /// </summary>
    /// <param name="code">
    ///     原始字节码数的/param>
    ///     <param name="exceptionTable">
    ///         异常的/param>
    ///         <param name="declaredMaxStack">声明的max_stack。</param>
    ///         <param name="declaredMaxLocals">声明的max_locals。</param>
    public static JvmCodeAnalysis analyze(
        byte[] code,
        IReadOnlyList<JvmExceptionTableEntry> exceptionTable,
        ushort declaredMaxStack,
        ushort declaredMaxLocals)
    {
        var instructions = decode_instructions(code);
        var leaders = find_leaders(instructions, exceptionTable);
        var blocks = build_basic_blocks(instructions, leaders, exceptionTable);
        compute_stack_depths(blocks, instructions);

        var maxStack = 0;
        foreach (var block in blocks)
        {
            var peak = block.entry_stack_depth + block.max_stack_depth_delta;
            if (peak > maxStack) maxStack = peak;
        }

        var handlers = new List<JvmExceptionHandler>();
        for (var i = 0; i < exceptionTable.Count; i++)
        {
            var et = exceptionTable[i];
            var handlerBlock = -1;
            for (var b = 0; b < blocks.Count; b++)
                if (blocks[b].start_offset == et.handler_pc)
                {
                    handlerBlock = b;
                    break;
                }

            handlers.Add(new JvmExceptionHandler
            {
                start_pc = et.start_pc,
                end_pc = et.end_pc,
                handler_pc = et.handler_pc,
                catch_type = et.catch_type,
                handler_block_index = handlerBlock
            });
        }

        return new JvmCodeAnalysis
        {
            declared_max_stack = declaredMaxStack,
            computed_max_stack = maxStack,
            declared_max_locals = declaredMaxLocals,
            basic_blocks = blocks,
            exception_handlers = handlers,
            instruction_count = instructions.Count
        };
    }

    #region 获取分支目标

    /// <summary>
    ///     获取指令的所有分支目标绝对偏的
    /// </summary>
    private static List<int> get_branch_targets(JvmDecodedInstruction inst)
    {
        var targets = new List<int>();

        switch (inst.opcode)
        {
            case >= 153 and <= 166:
            case 167:
            case 168:
            case 198:
            case 199:
                targets.Add(inst.offset + inst.operand);
                break;

            case 200:
            case 201:
                targets.Add(inst.offset + inst.operand);
                break;

            case 170:
            case 171:
                if (inst.switch_targets is not null) targets.AddRange(inst.switch_targets);

                break;
        }

        return targets;
    }

    #endregion

    #region 指令解码

    /// <summary>
    ///     解码全部指令，确定每个指令的偏移、大小和分支目标
    /// </summary>
    private static List<JvmDecodedInstruction> decode_instructions(byte[] code)
    {
        var result = new List<JvmDecodedInstruction>();
        var offset = 0;

        while (offset < code.Length)
        {
            var opcode = code[offset];
            var (operand, size, switchTargets) = DecodeOneInstruction(code, offset, opcode);

            result.Add(new JvmDecodedInstruction
            {
                offset = offset,
                opcode = opcode,
                operand = operand,
                size = size,
                switch_targets = switchTargets
            });

            offset += size;
        }

        return result;
    }

    /// <summary>
    ///     解码单条指令，返的(操作的 总字节数, switch目标列表)
    /// </summary>
    private static (int Operand, int Size, IReadOnlyList<int>? SwitchTargets) DecodeOneInstruction(
        byte[] code, int offset, byte opcode)
    {
        if (is_zero_operand_opcode(opcode)) return (0, 1, null);

        switch (opcode)
        {
            case 16:
                return (code[offset + 1], 2, null);

            case 17:
                return ((short)((code[offset + 1] << 8) | code[offset + 2]), 3, null);

            case 18:
                return (code[offset + 1], 2, null);

            case 19:
            case 20:
                return ((code[offset + 1] << 8) | code[offset + 2], 3, null);

            case 21:
            case 22:
            case 23:
            case 24:
            case 25:
                return (code[offset + 1], 2, null);

            case 54:
            case 55:
            case 56:
            case 57:
            case 58:
                return (code[offset + 1], 2, null);

            case 132:
                return ((code[offset + 1] << 8) | code[offset + 2], 3, null);

            case 153:
            case 154:
            case 155:
            case 156:
            case 157:
            case 158:
            case 159:
            case 160:
            case 161:
            case 162:
            case 163:
            case 164:
            case 165:
            case 166:
            {
                var branchOffset = (short)((code[offset + 1] << 8) | code[offset + 2]);
                return (branchOffset, 3, null);
            }

            case 167:
            case 168:
            {
                var branchOffset = (short)((code[offset + 1] << 8) | code[offset + 2]);
                return (branchOffset, 3, null);
            }

            case 169:
                return (code[offset + 1], 2, null);

            case 170:
                return decode_table_switch(code, offset);

            case 171:
                return decode_lookup_switch(code, offset);

            case 178:
            case 179:
            case 180:
            case 181:
            case 182:
            case 183:
            case 184:
            case 187:
            case 189:
            case 192:
            case 193:
                return ((code[offset + 1] << 8) | code[offset + 2], 3, null);

            case 185:
            case 186:
                return ((code[offset + 1] << 8) | code[offset + 2], 5, null);

            case 188:
                return (code[offset + 1], 2, null);

            case 196:
                return decode_wide(code, offset);

            case 197:
                return ((code[offset + 1] << 8) | code[offset + 2], 4, null);

            case 198:
            case 199:
            {
                var branchOffset = (short)((code[offset + 1] << 8) | code[offset + 2]);
                return (branchOffset, 3, null);
            }

            case 200:
            case 201:
            {
                var branchOffset = (code[offset + 1] << 24) | (code[offset + 2] << 16) | (code[offset + 3] << 8) |
                                   code[offset + 4];
                return (branchOffset, 5, null);
            }

            case 254:
            case 255:
                return (0, 1, null);

            default:
                return (0, 1, null);
        }
    }

    /// <summary>
    ///     解码 tableswitch 指令
    /// </summary>
    private static (int Operand, int Size, IReadOnlyList<int>? SwitchTargets) decode_table_switch(
        byte[] code, int offset)
    {
        var instructionOffset = offset;
        var pad = (4 - (offset + 1) % 4) % 4;
        var aligned = offset + 1 + pad;

        if (aligned + 12 > code.Length) return (0, 1, null);

        var defaultOffset = read_int32_be(code, aligned);
        var low = read_int32_be(code, aligned + 4);
        var high = read_int32_be(code, aligned + 8);
        var jumpCount = high - low + 1;
        if (jumpCount < 0) jumpCount = 0;

        var totalSize = 1 + pad + 12 + jumpCount * 4;
        if (instructionOffset + totalSize > code.Length) totalSize = code.Length - instructionOffset;

        var targets = new List<int>();
        targets.Add(instructionOffset + defaultOffset);
        for (var j = 0; j < jumpCount; j++)
        {
            var jumpAddr = aligned + 12 + j * 4;
            if (jumpAddr + 4 <= code.Length)
            {
                var targetOffset = read_int32_be(code, jumpAddr);
                targets.Add(instructionOffset + targetOffset);
            }
        }

        return (defaultOffset, totalSize, targets);
    }

    /// <summary>
    ///     解码 lookupswitch 指令
    /// </summary>
    private static (int Operand, int Size, IReadOnlyList<int>? SwitchTargets) decode_lookup_switch(
        byte[] code, int offset)
    {
        var instructionOffset = offset;
        var pad = (4 - (offset + 1) % 4) % 4;
        var aligned = offset + 1 + pad;

        if (aligned + 8 > code.Length) return (0, 1, null);

        var defaultOffset = read_int32_be(code, aligned);
        var npairs = read_int32_be(code, aligned + 4);

        var totalSize = 1 + pad + 8 + npairs * 8;
        if (instructionOffset + totalSize > code.Length) totalSize = code.Length - instructionOffset;

        var targets = new List<int>();
        targets.Add(instructionOffset + defaultOffset);
        for (var j = 0; j < npairs; j++)
        {
            var jumpAddr = aligned + 8 + j * 8 + 4;
            if (jumpAddr + 4 <= code.Length)
            {
                var targetOffset = read_int32_be(code, jumpAddr);
                targets.Add(instructionOffset + targetOffset);
            }
        }

        return (defaultOffset, totalSize, targets);
    }

    /// <summary>
    ///     解码 wide 指令
    /// </summary>
    private static (int Operand, int Size, IReadOnlyList<int>? SwitchTargets) decode_wide(
        byte[] code, int offset)
    {
        if (offset + 1 >= code.Length) return (0, 1, null);

        var subOpcode = code[offset + 1];
        if (subOpcode == 132) return ((code[offset + 2] << 8) | code[offset + 3], 6, null);

        return ((code[offset + 2] << 8) | code[offset + 3], 4, null);
    }

    /// <summary>
    ///     大端序读的int32
    /// </summary>
    private static int read_int32_be(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    #endregion

    #region 基本块构的

    /// <summary>
    ///     找出所有基本块入口点（领导者）
    /// </summary>
    private static HashSet<int> find_leaders(
        List<JvmDecodedInstruction> instructions,
        IReadOnlyList<JvmExceptionTableEntry> exceptionTable)
    {
        var leaders = new HashSet<int> { 0 };

        foreach (var inst in instructions)
        {
            var nextOffset = inst.offset + inst.size;

            if (is_conditional_branch(inst.opcode) || is_switch(inst.opcode))
                if (nextOffset < int.MaxValue)
                    leaders.Add(nextOffset);

            var targets = get_branch_targets(inst);
            foreach (var target in targets) leaders.Add(target);
        }

        foreach (var et in exceptionTable) leaders.Add(et.handler_pc);

        return leaders;
    }

    /// <summary>
    ///     构建基本块列的
    /// </summary>
    private static List<JvmBasicBlock> build_basic_blocks(
        List<JvmDecodedInstruction> instructions,
        HashSet<int> leaders,
        IReadOnlyList<JvmExceptionTableEntry> exceptionTable)
    {
        var sortedLeaders = new List<int>(leaders);
        sortedLeaders.Sort();

        var blocks = new List<JvmBasicBlock>();
        for (var li = 0; li < sortedLeaders.Count; li++)
        {
            var start = sortedLeaders[li];
            var end = li + 1 < sortedLeaders.Count ? sortedLeaders[li + 1] : int.MaxValue;

            var blockStartIdx = find_instruction_index(instructions, start);
            if (blockStartIdx < 0) continue;

            var lastInst = instructions[blockStartIdx];
            for (var i = blockStartIdx; i < instructions.Count; i++)
            {
                if (instructions[i].offset >= end) break;

                lastInst = instructions[i];
            }

            var blockEnd = lastInst.offset + lastInst.size;

            var item = new JvmBasicBlock
            {
                start_offset = start,
                end_offset = blockEnd,
                is_entry = start == 0,
                is_exception_handler_entry = is_handler_entry(start, exceptionTable),
                successors = [],
                predecessors = [],
                entry_stack_depth = start == 0 ? 0 : -1,
                max_stack_depth_delta = 0
            };
            blocks.Add(item);
        }

        compute_edges(blocks, instructions, exceptionTable);

        return blocks;
    }

    /// <summary>
    ///     计算基本块之间的控制流边
    /// </summary>
    private static void compute_edges(
        List<JvmBasicBlock> blocks,
        List<JvmDecodedInstruction> instructions,
        IReadOnlyList<JvmExceptionTableEntry> exceptionTable)
    {
        var succLists = new List<int>[blocks.Count];
        var predLists = new List<int>[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
        {
            succLists[i] = [];
            predLists[i] = [];
        }

        var offsetToBlock = new Dictionary<int, int>();
        for (var i = 0; i < blocks.Count; i++) offsetToBlock[blocks[i].start_offset] = i;

        for (var bi = 0; bi < blocks.Count; bi++)
        {
            var block = blocks[bi];
            var lastInstOffset = find_last_instruction_in_block(instructions, block);
            var lastInst = instructions[find_instruction_index(instructions, lastInstOffset)];
            var nextOffset = lastInst.offset + lastInst.size;

            if (is_unconditional_branch(lastInst.opcode))
            {
                if (lastInst.opcode is 172 or 173 or 174 or 175 or 176 or 177 or 191) continue;

                var targets = get_branch_targets(lastInst);
                foreach (var target in targets)
                    if (offsetToBlock.TryGetValue(target, out var ti))
                    {
                        succLists[bi].Add(ti);
                        predLists[ti].Add(bi);
                    }

                continue;
            }

            if (is_conditional_branch(lastInst.opcode) || is_switch(lastInst.opcode))
            {
                var targets = get_branch_targets(lastInst);
                foreach (var target in targets)
                    if (offsetToBlock.TryGetValue(target, out var ti))
                    {
                        succLists[bi].Add(ti);
                        predLists[ti].Add(bi);
                    }

                if (offsetToBlock.TryGetValue(nextOffset, out var fallThrough))
                {
                    succLists[bi].Add(fallThrough);
                    predLists[fallThrough].Add(bi);
                }
            }
            else
            {
                if (offsetToBlock.TryGetValue(nextOffset, out var fallThrough))
                {
                    succLists[bi].Add(fallThrough);
                    predLists[fallThrough].Add(bi);
                }
            }

            for (var ei = 0; ei < exceptionTable.Count; ei++)
            {
                var et = exceptionTable[ei];
                if (block.start_offset >= et.start_pc && block.end_offset <= et.end_pc)
                    if (offsetToBlock.TryGetValue(et.handler_pc, out var hi))
                    {
                        succLists[bi].Add(hi);
                        predLists[hi].Add(bi);
                    }
            }
        }

        for (var i = 0; i < blocks.Count; i++)
        {
            blocks[i].successors = distinct_and_sort(succLists[i]);
            blocks[i].predecessors = distinct_and_sort(predLists[i]);
        }
    }

    #endregion

    #region 栈深度计的

    /// <summary>
    ///     通过 CFG 不动点迭代传播栈深度，计算精确最大栈深度
    /// </summary>
    private static void compute_stack_depths(
        List<JvmBasicBlock> blocks,
        List<JvmDecodedInstruction> instructions)
    {
        var changed = true;
        var iteration = 0;
        var maxIterations = blocks.Count * 10;

        while (changed && iteration < maxIterations)
        {
            changed = false;
            iteration++;

            for (var bi = 0; bi < blocks.Count; bi++)
            {
                var block = blocks[bi];
                var predictedEntry = -1;

                if (block.is_entry)
                    predictedEntry = 0;
                else
                    foreach (var predIdx in block.predecessors)
                    {
                        var pred = blocks[predIdx];
                        var outDepth = compute_block_exit_stack(pred, instructions);
                        if (outDepth >= 0)
                        {
                            predictedEntry = outDepth;
                            break;
                        }
                    }

                if (block.is_exception_handler_entry) predictedEntry = System.Math.Max(predictedEntry, 1);

                if (predictedEntry >= 0 && predictedEntry != block.entry_stack_depth)
                {
                    block.entry_stack_depth = predictedEntry;
                    changed = true;
                }

                if (block.entry_stack_depth >= 0)
                {
                    var cached = block.max_stack_depth_delta;
                    var computed = compute_block_stack_delta(block, instructions);
                    block.max_stack_depth_delta = computed;
                    if (computed != cached) changed = true;
                }
            }
        }
    }

    /// <summary>
    ///     计算基本块出口的栈深的
    /// </summary>
    private static int compute_block_exit_stack(JvmBasicBlock block, List<JvmDecodedInstruction> instructions)
    {
        if (block.entry_stack_depth < 0) return -1;

        var depth = block.entry_stack_depth;
        for (var i = 0; i < instructions.Count; i++)
        {
            var inst = instructions[i];
            if (inst.offset < block.start_offset) continue;

            if (inst.offset >= block.end_offset) break;

            var (pops, pushes) = get_stack_effect(inst);
            depth -= pops;
            if (depth < 0) return -1;

            depth += pushes;
        }

        return depth;
    }

    /// <summary>
    ///     计算基本块内的最大栈深度（相对入口）
    /// </summary>
    private static int compute_block_stack_delta(
        JvmBasicBlock block, List<JvmDecodedInstruction> instructions)
    {
        var depth = 0;
        var maxDepth = 0;

        for (var i = 0; i < instructions.Count; i++)
        {
            var inst = instructions[i];
            if (inst.offset < block.start_offset) continue;

            if (inst.offset >= block.end_offset) break;

            var (pops, pushes) = get_stack_effect(inst);
            depth -= pops;
            depth += pushes;
            if (depth > maxDepth) maxDepth = depth;
        }

        return maxDepth;
    }

    /// <summary>
    ///     获取单条指令的栈效果 (弹出槽数, 压入槽数)
    /// </summary>
    /// <remarks>
    ///     long/double 类型的2 个槽位（因此 lload 压入 2，lstore 弹出 2）的
    ///     invoke* 指令的精确参数数量需要方法描述符，此处使用简化估算的
    /// </remarks>
    private static (int Pops, int Pushes) get_stack_effect(JvmDecodedInstruction inst)
    {
        return inst.opcode switch
        {
            // NOP
            0 => (0, 0),

            // 常量 push 1
            1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 => (0, 1),

            // bipush / sipush: push 1
            16 or 17 => (0, 1),

            // ldc: push 1
            18 or 19 => (0, 1),

            // ldc2_w: push 2 (long/double = 2 的
            20 => (0, 2),

            // iload / fload / aload: push 1
            21 or 23 or 25 => (0, 1),

            // lload / dload: push 2
            22 or 24 => (0, 2),

            // iload_0..3
            26 or 27 or 28 or 29 or 42 or 43 or 44 or 45 => (0, 1),

            // lload_0..3: push 2
            30 or 31 or 32 or 33 => (0, 2),

            // fload_0..3: push 1
            34 or 35 or 36 or 37 => (0, 1),

            // dload_0..3: push 2
            38 or 39 or 40 or 41 => (0, 2),

            // iaload: pop 2 (arrayref, index), push 1
            46 or 48 or 50 or 51 or 52 or 53 => (2, 1),

            // laload / daload: pop 2, push 2
            47 or 49 => (2, 2),

            // istore: pop 1
            54 or 56 or 58 => (1, 0),

            // lstore / dstore: pop 2
            55 or 57 => (2, 0),

            // istore_0..3: pop 1
            59 or 60 or 61 or 62 or 75 or 76 or 77 or 78 => (1, 0),

            // lstore_0..3: pop 2
            63 or 64 or 65 or 66 => (2, 0),

            // fstore_0..3: pop 1
            67 or 68 or 69 or 70 => (1, 0),

            // dstore_0..3: pop 2
            71 or 72 or 73 or 74 => (2, 0),

            // iastore: pop 3
            79 or 81 or 83 or 84 or 85 or 86 => (3, 0),

            // lastore / dastore: pop 4
            80 or 82 => (4, 0),

            // pop / pop2
            87 => (1, 0),
            88 => (2, 0),

            // dup / dup_x1 / dup2 / dup2_x1
            89 => (1, 2),
            90 => (2, 3),
            91 => (2, 3),
            92 => (2, 4),
            93 => (3, 5),
            94 => (3, 5),

            // swap
            95 => (2, 2),

            // 整数算术: pop 2, push 1
            96 or 100 or 104 or 108 or 112 or 120 or 122 or 124 or 126 or 128 or 130 => (2, 1),

            // 长整数算的 pop 4, push 2
            97 or 101 or 105 or 109 or 113 or 121 or 123 or 125 or 127 or 129 or 131 => (4, 2),

            // 浮点算术
            98 or 106 or 110 or 114 => (2, 1),
            99 or 107 or 111 or 115 => (4, 2),

            // 取负
            116 => (1, 1),
            117 => (2, 2),
            118 => (1, 1),
            119 => (2, 2),

            // iinc: 0
            132 => (0, 0),

            // 类型转换（含宽化/窄化的            133 => (1, 2),
            134 => (1, 1),
            135 => (1, 2),
            136 => (2, 1),
            137 => (2, 1),
            138 => (2, 2),
            139 => (1, 1),
            140 => (1, 2),
            141 => (1, 2),
            142 => (2, 1),
            143 => (2, 2),
            144 => (2, 1),
            145 => (1, 1),
            146 => (1, 1),
            147 => (1, 1),

            // 比较
            148 => (4, 1),
            149 or 150 => (2, 1),
            151 or 152 => (4, 1),

            // 条件跳转: pop 1
            153 or 154 or 155 or 156 or 157 or 158 or 198 or 199 => (1, 0),

            // if_icmpXX / if_acmpXX: pop 2
            159 or 160 or 161 or 162 or 163 or 164 or 165 or 166 => (2, 0),

            // goto: 0
            167 or 200 => (0, 0),

            // jsr / jsr_w: push returnAddress
            168 or 201 => (0, 1),

            // ret: 0
            169 => (0, 0),

            // tableswitch / lookupswitch: pop 1
            170 or 171 => (1, 0),

            // return
            172 or 174 or 176 => (1, 0),
            173 or 175 => (2, 0),
            177 => (0, 0),

            // getstatic: push 1
            178 => (0, 1),

            // putstatic: pop 1/2（需描述符确定类型宽度，简化为 pop 1的            179 => (1, 0),

            // getfield: pop objectref, push 1
            180 => (1, 1),

            // putfield: pop objectref + value（简化为 pop 2的            181 => (2, 0),

            // invokevirtual / invokespecial: pop objectref + args，push result
            // 简化为 pop objectref，忽的args 的result
            182 => (1, 0),
            183 => (1, 0),

            // invokestatic: pop args, push result（简化）
            184 => (0, 0),

            // invokeinterface: pop objectref + args, push result（简化）
            185 => (1, 0),

            // invokedynamic: varargs（简化）
            186 => (0, 0),

            // new: push objectref
            187 => (0, 1),

            // newarray: pop count, push arrayref
            188 => (1, 1),

            // anewarray: pop count, push arrayref
            189 => (1, 1),

            // arraylength: pop arrayref, push length
            190 => (1, 1),

            // athrow: pop exception ref
            191 => (1, 0),

            // checkcast: (objectref) -> objectref
            192 => (1, 1),

            // instanceof: objectref -> int
            193 => (1, 1),

            // monitorenter / monitorexit: pop objectref
            194 or 195 => (1, 0),

            // wide: pass-through to sub-instruction（简化）
            196 => (0, 0),

            // multianewarray: pop dimensions count, push arrayref
            197 => (inst.operand & 0xFF, 1),

            // breakpoint
            202 => (0, 0),

            // impdep1 / impdep2
            254 or 255 => (0, 0),

            _ => (0, 0)
        };
    }

    #endregion

    #region 辅助方法

    private static int find_instruction_index(List<JvmDecodedInstruction> instructions, int offset)
    {
        for (var i = 0; i < instructions.Count; i++)
            if (instructions[i].offset == offset)
                return i;

        return -1;
    }

    private static int find_last_instruction_in_block(
        List<JvmDecodedInstruction> instructions, JvmBasicBlock block)
    {
        var last = -1;
        for (var i = 0; i < instructions.Count; i++)
        {
            var inst = instructions[i];
            if (inst.offset >= block.start_offset && inst.offset < block.end_offset) last = inst.offset;
        }

        return last;
    }

    private static bool is_unconditional_branch(byte opcode)
    {
        return opcode is 167 or 172 or 173 or 174 or 175 or 176 or 177 or 191 or 200;
    }

    private static bool is_conditional_branch(byte opcode)
    {
        return opcode is >= 153 and <= 166 or 198 or 199;
    }

    private static bool is_switch(byte opcode)
    {
        return opcode is 170 or 171;
    }

    private static bool is_handler_entry(
        int offset, IReadOnlyList<JvmExceptionTableEntry> exceptionTable)
    {
        foreach (var et in exceptionTable)
            if (et.handler_pc == offset)
                return true;

        return false;
    }

    /// <summary>
    ///     判断操作码是否为零操作数指令（仅 1 字节操作码，无操作数的
    /// </summary>
    private static bool is_zero_operand_opcode(byte opcode)
    {
        return opcode switch
        {
            <= 15 => true,
            >= 26 and <= 53 => true,
            >= 59 and <= 131 => true,
            >= 133 and <= 152 => true,
            172 or 173 or 174 or 175 or 176 or 177 => true,
            190 or 191 or 194 or 195 => true,
            202 => true,
            254 or 255 => true,
            _ => false
        };
    }

    private static IReadOnlyList<int> distinct_and_sort(List<int> list)
    {
        var set = new HashSet<int>();
        foreach (var v in list) set.Add(v);

        var sorted = new List<int>(set);
        sorted.Sort();
        return sorted;
    }

    #endregion
}