using System.Diagnostics;
using Nyar.Types;
using Nyar.VM.Jit;
using Nyar.Binary.Nyar.Data;

using NyarNyarVM = Nyar.VM.NyarVM;

#pragma warning disable CS0618

namespace Nyar.Benchmarks;


/// <summary>

///     VM 执行性能基准测试

///     验证解释器 + JIT 在典型工作负载下的性能


/// </summary>
public sealed class VMBenchmark
{
    #region 常量

    private const int WARMUP_ITERATIONS = 50;
    private const int MEASUREMENT_ITERATIONS = 500;
    private const int FIB_N = 20;
    private const int LOOP_COUNT = 1_000_000;
    private const int CALL_DEPTH = 50;

    #endregion

    #region 属性

    
/// <summary>
    
///     Fibonacci(20) 解释器平均耗时（ms）
    

/// </summary>
    public float FibInterpretedMs { get; private set; }

    
/// <summary>
    
///     循环累加 1M 解释器平均耗时（ms）
    

/// </summary>
    public float LoopAccumInterpretedMs { get; private set; }

    
/// <summary>
    
///     函数调用深度 50 解释器平均耗时（ms）
    

/// </summary>
    public float CallDepthInterpretedMs { get; private set; }

    
/// <summary>
    
///     Fibonacci(20) JIT 平均耗时（ms）
    

/// </summary>
    public float FibJitMs { get; private set; }

    
/// <summary>
    
///     是否全部通过
    

/// </summary>
    public bool Success { get; private set; }

    #endregion

    #region 公有方法

    
/// <summary>
    
///     执行基准测试
    

/// </summary>
    public void Run()
    {
        FibInterpretedMs = BenchmarkFibInterpreted();
        LoopAccumInterpretedMs = BenchmarkLoopAccum();
        CallDepthInterpretedMs = BenchmarkCallDepth();
        FibJitMs = BenchmarkFibJit();

        Success = FibInterpretedMs < 50f
                  && LoopAccumInterpretedMs < 100f
                  && CallDepthInterpretedMs < 20f
                  && FibJitMs < FibInterpretedMs;

        Console.WriteLine("=== NyarVM Benchmark Result ===");
        Console.WriteLine($"Fibonacci({FIB_N}) Interpreted: {FibInterpretedMs:F2} ms (target < 50ms)");
        Console.WriteLine($"Loop Accum {LOOP_COUNT:N0}: {LoopAccumInterpretedMs:F2} ms (target < 100ms)");
        Console.WriteLine($"Call Depth {CALL_DEPTH}: {CallDepthInterpretedMs:F2} ms (target < 20ms)");
        Console.WriteLine($"Fibonacci({FIB_N}) JIT: {FibJitMs:F2} ms (target < interpreted)");
        Console.WriteLine($"JIT Speedup: {(FibInterpretedMs > 0 ? FibInterpretedMs / Math.Max(FibJitMs, 0.001f) : 0):F1}x");
        Console.WriteLine($"Overall: {(Success ? "✅" : "❌")}");
        Console.WriteLine("===============================");
    }

    #endregion

    #region 私有方法 - Fibonacci

    private static float BenchmarkFibInterpreted()
    {
        var vm = CreateVMWithFib(jitEnabled: false);

        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            vm.Run("bench_fib", "fib", Value.FromInt(FIB_N));
        }

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < MEASUREMENT_ITERATIONS; i++)
        {
            vm.Run("bench_fib", "fib", Value.FromInt(FIB_N));
        }

        sw.Stop();
        return sw.ElapsedMilliseconds / (float)MEASUREMENT_ITERATIONS;
    }

    private static float BenchmarkFibJit()
    {
        var vm = CreateVMWithFib(jitEnabled: true);

        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            vm.Run("bench_fib", "fib", Value.FromInt(FIB_N));
        }

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < MEASUREMENT_ITERATIONS; i++)
        {
            vm.Run("bench_fib", "fib", Value.FromInt(FIB_N));
        }

        sw.Stop();
        return sw.ElapsedMilliseconds / (float)MEASUREMENT_ITERATIONS;
    }

    #endregion

    #region 私有方法 - 循环累加

    private static float BenchmarkLoopAccum()
    {
        var vm = CreateVMWithLoopAccum();

        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            vm.Run("bench_loop", "accumulate", Value.FromInt(LOOP_COUNT));
        }

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < MEASUREMENT_ITERATIONS; i++)
        {
            vm.Run("bench_loop", "accumulate", Value.FromInt(LOOP_COUNT));
        }

        sw.Stop();
        return sw.ElapsedMilliseconds / (float)MEASUREMENT_ITERATIONS;
    }

    #endregion

    #region 私有方法 - 函数调用深度

    private static float BenchmarkCallDepth()
    {
        var vm = CreateVMWithCallDepth();

        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            vm.Run("bench_call", "deep_call", Value.FromInt(CALL_DEPTH));
        }

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < MEASUREMENT_ITERATIONS; i++)
        {
            vm.Run("bench_call", "deep_call", Value.FromInt(CALL_DEPTH));
        }

        sw.Stop();
        return sw.ElapsedMilliseconds / (float)MEASUREMENT_ITERATIONS;
    }

    #endregion

    #region 私有方法 - 字节码构建

    
/// <summary>
    
///     构建 Fibonacci 字节码模块
    
///     fib(n) = n <= 1 ? n : fib(n-1) + fib(n-2)
    

/// </summary>
    private static NyarNyarVM CreateVMWithFib(bool jitEnabled)
    {
        var bytecode = new BytecodeBuilder()
            .Const(0).StoreLocal(1)
            .LoadArg(0).Const(1).I32LeS()
            .JumpIfTrue("base_case")
            .LoadArg(0).Const(1).I32Sub().Call("fib")
            .LoadArg(0).Const(2).I32Sub().Call("fib")
            .I32Add()
            .Jump("end")
            .Label("base_case").LoadArg(0)
            .Label("end").Return()
            .Build("bench_fib", "fib", arity: 1, localCount: 1, out var module);

        var vm = new NyarNyarVM(new JitOptions { Enabled = jitEnabled });
        vm.Load(module);
        return vm;
    }

    
/// <summary>
    
///     构建循环累加字节码模块
    
///     accumulate(n) { i = 0; sum = 0; while (i < n) { sum = sum + i; i = i + 1 } return sum }
    

/// </summary>
    private static NyarNyarVM CreateVMWithLoopAccum()
    {
        var bytecode = new BytecodeBuilder()
            .Const(0).StoreLocal(1)
            .Const(0).StoreLocal(2)
            .Label("loop_check")
            .LoadLocal(1).LoadArg(0).I32LtS()
            .JumpIfFalse("loop_end")
            .LoadLocal(2).LoadLocal(1).I32Add().StoreLocal(2)
            .LoadLocal(1).Const(1).I32Add().StoreLocal(1)
            .Jump("loop_check")
            .Label("loop_end")
            .LoadLocal(2).Return()
            .Build("bench_loop", "accumulate", arity: 1, localCount: 2, out var module);

        var vm = new NyarNyarVM(new JitOptions { Enabled = false });
        vm.Load(module);
        return vm;
    }

    
/// <summary>
    
///     构建函数调用深度字节码模块
    
///     deep_call(n) = n <= 0 ? 0 : deep_call(n-1) + 1
    

/// </summary>
    private static NyarNyarVM CreateVMWithCallDepth()
    {
        var bytecode = new BytecodeBuilder()
            .LoadArg(0).Const(0).I32LeS()
            .JumpIfTrue("base_case")
            .LoadArg(0).Const(1).I32Sub().Call("deep_call")
            .Const(1).I32Add()
            .Jump("end")
            .Label("base_case").Const(0)
            .Label("end").Return()
            .Build("bench_call", "deep_call", arity: 1, localCount: 0, out var module);

        var vm = new NyarNyarVM(new JitOptions { Enabled = false });
        vm.Load(module);
        return vm;
    }

    #endregion

    #region 内部类型 - 字节码构建器

    
/// <summary>
    
///     简化字节码构建辅助类
    

/// </summary>
    private sealed class BytecodeBuilder
    {
        private readonly List<byte> _code = [];
        private readonly Dictionary<string, int> _labels = new();
        private readonly List<(string Label, int PatchOffset)> _pendingJumps = [];
        private readonly List<Value> _constants = [];

        public BytecodeBuilder Const(int value)
        {
            _constants.Add(Value.FromInt(value));
            _code.Add((byte)NyarHeadCode.Const);
            WriteI32(_constants.Count - 1);
            return this;
        }

        public BytecodeBuilder LoadArg(int index)
        {
            _code.Add((byte)NyarHeadCode.LoadArg);
            WriteI32(index);
            return this;
        }

        public BytecodeBuilder LoadLocal(int index)
        {
            _code.Add((byte)NyarHeadCode.LoadLocal);
            WriteI32(index);
            return this;
        }

        public BytecodeBuilder StoreLocal(int index)
        {
            _code.Add((byte)NyarHeadCode.StoreLocal);
            WriteI32(index);
            return this;
        }

        public BytecodeBuilder I32Add()
        {
            _code.Add((byte)NyarHeadCode.I32Add);
            return this;
        }

        public BytecodeBuilder I32Sub()
        {
            _code.Add((byte)NyarHeadCode.I32Sub);
            return this;
        }

        public BytecodeBuilder I32LtS()
        {
            _code.Add((byte)NyarHeadCode.I32LtS);
            return this;
        }

        public BytecodeBuilder I32LeS()
        {
            _code.Add((byte)NyarHeadCode.I32LeS);
            return this;
        }

        public BytecodeBuilder Jump(string label)
        {
            _code.Add((byte)NyarHeadCode.Jump);
            _pendingJumps.Add((label, _code.Count));
            WriteI32(0);
            return this;
        }

        public BytecodeBuilder JumpIfTrue(string label)
        {
            _code.Add((byte)NyarHeadCode.JumpIfTrue);
            _pendingJumps.Add((label, _code.Count));
            WriteI32(0);
            return this;
        }

        public BytecodeBuilder JumpIfFalse(string label)
        {
            _code.Add((byte)NyarHeadCode.JumpIfFalse);
            _pendingJumps.Add((label, _code.Count));
            WriteI32(0);
            return this;
        }

        public BytecodeBuilder Call(string functionName)
        {
            _code.Add((byte)NyarHeadCode.Call);
            WriteI32(0);
            return this;
        }

        public BytecodeBuilder Return()
        {
            _code.Add((byte)NyarHeadCode.Return);
            return this;
        }

        public BytecodeBuilder Label(string name)
        {
            _labels[name] = _code.Count;
            return this;
        }

        public byte[] Build(string moduleName, string functionName, int arity, int localCount, out NyarModule module)
        {
            foreach (var (label, patchOffset) in _pendingJumps)
            {
                if (_labels.TryGetValue(label, out var target))
                {
                    var offset = target - (patchOffset + 4);
                    WriteI32At(patchOffset, offset);
                }
            }

            module = new NyarModule(moduleName);
            module.Constants.AddRange(_constants);

            var func = new NyarFunction(functionName, arity, localCount, 0, _code.Count);
            func.Module = module;
            module.Functions.Add(func);

            var bytecode = _code.ToArray();
            module.RawBytecode = bytecode;
            return bytecode;
        }

        private void WriteI32(int value)
        {
            _code.Add((byte)(value & 0xFF));
            _code.Add((byte)((value >> 8) & 0xFF));
            _code.Add((byte)((value >> 16) & 0xFF));
            _code.Add((byte)((value >> 24) & 0xFF));
        }

        private void WriteI32At(int offset, int value)
        {
            _code[offset] = (byte)(value & 0xFF);
            _code[offset + 1] = (byte)((value >> 8) & 0xFF);
            _code[offset + 2] = (byte)((value >> 16) & 0xFF);
            _code[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }

    #endregion
}
