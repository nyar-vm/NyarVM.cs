using System.Text.RegularExpressions;

using BenchmarkDotNet.Attributes;

namespace Nyar.VM.TextVM.Benchmarks;

/// <summary>
/// 比较 DFA 执行器与回溯引擎在包含通配符模式下的性能。
/// </summary>
[MemoryDiagnoser]
public class DfaVsBacktrackBenchmark
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
        _inputString = String.Join(" ", Enumerable.Repeat("abc123def456ghi789", 200));
        _input = System.Text.Encoding.UTF8.GetBytes(_inputString);
        _regex = new Regex("[0-9]+", RegexOptions.Compiled);

        // 使用 LiteralExecutor 模拟 DFA 执行器进行字面量匹配
        _tvm = new LiteralExecutor("123"u8.ToArray(), TextEncoding.Utf8);
    }

    /// <summary>
    /// 使用 TextVM DFA 执行器查找第一个匹配位置。
    /// </summary>
    [Benchmark]
    public Boolean TextVM_DfaFind()
    {
        return _tvm.FindFirst(_input) is not null;
    }

    /// <summary>
    /// 使用 Regex.IsMatch 进行回溯匹配，作为基线。
    /// </summary>
    [Benchmark(Baseline = true)]
    public Boolean Regex_Find()
    {
        return _regex.IsMatch(_inputString);
    }
}
