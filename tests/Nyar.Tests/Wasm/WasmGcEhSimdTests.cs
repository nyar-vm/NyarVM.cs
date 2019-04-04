using Acorn.Wasm.Data;
using Nyar.Assembler;

namespace Nyar.Tests.Wasm;

/// <summary>
/// WASM 后端最小能力测试，覆盖当前已实现的类型映射与基础指令发射的
///</summary>
public class WasmGcEhSimdTests
{
    #region 操作码映射测试

    [Fact]
    public void OpcodeMapping_I32Add_GeneratesValidBody()
    {
        var unit = create_simple_add_unit();
        var moduleData = convert(unit);

        Assert.True(moduleData.codes.Count > 0);
        Assert.True(moduleData.codes[0].body.Length > 0);
    }

    [Fact]
    public void OpcodeMapping_F64Sqrt_GeneratesValidBody()
    {
        var unit = create_unit_with_f64_sqrt();
        var moduleData = convert(unit);

        Assert.True(moduleData.codes.Count > 0);
        var body = moduleData.codes[0].body;
        Assert.True(body.Length > 0, "F64Sqrt 应生成有效的函数体。");
    }

    [Fact]
    public void OpcodeMapping_EmptyNonVoidFunction_EmitsDefaultReturnValue()
    {
        var unit = new GenerateModule("default_return");
        unit.AddFunction(new GenerateFunction("ref_func", "anyref"));
        var moduleData = convert(unit);

        Assert.True(moduleData.codes.Count > 0);
        var body = moduleData.codes[0].body;
        Assert.True(body.Length > 1, "非 void 函数应补齐默认返回。");
    }

    [Fact]
    public void OpcodeMapping_LocalLoadStore_EmitsLocalInstructionsWithParameterOffset()
    {
        var unit = create_unit_with_local_round_trip();
        var moduleData = convert(unit);

        var body = moduleData.codes[0].body;
        var localSetIndex = Array.IndexOf(body, (byte)WasmOpcode.local_set);
        var localGetIndex = Array.IndexOf(body, (byte)WasmOpcode.local_get);

        Assert.True(localSetIndex >= 0, "应发射 `local.set`。");
        Assert.True(localGetIndex >= 0, "应发射 `local.get`。");
        Assert.True(localSetIndex + 1 < body.Length, "`local.set` 后应包含局部槽位索引。");
        Assert.True(localGetIndex + 1 < body.Length, "`local.get` 后应包含局部槽位索引。");
        Assert.Equal(1, body[localSetIndex + 1]);
        Assert.Equal(1, body[localGetIndex + 1]);
    }

    #endregion

    #region 类型映射测试

    [Fact]
    public void TypeMapping_V128_MapsCorrectly()
    {
        var unit = new GenerateModule("simd_types");
        var func = new GenerateFunction("simd_func", "void");
        func.AddParameter("v", "v128");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);

        var moduleData = convert(unit);

        Assert.True(moduleData.types.Count > 0);
        var funcType = moduleData.types[0];
        Assert.Equal(WasmValueType.v128, funcType.parameters[0]);
    }

    [Fact]
    public void TypeMapping_AnyRef_MapsCorrectly()
    {
        var unit = new GenerateModule("ref_types");
        var func = new GenerateFunction("ref_func", "void");
        func.AddParameter("obj", "anyref");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);

        var moduleData = convert(unit);

        Assert.True(moduleData.types.Count > 0);
        var funcType = moduleData.types[0];
        Assert.Equal(WasmValueType.any_ref, funcType.parameters[0]);
    }

    [Fact]
    public void TypeMapping_Funcref_MapsCorrectly()
    {
        var unit = new GenerateModule("funcref_types");
        var func = new GenerateFunction("funcref_func", "void");
        func.AddParameter("f", "funcref");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);

        var moduleData = convert(unit);

        Assert.True(moduleData.types.Count > 0);
        var funcType = moduleData.types[0];
        Assert.Equal(WasmValueType.func_ref, funcType.parameters[0]);
    }

    [Fact]
    public void TypeMapping_ExternRef_MapsCorrectly()
    {
        var unit = new GenerateModule("externref_types");
        var func = new GenerateFunction("externref_func", "void");
        func.AddParameter("e", "externref");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);

        var moduleData = convert(unit);

        Assert.True(moduleData.types.Count > 0);
        var funcType = moduleData.types[0];
        Assert.Equal(WasmValueType.extern_ref, funcType.parameters[0]);
    }

    [Fact]
    public void TypeMapping_LocalVariables_AreDeclaredInCodeSection()
    {
        var unit = create_unit_with_local_round_trip();
        var moduleData = convert(unit);

        var code = moduleData.codes[0];
        var local = Assert.Single(code.locals);
        Assert.Equal(1u, local.count);
        Assert.Equal(WasmValueType.int32, local.type);
    }

    #endregion

    #region 辅助方法

    private static GenerateModule create_simple_add_unit()
    {
        var unit = new GenerateModule("add_test");
        var func = new GenerateFunction("add", "i32");
        func.AddParameter("a", "i32");
        func.AddParameter("b", "i32");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.LoadArg,
            new GenerateOperand.Param(0, GenerateValueType.I32)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.LoadArg,
            new GenerateOperand.Param(1, GenerateValueType.I32)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.I32Add));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);
        return unit;
    }

    private static GenerateModule create_unit_with_f64_sqrt()
    {
        var unit = new GenerateModule("sqrt_test");
        var func = new GenerateFunction("sqrt_func", "f64");
        func.AddParameter("x", "f64");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.LoadArg,
            new GenerateOperand.Param(0, GenerateValueType.F64)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.F64Sqrt));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);
        return unit;
    }

    private static GenerateModule create_unit_with_local_round_trip()
    {
        var unit = new GenerateModule("local_round_trip");
        var func = new GenerateFunction("local_round_trip", "i32");
        func.AddParameter("value", "i32");
        func.AddLocalVariable("copy", "i32", 0);
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.LoadArg,
            new GenerateOperand.Param(0, GenerateValueType.I32)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.StoreLocal,
            new GenerateOperand.Local(0, GenerateValueType.I32)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.LoadLocal,
            new GenerateOperand.Local(0, GenerateValueType.I32)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);
        return unit;
    }

    private static WasmModuleData convert(GenerateModule unit)
    {
        return WasmBackend.ConvertModule(unit, new CompilationOptions());
    }

    #endregion
}