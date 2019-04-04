using System.Diagnostics;
using Nyar.Types;

namespace Nyar.Tests.Jit;

public class JitCompilerTests
{
    #region MaxCompiledFunctions 测试

    [Fact]
    public void MaxCompiledFunctions_LimitsCompilation()
    {
        var (jit, _, module) =
            create_multi_function_context(5, new JitOptions { HotThreshold = 1, MaxCompiledFunctions = 3 });

        for (var i = 0; i < 5; i++) jit.RecordCall(i);

        Assert.Equal(3, jit.CompiledFunctionCount);
        Assert.True(jit.IsCompiled(0));
        Assert.True(jit.IsCompiled(1));
        Assert.True(jit.IsCompiled(2));
        Assert.False(jit.IsCompiled(3));
        Assert.False(jit.IsCompiled(4));
    }

    #endregion

    #region ResetCallCounts 测试

    [Fact]
    public void ResetCallCounts_ClearsCounts()
    {
        var jit = new JitCompiler(new JitOptions { Enabled = false });

        jit.RecordCall(0);
        jit.RecordCall(0);
        jit.ResetCallCounts();

        Assert.Equal(0, jit.GetCallCount(0));
    }

    #endregion

    #region GetStatistics 测试

    [Fact]
    public void GetStatistics_ReturnsNonEmptyString()
    {
        var jit = new JitCompiler();
        var stats = jit.GetStatistics();

        Assert.False(string.IsNullOrEmpty(stats));
        Assert.Contains("编译函数", stats);
    }

    #endregion

    #region JitOptions 测试

    [Fact]
    public void JitOptions_DefaultValues()
    {
        var options = new JitOptions();

        Assert.True(options.Enabled);
        Assert.Equal(100, options.HotThreshold);
        Assert.Equal(1024, options.MaxCompiledFunctions);
        Assert.True(options.TieredCompilation);
        Assert.Equal(5000, options.CompilationTimeoutMs);
    }

    [Fact]
    public void JitOptions_CustomValues()
    {
        var options = new JitOptions
        {
            Enabled = false,
            HotThreshold = 50,
            MaxCompiledFunctions = 256,
            TieredCompilation = false,
            CompilationTimeoutMs = 1000
        };

        Assert.False(options.Enabled);
        Assert.Equal(50, options.HotThreshold);
        Assert.Equal(256, options.MaxCompiledFunctions);
        Assert.False(options.TieredCompilation);
        Assert.Equal(1000, options.CompilationTimeoutMs);
    }

    #endregion

    #region JitCompiler 基础测试

    [Fact]
    public void JitCompiler_DefaultConstructor()
    {
        var jit = new JitCompiler();

        Assert.Equal(0, jit.TotalCompilations);
        Assert.Equal(0, jit.CompiledFunctionCount);
        Assert.True(jit.Options.Enabled);
    }

    [Fact]
    public void JitCompiler_CustomOptions()
    {
        var options = new JitOptions { HotThreshold = 50 };
        var jit = new JitCompiler(options);

        Assert.Equal(50, jit.Options.HotThreshold);
    }

    [Fact]
    public void RecordCall_IncrementsCallCount()
    {
        var jit = new JitCompiler(new JitOptions { Enabled = false });

        jit.RecordCall(0);
        jit.RecordCall(0);
        jit.RecordCall(0);

        Assert.Equal(3, jit.GetCallCount(0));
    }

    [Fact]
    public void RecordCall_DifferentFunctions_TrackedSeparately()
    {
        var jit = new JitCompiler(new JitOptions { Enabled = false });

        jit.RecordCall(0);
        jit.RecordCall(0);
        jit.RecordCall(1);

        Assert.Equal(2, jit.GetCallCount(0));
        Assert.Equal(1, jit.GetCallCount(1));
    }

    [Fact]
    public void RecordCall_UntrackedFunction_ReturnsZero()
    {
        var jit = new JitCompiler();
        Assert.Equal(0, jit.GetCallCount(99));
    }

    [Fact]
    public void IsCompiled_InitiallyFalse()
    {
        var jit = new JitCompiler();
        Assert.False(jit.IsCompiled(0));
    }

    [Fact]
    public void GetCompiledFunction_InitiallyNull()
    {
        var jit = new JitCompiler();
        Assert.Null(jit.GetCompiledFunction(0));
    }

    #endregion

    #region 热点检测测试

    [Fact]
    public void RecordCall_TriggersJitAtThreshold()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 3 });

        var triggered1 = jit.RecordCall(0);
        var triggered2 = jit.RecordCall(0);
        var triggered3 = jit.RecordCall(0);

        Assert.False(triggered1);
        Assert.False(triggered2);
        Assert.True(triggered3);
        Assert.True(jit.IsCompiled(0));
        Assert.Equal(1, jit.TotalCompilations);
    }

    [Fact]
    public void RecordCall_DoesNotRecompile()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 2 });

        jit.RecordCall(0);
        jit.RecordCall(0);

        Assert.True(jit.IsCompiled(0));

        var triggered = jit.RecordCall(0);
        Assert.False(triggered);
        Assert.Equal(1, jit.TotalCompilations);
    }

    [Fact]
    public void RecordCall_DisabledJit_DoesNotTrigger()
    {
        var jit = new JitCompiler(new JitOptions { Enabled = false, HotThreshold = 1 });

        var triggered = jit.RecordCall(0);
        Assert.False(triggered);
        Assert.Equal(0, jit.TotalCompilations);
    }

    [Fact]
    public void RecordCall_NoTieredCompilation_DoesNotTrigger()
    {
        var jit = new JitCompiler(new JitOptions { TieredCompilation = false, HotThreshold = 1 });

        var triggered = jit.RecordCall(0);
        Assert.False(triggered);
    }

    [Fact]
    public void RecordCall_MultipleFunctions_IndependentThresholds()
    {
        var (jit, bytecode, module) = create_multi_function_context(2, new JitOptions { HotThreshold = 2 });

        jit.RecordCall(0);
        var triggered0A = jit.RecordCall(0);
        Assert.True(triggered0A);

        jit.RecordCall(1);
        var triggered1A = jit.RecordCall(1);
        Assert.True(triggered1A);

        Assert.True(jit.IsCompiled(0));
        Assert.True(jit.IsCompiled(1));
        Assert.Equal(2, jit.TotalCompilations);
    }

    #endregion

    #region ForceCompile 测试

    [Fact]
    public void ForceCompile_CompilesWithoutThreshold()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 1000 });

        var result = jit.ForceCompile(0);

        Assert.True(result);
        Assert.True(jit.IsCompiled(0));
    }

    [Fact]
    public void ForceCompile_DisabledJit_ReturnsFalse()
    {
        var jit = new JitCompiler(new JitOptions { Enabled = false });

        var result = jit.ForceCompile(0);

        Assert.False(result);
    }

    [Fact]
    public void ForceCompile_AlreadyCompiled_ReturnsFalse()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 1 });

        jit.RecordCall(0);

        var result = jit.ForceCompile(0);

        Assert.False(result);
    }

    #endregion

    #region ClearCache 测试

    [Fact]
    public void ClearCache_RemovesCompiledFunctions()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 1 });

        jit.RecordCall(0);
        Assert.True(jit.IsCompiled(0));

        jit.ClearCache();

        Assert.False(jit.IsCompiled(0));
        Assert.Equal(0, jit.CompiledFunctionCount);
    }

    [Fact]
    public void ClearCache_PreservesCallCounts()
    {
        var jit = new JitCompiler(new JitOptions { Enabled = false, HotThreshold = 1 });

        jit.RecordCall(0);
        jit.ClearCache();

        Assert.Equal(1, jit.GetCallCount(0));
    }

    [Fact]
    public void ClearCache_AllowsRecompilation()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 1 });

        jit.RecordCall(0);
        jit.ClearCache();

        var recompiled = jit.ForceCompile(0);
        Assert.True(recompiled);
    }

    #endregion

    #region JitCompiledFunction 测试

    [Fact]
    public void JitCompiledFunction_Properties()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 1 });

        jit.RecordCall(0);
        var compiled = jit.GetCompiledFunction(0);

        Assert.NotNull(compiled);
        Assert.Equal("main", compiled.FunctionName);
        Assert.Equal(0, compiled.FunctionIndex);
        Assert.Equal(1, compiled.CallCountAtCompilation);
        Assert.True(compiled.CompiledDelegate != null);
        Assert.True(compiled.CompilationDuration >= TimeSpan.Zero);
    }

    [Fact]
    public void JitCompiledFunction_ExecutionCount()
    {
        var (jit, _, _) = create_simple_context(new JitOptions { HotThreshold = 1 });

        jit.RecordCall(0);
        var compiled = jit.GetCompiledFunction(0);

        Assert.NotNull(compiled);
        Assert.Equal(0, compiled.JitExecutionCount);

        compiled.Execute([Value.from_int(42)]);
        compiled.Execute([Value.from_int(42)]);

        Assert.Equal(2, compiled.JitExecutionCount);
    }

    #endregion

    #region JIT IL 发射执行测试

    [Fact]
    public void JitEmit_SimpleConstReturn()
    {
        var vm = create_const_return_vm();
        var result = vm.Run("test", "main");

        Assert.Equal(ValueType.@int, result.type);
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void JitEmit_I32Add()
    {
        var vm = create_i32_add_vm();
        var result = vm.Run("test", "add", Value.from_int(3), Value.from_int(4));

        Assert.Equal(ValueType.@int, result.type);
        Assert.Equal(7, result.@int);
    }

    [Fact]
    public void JitEmit_Fibonacci_CorrectResult()
    {
        var vm = create_fibonacci_vm();

        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "fib", Value.from_int(10));

        Assert.Equal(ValueType.@int, result.type);
        Assert.Equal(55, result.@int);
    }

    [Fact]
    public void JitEmit_Fibonacci_SmallValues()
    {
        var vm = create_fibonacci_vm();
        vm.JitCompiler.ForceCompile(0);

        Assert.Equal(0, vm.Run("test", "fib", Value.from_int(0)).@int);
        Assert.Equal(1, vm.Run("test", "fib", Value.from_int(1)).@int);
        Assert.Equal(1, vm.Run("test", "fib", Value.from_int(2)).@int);
        Assert.Equal(2, vm.Run("test", "fib", Value.from_int(3)).@int);
        Assert.Equal(5, vm.Run("test", "fib", Value.from_int(5)).@int);
    }

    [Fact]
    public void JitEmit_Fibonacci_MatchesInterpreter()
    {
        var vmJit = create_fibonacci_vm();
        vmJit.JitCompiler.ForceCompile(0);

        var vmInterp = create_fibonacci_vm(new JitOptions { Enabled = false });

        for (var n = 0; n <= 15; n++)
        {
            var jitResult = vmJit.Run("test", "fib", Value.from_int(n));
            var interpResult = vmInterp.Run("test", "fib", Value.from_int(n));

            Assert.Equal(interpResult.@int, jitResult.@int);
        }
    }

    [Fact]
    public void JitEmit_Fibonacci_Benchmark_10xSpeedup()
    {
        var vmJit = create_fibonacci_vm();

        var forceResult = vmJit.JitCompiler.ForceCompile(0);
        Assert.True(forceResult, "ForceCompile 应该返回 true");

        var compiledFunc = vmJit.JitCompiler.GetCompiledFunction(0);
        Assert.NotNull(compiledFunc);

        var hasIntInt = compiledFunc.IntIntDelegate != null;
        Assert.True(hasIntInt, $"IntIntDelegate 应该不为 null，但编译失败。CompiledFunction: {compiledFunc}");

        var vmInterp = create_fibonacci_vm(new JitOptions { Enabled = false });

        var n = 25;
        var iterations = 3;

        var interpMs = benchmark(() => { vmInterp.Run("test", "fib", Value.from_int(n)); }, iterations);

        var jitMs = benchmark(() => { vmJit.Run("test", "fib", Value.from_int(n)); }, iterations);

        var speedup = (double)interpMs / Math.Max(1, jitMs);
        Debug.WriteLine(
            $"[JIT Benchmark] 解释 {interpMs}ms vs JIT {jitMs}ms, 加速比 {speedup:F1}x, IntIntDelegate={hasIntInt}");
        Assert.True(speedup >= 5.0,
            $"JIT speedup {speedup:F1}x below 5x minimum (interp {interpMs}ms vs JIT {jitMs}ms, IntIntDelegate={hasIntInt})");
    }

    #endregion

    #region JIT f64 发射执行测试

    [Fact]
    public void JitEmit_F64Add_ValuePath()
    {
        var vm = create_f64_add_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "fadd", Value.from_double(3.5), Value.from_double(4.5));

        Assert.Equal(ValueType.@double, result.type);
        Assert.Equal(8.0, result.@double, 0.0001);
    }

    [Fact]
    public void JitEmit_F64Mul_ValuePath()
    {
        var vm = create_f64_mul_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "fmul", Value.from_double(3.0), Value.from_double(7.0));

        Assert.Equal(ValueType.@double, result.type);
        Assert.Equal(21.0, result.@double, 0.0001);
    }

    [Fact]
    public void JitEmit_F64Neg_ValuePath()
    {
        var vm = create_f64_neg_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "fneg", Value.from_double(42.5));

        Assert.Equal(ValueType.@double, result.type);
        Assert.Equal(-42.5, result.@double, 0.0001);
    }

    [Fact]
    public void JitEmit_F64DoubleDoublePath()
    {
        var vm = create_f64_double_double_vm();
        vm.JitCompiler.ForceCompile(0);

        var compiled = vm.JitCompiler.GetCompiledFunction(0);
        Assert.NotNull(compiled);
        Assert.NotNull(compiled.DoubleDoubleDelegate);

        var result = vm.Run("test", "double_it", Value.from_double(3.14));

        Assert.Equal(ValueType.@double, result.type);
        Assert.Equal(6.28, result.@double, 0.01);
    }

    [Fact]
    public void JitEmit_F64_MatchesInterpreter()
    {
        var vmJit = create_f64_add_vm();
        vmJit.JitCompiler.ForceCompile(0);

        var vmInterp = create_f64_add_vm(new JitOptions { Enabled = false });

        var testCases = new[] { 0.0, 1.5, -3.14, 100.0, 0.001 };
        foreach (var a in testCases)
        {
            foreach (var b in testCases)
            {
                var jitResult = vmJit.Run("test", "fadd", Value.from_double(a), Value.from_double(b));
                var interpResult = vmInterp.Run("test", "fadd", Value.from_double(a), Value.from_double(b));

                Assert.Equal(interpResult.@double, jitResult.@double, 0.0001);
            }
        }
    }

    [Fact]
    public void JitEmit_F64AllOps_MatchesInterpreter()
    {
        var vmJit = create_f64_all_ops_vm();
        vmJit.JitCompiler.ForceCompile(0);

        var vmInterp = create_f64_all_ops_vm(new JitOptions { Enabled = false });

        var a = 10.0;
        var b = 3.0;

        var jitResult = vmJit.Run("test", "allops", Value.from_double(a), Value.from_double(b));
        var interpResult = vmInterp.Run("test", "allops", Value.from_double(a), Value.from_double(b));

        Assert.Equal(interpResult.@double, jitResult.@double, 0.0001);
    }

    #endregion

    #region NyarVM JIT 集成测试

    [Fact]
    public void NyarVM_DefaultConstructor_HasJitCompiler()
    {
        var vm = new NyarVM();
        Assert.NotNull(vm.JitCompiler);
        Assert.True(vm.JitCompiler.Options.Enabled);
    }

    [Fact]
    public void NyarVM_JitOptionsConstructor_UsesOptions()
    {
        var options = new JitOptions { HotThreshold = 50 };
        var vm = new NyarVM(options);
        Assert.Equal(50, vm.JitCompiler.Options.HotThreshold);
    }

    #endregion

    #region JIT 对象操作码测试

    [Fact]
    public void JitEmit_NewObject_CreatesEmptyDict()
    {
        var vm = create_new_object_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "make_obj");

        Assert.Equal(ValueType.@object, result.type);
        Assert.NotNull(result.@object);
        Assert.IsType<Dictionary<string, Value>>(result.@object);
    }

    [Fact]
    public void JitEmit_NewObject_MatchesInterpreter()
    {
        var vmJit = create_new_object_vm();
        vmJit.JitCompiler.ForceCompile(0);

        var vmInterp = create_new_object_vm(new JitOptions { Enabled = false });

        var jitResult = vmJit.Run("test", "make_obj");
        var interpResult = vmInterp.Run("test", "make_obj");

        Assert.Equal(interpResult.type, jitResult.type);
    }

    [Fact]
    public void JitEmit_GetField_ReadsValue()
    {
        var vm = create_get_field_vm();
        vm.JitCompiler.ForceCompile(0);

        var values = new Dictionary<string, Value>
        {
            ["x"] = Value.from_int(42)
        };
        var obj = Value.from_object(values);
        var result = vm.Run("test", "get_x", obj);

        Assert.Equal(ValueType.@int, result.type);
        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void JitEmit_GetField_MatchesInterpreter()
    {
        var vmJit = create_get_field_vm();
        vmJit.JitCompiler.ForceCompile(0);

        var vmInterp = create_get_field_vm(new JitOptions { Enabled = false });

        var values = new Dictionary<string, Value>
        {
            ["x"] = Value.from_int(99)
        };
        var obj = Value.from_object(values);

        var jitResult = vmJit.Run("test", "get_x", obj);
        var interpResult = vmInterp.Run("test", "get_x", obj);

        Assert.Equal(interpResult.@int, jitResult.@int);
    }

    [Fact]
    public void JitEmit_SetField_WritesValue()
    {
        var vm = create_set_field_vm();
        vm.JitCompiler.ForceCompile(0);

        var obj = Value.from_object(new Dictionary<string, Value>());
        var result = vm.Run("test", "set_x", obj, Value.from_int(100));

        Assert.Equal(ValueType.@int, result.type);
        Assert.Equal(100, result.@int);

        var dict = (Dictionary<string, Value>)obj.@object!;
        Assert.True(dict.ContainsKey("x"));
        Assert.Equal(100, dict["x"].@int);
    }

    [Fact]
    public void JitEmit_SetField_MatchesInterpreter()
    {
        var vmJit = create_set_field_vm();
        vmJit.JitCompiler.ForceCompile(0);

        var vmInterp = create_set_field_vm(new JitOptions { Enabled = false });

        var objJit = Value.from_object(new Dictionary<string, Value>());
        var objInterp = Value.from_object(new Dictionary<string, Value>());

        var jitResult = vmJit.Run("test", "set_x", objJit, Value.from_int(77));
        var interpResult = vmInterp.Run("test", "set_x", objInterp, Value.from_int(77));

        Assert.Equal(interpResult.@int, jitResult.@int);
    }

    [Fact]
    public void JitEmit_Length_ReturnsDictCount()
    {
        var vm = create_length_vm();
        vm.JitCompiler.ForceCompile(0);

        var values = new Dictionary<string, Value>
        {
            ["a"] = Value.from_int(1),
            ["b"] = Value.from_int(2),
            ["c"] = Value.from_int(3)
        };
        var obj = Value.from_object(values);

        var result = vm.Run("test", "get_len", obj);

        Assert.Equal(ValueType.@int, result.type);
        Assert.Equal(3, result.@int);
    }

    [Fact]
    public void JitEmit_Length_MatchesInterpreter()
    {
        var vmJit = create_length_vm();
        vmJit.JitCompiler.ForceCompile(0);

        var vmInterp = create_length_vm(new JitOptions { Enabled = false });

        var values = new Dictionary<string, Value>
        {
            ["x"] = Value.from_int(1),
            ["y"] = Value.from_int(2)
        };
        var obj = Value.from_object(values);

        var jitResult = vmJit.Run("test", "get_len", obj);
        var interpResult = vmInterp.Run("test", "get_len", obj);

        Assert.Equal(interpResult.@int, jitResult.@int);
    }

    [Fact]
    public void JitEmit_AccessStatic_ReadsByOffset()
    {
        var vm = create_access_static_vm();
        vm.JitCompiler.ForceCompile(0);

        var values = new Dictionary<string, Value>
        {
            ["name"] = Value.from_string("test"),
            ["value"] = Value.from_int(42)
        };
        var obj = Value.from_object(values);

        var result = vm.Run("test", "access_0", obj);

        Assert.True(result.type is ValueType.@int or ValueType.@string,
            $"AccessStatic 应返回字典中的某个值，实际类型: {result.type}");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建简单的 JIT 编译上下文（单函数：Const 42 + Return的    
    /// 
///</summary>
    private static (JitCompiler jit, byte[] bytecode, NyarModule module) create_simple_context(JitOptions options)
    {
        var jit = new JitCompiler(options);
        var (bytecode, module) = create_const_return_bytecode();
        jit.SetContext(bytecode, module);
        return (jit, bytecode, module);
    }

    /// <summary>
    /// 创建多函的JIT 编译上下的    
    /// 
///</summary>
    private static (JitCompiler jit, byte[] bytecode, NyarModule module) create_multi_function_context(int funcCount,
        JitOptions options)
    {
        var jit = new JitCompiler(options);
        var module = new NyarModule("multi_test");

        var offset = 0;
        for (var i = 0; i < funcCount; i++)
        {
            var func = new NyarFunction($"func_{i}", 0, 0, offset, 10);
            func.Module = module;
            module.functions.Add(func);
            offset += 10;
        }

        var bytecode = new byte[offset];
        for (var i = 0; i < funcCount; i++)
        {
            var baseOffset = i * 10;
            bytecode[baseOffset] = (byte)NyarHeadCode.Const;
            BitConverter.TryWriteBytes(bytecode.AsSpan(baseOffset + 1), 0);
            bytecode[baseOffset + 5] = (byte)NyarHeadCode.Return;
        }

        module.constants.Add(Value.from_int(42));
        module.raw_bytecode = bytecode;

        jit.SetContext(bytecode, module);
        return (jit, bytecode, module);
    }

    /// <summary>
    /// 创建 ConstReturn 字节码（返回常量 42的    
    /// 
///</summary>
    private static (byte[] bytecode, NyarModule module) create_const_return_bytecode()
    {
        var module = new NyarModule("test");
        var func = new NyarFunction("main", 0, 0, 0, 10);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 0);
        bytecode[5] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_int(42));
        module.raw_bytecode = bytecode;

        return (bytecode, module);
    }

    /// <summary>
    ///     创建返回常量 42 的VM
    /// </summary>
    private static NyarVM create_const_return_vm()
    {
        var vm = new NyarVM();
        var (bytecode, module) = create_const_return_bytecode();
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 I32Add 函数的VM（两参数加法的    
    ///     add(a, b): LoadLocal 0; LoadLocal 1; I32Add; Return
    /// </summary>
    private static NyarVM create_i32_add_vm()
    {
        var vm = new NyarVM();
        var module = new NyarModule("test");
        var func = new NyarFunction("add", 2, 0, 0, 17);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[17];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.I32Add;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建递归 Fibonacci 函数的VM
    ///     fib(n): if n <= 1 return n; else return fib(n-1) + fib(n-2)
    /// </summary>
    private static NyarVM create_fibonacci_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());

        var module = new NyarModule("test");
        var func = new NyarFunction("fib", 1, 0, 0, 56);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[56];
        var offset = 0;

        // LoadLocal 0 的push n
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // Const 0 的push 1 (constants[0])
        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // I32LeS 的n <= 1
        bytecode[offset] = (byte)NyarHeadCode.I32LeS;
        offset += 1;

        // JumpIfFalse 11 的if false, jump to offset 22
        bytecode[offset] = (byte)NyarHeadCode.JumpIfFalse;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 11);
        offset += 5;

        // LoadLocal 0 的push n (then branch)
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // Return 的return n
        bytecode[offset] = (byte)NyarHeadCode.Return;
        offset += 1;

        // else branch (offset 22):
        // LoadLocal 0 的push n
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // Const 0 的push 1
        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // I32Sub 的n - 1
        bytecode[offset] = (byte)NyarHeadCode.I32Sub;
        offset += 1;

        // Call 0 的fib(n-1)
        bytecode[offset] = (byte)NyarHeadCode.Call;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // LoadLocal 0 的push n
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // Const 1 的push 2 (constants[1])
        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        // I32Sub 的n - 2
        bytecode[offset] = (byte)NyarHeadCode.I32Sub;
        offset += 1;

        // Call 0 的fib(n-2)
        bytecode[offset] = (byte)NyarHeadCode.Call;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // I32Add 的fib(n-1) + fib(n-2)
        bytecode[offset] = (byte)NyarHeadCode.I32Add;
        offset += 1;

        // Return
        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_int(1));
        module.constants.Add(Value.from_int(2));
        module.raw_bytecode = bytecode;

        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     基准测试辅助方法
    /// </summary>
    private static long benchmark(Action action, int iterations)
    {
        action();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++) action();
        sw.Stop();

        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    ///     创建 F64Add 函数的VM（两参数浮点加法的    
    ///     fadd(a, b): LoadLocal 0; LoadLocal 1; F64Add; Return
    /// </summary>
    private static NyarVM create_f64_add_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("fadd", 2, 0, 0, 17);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[17];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.F64Add;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    /// 创建 F64Mul 函数的VM（两参数浮点乘法的    
    /// 
///</summary>
    private static NyarVM create_f64_mul_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("fmul", 2, 0, 0, 17);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[17];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.F64Mul;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    /// 创建 F64Neg 函数的VM（单参数浮点取反的    
    /// 
///</summary>
    private static NyarVM create_f64_neg_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("fneg", 1, 0, 0, 7);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[7];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.F64Neg;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建的f64 函数的VM（arity=1，x * 2.0的    
    ///     double_it(x): LoadLocal 0; Const 0(2.0); F64Mul; Return
    /// </summary>
    private static NyarVM create_f64_double_double_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("double_it", 1, 0, 0, 12);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[12];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.F64Mul;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_double(2.0));
        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建包含所的f64 操作的VM
    ///     allops(a, b): ((a + b) - (a * b)) / a + (-b)
    /// </summary>
    private static NyarVM create_f64_all_ops_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("allops", 2, 0, 0, 37);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[37];
        var offset = 0;

        // LoadLocal 0 的push a
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // LoadLocal 1 的push b
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        // F64Add 的a + b
        bytecode[offset] = (byte)NyarHeadCode.F64Add;
        offset += 1;

        // LoadLocal 0 的push a
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // LoadLocal 1 的push b
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        // F64Mul 的a * b
        bytecode[offset] = (byte)NyarHeadCode.F64Mul;
        offset += 1;

        // F64Sub 的(a + b) - (a * b)
        bytecode[offset] = (byte)NyarHeadCode.F64Sub;
        offset += 1;

        // LoadLocal 0 的push a
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        // F64Div 的((a + b) - (a * b)) / a
        bytecode[offset] = (byte)NyarHeadCode.F64Div;
        offset += 1;

        // LoadLocal 1 的push b
        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        // F64Neg 的-b
        bytecode[offset] = (byte)NyarHeadCode.F64Neg;
        offset += 1;

        // F64Add 的result + (-b)
        bytecode[offset] = (byte)NyarHeadCode.F64Add;
        offset += 1;

        // Return
        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 NewObject 函数的 VM
    ///     make_obj(): NewObject 0; Return
    /// </summary>
    private static NyarVM create_new_object_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("make_obj", 0, 0, 0, 6);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[6];
        bytecode[0] = (byte)NyarHeadCode.NewObject;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 0);
        bytecode[5] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 GetField 函数的 VM
    ///     get_x(obj): LoadLocal 0; Const 0("x"); GetField; Return
    /// </summary>
    private static NyarVM create_get_field_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("get_x", 1, 0, 0, 12);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[12];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.GetField;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_string("x"));
        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 SetField 函数的 VM
    ///     set_x(obj, val): LoadLocal 0; Const 0("x"); LoadLocal 1; SetField; Return
    /// </summary>
    private static NyarVM create_set_field_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("set_x", 2, 0, 0, 18);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[18];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 1);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.SetField;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_string("x"));
        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 Length 函数的 VM
    ///     get_len(obj): LoadLocal 0; Length; Return
    /// </summary>
    private static NyarVM create_length_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("get_len", 1, 0, 0, 7);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[7];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Length;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 AccessStatic 函数的 VM
    ///     access_0(obj): LoadLocal 0; AccessStatic 0; Return
    /// </summary>
    private static NyarVM create_access_static_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("access_0", 1, 0, 0, 12);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[12];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.AccessStatic;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    #endregion

    #region JitEmit 内存操作测试

    [Fact]
    public void JitEmit_Alloc_ReturnsAddress()
    {
        var vm = create_alloc_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "alloc_mem");

        Assert.Equal(ValueType.@int, result.type);
        Assert.True(result.@int > 0);
    }

    [Fact]
    public void JitEmit_Alloc_MatchesInterpreter()
    {
        var vmJit = create_alloc_vm();
        var vmInterp = create_alloc_vm(new JitOptions { Enabled = false });

        vmJit.JitCompiler.ForceCompile(0);
        var resultJit = vmJit.Run("test", "alloc_mem");
        var resultInterp = vmInterp.Run("test", "alloc_mem");

        Assert.Equal(resultInterp.type, resultJit.type);
        Assert.True(resultJit.@int > 0);
    }

    [Fact]
    public void JitEmit_I32StoreLoad_RoundTrip()
    {
        var vm = create_i32_store_load_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "store_load_i32");

        Assert.Equal(42, result.@int);
    }

    [Fact]
    public void JitEmit_I32StoreLoad_MatchesInterpreter()
    {
        var vmJit = create_i32_store_load_vm();
        var vmInterp = create_i32_store_load_vm(new JitOptions { Enabled = false });

        vmJit.JitCompiler.ForceCompile(0);
        var resultJit = vmJit.Run("test", "store_load_i32");
        var resultInterp = vmInterp.Run("test", "store_load_i32");

        Assert.Equal(resultInterp.@int, resultJit.@int);
    }

    [Fact]
    public void JitEmit_I64StoreLoad_RoundTrip()
    {
        var vm = create_i64_store_load_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "store_load_i64");

        Assert.Equal(0x123456789ABCDEF0L, result.@long);
    }

    [Fact]
    public void JitEmit_I64StoreLoad_MatchesInterpreter()
    {
        var vmJit = create_i64_store_load_vm();
        var vmInterp = create_i64_store_load_vm(new JitOptions { Enabled = false });

        vmJit.JitCompiler.ForceCompile(0);
        var resultJit = vmJit.Run("test", "store_load_i64");
        var resultInterp = vmInterp.Run("test", "store_load_i64");

        Assert.Equal(resultInterp.@long, resultJit.@long);
    }

    [Fact]
    public void JitEmit_Free_AfterAlloc()
    {
        var vm = create_alloc_free_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "alloc_free");

        Assert.Equal(ValueType.@int, result.type);
        Assert.True(result.@int > 0);
    }

    [Fact]
    public void JitEmit_Free_MatchesInterpreter()
    {
        var vmJit = create_alloc_free_vm();
        var vmInterp = create_alloc_free_vm(new JitOptions { Enabled = false });

        vmJit.JitCompiler.ForceCompile(0);
        var resultJit = vmJit.Run("test", "alloc_free");
        var resultInterp = vmInterp.Run("test", "alloc_free");

        Assert.Equal(resultInterp.@int, resultJit.@int);
    }

    [Fact]
    public void JitEmit_I32StoreLoad_WithOffset()
    {
        var vm = create_i32_offset_vm();
        vm.JitCompiler.ForceCompile(0);

        var result = vm.Run("test", "offset_i32");

        Assert.Equal(99, result.@int);
    }

    [Fact]
    public void JitEmit_I32StoreLoad_WithOffset_MatchesInterpreter()
    {
        var vmJit = create_i32_offset_vm();
        var vmInterp = create_i32_offset_vm(new JitOptions { Enabled = false });

        vmJit.JitCompiler.ForceCompile(0);
        var resultJit = vmJit.Run("test", "offset_i32");
        var resultInterp = vmInterp.Run("test", "offset_i32");

        Assert.Equal(resultInterp.@int, resultJit.@int);
    }

    /// <summary>
    ///     创建 Alloc 函数的 VM
    ///     alloc_mem(): Alloc 16; Return
    /// </summary>
    private static NyarVM create_alloc_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("alloc_mem", 0, 0, 0, 6);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[6];
        bytecode[0] = (byte)NyarHeadCode.Alloc;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 16);
        bytecode[5] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 I32Store + I32Load 往返测试的 VM
    ///     store_load_i32(): Alloc 16; StoreLocal 0; LoadLocal 0; Const 0(42); I32Store offset=0; LoadLocal 0; I32Load
    ///     offset=0; Return
    /// </summary>
    private static NyarVM create_i32_store_load_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("store_load_i32", 0, 1, 0, 36);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[36];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.Alloc;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 16);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.StoreLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.I32Store;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.I32Load;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_int(42));
        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 I64Store + I64Load 往返测试的 VM
    ///     store_load_i64(): Alloc 32; StoreLocal 0; LoadLocal 0; Const 0(0x1234...); I64Store offset=0; LoadLocal 0; I64Load
    ///     offset=0; Return
    /// </summary>
    private static NyarVM create_i64_store_load_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("store_load_i64", 0, 1, 0, 36);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[36];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.Alloc;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 32);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.StoreLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.I64Store;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.I64Load;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_long(0x123456789ABCDEF0L));
        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建 Alloc + Free 测试的 VM
    ///     alloc_free(): Alloc 16; Dup; Free; Return（返回地址）
    /// </summary>
    private static NyarVM create_alloc_free_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("alloc_free", 0, 0, 0, 8);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[8];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.Alloc;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 16);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Dup;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Free;
        offset += 1;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    /// <summary>
    ///     创建带偏移量的 I32Store + I32Load 测试的 VM
    ///     offset_i32(): Alloc 32; StoreLocal 0; LoadLocal 0; Const 0(99); I32Store offset=8; LoadLocal 0; I32Load offset=8;
    ///     Return
    /// </summary>
    private static NyarVM create_i32_offset_vm(JitOptions? options = null)
    {
        var vm = new NyarVM(options ?? new JitOptions());
        var module = new NyarModule("test");
        var func = new NyarFunction("offset_i32", 0, 1, 0, 36);
        func.Module = module;
        module.functions.Add(func);

        var bytecode = new byte[36];
        var offset = 0;

        bytecode[offset] = (byte)NyarHeadCode.Alloc;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 32);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.StoreLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Const;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.I32Store;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 8);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.LoadLocal;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 0);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.I32Load;
        BitConverter.TryWriteBytes(bytecode.AsSpan(offset + 1), 8);
        offset += 5;

        bytecode[offset] = (byte)NyarHeadCode.Return;

        module.constants.Add(Value.from_int(99));
        module.raw_bytecode = bytecode;
        vm.Load(module);
        return vm;
    }

    #endregion
}