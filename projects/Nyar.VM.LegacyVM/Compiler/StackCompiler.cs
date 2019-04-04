using Nyar.Assembler;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.VM.LegacyVM.Compiler;

/// <summary>
///     鏍堝紡瀛楄妭鐮佺紪璇戝櫒锛屼负璇█缂栬瘧鍣ㄦ彁渚?GenerateInstruction 鍙戝皠鍩虹璁炬柦銆?///     绠＄悊甯搁噺姹犮€佸眬閮ㄥ彉閲忋€佹爣绛俱€佽烦杞洖濉瓑銆?/// </summary>
public sealed class StackCompiler
{
    private readonly GenerateModule _module;
    private GenerateFunction _currentFunction;
    private readonly GenerateConstantPool _constants;

    private readonly Dictionary<string, int> _locals;
    private int _nextLocalIndex;

    private int _nextLabelId;
    private readonly Dictionary<string, int> _labelPositions;
    private readonly List<(string Label, int InstructionIndex, bool IsConditional)> _pendingPatches;

    /// <summary>
    ///     break 鏍囩鏍堬紝鐢ㄤ簬宓屽寰幆涓殑 break 璺宠浆鐩爣
    /// </summary>
    private readonly Stack<string> _breakLabels = new();

    /// <summary>
    ///     continue 鏍囩鏍堬紝鐢ㄤ簬宓屽寰幆涓殑 continue 璺宠浆鐩爣
    /// </summary>
    private readonly Stack<string> _continueLabels = new();

    /// <summary>
    ///     鍒涘缓鏍堢紪璇戝櫒
    /// </summary>
    /// <param name="moduleName">妯″潡鍚嶇О</param>
    public StackCompiler(string moduleName)
    {
        _module = new GenerateModule(moduleName);
        _currentFunction = null!;
        _constants = _module.constants;

        _locals = new Dictionary<string, int>();
        _nextLocalIndex = 0;

        _nextLabelId = 0;
        _labelPositions = new Dictionary<string, int>();
        _pendingPatches = [];
    }

    /// <summary>
    ///     褰撳墠妯″潡
    /// </summary>
    public GenerateModule module => _module;

    /// <summary>
    ///     褰撳墠鍑芥暟
    /// </summary>
    public GenerateFunction current_function => _currentFunction;

    /// <summary>
    ///     甯搁噺姹?    /// </summary>
    public GenerateConstantPool constants => _constants;

    /// <summary>
    ///     褰撳墠鎸囦护浣嶇疆
    /// </summary>
    public int current_position => _currentFunction.instructions.Count;

    #region 鍑芥暟绠＄悊

    /// <summary>
    ///     寮€濮嬬紪璇戞柊鍑芥暟
    /// </summary>
    /// <param name="name">鍑芥暟鍚嶇О</param>
    /// <param name="returnType">杩斿洖鍊肩被鍨?/param>
    public void begin_function(string name, string returnType = "any")
    {
        _currentFunction = new GenerateFunction(name, returnType);
        _locals.Clear();
        _nextLocalIndex = 0;
        _labelPositions.Clear();
        _pendingPatches.Clear();
        _nextLabelId = 0;
    }

    /// <summary>
    ///     娣诲姞鍑芥暟鍙傛暟
    /// </summary>
    /// <param name="name">鍙傛暟鍚嶇О</param>
    /// <param name="type">鍙傛暟绫诲瀷</param>
    public void add_parameter(string name, string type = "any")
    {
        _currentFunction.add_parameter(name, type);
        _locals[name] = _nextLocalIndex++;
        _currentFunction.add_local_variable(name, type, _locals[name]);
    }

    /// <summary>
    ///     缁撴潫褰撳墠鍑芥暟缂栬瘧锛屽皢鍏舵坊鍔犲埌妯″潡
    /// </summary>
    public void end_function()
    {
        resolve_pending_patches();
        _module.add_function(_currentFunction);
    }

    #endregion

    #region 鎸囦护鍙戝皠

    /// <summary>
    ///     鍙戝皠涓€鏉℃寚浠?    /// </summary>
    public void emit(NyarHeadCode opcode, params GenerateOperand[] operands)
    {
        _currentFunction.add_instruction(new GenerateInstruction(opcode, operands));
    }

    /// <summary>
    ///     鍙戝皠鏃犳搷浣滄暟鎸囦护
    /// </summary>
    public void emit(NyarHeadCode opcode)
    {
        _currentFunction.add_instruction(new GenerateInstruction(opcode));
    }

    #endregion

    #region 甯搁噺

    /// <summary>
    ///     鍙戝皠鏁存暟甯搁噺骞惰繑鍥炲父閲忔睜绱㈠紩
    /// </summary>
    public int emit_const_i64(long value)
    {
        var index = _constants.add_int64(value);
        emit(NyarHeadCode.@const, new GenerateOperand.Const(index, GenerateValueType.i64));
        return index;
    }

    /// <summary>
    ///     鍙戝皠娴偣甯搁噺骞惰繑鍥炲父閲忔睜绱㈠紩
    /// </summary>
    public int emit_const_f64(double value)
    {
        var index = _constants.add_float64(value);
        emit(NyarHeadCode.@const, new GenerateOperand.Const(index, GenerateValueType.f64));
        return index;
    }

    /// <summary>
    ///     鍙戝皠瀛楃涓插父閲忓苟杩斿洖甯搁噺姹犵储寮?    /// </summary>
    public int emit_const_str(string value)
    {
        var index = _constants.add_string(value);
        emit(NyarHeadCode.@const, new GenerateOperand.Const(index, GenerateValueType.utf8));
        return index;
    }

    /// <summary>
    ///     鍙戝皠绌哄€?    /// </summary>
    public void emit_null()
    {
        emit(NyarHeadCode.@const, new GenerateOperand.Null(GenerateValueType.i64));
    }

    #endregion

    #region 灞€閮ㄥ彉閲?
    /// <summary>
    ///     澹版槑灞€閮ㄥ彉閲忓苟杩斿洖绱㈠紩
    /// </summary>
    public int declare_local(string name)
    {
        var index = _nextLocalIndex++;
        _locals[name] = index;
        _currentFunction.add_local_variable(name, "any", index);
        return index;
    }

    /// <summary>
    ///     鑾峰彇灞€閮ㄥ彉閲忕储寮?    /// </summary>
    public int get_local_index(string name)
    {
        if (_locals.TryGetValue(name, out var index))
        {
            return index;
        }

        return declare_local(name);
    }

    /// <summary>
    ///     鍙戝皠鍔犺浇灞€閮ㄥ彉閲?    /// </summary>
    public void emit_load_local(string name)
    {
        var index = get_local_index(name);
        emit(NyarHeadCode.load_local, new GenerateOperand.Local(index, GenerateValueType.i64));
    }

    /// <summary>
    ///     鍙戝皠鍔犺浇鍙傛暟
    /// </summary>
    public void emit_load_arg(int paramIndex)
    {
        emit(NyarHeadCode.load_arg, new GenerateOperand.Param(paramIndex, GenerateValueType.i64));
    }

    /// <summary>
    ///     鍙戝皠瀛樺偍灞€閮ㄥ彉閲?    /// </summary>
    public void emit_store_local(string name)
    {
        var index = get_local_index(name);
        emit(NyarHeadCode.store_local, new GenerateOperand.Local(index, GenerateValueType.i64));
    }

    #endregion

    #region 绠楁湳杩愮畻

    /// <summary>
    ///     鍙戝皠 i64 鍔犳硶
    /// </summary>
    public void emit_add()
    {
        emit(NyarHeadCode.i64_add);
    }

    /// <summary>
    ///     鍙戝皠 i64 鍑忔硶
    /// </summary>
    public void emit_sub()
    {
        emit(NyarHeadCode.i64_sub);
    }

    /// <summary>
    ///     鍙戝皠 i64 涔樻硶
    /// </summary>
    public void emit_mul()
    {
        emit(NyarHeadCode.i64_mul);
    }

    /// <summary>
    ///     鍙戝皠 i64 闄ゆ硶
    /// </summary>
    public void emit_div()
    {
        emit(NyarHeadCode.i64_div_s);
    }

    /// <summary>
    ///     鍙戝皠 i64 鍙栦綑
    /// </summary>
    public void emit_rem()
    {
        emit(NyarHeadCode.i64_rem_s);
    }

    /// <summary>
    ///     鍙戝皠 i64 鍙栬礋
    /// </summary>
    public void emit_neg()
    {
        emit(NyarHeadCode.i64_neg);
    }

    #endregion

    #region 姣旇緝杩愮畻

    /// <summary>
    ///     鍙戝皠 i64 鐩哥瓑姣旇緝
    /// </summary>
    public void emit_eq()
    {
        emit(NyarHeadCode.i64_eq);
    }

    /// <summary>
    ///     鍙戝皠 i64 涓嶇瓑姣旇緝
    /// </summary>
    public void emit_ne()
    {
        emit(NyarHeadCode.i64_ne);
    }

    /// <summary>
    ///     鍙戝皠 i64 灏忎簬姣旇緝
    /// </summary>
    public void emit_lt()
    {
        emit(NyarHeadCode.i64_lt_s);
    }

    /// <summary>
    ///     鍙戝皠 i64 澶т簬姣旇緝
    /// </summary>
    public void emit_gt()
    {
        emit(NyarHeadCode.i64_gt_s);
    }

    /// <summary>
    ///     鍙戝皠 i64 灏忎簬绛変簬姣旇緝
    /// </summary>
    public void emit_le()
    {
        emit(NyarHeadCode.i64_le_s);
    }

    /// <summary>
    ///     鍙戝皠 i64 澶т簬绛変簬姣旇緝
    /// </summary>
    public void emit_ge()
    {
        emit(NyarHeadCode.i64_ge_s);
    }

    #endregion

    #region 浣嶈繍绠?
    /// <summary>
    ///     鍙戝皠 i64 鎸変綅涓?    /// </summary>
    public void emit_and()
    {
        emit(NyarHeadCode.i64_and);
    }

    /// <summary>
    ///     鍙戝皠 i64 鎸変綅鎴?    /// </summary>
    public void emit_or()
    {
        emit(NyarHeadCode.i64_or);
    }

    /// <summary>
    ///     鍙戝皠 i64 鎸変綅寮傛垨
    /// </summary>
    public void emit_xor()
    {
        emit(NyarHeadCode.i64_xor);
    }

    /// <summary>
    ///     鍙戝皠 i64 鎸変綅鍙栧弽
    /// </summary>
    public void emit_not()
    {
        emit(NyarHeadCode.i64_not);
    }

    /// <summary>
    ///     鍙戝皠 i64 宸︾Щ
    /// </summary>
    public void emit_shl()
    {
        emit(NyarHeadCode.i64_shl);
    }

    /// <summary>
    ///     鍙戝皠 i64 鏈夌鍙峰彸绉?    /// </summary>
    public void emit_shr()
    {
        emit(NyarHeadCode.i64_shr_s);
    }

    #endregion

    #region 鏍囩涓庤烦杞?
    /// <summary>
    ///     鐢熸垚鍞竴鏍囩鍚?    /// </summary>
    public string generate_label(string prefix = "L")
    {
        return $"{prefix}{_nextLabelId++}";
    }

    /// <summary>
    ///     鍦ㄥ綋鍓嶆寚浠や綅缃爣璁版爣绛?    /// </summary>
    public void mark_label(string name)
    {
        _labelPositions[name] = current_position;
        _currentFunction.add_label(name);
    }

    /// <summary>
    ///     鍙戝皠鏃犳潯浠惰烦杞?    /// </summary>
    public void emit_jump(string label)
    {
        if (_labelPositions.TryGetValue(label, out var targetPos))
        {
            emit(NyarHeadCode.jump, new GenerateOperand.Label(label));
        }
        else
        {
            _pendingPatches.Add((label, current_position, false));
            emit(NyarHeadCode.jump, new GenerateOperand.Label(label));
        }
    }

    /// <summary>
    ///     鍙戝皠鏉′欢璺宠浆锛堟爤椤朵负 false 鏃惰烦杞級
    /// </summary>
    public void emit_jump_if_false(string label)
    {
        if (_labelPositions.TryGetValue(label, out var targetPos))
        {
            emit(NyarHeadCode.jump_if_false, new GenerateOperand.Label(label));
        }
        else
        {
            _pendingPatches.Add((label, current_position, true));
            emit(NyarHeadCode.jump_if_false, new GenerateOperand.Label(label));
        }
    }

    /// <summary>
    ///     鍙戝皠鏉′欢璺宠浆锛堟爤椤朵负 true 鏃惰烦杞級
    /// </summary>
    public void emit_jump_if_true(string label)
    {
        if (_labelPositions.TryGetValue(label, out var targetPos))
        {
            emit(NyarHeadCode.jump_if_true, new GenerateOperand.Label(label));
        }
        else
        {
            _pendingPatches.Add((label, current_position, true));
            emit(NyarHeadCode.jump_if_true, new GenerateOperand.Label(label));
        }
    }

    /// <summary>
    ///     鍙戝皠鍑芥暟璋冪敤
    /// </summary>
    public void emit_call(string funcName, int argCount)
    {
        emit(NyarHeadCode.call_static, new GenerateOperand.FuncRef(funcName,
            new GenerateFunctionType { parameters = [], results = [GenerateValueType.any] }));
    }

    /// <summary>
    ///     鍙戝皠璋冪敤鍐呯疆鍑芥暟/FFI
    /// </summary>
    public void emit_call_native(string funcName)
    {
        emit(NyarHeadCode.call_native, new GenerateOperand.Str(funcName));
    }

    /// <summary>
    ///     鍙戝皠杩斿洖鎸囦护
    /// </summary>
    public void emit_return()
    {
        emit(NyarHeadCode.@return);
    }

    /// <summary>
    ///     鍙戝皠寮瑰嚭鏍堥《
    /// </summary>
    public void emit_pop()
    {
        emit(NyarHeadCode.pop);
    }

    /// <summary>
    ///     鍙戝皠澶嶅埗鏍堥《
    /// </summary>
    public void emit_dup()
    {
        emit(NyarHeadCode.dup);
    }

    /// <summary>
    ///     鍙戝皠鏃犳潯浠惰烦杞紙涓嶈褰曞洖濉紝鐢ㄤ簬鏄庣‘鐨?forward jump锛?    /// </summary>
    public void emit_jump_forward(string label)
    {
        emit(NyarHeadCode.jump, new GenerateOperand.Label(label));
    }

    #endregion

    #region 寰幆鏍囩绠＄悊

    /// <summary>
    ///     杩涘叆寰幆浣滅敤鍩燂紝璁板綍 break 鍜?continue 鐨勮烦杞洰鏍囨爣绛?    /// </summary>
    /// <param name="breakLabel">break 璺宠浆鐩爣</param>
    /// <param name="continueLabel">continue 璺宠浆鐩爣</param>
    public void enter_loop(string breakLabel, string continueLabel)
    {
        _breakLabels.Push(breakLabel);
        _continueLabels.Push(continueLabel);
    }

    /// <summary>
    ///     绂诲紑寰幆浣滅敤鍩?    /// </summary>
    public void leave_loop()
    {
        _breakLabels.Pop();
        _continueLabels.Pop();
    }

    /// <summary>
    ///     鍙戝皠 break 璺宠浆
    /// </summary>
    public void emit_break()
    {
        if (_breakLabels.TryPeek(out var label))
        {
            emit_jump(label);
        }
    }

    /// <summary>
    ///     鍙戝皠 continue 璺宠浆
    /// </summary>
    public void emit_continue()
    {
        if (_continueLabels.TryPeek(out var label))
        {
            emit_jump(label);
        }
    }

    #endregion

    #region 瀛楃涓叉搷浣?
    /// <summary>
    ///     鍙戝皠瀛楃涓叉嫾鎺?    /// </summary>
    public void emit_utf8_concat()
    {
        emit(NyarHeadCode.utf8_concat);
    }

    /// <summary>
    ///     鍙戝皠瀛楃涓查暱搴?    /// </summary>
    public void emit_string_len()
    {
        emit(NyarHeadCode.utf8_len_chars);
    }

    #endregion

    #region 瀵硅薄鎿嶄綔

    /// <summary>
    ///     鍙戝皠鍒涘缓瀵硅薄
    /// </summary>
    public void emit_new_object()
    {
        emit(NyarHeadCode.new_object);
    }

    /// <summary>
    ///     鍙戝皠鑾峰彇灞炴€э紙瀛楁鍚嶄綔涓哄父閲忥級
    /// </summary>
    public void emit_get_field(string fieldName)
    {
        var index = _constants.add_string(fieldName);
        emit(NyarHeadCode.get_field, new GenerateOperand.Const(index, GenerateValueType.utf8));
    }

    /// <summary>
    ///     鍙戝皠璁剧疆灞炴€?    /// </summary>
    public void emit_set_field(string fieldName)
    {
        var index = _constants.add_string(fieldName);
        emit(NyarHeadCode.set_field, new GenerateOperand.Const(index, GenerateValueType.utf8));
    }

    /// <summary>
    ///     鍙戝皠鑾峰彇绱㈠紩
    /// </summary>
    public void emit_get_index()
    {
        emit(NyarHeadCode.get_offset_index);
    }

    /// <summary>
    ///     鍙戝皠璁剧疆绱㈠紩
    /// </summary>
    public void emit_set_index()
    {
        emit(NyarHeadCode.set_offset_index);
    }

    /// <summary>
    ///     鍙戝皠鏁扮粍 push
    /// </summary>
    public void emit_array_push()
    {
        emit(NyarHeadCode.array_push);
    }

    /// <summary>
    ///     鍙戝皠鑾峰彇闀垮害
    /// </summary>
    public void emit_length()
    {
        emit(NyarHeadCode.length);
    }

    #endregion

    #region 鍥炲～

    /// <summary>
    ///     瑙ｆ瀽鎵€鏈夊緟鍥炲～鐨勮烦杞爣绛?    /// </summary>
    private void resolve_pending_patches()
    {
        foreach (var (label, _ /*instructionIndex*/, _ /*isConditional*/) in _pendingPatches)
        {
            if (_labelPositions.TryGetValue(label, out var targetPos))
            {
                // 璺宠浆鎸囦护鏈韩宸插瓨鍦ㄤ簬鍑芥暟鎸囦护鍒楄〃涓紝鏃犻渶棰濆澶勭悊
                // Label 鎿嶄綔鏁板寘鍚爣绛惧悕锛孷M 杩愯鏃惰В鏋?            }
        }

        _pendingPatches.Clear();
    }

    #endregion
}
