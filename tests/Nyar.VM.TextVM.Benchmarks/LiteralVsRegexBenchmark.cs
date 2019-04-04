using System.Text.RegularExpressions;

using BenchmarkDotNet.Attributes;

namespace Nyar.VM.TextVM.Benchmarks;

/// <summary>
/// 比较 TextVM 与 System.Text.RegularExpressions.Regex 在简单模式下的性能。
/// </summary>
[MemoryDiagnoser]
public class LiteralVsRegexBenchmark
{
    private Byte[] _input = null!;

    private String _inputString = null!;

    private CompiledUnit _tvm = null!;

    private Regex _regex = null!;

    /// <summary>
    /// 初始化测试数据。
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _inputString = String.Join(" ", Enumerable.Repeat("The quick brown fox jumps over the lazy dog", 100));
        _input = System.Text.Encoding.UTF8.GetBytes(_inputString);
        _tvm = new LiteralExecutor("fox"u8.ToArray(), TextEncoding.Utf8);
        _regex = new Regex("fox", RegexOptions.Compiled);
    }

    /// <summary>
    /// 使用 TextVM 进行查找匹配。
    /// </summary>
    [Benchmark]
    public Boolean TextVM_Find()
    {
        return _tvm.IsMatch(_input);
    }

    /// <summary>
    /// 使用 Regex.IsMatch 进行查找匹配，作为基线。
    /// </summary>
    [Benchmark(Baseline = true)]
    public Boolean Regex_IsMatch()
    {
        return _regex.IsMatch(_inputString);
    }
}
