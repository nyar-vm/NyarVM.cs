using BenchmarkDotNet.Attributes;

namespace Nyar.VM.TextVM.Benchmarks;

/// <summary>
/// 比较 TextVM 字面量执行器与 String.IndexOf 在纯字面量模式下的性能。
/// </summary>
[MemoryDiagnoser]
public class LiteralVsIndexOfBenchmark
{
    private Byte[] _input = null!;

    private CompiledUnit _tvm = null!;

    private Byte[] _pattern = null!;

    /// <summary>
    /// 初始化测试数据。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _input = System.Text.Encoding.UTF8.GetBytes(String.Join(" ",
            Enumerable.Repeat("Hello World ERROR 404 test", 1000)));
        _pattern = "ERROR 404"u8.ToArray();
        _tvm = new LiteralExecutor(_pattern, TextEncoding.Utf8);
    }

    /// <summary>
    /// 使用 TextVM LiteralExecutor 进行匹配。
    /// </summary>
    [Benchmark]
    public Boolean TextVM_IsMatch()
    {
        return _tvm.IsMatch(_input);
    }

    /// <summary>
    /// 使用 String.IndexOf 进行匹配，作为基线。
    /// </summary>
    [Benchmark(Baseline = true)]
    public Boolean IndexOf_IsMatch()
    {
        return _input.AsSpan().IndexOf(_pattern) >= 0;
    }
}
