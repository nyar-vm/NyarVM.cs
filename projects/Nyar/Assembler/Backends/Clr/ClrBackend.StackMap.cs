using System.Diagnostics;
using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler.Backends.Clr;

/// <summary>
///     CLR 鍚庣 partial锛氭爤璁＄畻鍜岃鑼冨寲鐩稿叧鏂规硶銆?///
/// </summary>
public partial class ClrBackend
{
    /// <summary>
    ///     璁＄畻 CLR 杈撳嚭鏂囦欢鍚嶏紙涓嶅惈鎵╁睍鍚嶏級銆?    ///     浼樺厛浣跨敤鍏ュ彛鍑芥暟鍚嶏紙鍘绘帀妯″潡鍓嶇紑濡?"legion.legion" 鈫?"legion"锛夛紝鍚﹀垯鐢ㄦā鍧楀悕銆?    ///
    /// </summary>
    private static string compute_output_name(string? entryFunctionName, string moduleName)
    {
        var entryName = entryFunctionName;
        if (entryName is not null)
        {
            var lastDot = entryName.LastIndexOf('.');
            if (lastDot >= 0) entryName = entryName[(lastDot + 1)..];
        }

        return sanitize_type_name(entryName ?? moduleName);
    }

    private static string sanitize_type_name(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName)) return "Module";
        var chars = moduleName.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray();
        var name = new string(chars);
        if (char.IsDigit(name[0])) name = $"M_{name}";
        return name;
    }

    /// <summary>
    ///     浠庡畬鍏ㄩ檺瀹氬嚱鏁板悕涓彁鍙栫煭鍚嶇О锛堟渶鍚庝竴涓偣涔嬪悗鐨勯儴鍒嗭級銆?    ///     渚嬪 "std.io.print" 鈫?"print"銆?    ///
    /// </summary>
    private static string get_short_function_name(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return fullName;
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName[(lastDot + 1)..] : fullName;
    }

    private static bool validate_branch_labels(GenerateModule module, ICollection<Diagnostic> diagnostics)
    {
        foreach (var function in module.functions)
        {
            var labels = function.labels.Select(label => label.name).ToHashSet(StringComparer.Ordinal);
            foreach (var instruction in function.instructions.Where(instruction =>
                         instruction.opcode is NyarHeadCode.jump or NyarHeadCode.jump_if_true
                             or NyarHeadCode.jump_if_false))
            {
                if (instruction.operands.FirstOrDefault() is not GenerateOperand.Label label)
                {
                    diagnostics.Add(new Diagnostic(
                        default,
                        $"CLR 分支指令 `{instruction.opcode}` 缺少标签操作数。",
                        DiagnosticSeverity.error));
                    return false;
                }

                if (!labels.Contains(label.name))
                {
                    diagnostics.Add(new Diagnostic(
                        default,
                        $"CLR 分支目标标签 `{label.name}` 未定义。",
                        DiagnosticSeverity.error));
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     鍒ゆ柇 CLR 鎸囦护鏄惁涓洪粯璁ゅ€兼帹鍏ワ紙ldc.i4.0銆乴dnull 绛夋棤鎰忎箟鍗犱綅鍊硷級銆?    ///     鐢ㄤ簬 void 杩斿洖瑙勮寖鍖栦腑璇嗗埆 `ldnull; pop; ret`
    ///     绛夋湁瀹虫ā寮忋€?    ///
    /// </summary>
    private static bool is_default_value_push(ClrInstruction instruction)
    {
        return instruction.opcode is ClrOpcode.ldc_i4_0 or ClrOpcode.ldc_i4_m1 or ClrOpcode.ldc_i4_1
                   or ClrOpcode.ldc_i4_2 or ClrOpcode.ldc_i4_3 or ClrOpcode.ldc_i4_4
                   or ClrOpcode.ldc_i4_5 or ClrOpcode.ldc_i4_6 or ClrOpcode.ldc_i4_7
                   or ClrOpcode.ldc_i4_8 or ClrOpcode.ldnull
               || instruction.opcode is ClrOpcode.ldc_i4_s or ClrOpcode.ldc_i4
                   or ClrOpcode.ldc_i8 or ClrOpcode.ldc_r4 or ClrOpcode.ldc_r8
                   or ClrOpcode.ldstr;
    }

    /// <summary>
    ///     判断下一条 Nyar 指令是否会立刻消费当前栈顶值。
    ///     这里只处理立即存储场景，避免把后续 `call` 误判成一定会消费当前残留值。
    /// </summary>
    private static bool has_next_consuming_instruction(GenerateFunction function, int currentIndex)
    {
        var nextIndex = currentIndex + 1;
        if (nextIndex >= function.instructions.Count) return false;
        var nextOpcode = function.instructions[nextIndex].opcode;
        if (nextOpcode is
            NyarHeadCode.store_local or
            NyarHeadCode.store_arg)
            return true;
        return false;
    }

    /// <summary>
    ///     鍒ゆ柇缁欏畾浣嶇疆鐨?const Str 鎸囦护鏄惁鏄敤浜?set_field/get_field 鐨勫瓧娈靛悕銆?    ///     LIR 涓瓧娈靛悕 const Str 鍑虹幇鍦ㄤ袱绉嶄綅缃細
    ///     1. 绱ч偦 set_field/get_field锛堝 emit_set_field銆乪mit_get_field 璺緞锛夛紱
    ///     2. lower_struct_literal 璺緞锛歞up; const Str fieldName; &lt;value-expr&gt;; set_field銆?    ///     绠楁硶锛氶€氳繃妯℃嫙浠庤
    ///     const Str 璧风殑鏍堟繁搴﹀彉鍖栵紙鎶?const Str 瑙嗕负 +1锛夛紝
    ///     瀵绘壘鎶婂畠娑堣垂鎺夌殑 set_field/get_field锛堟爤娣卞害鍒氬ソ鍥炲埌 const Str 涔嬪墠鐨勫€兼椂閬囧埌锛夈€?    ///
    /// </summary>
    private static bool is_field_name_for_set_or_get_field(GenerateFunction function, int constStrIndex)
    {
        var depth = 1;
        for (var i = constStrIndex + 1; i < function.instructions.Count; i++)
        {
            var inst = function.instructions[i];
            if (inst.opcode == NyarHeadCode.set_field)
            {
                if (depth >= 2) return true;
            }
            else if (inst.opcode == NyarHeadCode.get_field)
            {
                if (depth == 1) return true;
            }

            // 閬囧埌涓嶅彲鑳藉嚭鐜板湪瀛楁鍚嶄腑闂寸殑鎸囦护 鈫?涓
            if (inst.opcode is NyarHeadCode.jump or
                NyarHeadCode.jump_if_true or
                NyarHeadCode.jump_if_false or
                NyarHeadCode.@return)
                return false;
            var delta = compute_nyar_stack_delta(inst);
            depth += delta;
            if (depth <= 0) return false;
        }

        return false;
    }

    /// <summary>
    ///     浠?set_field 浣嶇疆鍙嶅悜鎵弿锛屽鎵惧叾娑堣垂鐨?const Str 瀛楁鍚嶃€?    ///     澶勭悊涓ょ Nyar IR 妯″紡锛?    ///     A: const Str fieldName
    ///     鈫?set_field 锛堝€煎凡鍦ㄦ爤涓婏紝瀛楁鍚嶇揣閭?set_field锛?    ///     B: const Str fieldName 鈫?&lt;value-expr&gt; 鈫?set_field
    ///     锛堝€煎湪瀛楁鍚嶅拰 set_field 涔嬮棿锛?    ///
    /// </summary>
    private static string? find_field_name_backward(GenerateFunction function, int setFieldIndex)
    {
        var idx = find_const_str_index_before_set_field(function, setFieldIndex);
        if (idx < 0) return null;
        var inst = function.instructions[idx];
        if (inst.operands.Count > 0 && inst.operands[0] is GenerateOperand.Str strOp) return strOp.value;
        return null;
    }

    /// <summary>
    ///     浠?set_field 浣嶇疆鍙嶅悜鎵弿锛屽鎵惧叾鍓嶉潰鐨?const Str 瀛楁鍚嶇殑鎸囦护绱㈠紩銆?    ///     杩斿洖 -1 琛ㄧず鏈壘鍒般€?    ///
    /// </summary>
    private static int find_const_str_index_before_set_field(GenerateFunction function, int setFieldIndex)
    {
        if (setFieldIndex <= 0) return -1;
        var prev1 = function.instructions[setFieldIndex - 1];
        // 妯″紡 B: const Str fieldName, &lt;value&gt;, set_field
        // 鍓嶄竴鏉℃寚浠ゆ槸鍊硷紙闈?const Str锛夛紝鍐嶅墠涓€鏉℃槸瀛楁鍚?const Str
        var isPrev1Str = prev1 is { opcode: NyarHeadCode.@const, operands.Count: > 0 } &&
                         prev1.operands[0] is GenerateOperand.Str;
        if (!isPrev1Str && setFieldIndex >= 2)
        {
            var prev2 = function.instructions[setFieldIndex - 2];
            if (prev2 is { opcode: NyarHeadCode.@const, operands.Count: > 0 } &&
                prev2.operands[0] is GenerateOperand.Str)
                return setFieldIndex - 2;
        }

        // 妯″紡 A: const Str fieldName, set_field
        if (isPrev1Str)
        {
            // 妯″紡 A 鍙樹綋: const Str fieldName, const Str value, set_field
            if (setFieldIndex >= 2)
            {
                var prev2 = function.instructions[setFieldIndex - 2];
                var isPrev2Str = prev2 is { opcode: NyarHeadCode.@const, operands.Count: > 0 } &&
                                 prev2.operands[0] is GenerateOperand.Str;
                if (isPrev2Str) return setFieldIndex - 2;
            }

            return setFieldIndex - 1;
        }

        // 鍓嶄竴鏉℃寚浠や笉鏄?const锛堝 load_local 绛夛級涓?setFieldIndex >= 2
        if (setFieldIndex >= 2)
        {
            var prev2 = function.instructions[setFieldIndex - 2];
            if (prev2 is { opcode: NyarHeadCode.@const, operands.Count: > 0 } &&
                prev2.operands[0] is GenerateOperand.Str)
                return setFieldIndex - 2;
        }

        return -1;
    }

    /// <summary>
    ///     浼扮畻 Nyar 鎸囦护瀵规爤娣卞害鐨勫奖鍝嶏紙浠呯敤浜庡瓧娈靛悕璇嗗埆鐨勫眬閮ㄦ壂鎻忥紝鏃犻渶绮剧‘锛夈€?    ///
    /// </summary>
    private static int compute_nyar_stack_delta(GenerateInstruction inst)
    {
        switch (inst.opcode)
        {
            case NyarHeadCode.@const:
            case NyarHeadCode.load_local:
            case NyarHeadCode.load_arg:
            case NyarHeadCode.new_object:
            case NyarHeadCode.dup:
                return 1;
            case NyarHeadCode.pop:
            case NyarHeadCode.store_local:
            case NyarHeadCode.store_arg:
            case NyarHeadCode.@return:
                return -1;
            case NyarHeadCode.set_field:
                return -3;
            case NyarHeadCode.get_field:
                return 0; // -1 obj +1 value
            case NyarHeadCode.call:
            case NyarHeadCode.call_static:
            {
                if (inst.operands.Count > 0 && inst.operands[0] is GenerateOperand.FuncRef funcRef)
                {
                    var pops = funcRef.signature.parameters.Count;
                    var pushes = funcRef.signature.results.Count;
                    if (pushes > 0 && (funcRef.signature.results[0] == GenerateValueType.@void ||
                                       funcRef.signature.results[0] == GenerateValueType.unit))
                        pushes = 0;
                    return pushes - pops;
                }

                return 0;
            }
            default:
                return 0;
        }
    }

    /// <summary>
    ///     鍒ゆ柇涓嬩竴鏉?Nyar 鎸囦护鏄惁浼氭秷璐规爤椤剁殑鍊笺€?    ///     鐢ㄤ簬 void 鏂规硶涓垽鏂潪 void 璋冪敤鐨勮繑鍥炲€兼槸鍚﹂渶瑕佽嚜鍔ㄦ彃鍏?pop銆?    ///
    ///     濡傛灉涓嬩竴鏉℃寚浠や細寮瑰嚭鑷冲皯 1 涓€硷紙濡?store_local銆佷簩鍏冭繍绠椼€佹潯浠跺垎鏀瓑锛夛紝
    ///     鍒欒繑鍥炲€间細琚秷璐癸紝涓嶉渶瑕侀澶栨彃鍏?pop銆?    ///     濡傛灉涓嬩竴鏉℃寚浠や笉寮瑰嚭浠讳綍鍊硷紙濡?nop銆乴oad_local銆丂const銆乯ump銆丂return 绛夛級锛?    ///
    ///     鍒欒繑鍥炲€兼粸鐣欏湪鏍堜笂锛岄渶瑕佹彃鍏?pop銆?    ///     娉ㄦ剰锛歝all/call_static/call_dynamic 浼氫粠鏍堜笂寮瑰嚭鍏跺弬鏁帮紙鍖呮嫭鍓嶄竴涓皟鐢ㄧ殑杩斿洖鍊硷級锛?    ///
    ///     鍥犳蹇呴』浠?涓嶆秷璐?鍒楄〃涓帓闄わ紝鍚﹀垯浼氶敊璇湴鎻掑叆 pop 瀵艰嚧鍙傛暟涓㈠け銆?    ///
    /// </summary>
    private static bool next_nyar_instruction_consumes_value(GenerateFunction function, int currentIndex)
    {
        var nextIndex = currentIndex + 1;
        if (nextIndex >= function.instructions.Count) return false;
        // 向前扫描，跳过只推入栈的指令，找到第一条可能消费返回值的指令。
        // 返回值在栈顶，只推入指令（如 const、load_local）会在其上方推入新值，
        // 返回值仍在栈中，直到遇到消费指令（如 i32_eq、store_local）才会被消费。
        // 例如 `args.length() == 0` 的 LIR 序列为：
        //   call_static length  // 推入 i32
        //   const 0             // 推入 0（不消费 length 的结果）
        //   i32_eq              // 消费 2 个值（包括 length 的结果）
        // 如果只检查 const（不消费），会错误地插入 pop 导致 i32_eq 栈下溢。
        for (var i = currentIndex + 1; i < function.instructions.Count; i++)
        {
            var opcode = function.instructions[i].opcode;
            // 调试输出：追踪 legion.legion 的扫描过程
            if (function.name == "legion.legion" && currentIndex <= 5)
                Console.WriteLine(
                    $"[DEBUG next_nyar] {function.name} currentIndex={currentIndex} scanIdx={i} opcode={opcode}");
            // 只推入或无栈效果的指令：跳过，返回值仍在栈中
            if (opcode is
                NyarHeadCode.nop or
                NyarHeadCode.load_local or NyarHeadCode.load_arg or NyarHeadCode.load_global or
                NyarHeadCode.@const or
                NyarHeadCode.dup or NyarHeadCode.swap or
                NyarHeadCode.enter_effect_handler or NyarHeadCode.exit_effect_handler or
                NyarHeadCode.enter_try or NyarHeadCode.exit_try or NyarHeadCode.resume or
                NyarHeadCode.new_object or NyarHeadCode.new_closure)
                continue;
            // 跳转和返回：返回值不会被后续线性指令消费
            if (opcode is NyarHeadCode.jump or NyarHeadCode.@return)
            {
                if (function.name == "legion.legion" && currentIndex <= 5)
                    Console.WriteLine(
                        $"[DEBUG next_nyar] {function.name} currentIndex={currentIndex} -> FALSE (jump/return at {i})");
                return false;
            }

            // 其他指令（store_local、二元运算、call 等）会消费栈值
            if (function.name == "legion.legion" && currentIndex <= 5)
                Console.WriteLine(
                    $"[DEBUG next_nyar] {function.name} currentIndex={currentIndex} -> TRUE (consuming at {i}: {opcode})");
            return true;
        }

        if (function.name == "legion.legion" && currentIndex <= 5)
            Console.WriteLine($"[DEBUG next_nyar] {function.name} currentIndex={currentIndex} -> FALSE (end of list)");
        return false;
    }

    /// <summary>
    ///     娓呯悊 void 鏂规硶涓?`榛樿鍊兼帹鍏? pop; 鍒嗘敮` 鐨勫啑浣欐ā寮忋€?    ///     match 鍒嗘敮闄嶇骇鏃讹紝`lower_statement` 鐨?default
    ///     鍒嗘敮涓?`None`/`Literal&lt;object?&gt;` 琛ㄨ揪寮?    ///     鐢熸垚 `ldnull; pop`锛屽叾鍚庣揣璺?`br`/`brfalse`/`brtrue`
    ///     璺宠浆鍒颁笅涓€鍒嗘敮銆?    ///     杩欎簺 `ldnull; pop` 瀵规眰鍊兼爤鏃犲噣鏁堟灉锛屽睘浜庡啑浣欐寚浠わ紝涓€骞剁Щ闄ゅ彲鍑忓皯 IL 浣撶Н銆?    ///
    /// </summary>
    private static void clean_void_branch_nops(List<ClrInstruction> instructions)
    {
        for (var i = 0; i < instructions.Count - 2; i++)
        {
            if (!is_default_value_push(instructions[i])) continue;
            if (instructions[i + 1].opcode != ClrOpcode.pop) continue;
            var branchOpcode = instructions[i + 2].opcode;
            if (branchOpcode is not (ClrOpcode.br or ClrOpcode.brfalse or ClrOpcode.brtrue
                or ClrOpcode.br_s or ClrOpcode.brfalse_s or ClrOpcode.brtrue_s))
                continue;
            // 灏?ldnull/ldc.i4.0 鍜?pop 鏇挎崲涓?nop锛屼繚鐣欏垎鏀寚浠?            // 浣跨敤 nop 鏇挎崲鑰岄潪绉婚櫎锛屼互淇濇寔 sourceInstructionToEmittedIndex 鏄犲皠鏈夋晥
            instructions[i] = new ClrInstruction { opcode = ClrOpcode.nop };
            instructions[i + 1] = new ClrInstruction { opcode = ClrOpcode.nop };
        }
    }

    /// <summary>
    ///     闈?void 鏂规硶鐨勮繑鍥炲€艰鑼冨寲銆?    ///     褰撴柟娉曠鍚嶅０鏄庤繑鍥為潪 void 绫诲瀷鏃讹紝姣忔潯 <c>ret</c> 鍓嶆眰鍊兼爤蹇呴』鏈夊€笺€?    ///
    ///     澶勭悊浠ヤ笅鏈夊妯″紡锛?    ///     1. <c>nop; ret</c>锛歯op 鍓嶆棤鍊间骇鐢熸寚浠?鈫?鏇挎崲 nop 涓?ldnull
    ///     2. <c>void call; nop; ret</c>锛歷oid call 涓嶆帹鍏ヨ繑鍥炲€硷紝nop 鍓嶇殑 call 琚?produces_stack_value 璇垽 鈫?鏇挎崲 nop 涓?ldnull
    ///     3. <c>void call; ret</c>锛歷oid call 涓嶆帹鍏ヨ繑鍥炲€?鈫?鍦?ret 鍓嶆彃鍏?ldnull
    ///     4. <c>pop; ret</c>锛歱op 娑堣垂浜嗗€煎悗鏍堜负绌?鈫?鍦?ret 鍓嶆彃鍏?ldnull
    ///     5. <c>stloc; ret</c>锛歴tloc 瀛樺偍浜嗗€煎悗鏍堜负绌?鈫?鍦?ret 鍓嶆彃鍏?ldnull
    /// </summary>
    private static void normalize_non_void_returns(List<ClrInstruction> instructions, HashSet<int> voidCallIndices,
        List<PendingClrBranch> pendingBranches)
    {
        // 浠庡悗鍚戝墠鎵弿锛岄伩鍏嶆彃鍏ユ寚浠ゆ椂绱㈠紩鍋忕Щ
        for (var i = instructions.Count - 1; i >= 1; i--)
        {
            if (instructions[i].opcode != ClrOpcode.ret) continue;
            var prevIndex = i - 1;
            var prev = instructions[prevIndex];
            if (prev.opcode == ClrOpcode.nop)
            {
                if (prevIndex >= 1 &&
                    produces_stack_value(instructions[prevIndex - 1], voidCallIndices, prevIndex - 1)) continue;
                instructions[prevIndex] = new ClrInstruction { opcode = ClrOpcode.ldnull };
            }
            else if (prev.opcode is ClrOpcode.call or ClrOpcode.callvirt)
            {
                // void call 涓嶄骇鐢熸爤鍊硷紝闇€瑕佸湪 ret 鍓嶆彃鍏?ldnull
                if (voidCallIndices.Contains(prevIndex))
                {
                    instructions.Insert(i, new ClrInstruction { opcode = ClrOpcode.ldnull });
                    // 鎻掑叆鍚庯紝鎵€鏈夌储寮?>= i 鐨勫垎鏀渶瑕?+1
                    shift_pending_branches(pendingBranches, i);
                }
            }
            else if (prev.opcode == ClrOpcode.pop)
            {
                // pop 娑堣垂浜嗗€煎悗鏍堜负绌猴紝闇€瑕佸湪 ret 鍓嶆彃鍏?ldnull
                instructions.Insert(i, new ClrInstruction { opcode = ClrOpcode.ldnull });
                shift_pending_branches(pendingBranches, i);
            }
            else if (is_store_local_opcode(prev.opcode))
            {
                // stloc/stloc_s/stloc.N 瀛樺偍浜嗗€煎悗鏍堜负绌猴紝闇€瑕佸湪 ret 鍓嶆彃鍏?ldnull
                instructions.Insert(i, new ClrInstruction { opcode = ClrOpcode.ldnull });
                shift_pending_branches(pendingBranches, i);
            }
        }
    }

    /// <summary>
    ///     鍦ㄦ寚浠ゅ垪琛ㄤ腑鎻掑叆鏂版寚浠ゅ悗锛屾洿鏂板緟澶勭悊鍒嗘敮璁板綍鐨勫彂灏勭储寮曘€?    ///     鎵€鏈夌储寮?&gt;= insertPosition 鐨勫垎鏀褰曢渶瑕?+count銆?    ///
    /// </summary>
    private static void shift_pending_branches(List<PendingClrBranch> pendingBranches, int insertPosition,
        int count = 1)
    {
        for (var b = 0; b < pendingBranches.Count; b++)
            if (pendingBranches[b].emitted_instruction_index >= insertPosition)
                pendingBranches[b] = pendingBranches[b] with
                {
                    emitted_instruction_index = pendingBranches[b].emitted_instruction_index + count
                };
    }

    /// <summary>
    ///     鍒ゆ柇鎿嶄綔鐮佹槸鍚︿负灞€閮ㄥ彉閲忓瓨鍌ㄦ寚浠?    ///
    /// </summary>
    private static bool is_store_local_opcode(ClrOpcode opcode)
    {
        return opcode is ClrOpcode.stloc_0 or ClrOpcode.stloc_1 or ClrOpcode.stloc_2 or ClrOpcode.stloc_3
            or ClrOpcode.stloc_s or ClrOpcode.stloc;
    }

    /// <summary>
    ///     void 鏂规硶鐨勬爤娣卞害瑙勮寖鍖栵紙瀹夊叏缃戯級銆?    ///     涓昏淇宸插湪鎸囦护鍙戝皠寰幆涓畬鎴愶紙闈?void 璋冪敤鍚庤嚜鍔ㄦ彃鍏?pop锛夈€?    ///
    ///     姝ゆ柟娉曚綔涓哄畨鍏ㄧ綉锛屾娴嬫寚浠ゅ惊鐜湭瑕嗙洊鐨勮竟缂樻儏鍐碉細
    ///     鍦?<c>ret</c> 鍓嶅瓨鍦ㄥ涓湭娑堣垂鐨勯潪 void <c>call</c>/<c>callvirt</c> 鎴栧叾浠栨帹鍏ュ€兼寚浠ゃ€?    ///     浠?<c>ret</c>
    ///     鍚戝悗鎵弿鍒版渶杩戠殑璺宠浆鐩爣鎴栨柟娉曞紑澶达紝妯℃嫙鏍堟繁搴﹀苟鎻掑叆瓒冲澶氱殑 <c>pop</c>銆?    ///     蹇呴』鍦?<c>patch_branch_targets</c>
    ///     涔嬪墠鎵ц锛屽悓鏃舵洿鏂扮储寮曟槧灏勩€?    ///
    /// </summary>
    private static void normalize_void_returns_stack_depth(
        List<ClrInstruction> instructions,
        int[] sourceInstructionToEmittedIndex,
        List<PendingClrBranch> pendingBranches,
        HashSet<int> voidCallIndices,
        Dictionary<int, int> callPopCounts,
        GenerateFunction function)
    {
        var traceExecutePack = string.Equals(function.name, "legion.execute_pack", StringComparison.Ordinal);
        var traceStopwatch = traceExecutePack ? Stopwatch.StartNew() : null;
        var tracedRetCount = 0;
        // 临时绕过 void 栈深规范化。
        // 当前该函数会在本阶段出现崩溃（疑似 StackOverflow），先验证其余 CLR 发射链路是否可走通。
        Console.WriteLine($"[ClrBackend] {function.name}::normalize_void_returns_stack_depth skipped");
        return;
        if (traceExecutePack)
            Console.WriteLine(
                $"[ClrBackend] execute_pack::normalize_void_returns_stack_depth start instructions={instructions.Count}, pendingBranches={pendingBranches.Count}, voidCalls={voidCallIndices.Count}, callPops={callPopCounts.Count}");
        // 鏀堕泦鎵€鏈夎烦杞洰鏍囩殑鎸囦护绱㈠紩锛岀敤浜庣‘瀹氬熀鏈潡杈圭晫
        var branchTargets = new HashSet<int>();
        foreach (var branch in pendingBranches)
        {
            var label = function.labels.FirstOrDefault(l => l.name == branch.label_name);
            if (label is { instruction_index: >= 0 } &&
                label.instruction_index < sourceInstructionToEmittedIndex.Length)
            {
                var targetIndex = sourceInstructionToEmittedIndex[label.instruction_index];
                branchTargets.Add(targetIndex);
            }
        }

        if (traceExecutePack)
            Console.WriteLine(
                $"[ClrBackend] execute_pack::normalize_void_returns_stack_depth branchTargets={branchTargets.Count}, elapsed={traceStopwatch!.ElapsedMilliseconds} ms");
        for (var i = instructions.Count - 1; i >= 0; i--)
        {
            if (instructions[i].opcode != ClrOpcode.ret) continue;
            // 浠?ret 鍚戝墠鎵弿鍒板熀鏈潡寮€澶达紙璺宠浆鐩爣鎴栨柟娉曞紑澶达級锛?            // 妯℃嫙鏍堟繁搴︼紝璁＄畻闇€瑕佹彃鍏ュ灏戜釜 pop
            var blockStart = i - 1;
            while (blockStart >= 0)
            {
                if (branchTargets.Contains(blockStart))
                {
                    // blockStart 鏄烦杞洰鏍囷紝瀹冩槸鍩烘湰鍧楃殑绗竴鏉℃寚浠?                    // 鎴戜滑闇€瑕佸寘鍚畠锛屾墍浠ヤ粠 blockStart 寮€濮嬫ā鎷?                    break;
                }

                // 濡傛灉閬囧埌璺宠浆鎴栬繑鍥烇紝鍋滄锛堣繖鏄墠涓€涓熀鏈潡鐨勭粨灏撅級
                // 鍖呮嫭鏉′欢鍒嗘敮锛坆rfalse/brtrue 绛夛級锛屽畠浠篃鏄熀鏈潡杈圭晫
                var op = instructions[blockStart].opcode;
                if (op is ClrOpcode.br or ClrOpcode.br_s or ClrOpcode.leave or ClrOpcode.leave_s
                    or ClrOpcode.ret or ClrOpcode.@throw or ClrOpcode.rethrow
                    or ClrOpcode.brfalse or ClrOpcode.brfalse_s or ClrOpcode.brtrue or ClrOpcode.brtrue_s
                    or ClrOpcode.beq or ClrOpcode.beq_s or ClrOpcode.bne_un or ClrOpcode.bne_un_s
                    or ClrOpcode.blt or ClrOpcode.blt_s or ClrOpcode.ble or ClrOpcode.ble_s
                    or ClrOpcode.bgt or ClrOpcode.bgt_s or ClrOpcode.bge or ClrOpcode.bge_s
                    or ClrOpcode.blt_un or ClrOpcode.blt_un_s or ClrOpcode.ble_un or ClrOpcode.ble_un_s
                    or ClrOpcode.bgt_un or ClrOpcode.bgt_un_s or ClrOpcode.bge_un or ClrOpcode.bge_un_s)
                    blockStart++; // 浠庝笅涓€鏉℃寚浠ゅ紑濮?                    break;
                blockStart--;
            }

            if (blockStart < 0) blockStart = 0;
            var depth = 0;
            var isLegion = function.name.Contains("legion");
            if (isLegion)
                if (branchTargets.Count > 0)
                {
                }

            for (var k = blockStart; k < i; k++)
            {
                var inst = instructions[k];
                var (push, pop) = get_stack_effect(inst.opcode);
                if (inst.opcode is ClrOpcode.call or ClrOpcode.callvirt)
                {
                    // 浣跨敤璁板綍鐨勫弬鏁板脊鍑烘暟閲忥紱鏈褰曟椂榛樿 1锛堝ぇ澶氭暟 call 鑷冲皯寮瑰嚭涓€涓弬鏁帮級
                    var hasEntry = callPopCounts.TryGetValue(k, out var count);
                    pop = hasEntry ? count : 1;
                    if (voidCallIndices.Contains(k)) push = 0;
                    if (isLegion && (hasEntry || voidCallIndices.Contains(k)))
                    {
                    }
                }

                depth -= pop;
                if (depth < 0)
                {
                    if (isLegion)
                    {
                    }

                    depth = 0;
                }

                depth += push;
            }

            if (isLegion)
            {
            }

            if (traceExecutePack && tracedRetCount < 12)
            {
                Console.WriteLine(
                    $"[ClrBackend] execute_pack::void_stack ret={i}, blockStart={blockStart}, depth={depth}, instructions={instructions.Count}, elapsed={traceStopwatch!.ElapsedMilliseconds} ms");
                tracedRetCount++;
            }

            // 鎻掑叆瓒冲澶氱殑 pop 浣挎爤娣卞害褰掗浂
            if (depth > 0)
            {
                var insertPos = i;
                for (var p = 0; p < depth; p++)
                {
                    instructions.Insert(insertPos, new ClrInstruction { opcode = ClrOpcode.pop });
                    insertPos++;
                }

                // 鏇存柊 sourceInstructionToEmittedIndex锛氭墍鏈?>= i 鐨勭储寮?+depth
                for (var m = 0; m < sourceInstructionToEmittedIndex.Length; m++)
                    if (sourceInstructionToEmittedIndex[m] >= i)
                        sourceInstructionToEmittedIndex[m] += depth;
                // 鏇存柊 pendingBranches锛氭墍鏈?>= i 鐨勫垎鏀储寮?+depth
                shift_pending_branches(pendingBranches, i, depth);
                // 鏇存柊 callPopCounts锛氭墍鏈?>= i 鐨勯敭 +depth
                var oldCallPopEntries = callPopCounts.ToList();
                callPopCounts.Clear();
                foreach (var (k, v) in oldCallPopEntries) callPopCounts[k >= i ? k + depth : k] = v;
                // 鏇存柊 voidCallIndices锛氭墍鏈?>= i 鐨勭储寮?+depth
                var updatedVoidCalls = new HashSet<int>();
                foreach (var v in voidCallIndices) updatedVoidCalls.Add(v >= i ? v + depth : v);
                voidCallIndices.Clear();
                foreach (var v in updatedVoidCalls) voidCallIndices.Add(v);
                // 鏇存柊 branchTargets锛氭墍鏈?>= i 鐨勭洰鏍?+depth
                var updatedTargets = new HashSet<int>();
                foreach (var t in branchTargets) updatedTargets.Add(t >= i ? t + depth : t);
                branchTargets = updatedTargets;
                // 璺宠繃鍒氭彃鍏ョ殑 pop 鎸囦护
                i += depth;
                if (traceExecutePack && tracedRetCount < 20)
                {
                    Console.WriteLine(
                        $"[ClrBackend] execute_pack::void_stack inserted={depth}, next_i={i}, instructions={instructions.Count}, elapsed={traceStopwatch!.ElapsedMilliseconds} ms");
                    tracedRetCount++;
                }
            }
        }

        if (traceExecutePack)
            Console.WriteLine(
                $"[ClrBackend] execute_pack::normalize_void_returns_stack_depth done in {traceStopwatch!.ElapsedMilliseconds} ms, final_instructions={instructions.Count}");
    }

    /// <summary>
    ///     鑾峰彇鍒嗘敮鎸囦护鐨勭洰鏍囨寚浠ょ储寮曪紙鍦ㄦ寚浠ゅ垪琛ㄤ腑鐨勪綅缃級銆?    ///     濡傛灉涓嶆槸鍒嗘敮鎸囦护鎴栨棤娉曠‘瀹氱洰鏍囷紝杩斿洖 -1銆?    ///     娉ㄦ剰锛氳繑鍥炵殑鏄寚浠ゅ湪
    ///     <c>instructions</c> 鍒楄〃涓殑绱㈠紩锛岃€岄潪 IL 鍋忕Щ閲忋€?    ///
    /// </summary>
    private static int get_branch_target(ClrInstruction instruction)
    {
        if (instruction.operand is not ClrBranchTarget32Operand br32) return -1;
        var targetOffset = br32.offset;
        return -1;
    }

    /// <summary>
    ///     淇姣旇緝鎸囦护鐨勭被鍨嬩笉鍖归厤闂銆?    ///     褰?<c>ceq</c>/<c>clt</c>/<c>cgt</c> 绛夋瘮杈冩寚浠ょ殑鎿嶄綔鏁扮被鍨嬩笉涓€鑷存椂
    ///     锛堜緥濡?<c>object</c> 涓?<c>i32</c>锛夛紝CLR 楠岃瘉鍣ㄤ細鎷掔粷銆?    ///     妫€娴?<c>ldloc(object) + ldc_i4 + ceq/clt/cgt</c> 妯″紡锛?
    ///     ///     淇姣旇緝鎸囦护鐨勭被鍨嬩笉鍖归厤锛氬綋 <c>ldloc(object) + ldc_i4 + ceq/clt/cgt</c> 妯″紡鍑虹幇鏃讹紝
    ///     鍦?<c>ldloc</c> 鍚庢彃鍏?<c>unbox.any System.Int32</c>锛?    ///     灏?<c>object</c> 鎷嗙涓?<c>i32</c>锛屼娇涓や釜鎿嶄綔鏁扮被鍨嬩竴鑷淬€?
    ///     ///     蹇呴』鍦?<c>patch_branch_targets</c> 涔嬪墠鎵ц锛屽悓鏃舵洿鏂扮储寮曟槧灏勩€?    ///
    /// </summary>
    private static void fix_comparison_type_mismatch(
        List<ClrInstruction> instructions,
        IReadOnlyList<GenerateTypeReference> localVariableTypes,
        IReadOnlyDictionary<string, uint> typeRefTokenMap,
        int[] sourceInstructionToEmittedIndex,
        List<PendingClrBranch> pendingBranches,
        IReadOnlySet<int> objectCallIndices)
    {
        // 查找 System.Int32 的 TypeRef 令牌，用于 unbox.any 指令
        if (!typeRefTokenMap.TryGetValue("System.Int32", out var int32TypeToken)) return;
        for (var i = 2; i < instructions.Count; i++)
        {
            var cmpOpcode = instructions[i].opcode;
            if (cmpOpcode is not (ClrOpcode.ceq or ClrOpcode.cgt or ClrOpcode.cgt_un
                or ClrOpcode.clt or ClrOpcode.clt_un))
                continue;
            // 检查 i-1 是否为 ldc_i4 指令
            if (!is_ldc_i4(instructions[i - 1].opcode)) continue;
            var prevPrev = instructions[i - 2];
            // 模式 1：ldloc(object) + ldc_i4 + ceq/clt/cgt
            var localIndex = get_ldloc_local_index(prevPrev);
            if (localIndex >= 0 && localIndex < localVariableTypes.Count)
            {
                var localType = localVariableTypes[localIndex];
                if (is_object_element_type(localType))
                {
                    insert_unbox_any_int32(instructions, i - 1, int32TypeToken,
                        sourceInstructionToEmittedIndex, pendingBranches);
                    i += 2;
                    continue;
                }
            }

            // 模式 2：call/callvirt(object) + ldc_i4 + ceq/clt/cgt
            if (prevPrev.opcode is ClrOpcode.call or ClrOpcode.callvirt &&
                objectCallIndices.Contains(i - 2))
            {
                insert_unbox_any_int32(instructions, i - 1, int32TypeToken,
                    sourceInstructionToEmittedIndex, pendingBranches);
                i += 2;
            }
        }
    }

    /// <summary>
    ///     在指定位置插入 <c>unbox.any System.Int32</c> 指令，
    ///     并同步更新 <paramref name="sourceInstructionToEmittedIndex" /> 和 <paramref name="pendingBranches" />。
    /// </summary>
    private static void insert_unbox_any_int32(
        List<ClrInstruction> instructions,
        int insertPos,
        uint int32TypeToken,
        int[] sourceInstructionToEmittedIndex,
        List<PendingClrBranch> pendingBranches)
    {
        instructions.Insert(insertPos, new ClrInstruction
        {
            opcode = ClrOpcode.unbox_any,
            operand = new ClrTokenOperand { value = int32TypeToken }
        });
        // 更新 sourceInstructionToEmittedIndex：所有 >= insertPos 的索引 +1
        for (var m = 0; m < sourceInstructionToEmittedIndex.Length; m++)
            if (sourceInstructionToEmittedIndex[m] >= insertPos)
                sourceInstructionToEmittedIndex[m] += 1;
        // 更新 pendingBranches：所有 >= insertPos 的分支索引 +1
        shift_pending_branches(pendingBranches, insertPos);
    }

    /// <summary>
    ///     判断 <see cref="GenerateValueType" /> 是否会映射为 CLR 的 <c>object</c> 元素类型。
    ///     与 <see cref="is_object_element_type" /> 对应，但作用于 <see cref="GenerateValueType" /> 枚举。
    ///     用于识别返回 object 的 call/callvirt 指令。
    /// </summary>
    private static bool is_object_value_type(GenerateValueType valueType)
    {
        return valueType is GenerateValueType.@object
            or GenerateValueType.any
            or GenerateValueType.external_ref
            or GenerateValueType.function_ref
            or GenerateValueType.@null;
    }

    /// <summary>
    ///     鍒ゆ柇 CLR 鎿嶄綔鐮佹槸鍚︿负 <c>ldc_i4</c> 鍙樹綋锛堝姞杞?int32 甯搁噺锛夈€?    ///
    /// </summary>
    private static bool is_ldc_i4(ClrOpcode opcode)
    {
        return opcode is ClrOpcode.ldc_i4 or ClrOpcode.ldc_i4_0 or ClrOpcode.ldc_i4_1
            or ClrOpcode.ldc_i4_2 or ClrOpcode.ldc_i4_3 or ClrOpcode.ldc_i4_4
            or ClrOpcode.ldc_i4_5 or ClrOpcode.ldc_i4_6 or ClrOpcode.ldc_i4_7
            or ClrOpcode.ldc_i4_8 or ClrOpcode.ldc_i4_m1 or ClrOpcode.ldc_i4_s;
    }

    /// <summary>
    ///     浠?<c>ldloc</c> 鎸囦护涓彁鍙栧眬閮ㄥ彉閲忕储寮曘€?    ///     濡傛灉涓嶆槸 <c>ldloc</c> 鎸囦护锛岃繑鍥?-1銆?    ///
    /// </summary>
    private static int get_ldloc_local_index(ClrInstruction instruction)
    {
        return instruction.opcode switch
        {
            ClrOpcode.ldloc_0 => 0,
            ClrOpcode.ldloc_1 => 1,
            ClrOpcode.ldloc_2 => 2,
            ClrOpcode.ldloc_3 => 3,
            ClrOpcode.ldloc_s => instruction.operand is ClrLocalIndexOperand locS ? (int)locS.index : -1,
            ClrOpcode.ldloc => instruction.operand is ClrLocalIndexOperand loc ? (int)loc.index : -1,
            _ => -1
        };
    }

    /// <summary>
    ///     判断类型名是否为内建类型别名。
    ///     这些类型不应生成 TypeDef，否则会导致 ILVerify 的类型推断与方法签名不匹配。
    /// </summary>
    private static bool is_builtin_type_alias(string typeName)
    {
        return GenerateTypeReference.parse(typeName).is_builtin_alias;
    }

    /// <summary>
    ///     判断类型名是否为被擦除后的对象类型。
    ///     当 `get_field` / `set_field` 遇到这些类型时，需要结合 `fieldTokenMap` 反推出实际宿主类型。
    /// </summary>
    private static bool is_erased_object_type(string typeName)
    {
        return GenerateTypeReference.parse(typeName).is_erased_object_type;
    }

    /// <summary>
    ///     褰?get_field/set_field 鐨?currentType 涓鸿鎿﹂櫎绫诲瀷鏃讹紝灏濊瘯浠?fieldTokenMap 涓?    ///     瑙ｆ瀽瀹為檯鐨勭粨鏋勪綋绫诲瀷銆傛悳绱㈡墍鏈変互
    ///     ".fieldName" 缁撳熬涓旂被鍨嬮儴鍒嗛潪鎿﹂櫎绫诲瀷鐨勯敭銆?    ///     濡傛灉鎭板ソ鎵惧埌涓€涓尮閰嶏紝鍒欒繑鍥炶閿殑绫诲瀷閮ㄥ垎锛涘惁鍒欒繑鍥?null銆?    ///
    /// </summary>
    private static string? resolve_field_type_from_map(string fieldName, IDictionary<string, uint> fieldTokenMap)
    {
        var suffix = $".{fieldName}";
        string? resolvedType = null;
        foreach (var key in fieldTokenMap.Keys)
        {
            if (!key.EndsWith(suffix, StringComparison.Ordinal)) continue;
            var typePart = key[..^suffix.Length];
            if (is_erased_object_type(typePart) || is_builtin_type_alias(typePart)) continue;
            if (resolvedType == null)
                resolvedType = typePart;
            else if (!string.Equals(resolvedType, typePart, StringComparison.Ordinal))
                // 澶氫釜涓嶅悓绫诲瀷鏈夊悓鍚嶅瓧娈碉紝鏃犳硶纭畾
                return null;
        }

        return resolvedType;
    }

    /// <summary>
    ///     鏋勫缓瀛楁鐨?qualified key銆?    ///     褰?currentType 涓鸿鎿﹂櫎绫诲瀷锛坋xternal_ref/any/object锛夋椂锛?    ///
    ///     灏濊瘯浠?fieldTokenMap 涓В鏋愬疄闄呯殑缁撴瀯浣撶被鍨嬶紝閬垮厤瀛楁 token 涓嶅尮閰嶃€?    ///
    /// </summary>
    private static string build_field_qualified_key(string? currentType, string fieldName,
        IDictionary<string, uint> fieldTokenMap, string? functionName = null)
    {
        var ownerTypeFromFunction = try_resolve_owner_type_from_function(functionName, fieldName, fieldTokenMap);
        var parsedCurrentType = currentType is null ? null : GenerateTypeReference.parse(currentType);
        if (ownerTypeFromFunction is not null &&
            (currentType is null ||
             is_erased_object_type(currentType) ||
             is_builtin_type_alias(currentType) ||
             parsedCurrentType?.is_array_family == true))
            return $"{ownerTypeFromFunction}.{fieldName}";

        if (currentType != null && is_erased_object_type(currentType))
        {
            var resolved = resolve_field_type_from_map(fieldName, fieldTokenMap);
            if (resolved != null) return $"{resolved}.{fieldName}";
        }

        if (ownerTypeFromFunction is not null &&
            (currentType is null || !fieldTokenMap.ContainsKey($"{currentType}.{fieldName}")))
            return $"{ownerTypeFromFunction}.{fieldName}";

        return currentType != null
            ? $"{currentType}.{fieldName}"
            : fieldName;
    }

    /// <summary>
    ///     根据当前函数名推断字段所属的结构体类型。
    ///     仅在函数名形如 `TypeName.method` 且该类型已声明对应字段时返回。
    /// </summary>
    private static string? try_resolve_owner_type_from_function(
        string? functionName,
        string fieldName,
        IDictionary<string, uint> fieldTokenMap)
    {
        if (string.IsNullOrWhiteSpace(functionName)) return null;

        var lastDot = functionName.LastIndexOf('.');
        if (lastDot <= 0) return null;

        var ownerType = functionName[..lastDot];
        var lastOwnerSeparator = ownerType.LastIndexOf('.');
        var ownerLeafName = lastOwnerSeparator >= 0
            ? ownerType[(lastOwnerSeparator + 1)..]
            : ownerType;
        if (string.IsNullOrEmpty(ownerLeafName) || !char.IsUpper(ownerLeafName[0])) return null;

        return ownerType;
    }

    /// <summary>
    ///     判断类型引用是否会映射为 CLR 的 `object` 元素类型（0x1C）。
    ///     也就是排除基础值类型和文本类型之后的其他引用类型。
    /// </summary>
    private static bool is_object_element_type(GenerateTypeReference typeName)
    {
        return typeName.is_clr_object_element_type;
    }

    /// <summary>
    ///     返回 `swap` 临时变量的起始索引。
    ///     `swap` 需要两个连续槽位，位于函数声明的局部变量之后。
    /// </summary>
    private static int swap_temp_index(GenerateFunction function)
    {
        return function.local_variables.Count;
    }

    /// <summary>
    ///     鍒ゆ柇 CLR 鎸囦护鏄惁鍦ㄦ眰鍊兼爤涓婁骇鐢熶竴涓€笺€?    ///     鐢ㄤ簬 <see cref="normalize_non_void_returns" /> 鍒ゆ柇 <c>nop</c>
    ///     鍓嶆槸鍚﹀凡鏈夎繑鍥炲€笺€?    ///     褰?<paramref name="voidCallIndices" /> 鍖呭惈 <paramref name="index" /> 鏃讹紝
    ///     <c>call</c>/<c>callvirt</c> 鎸囦护涓嶈涓轰骇鐢熸爤鍊硷紙void 璋冪敤鏃犺繑鍥炲€硷級銆?    ///
    /// </summary>
    private static bool produces_stack_value(ClrInstruction instruction, HashSet<int> voidCallIndices, int index)
    {
        if (instruction.opcode is ClrOpcode.call or ClrOpcode.callvirt) return !voidCallIndices.Contains(index);
        return produces_stack_value(instruction);
    }

    /// <summary>
    ///     鍒ゆ柇 CLR 鎸囦护鏄惁鍦ㄦ眰鍊兼爤涓婁骇鐢熶竴涓€硷紙涓嶈€冭檻 void call 杩借釜锛夈€?    ///
    /// </summary>
    private static bool produces_stack_value(ClrInstruction instruction)
    {
        return instruction.opcode is
            ClrOpcode.ldc_i4_0 or ClrOpcode.ldc_i4_m1 or ClrOpcode.ldc_i4_1
            or ClrOpcode.ldc_i4_2 or ClrOpcode.ldc_i4_3 or ClrOpcode.ldc_i4_4
            or ClrOpcode.ldc_i4_5 or ClrOpcode.ldc_i4_6 or ClrOpcode.ldc_i4_7
            or ClrOpcode.ldc_i4_8 or ClrOpcode.ldc_i4_s or ClrOpcode.ldc_i4
            or ClrOpcode.ldc_i8 or ClrOpcode.ldc_r4 or ClrOpcode.ldc_r8
            or ClrOpcode.ldnull or ClrOpcode.ldstr
            or ClrOpcode.ldloc_0 or ClrOpcode.ldloc_1 or ClrOpcode.ldloc_2 or ClrOpcode.ldloc_3
            or ClrOpcode.ldloc_s or ClrOpcode.ldloc
            or ClrOpcode.ldarg_0 or ClrOpcode.ldarg_1 or ClrOpcode.ldarg_2 or ClrOpcode.ldarg_3
            or ClrOpcode.ldarg_s or ClrOpcode.ldarg
            or ClrOpcode.call or ClrOpcode.callvirt or ClrOpcode.newobj
            or ClrOpcode.ceq or ClrOpcode.clt or ClrOpcode.cgt or ClrOpcode.clt_un or ClrOpcode.cgt_un
            or ClrOpcode.add or ClrOpcode.sub or ClrOpcode.mul or ClrOpcode.div or ClrOpcode.rem
            or ClrOpcode.and or ClrOpcode.or or ClrOpcode.xor
            or ClrOpcode.shl or ClrOpcode.shr or ClrOpcode.shr_un
            or ClrOpcode.neg or ClrOpcode.not
            or ClrOpcode.conv_i4 or ClrOpcode.conv_i8 or ClrOpcode.conv_r4 or ClrOpcode.conv_r8
            or ClrOpcode.dup or ClrOpcode.ldlen or ClrOpcode.ldfld or ClrOpcode.ldelem_ref;
    }

    /// <summary>
    ///     鑾峰彇鍐呯疆 Nyar 鎿嶄綔鐮佸搴旂殑 CLR call/callvirt 鎸囦护鐨勮皟鐢ㄤ俊鎭€?    ///     杩斿洖 (popCount, isVoid)锛屽叾涓?popCount 涓?-1
    ///     琛ㄧず涓嶆槸鍐呯疆璋冪敤鎿嶄綔鐮併€?    ///
    /// </summary>
    private static (int popCount, bool isVoid) get_builtin_call_info(NyarHeadCode opcode)
    {
        switch (opcode)
        {
            case NyarHeadCode.utf8_concat:
            case NyarHeadCode.utf8_eq:
            case NyarHeadCode.utf8_ne:
                return (2, false); // 寮瑰嚭 2 涓瓧绗︿覆鍙傛暟锛岃繑鍥為潪 void
            case NyarHeadCode.utf8_len_bytes:
            case NyarHeadCode.utf8_len_chars:
                return (1, false); // 寮瑰嚭 1 涓?this 寮曠敤锛坈allvirt锛夛紝杩斿洖 i32
            case NyarHeadCode.utf8_substr:
                return (3, false); // 寮瑰嚭 (string, start, length)锛岃繑鍥?string
            case NyarHeadCode.f64_sqrt:
                return (1, false); // 寮瑰嚭 1 涓?f64 鍙傛暟锛岃繑鍥?f64
            case NyarHeadCode.array_push:
                return (2, true); // 寮瑰嚭 (list_ref, value_ref)锛寁oid 杩斿洖
            default:
                return (-1, false);
        }
    }

    /// <summary>
    ///     鍩轰簬 IL 鎸囦护娴佽绠楁柟娉曠殑鏈€澶ф眰鍊兼爤娣卞害銆?    ///     绾挎€ф壂鎻忔瘡鏉℃寚浠ょ殑鏍堟晥鏋滐紙鎺ㄥ叆/寮瑰嚭锛夛紝璺熻釜宄板€兼繁搴︺€?    ///
    ///     鍒嗘敮鎸囦护鎸夌洰鏍囧亸绉婚噺璺宠浆锛屾澶勬寜绾挎€ц矾寰勫彇宄板€笺€?    ///
    /// </summary>
    private static ushort compute_max_stack(IReadOnlyList<ClrInstruction> instructions)
    {
        var depth = 0;
        var maxDepth = 0;
        for (var i = 0; i < instructions.Count; i++)
        {
            var opcode = instructions[i].opcode;
            var (push, pop) = get_stack_effect(opcode);
            depth -= pop;
            if (depth < 0) depth = 0;
            depth += push;
            if (depth > maxDepth) maxDepth = depth;
        }

        // 鑷冲皯淇濈暀 8 浣滀负瀹夊叏涓嬮檺
        return (ushort)Math.Max(maxDepth, 8);
    }

    /// <summary>
    ///     杩斿洖 CLR 鎸囦护鐨勬爤鏁堟灉 (push, pop)銆?    ///     push = 璇ユ寚浠ゅ悜姹傚€兼爤鎺ㄥ叆鐨勫€兼暟閲忋€?    ///     pop = 璇ユ寚浠や粠姹傚€兼爤寮瑰嚭鐨勫€兼暟閲忋€?
    ///     ///
    /// </summary>
    private static (int push, int pop) get_stack_effect(ClrOpcode opcode)
    {
        switch (opcode)
        {
            // 鏃犳爤鏁堟灉
            case ClrOpcode.nop:
            case ClrOpcode.@break:
                return (0, 0);
            // 鎺ㄥ叆 1 涓€?            case ClrOpcode.ldarg_0:
            case ClrOpcode.ldarg_1:
            case ClrOpcode.ldarg_2:
            case ClrOpcode.ldarg_3:
            case ClrOpcode.ldarg_s:
            case ClrOpcode.ldarga_s:
            case ClrOpcode.ldarg:
            case ClrOpcode.ldarga:
                return (1, 0);
            case ClrOpcode.ldloc_0:
            case ClrOpcode.ldloc_1:
            case ClrOpcode.ldloc_2:
            case ClrOpcode.ldloc_3:
            case ClrOpcode.ldloc_s:
            case ClrOpcode.ldloca_s:
            case ClrOpcode.ldloc:
            case ClrOpcode.ldloca:
                return (1, 0);
            // 鎺ㄥ叆甯搁噺鍊硷紙涓嶅脊鍑轰换浣曞€硷級
            case ClrOpcode.ldnull:
            case ClrOpcode.ldc_i4_m1:
            case ClrOpcode.ldc_i4_0:
            case ClrOpcode.ldc_i4_1:
            case ClrOpcode.ldc_i4_2:
            case ClrOpcode.ldc_i4_3:
            case ClrOpcode.ldc_i4_4:
            case ClrOpcode.ldc_i4_5:
            case ClrOpcode.ldc_i4_6:
            case ClrOpcode.ldc_i4_7:
            case ClrOpcode.ldc_i4_8:
            case ClrOpcode.ldc_i4_s:
            case ClrOpcode.ldc_i4:
            case ClrOpcode.ldc_i8:
            case ClrOpcode.ldc_r4:
            case ClrOpcode.ldc_r8:
            case ClrOpcode.ldstr:
            case ClrOpcode.ldtoken:
                return (1, 0);
            // 鍔犺浇瀛楁锛氬脊鍑哄璞″紩鐢紝鎺ㄥ叆瀛楁鍊?            case ClrOpcode.ldfld:
            case ClrOpcode.ldsfld:
            case ClrOpcode.ldflda:
            case ClrOpcode.ldsflda:
                return (1, 1);
            // 鏁扮粍闀垮害锛氬脊鍑烘暟缁勫紩鐢紝鎺ㄥ叆闀垮害
            case ClrOpcode.ldlen:
                return (1, 1);
            // ldind 绯诲垪锛氬脊鍑哄湴鍧€鎺ㄥ叆鍊?(pop 1, push 1)
            case ClrOpcode.ldind_i1:
            case ClrOpcode.ldind_u1:
            case ClrOpcode.ldind_i2:
            case ClrOpcode.ldind_u2:
            case ClrOpcode.ldind_i4:
            case ClrOpcode.ldind_u4:
            case ClrOpcode.ldind_i8:
            case ClrOpcode.ldind_i:
            case ClrOpcode.ldind_r4:
            case ClrOpcode.ldind_r8:
            case ClrOpcode.ldind_ref:
            case ClrOpcode.ldobj:
            case ClrOpcode.isinst:
            case ClrOpcode.castclass:
                return (1, 1);
            // ldelem 绯诲垪锛氬脊鍑烘暟缁勫紩鐢ㄥ拰绱㈠紩锛屾帹鍏ュ厓绱犲€?(pop 2, push 1)
            case ClrOpcode.ldelem_i1:
            case ClrOpcode.ldelem_u1:
            case ClrOpcode.ldelem_i2:
            case ClrOpcode.ldelem_u2:
            case ClrOpcode.ldelem_i4:
            case ClrOpcode.ldelem_u4:
            case ClrOpcode.ldelem_i8:
            case ClrOpcode.ldelem_i:
            case ClrOpcode.ldelem_r4:
            case ClrOpcode.ldelem_r8:
            case ClrOpcode.ldelem_ref:
            case ClrOpcode.ldelem_any:
            case ClrOpcode.ldelema:
                return (1, 2);
            // 瀛樺偍锛氬脊鍑哄€硷紙涓嶆帹鍏ワ級
            case ClrOpcode.stloc_0:
            case ClrOpcode.stloc_1:
            case ClrOpcode.stloc_2:
            case ClrOpcode.stloc_3:
            case ClrOpcode.stloc_s:
            case ClrOpcode.stloc:
                return (0, 1);
            case ClrOpcode.starg_s:
            case ClrOpcode.starg:
                return (0, 1);
            case ClrOpcode.stfld:
                return (0, 2); // 寮瑰嚭瀵硅薄+鍊?
            case ClrOpcode.stsfld:
                return (0, 1); // 寮瑰嚭鍊?
            case ClrOpcode.stind_ref:
            case ClrOpcode.stind_i1:
            case ClrOpcode.stind_i2:
            case ClrOpcode.stind_i4:
            case ClrOpcode.stind_i8:
            case ClrOpcode.stind_r4:
            case ClrOpcode.stind_r8:
            case ClrOpcode.stind_i:
            case ClrOpcode.stobj:
                return (0, 2); // 寮瑰嚭鍦板潃+鍊?
            case ClrOpcode.stelem_i1:
            case ClrOpcode.stelem_i2:
            case ClrOpcode.stelem_i4:
            case ClrOpcode.stelem_i8:
            case ClrOpcode.stelem_r4:
            case ClrOpcode.stelem_r8:
            case ClrOpcode.stelem_i:
            case ClrOpcode.stelem_ref:
            case ClrOpcode.stelem_any:
                return (0, 3); // 寮瑰嚭鏁扮粍+绱㈠紩+鍊?
                // 鏍堟搷浣?            case ClrOpcode.dup:
                return (1, 0); // 瀹為檯鏄帹鍏ユ爤椤剁殑鍓湰锛屽噣澧?1
            case ClrOpcode.pop:
                return (0, 1);
            // 绠楁湳/閫昏緫锛氬脊鍑?2 鎺ㄥ叆 1
            case ClrOpcode.add:
            case ClrOpcode.sub:
            case ClrOpcode.mul:
            case ClrOpcode.div:
            case ClrOpcode.div_un:
            case ClrOpcode.rem:
            case ClrOpcode.rem_un:
            case ClrOpcode.and:
            case ClrOpcode.or:
            case ClrOpcode.xor:
            case ClrOpcode.shl:
            case ClrOpcode.shr:
            case ClrOpcode.shr_un:
            case ClrOpcode.ceq:
            case ClrOpcode.clt:
            case ClrOpcode.cgt:
            case ClrOpcode.clt_un:
            case ClrOpcode.cgt_un:
                return (1, 2);
            // 涓€鍏冭繍绠楋細寮瑰嚭 1 鎺ㄥ叆 1
            case ClrOpcode.neg:
            case ClrOpcode.not:
            case ClrOpcode.conv_i1:
            case ClrOpcode.conv_i2:
            case ClrOpcode.conv_i4:
            case ClrOpcode.conv_i8:
            case ClrOpcode.conv_r4:
            case ClrOpcode.conv_r8:
            case ClrOpcode.conv_u4:
            case ClrOpcode.conv_u8:
            case ClrOpcode.conv_u:
            case ClrOpcode.conv_r_un:
            case ClrOpcode.box:
            case ClrOpcode.unbox:
            case ClrOpcode.unbox_any:
                return (1, 1);
            // 杞崲婧㈠嚭锛氬脊鍑?1 鎺ㄥ叆 1
            case ClrOpcode.conv_ovf_i1:
            case ClrOpcode.conv_ovf_u1:
            case ClrOpcode.conv_ovf_i2:
            case ClrOpcode.conv_ovf_u2:
            case ClrOpcode.conv_ovf_i4:
            case ClrOpcode.conv_ovf_u4:
            case ClrOpcode.conv_ovf_i8:
            case ClrOpcode.conv_ovf_u8:
            case ClrOpcode.conv_ovf_i:
            case ClrOpcode.conv_ovf_u:
            case ClrOpcode.conv_ovf_i1_un:
            case ClrOpcode.conv_ovf_u1_un:
            case ClrOpcode.conv_ovf_i2_un:
            case ClrOpcode.conv_ovf_u2_un:
            case ClrOpcode.conv_ovf_i4_un:
            case ClrOpcode.conv_ovf_u4_un:
            case ClrOpcode.conv_ovf_i8_un:
            case ClrOpcode.conv_ovf_u8_un:
            case ClrOpcode.conv_ovf_i_un:
            case ClrOpcode.conv_ovf_u_un:
                return (1, 1);
            // 鍒嗘敮锛氬脊鍑?0 鎴?1
            case ClrOpcode.br:
            case ClrOpcode.br_s:
            case ClrOpcode.leave:
            case ClrOpcode.leave_s:
                return (0, 0);
            case ClrOpcode.brfalse:
            case ClrOpcode.brfalse_s:
            case ClrOpcode.brtrue:
            case ClrOpcode.brtrue_s:
                return (0, 1);
            case ClrOpcode.beq:
            case ClrOpcode.bne_un:
            case ClrOpcode.blt:
            case ClrOpcode.ble:
            case ClrOpcode.bgt:
            case ClrOpcode.bge:
            case ClrOpcode.blt_un:
            case ClrOpcode.ble_un:
            case ClrOpcode.bgt_un:
            case ClrOpcode.bge_un:
            case ClrOpcode.beq_s:
            case ClrOpcode.bne_un_s:
            case ClrOpcode.blt_s:
            case ClrOpcode.ble_s:
            case ClrOpcode.bgt_s:
            case ClrOpcode.bge_s:
            case ClrOpcode.blt_un_s:
            case ClrOpcode.ble_un_s:
            case ClrOpcode.bgt_un_s:
            case ClrOpcode.bge_un_s:
                return (0, 2);
            // switch锛氬脊鍑?1锛堢储寮曞€硷級
            case ClrOpcode.@switch:
                return (0, 1);
            // 璋冪敤锛氫繚瀹堜及璁′负寮瑰嚭 0 鎺ㄥ叆 1锛坈all/newobj 鍙兘鎺ㄥ叆杩斿洖鍊硷級
            // 绮剧‘璁＄畻闇€瑕佽В鏋愭柟娉曠鍚嶏紝姝ゅ淇濆畧澶勭悊
            case ClrOpcode.call:
            case ClrOpcode.callvirt:
                return (1, 0); // 淇濆畧锛氬亣璁炬帹鍏?1 涓繑鍥炲€硷紝寮瑰嚭鐨勫弬鏁扮敱璋冪敤鑰呰礋璐ｅ钩琛?
            case ClrOpcode.newobj:
                return (1, 0); // 鎺ㄥ叆鏂板璞★紝鍙傛暟寮瑰嚭鐢辫皟鐢ㄨ€呰礋璐?
            case ClrOpcode.calli:
                return (0, 0); // 闂存帴璋冪敤锛屼繚瀹堝鐞?
            case ClrOpcode.jmp:
                return (0, 0);
            // 杩斿洖
            case ClrOpcode.ret:
                return (0, 0); // ret 鍙兘寮瑰嚭杩斿洖鍊硷紝浣嗘澶勪笉璁″叆锛堟柟娉曠粨鏉熸椂鏍堝凡娓呯┖锛?
            // 寮傚父澶勭悊
            case ClrOpcode.@throw:
                return (0, 1);
            case ClrOpcode.rethrow:
                return (0, 1);
            case ClrOpcode.endfilter:
                return (0, 1);
            // 瀵硅薄鎿嶄綔
            case ClrOpcode.newarr:
                return (1, 1); // 寮瑰嚭闀垮害鎺ㄥ叆鏁扮粍
            case ClrOpcode.initobj:
                return (0, 1); // 寮瑰嚭鍦板潃
            case ClrOpcode.cpobj:
                return (0, 2); // 寮瑰嚭鐩爣+婧愬湴鍧€
            case ClrOpcode.cpblk:
            case ClrOpcode.initblk:
                return (0, 3); // 寮瑰嚭鐩爣+婧?闀垮害
            case ClrOpcode.localloc:
                return (1, 1); // 寮瑰嚭澶у皬鎺ㄥ叆鍦板潃
            case ClrOpcode.ldftn:
            case ClrOpcode.ldvirtftn:
                return (1, 1); // 寮瑰嚭瀵硅薄鎺ㄥ叆鍑芥暟鎸囬拡
            case ClrOpcode.mkrefany:
                return (1, 1);
            case ClrOpcode.refanyval:
                return (1, 1);
            case ClrOpcode.refanytype:
                return (1, 1);
            case ClrOpcode.constrained:
            case ClrOpcode.@volatile:
            case ClrOpcode.unaligned:
            case ClrOpcode.tail:
            case ClrOpcode.@readonly:
            case ClrOpcode.@sizeof:
            case ClrOpcode.arglist:
                return (0, 0); // 鍓嶇紑鎸囦护鎴栨棤鏍堟晥鏋?
            default:
                return (0, 0); // 鏈煡鎸囦护淇濆畧澶勭悊
        }
    }

    private sealed record PendingClrBranch(int emitted_instruction_index, string label_name);
}