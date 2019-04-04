using Nyar.Types;

namespace Nyar.Tests.Jit;

public class OsrTests
{
    #region OsrCompiler 测试

    [Fact]
    public void OsrCompiler_UnsupportedOpcode_ReturnsNull()
    {
        var compiler = new OsrCompiler();
        var bytecode = new byte[6];
        bytecode[0] = (byte)NyarHeadCode.Throw;
        bytecode[1] = (byte)NyarHeadCode.Return;

        var module = new NyarModule("test");
        var func = new NyarFunction("f", 0, 0, 0, 2);
        func.Module = module;
        module.functions.Add(func);
        module.raw_bytecode = bytecode;

        var result = compiler.Compile(0, bytecode, module, 0, 0, 0);

        Assert.Null(result);
    }

    #endregion

    #region OsrManager 测试

    [Fact]
    public void OsrManager_RecordBackEdge_ForwardJump_NotOsr()
    {
        var osr = new OsrManager();
        var result = osr.RecordBackEdge(0, 100, 50);

        Assert.False(result);
    }

    [Fact]
    public void OsrManager_RecordBackEdge_BackwardJump_CountsUp()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 3
        };

        Assert.False(osr.RecordBackEdge(0, 10, 50));
        Assert.False(osr.RecordBackEdge(0, 10, 50));
        Assert.True(osr.RecordBackEdge(0, 10, 50));
    }

    [Fact]
    public void OsrManager_RecordBackEdge_Disabled_NeverTriggers()
    {
        var osr = new OsrManager
        {
            Enabled = false,
            OsrThreshold = 1
        };

        Assert.False(osr.RecordBackEdge(0, 10, 50));
    }

    [Fact]
    public void OsrManager_RegisterOsrEntry_CreatesEntry()
    {
        var osr = new OsrManager();
        var entry = osr.RegisterOsrEntry(0, 10, 2, 3);

        Assert.Equal(0, entry.FunctionIndex);
        Assert.Equal(10, entry.BytecodePc);
        Assert.Equal(2, entry.StackDepth);
        Assert.Equal(3, entry.LocalCount);
        Assert.False(entry.IsCompiled);
    }

    [Fact]
    public void OsrManager_RegisterOsrEntry_SameKey_ReturnsExisting()
    {
        var osr = new OsrManager();
        var entry1 = osr.RegisterOsrEntry(0, 10, 2, 3);
        var entry2 = osr.RegisterOsrEntry(0, 10, 5, 6);

        Assert.Same(entry1, entry2);
    }

    [Fact]
    public void OsrManager_FindCompiledOsrEntry_NotCompiled_ReturnsNull()
    {
        var osr = new OsrManager();
        osr.RegisterOsrEntry(0, 10, 2, 3);

        Assert.Null(osr.FindCompiledOsrEntry(0, 10));
    }

    [Fact]
    public void OsrManager_FindCompiledOsrEntry_Compiled_ReturnsEntry()
    {
        var osr = new OsrManager();
        var entry = osr.RegisterOsrEntry(0, 10, 2, 3);

        Func<Value[], Value[], Value> dummy = (l, s) => Value.@null;
        osr.OnOsrCompiled(entry, dummy);

        var found = osr.FindCompiledOsrEntry(0, 10);
        Assert.NotNull(found);
        Assert.Same(entry, found);
    }

    [Fact]
    public void OsrManager_OnOsrTransition_IncrementCount()
    {
        var osr = new OsrManager();
        osr.OnOsrTransition();
        osr.OnOsrTransition();
        osr.OnOsrTransition();

        var stats = osr.GetStatistics();
        Assert.Contains("OSR 迁移: 3", stats);
    }

    [Fact]
    public void OsrManager_Clear_ResetsAll()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 1
        };
        osr.RecordBackEdge(0, 10, 50);
        osr.RegisterOsrEntry(0, 10, 2, 3);
        osr.OnOsrTransition();

        osr.Clear();

        var stats = osr.GetStatistics();
        Assert.Contains("OSR 编译: 0", stats);
        Assert.Contains("OSR 迁移: 0", stats);
    }

    [Fact]
    public void OsrManager_DifferentFunctions_Independent()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 2
        };

        osr.RecordBackEdge(0, 10, 50);
        Assert.False(osr.RecordBackEdge(1, 10, 50));
        Assert.True(osr.RecordBackEdge(0, 10, 50));
    }

    #endregion

    #region OsrEntry 测试

    [Fact]
    public void OsrEntry_Properties_Correct()
    {
        var entry = new OsrEntry(3, 42, 5, 8);

        Assert.Equal(3, entry.FunctionIndex);
        Assert.Equal(42, entry.BytecodePc);
        Assert.Equal(5, entry.StackDepth);
        Assert.Equal(8, entry.LocalCount);
        Assert.False(entry.IsCompiled);
    }

    [Fact]
    public void OsrEntry_SetCompiledDelegate_IsCompiledTrue()
    {
        var entry = new OsrEntry(0, 0, 0, 0);
        Assert.False(entry.IsCompiled);

        Func<Value[], Value[], Value> dummy = (l, s) => Value.@null;
        entry.CompiledDelegate = dummy;

        Assert.True(entry.IsCompiled);
    }

    #endregion

    #region OSR 集成测试

    [Fact]
    public void OsrIntegration_LoopBackEdge_TriggersOsr()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 100
        };

        for (var i = 0; i < 99; i++)
        {
            var shouldOsr = osr.RecordBackEdge(0, 10, 50);
            Assert.False(shouldOsr);
        }

        var result = osr.RecordBackEdge(0, 10, 50);
        Assert.True(result);
    }

    [Fact]
    public void OsrIntegration_FullCycle_RegisterCompileFind()
    {
        var osr = new OsrManager();
        var entry = osr.RegisterOsrEntry(0, 10, 2, 3);

        Assert.Null(osr.FindCompiledOsrEntry(0, 10));

        Func<Value[], Value[], Value> compiled = (locals, stack) =>
        {
            return locals.Length > 0 ? locals[0] : Value.@null;
        };
        osr.OnOsrCompiled(entry, compiled);

        var found = osr.FindCompiledOsrEntry(0, 10);
        Assert.NotNull(found);
        Assert.True(found.IsCompiled);

        var locals = new[] { Value.from_int(42) };
        var stack = new[] { Value.from_int(1), Value.from_int(2) };
        var result = found.CompiledDelegate!(locals, stack);
        Assert.Equal(42, result.@int);
    }

    #endregion

    #region OSR 统计测试

    [Fact]
    public void OsrManager_GetStatistics_ContainsAllFields()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 1
        };
        osr.RecordBackEdge(0, 10, 50);
        osr.RegisterOsrEntry(0, 10, 2, 3);
        osr.OnOsrTransition();

        var stats = osr.GetStatistics();

        Assert.Contains("OSR 编译: 0", stats);
        Assert.Contains("OSR 迁移: 1", stats);
        Assert.Contains("回边计数器: 1", stats);
    }

    [Fact]
    public void OsrManager_MultipleTransitions_CountsCorrectly()
    {
        var osr = new OsrManager();
        for (var i = 0; i < 5; i++) osr.OnOsrTransition();

        var stats = osr.GetStatistics();
        Assert.Contains("OSR 迁移: 5", stats);
    }

    #endregion

    #region OsrCompiler 兼容性测试

    [Fact]
    public void OsrCompiler_SupportedOpcodes_ReturnsNonNull()
    {
        var compiler = new OsrCompiler();
        var bytecode = new byte[11];
        bytecode[0] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 0);
        bytecode[5] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(6), 0);
        bytecode[10] = (byte)NyarHeadCode.Return;

        var module = new NyarModule("test");
        var func = new NyarFunction("f", 1, 0, 0, 11);
        func.Module = module;
        module.functions.Add(func);
        module.constants.Add(Value.from_int(42));
        module.raw_bytecode = bytecode;

        var result = compiler.Compile(0, bytecode, module, 0, 1, 0);

        Assert.NotNull(result);
    }

    [Fact]
    public void OsrCompiler_MixedSupportedUnsupported_ReturnsNull()
    {
        var compiler = new OsrCompiler();
        var bytecode = new byte[7];
        bytecode[0] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 0);
        bytecode[5] = (byte)NyarHeadCode.Throw;
        bytecode[6] = (byte)NyarHeadCode.Return;

        var module = new NyarModule("test");
        var func = new NyarFunction("f", 0, 0, 0, 7);
        func.Module = module;
        module.functions.Add(func);
        module.constants.Add(Value.from_int(42));
        module.raw_bytecode = bytecode;

        var result = compiler.Compile(0, bytecode, module, 0, 0, 0);

        Assert.Null(result);
    }

    #endregion

    #region OsrEntry 边界测试

    [Fact]
    public void OsrEntry_ZeroValues_Valid()
    {
        var entry = new OsrEntry(0, 0, 0, 0);

        Assert.Equal(0, entry.FunctionIndex);
        Assert.Equal(0, entry.BytecodePc);
        Assert.Equal(0, entry.StackDepth);
        Assert.Equal(0, entry.LocalCount);
        Assert.False(entry.IsCompiled);
    }

    [Fact]
    public void OsrEntry_CompiledDelegate_PreservesFunction()
    {
        var entry = new OsrEntry(0, 0, 2, 1);

        Func<Value[], Value[], Value> addFunc = (locals, stack) =>
        {
            var a = locals[0].@int;
            var b = stack[0].@int;
            return Value.from_int(a + b);
        };
        entry.CompiledDelegate = addFunc;

        var locals = new[] { Value.from_int(10) };
        var stack = new[] { Value.from_int(20) };
        var result = entry.CompiledDelegate!(locals, stack);

        Assert.Equal(30, result.@int);
    }

    #endregion

    #region OSR 阈值测试

    [Fact]
    public void OsrManager_CustomThreshold_Respected()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 500
        };

        for (var i = 0; i < 499; i++) Assert.False(osr.RecordBackEdge(0, 10, 50));

        Assert.True(osr.RecordBackEdge(0, 10, 50));
    }

    [Fact]
    public void OsrManager_SameLoopDifferentFunction_IndependentCounting()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 3
        };

        osr.RecordBackEdge(0, 10, 50);
        osr.RecordBackEdge(1, 10, 50);
        osr.RecordBackEdge(0, 10, 50);
        osr.RecordBackEdge(1, 10, 50);

        Assert.True(osr.RecordBackEdge(0, 10, 50));
        Assert.True(osr.RecordBackEdge(1, 10, 50));
    }

    [Fact]
    public void OsrManager_SameFunctionDifferentLoopHeads_IndependentCounting()
    {
        var osr = new OsrManager
        {
            OsrThreshold = 2
        };

        osr.RecordBackEdge(0, 10, 50);
        osr.RecordBackEdge(0, 20, 60);

        Assert.True(osr.RecordBackEdge(0, 10, 50));
        Assert.True(osr.RecordBackEdge(0, 20, 60));
    }

    #endregion
}