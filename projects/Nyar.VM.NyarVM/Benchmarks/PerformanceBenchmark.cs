using System.Diagnostics;
using System.Text;
using Nyar.Types;
using Nyar.VM.NyarVM.Jit;
using Std.Data.Binary.NyarIR.Data;
using RuntimeNyarFunction = Nyar.Types.NyarFunction;

namespace Nyar.VM.NyarVM.Benchmarks;

/// <summary>
///     NyarVM 性能基准测试套件
///     覆盖算术、循环、函数调用、对象操作、内存操作等典型工作负载
///     提供解释模式和 JIT 模式的对比数据
/// </summary>
public sealed class PerformanceBenchmark
{
    #region 私有方法 - 循环累加

    private static double benchmark_loop_accum(int count)
    {
        var bc = new BenchBytecodeBuilder();
        bc.@const(0);
        bc.store_local(1);
        bc.@const(0);
        bc.store_local(2);

        var loopCheck = bc.position;
        bc.load_local(1);
        bc.load_arg(0);
        bc.i32_lt_s();
        var exitJump = bc.reserve_jump_if_false();

        bc.load_local(2);
        bc.load_local(1);
        bc.i32_add();
        bc.store_local(2);

        bc.load_local(1);
        bc.@const(1);
        bc.i32_add();
        bc.store_local(1);

        bc.jump(loopCheck - (bc.position + 5));

        bc.patch_jump_if_false(exitJump, bc.position);
        bc.load_local(2);
        bc.@return();

        var (bytecode, module) = bc.build("bench_loop", "accumulate", 1, 2);

        var vm = new NyarVm();
        vm.load(module);

        var sw = Stopwatch.StartNew();
        vm.run("bench_loop", "accumulate", Value.from_int(count));
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    #endregion

    #region 私有方法 - 函数调用深度

    private static double benchmark_call_depth(int depth)
    {
        var bc = new BenchBytecodeBuilder();
        bc.load_arg(0);
        bc.@const(0);
        bc.i32_le_s();
        var baseJump = bc.reserve_jump_if_true();

        bc.load_arg(0);
        bc.@const(1);
        bc.i32_sub();
        bc.call(0);
        bc.@const(1);
        bc.i32_add();
        var endJump = bc.reserve_jump();

        bc.patch_jump_if_true(baseJump, bc.position);
        bc.@const(0);

        bc.patch_jump(endJump, bc.position);
        bc.@return();

        var (bytecode, module) = bc.build("bench_call", "deep_call", 1, 0);

        var vm = new NyarVm();
        vm.load(module);

        var sw = Stopwatch.StartNew();
        vm.run("bench_call", "deep_call", Value.from_int(depth));
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    #endregion

    #region 私有方法 - 整数算术

    private static double benchmark_arithmetic_int(int ops)
    {
        var bc = new BenchBytecodeBuilder();
        bc.@const(1);
        bc.store_local(1);
        bc.@const(0);
        bc.store_local(2);

        var loopCheck = bc.position;
        bc.load_local(2);
        bc.load_arg(0);
        bc.i32_lt_s();
        var exitJump = bc.reserve_jump_if_false();

        bc.load_local(1);
        bc.@const(3);
        bc.i32_add();
        bc.@const(2);
        bc.i32_sub();
        bc.@const(4);
        bc.i32_mul();
        bc.@const(2);
        bc.i32_div();
        bc.store_local(1);

        bc.load_local(2);
        bc.@const(1);
        bc.i32_add();
        bc.store_local(2);

        bc.jump(loopCheck - (bc.position + 5));

        bc.patch_jump_if_false(exitJump, bc.position);
        bc.load_local(1);
        bc.@return();

        var (bytecode, module) = bc.build("bench_int", "int_ops", 1, 2);

        var vm = new NyarVm();
        vm.load(module);

        var sw = Stopwatch.StartNew();
        vm.run("bench_int", "int_ops", Value.from_int(ops));
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    #endregion

    #region 私有方法 - 浮点算术

    private static double benchmark_arithmetic_float(int ops)
    {
        var bc = new BenchBytecodeBuilder();
        bc.@const(0);
        bc.store_local(1);
        bc.@const(0);
        bc.store_local(2);

        var loopCheck = bc.position;
        bc.load_local(2);
        bc.load_arg(0);
        bc.i32_lt_s();
        var exitJump = bc.reserve_jump_if_false();

        bc.load_local(1);
        bc.@const(1);
        bc.f64_add();
        bc.@const(2);
        bc.f64_mul();
        bc.@const(3);
        bc.f64_sub();
        bc.@const(4);
        bc.f64_div();
        bc.store_local(1);

        bc.load_local(2);
        bc.@const(1);
        bc.i32_add();
        bc.store_local(2);

        bc.jump(loopCheck - (bc.position + 5));

        bc.patch_jump_if_false(exitJump, bc.position);
        bc.load_local(1);
        bc.@return();

        var (bytecode, module) = bc.build("bench_float", "float_ops", 1, 2);

        var vm = new NyarVm();
        vm.load(module);

        var sw = Stopwatch.StartNew();
        vm.run("bench_float", "float_ops", Value.from_int(ops));
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    #endregion

    #region 私有方法 - 内存操作

    private static double benchmark_memory_load_store(int ops)
    {
        var bc = new BenchBytecodeBuilder();
        bc.@const(0);
        bc.store_local(1);
        bc.@const(0);
        bc.store_local(2);
        bc.@const(0);
        bc.store_local(3);

        var loopCheck = bc.position;
        bc.load_local(2);
        bc.load_arg(0);
        bc.i32_lt_s();
        var exitJump = bc.reserve_jump_if_false();

        bc.load_local(1);
        bc.store_local(3);
        bc.load_local(3);
        bc.store_local(1);
        bc.load_local(1);
        bc.store_local(3);
        bc.load_local(3);
        bc.store_local(1);

        bc.load_local(2);
        bc.@const(1);
        bc.i32_add();
        bc.store_local(2);

        bc.jump(loopCheck - (bc.position + 5));

        bc.patch_jump_if_false(exitJump, bc.position);
        bc.load_local(1);
        bc.@return();

        var (bytecode, module) = bc.build("bench_mem", "mem_ops", 1, 3);

        var vm = new NyarVm();
        vm.load(module);

        var sw = Stopwatch.StartNew();
        vm.run("bench_mem", "mem_ops", Value.from_int(ops));
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    #endregion

    #region 内部类型 - 字节码构建器

    private sealed class BenchBytecodeBuilder
    {
        private readonly List<byte> _code = [];
        private readonly List<Value> _constants = [];

        public int position => _code.Count;

        public BenchBytecodeBuilder @const(int value)
        {
            _constants.Add(Value.from_int(value));
            _code.Add((byte)NyarHeadCode.@const);
            write_i32(_constants.Count - 1);
            return this;
        }

        public BenchBytecodeBuilder load_arg(int index)
        {
            _code.Add((byte)NyarHeadCode.load_arg);
            write_i32(index);
            return this;
        }

        public BenchBytecodeBuilder load_local(int index)
        {
            _code.Add((byte)NyarHeadCode.load_local);
            write_i32(index);
            return this;
        }

        public BenchBytecodeBuilder store_local(int index)
        {
            _code.Add((byte)NyarHeadCode.store_local);
            write_i32(index);
            return this;
        }

        public BenchBytecodeBuilder i32_add()
        {
            _code.Add((byte)NyarHeadCode.i32_add);
            return this;
        }

        public BenchBytecodeBuilder i32_sub()
        {
            _code.Add((byte)NyarHeadCode.i32_sub);
            return this;
        }

        public BenchBytecodeBuilder i32_mul()
        {
            _code.Add((byte)NyarHeadCode.i32_mul);
            return this;
        }

        public BenchBytecodeBuilder i32_div()
        {
            _code.Add((byte)NyarHeadCode.i32_div_s);
            return this;
        }

        public BenchBytecodeBuilder i32_lt_s()
        {
            _code.Add((byte)NyarHeadCode.i32_lt_s);
            return this;
        }

        public BenchBytecodeBuilder i32_le_s()
        {
            _code.Add((byte)NyarHeadCode.i32_le_s);
            return this;
        }

        public BenchBytecodeBuilder f64_add()
        {
            _code.Add((byte)NyarHeadCode.f64_add);
            return this;
        }

        public BenchBytecodeBuilder f64_sub()
        {
            _code.Add((byte)NyarHeadCode.f64_sub);
            return this;
        }

        public BenchBytecodeBuilder f64_mul()
        {
            _code.Add((byte)NyarHeadCode.f64_mul);
            return this;
        }

        public BenchBytecodeBuilder f64_div()
        {
            _code.Add((byte)NyarHeadCode.f64_div);
            return this;
        }

        public BenchBytecodeBuilder jump(int offset)
        {
            _code.Add((byte)NyarHeadCode.jump);
            write_i32(offset);
            return this;
        }

        public int reserve_jump_if_true()
        {
            var pos = _code.Count;
            _code.Add((byte)NyarHeadCode.jump_if_true);
            write_i32(0);
            return pos;
        }

        public int reserve_jump_if_false()
        {
            var pos = _code.Count;
            _code.Add((byte)NyarHeadCode.jump_if_false);
            write_i32(0);
            return pos;
        }

        public int reserve_jump()
        {
            var pos = _code.Count;
            _code.Add((byte)NyarHeadCode.jump);
            write_i32(0);
            return pos;
        }

        public void patch_jump_if_true(int jumpPos, int targetPc)
        {
            write_i32_at(jumpPos + 1, targetPc - (jumpPos + 5));
        }

        public void patch_jump_if_false(int jumpPos, int targetPc)
        {
            write_i32_at(jumpPos + 1, targetPc - (jumpPos + 5));
        }

        public void patch_jump(int jumpPos, int targetPc)
        {
            write_i32_at(jumpPos + 1, targetPc - (jumpPos + 5));
        }

        public BenchBytecodeBuilder call(int funcIndex)
        {
            _code.Add((byte)NyarHeadCode.call);
            write_i32(funcIndex);
            return this;
        }

        public BenchBytecodeBuilder @return()
        {
            _code.Add((byte)NyarHeadCode.@return);
            return this;
        }

        public (byte[] bytecode, NyarModule module) build(string moduleName, string functionName, int arity,
            int localCount)
        {
            var module = new NyarModule(moduleName);
            module.constants.AddRange(_constants);

            var func = new RuntimeNyarFunction(functionName, arity, localCount, 0, _code.Count)
            {
                module = module
            };
            module.functions.Add(func);

            var bytecode = _code.ToArray();
            module.raw_bytecode = bytecode;
            return (bytecode, module);
        }

        private void write_i32(int value)
        {
            _code.Add((byte)(value & 0xFF));
            _code.Add((byte)((value >> 8) & 0xFF));
            _code.Add((byte)((value >> 16) & 0xFF));
            _code.Add((byte)((value >> 24) & 0xFF));
        }

        private void write_i32_at(int offset, int value)
        {
            _code[offset] = (byte)(value & 0xFF);
            _code[offset + 1] = (byte)((value >> 8) & 0xFF);
            _code[offset + 2] = (byte)((value >> 16) & 0xFF);
            _code[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }

    #endregion

    #region 常量

    private const int _warmup_iterations = 30;
    private const int _measurement_iterations = 200;

    #endregion

    #region 结果类型

    /// <summary>
    ///     单项基准测试结果
    /// </summary>
    public sealed class BenchmarkResult
    {
        /// <summary>
        ///     测试名称
        /// </summary>
        public string name { get; init; } = "";


        /// <summary>
        ///     平均耗时（毫秒）
        /// </summary>
        public double avg_ms { get; init; }


        /// <summary>
        ///     最小耗时（毫秒）
        /// </summary>
        public double min_ms { get; init; }


        /// <summary>
        ///     最大耗时（毫秒）
        /// </summary>
        public double max_ms { get; init; }


        /// <summary>
        ///     每秒指令数（IPS）
        /// </summary>
        public double ips { get; init; }


        /// <summary>
        ///     是否通过性能目标
        /// </summary>
        public bool passed { get; init; }


        /// <summary>
        ///     目标描述
        /// </summary>
        public string target { get; init; } = "";
    }

    /// <summary>
    ///     基准测试套件结果
    /// </summary>
    public sealed class SuiteResult
    {
        /// <summary>
        ///     所有测试结果
        /// </summary>
        public List<BenchmarkResult> results { get; } = [];


        /// <summary>
        ///     是否全部通过
        /// </summary>
        public bool all_passed => results.All(r => r.passed);


        /// <summary>
        ///     总耗时（毫秒）
        /// </summary>
        public double total_ms => results.Sum(r => r.avg_ms);
    }

    #endregion

    #region 公有方法

    /// <summary>
    ///     执行完整基准测试套件
    /// </summary>
    /// <returns>套件结果。</returns>
    public SuiteResult run_all()
    {
        var suite = new SuiteResult();

        suite.results.Add(run_benchmark("fibonacci_20", () => benchmark_fib(20), 50.0, "Fib(20) < 50ms"));
        suite.results.Add(run_benchmark("loop_accum_1m", () => benchmark_loop_accum(1_000_000), 100.0,
            "1M loop < 100ms"));
        suite.results.Add(run_benchmark("call_depth_50", () => benchmark_call_depth(50), 20.0, "depth-50 < 20ms"));
        suite.results.Add(run_benchmark("arithmetic_int", () => benchmark_arithmetic_int(500_000), 80.0,
            "500K int ops < 80ms"));
        suite.results.Add(run_benchmark("arithmetic_float", () => benchmark_arithmetic_float(500_000), 80.0,
            "500K float ops < 80ms"));
        suite.results.Add(run_benchmark("memory_load_store", () => benchmark_memory_load_store(200_000), 60.0,
            "200K mem ops < 60ms"));
        suite.results.Add(run_benchmark("fibonacci_20_jit", () => benchmark_fib_jit(20), 30.0, "Fib(20) JIT < 30ms"));

        return suite;
    }

    /// <summary>
    ///     生成格式化报告
    /// </summary>
    /// <param name="result">套件结果。</param>
    /// <returns>格式化报告字符串。</returns>
    public string generate_report(SuiteResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              NyarVM Performance Benchmark Report            ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine("║  Benchmark              │  Avg (ms)  │  IPS       │ Status ║");
        sb.AppendLine("╠═════════════════════════╪════════════╪════════════╪════════╣");

        foreach (var r in result.results)
        {
            var name = r.name.PadRight(23);
            var avg = r.avg_ms.ToString("F2").PadLeft(10);
            var ips = r.ips > 0 ? format_ips(r.ips) : "N/A".PadLeft(11);
            var status = r.passed ? "✅" : "❌";
            sb.AppendLine($"║  {name}│{avg}  │{ips}  │  {status}   ║");
        }

        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        var overall = result.all_passed ? "✅ ALL PASSED" : "❌ SOME FAILED";
        sb.AppendLine($"║  Overall: {overall,-47}║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");

        return sb.ToString();
    }

    #endregion

    #region 私有方法 - 基准运行器

    private BenchmarkResult run_benchmark(string name, Func<double> benchmark, double targetMs, string targetDesc)
    {
        for (var i = 0; i < _warmup_iterations; i++) benchmark();

        var times = new List<double>();
        for (var i = 0; i < _measurement_iterations; i++) times.Add(benchmark());

        var avgMs = times.Average();
        var minMs = times.Min();
        var maxMs = times.Max();

        return new BenchmarkResult
        {
            name = name,
            avg_ms = avgMs,
            min_ms = minMs,
            max_ms = maxMs,
            ips = 0,
            passed = avgMs < targetMs,
            target = targetDesc
        };
    }

    private static string format_ips(double ips)
    {
        if (ips >= 1_000_000_000) return $"{ips / 1_000_000_000:F1}G/s".PadLeft(11);

        if (ips >= 1_000_000) return $"{ips / 1_000_000:F1}M/s".PadLeft(11);

        if (ips >= 1_000) return $"{ips / 1_000:F1}K/s".PadLeft(11);

        return $"{ips:F0}/s".PadLeft(11);
    }

    #endregion

    #region 私有方法 - Fibonacci

    private static double benchmark_fib(int n)
    {
        var vm = create_vm_with_fib(false);
        var sw = Stopwatch.StartNew();
        vm.run("bench_fib", "fib", Value.from_int(n));
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    private static double benchmark_fib_jit(int n)
    {
        var vm = create_vm_with_fib(true);
        for (var i = 0; i < 5; i++) vm.run("bench_fib", "fib", Value.from_int(n));

        var sw = Stopwatch.StartNew();
        vm.run("bench_fib", "fib", Value.from_int(n));
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    private static NyarVm create_vm_with_fib(bool jitEnabled)
    {
        var bc = new BenchBytecodeBuilder();
        var loopCheck = bc.position;

        bc.load_arg(0);
        bc.@const(1);
        bc.i32_le_s();
        var baseCaseJump = bc.reserve_jump_if_true();

        bc.load_arg(0);
        bc.@const(1);
        bc.i32_sub();
        bc.call(0);

        bc.load_arg(0);
        bc.@const(2);
        bc.i32_sub();
        bc.call(0);

        bc.i32_add();
        var endJump = bc.reserve_jump();

        bc.patch_jump_if_true(baseCaseJump, bc.position);
        bc.load_arg(0);

        bc.patch_jump(endJump, bc.position);
        bc.@return();

        var (bytecode, module) = bc.build("bench_fib", "fib", 1, 1);

        var vm = new NyarVm(new JitOptions
            { enabled = jitEnabled, tiered_compilation = jitEnabled, hot_threshold = 2 });
        vm.load(module);
        return vm;
    }

    #endregion
}