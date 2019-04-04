using BenchmarkDotNet.Running;

namespace Nyar.VM.TextVM.Benchmarks;

/// <summary>
/// TextVM 基准测试入口程序。
/// </summary>
public static class Program
{
    /// <summary>
    /// 运行所有基准测试。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    public static void Main(String[] args)
    {
        BenchmarkRunner.Run<LiteralVsIndexOfBenchmark>();
        BenchmarkRunner.Run<LiteralVsRegexBenchmark>();
        BenchmarkRunner.Run<DfaVsBacktrackBenchmark>();
    }
}
