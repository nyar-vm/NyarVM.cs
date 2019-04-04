using System.Diagnostics;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.VM.NyarVM.Profiling;

/// <summary>
///     OpCode 级性能分析器，用于识别解释器热点
/// </summary>
public sealed class NyarProfiler
{
    /// <summary>
    ///     OpCode → 执行次数
    /// </summary>
    private readonly long[] _counts;

    /// <summary>
    ///     临时 Stopwatch 用于单次测量
    /// </summary>
    private readonly Stopwatch _sw;

    /// <summary>
    ///     OpCode → 累积耗时（Stopwatch Ticks）
    /// </summary>
    private readonly long[] _total_ticks;

    /// <summary>
    ///     是否启用 Profiling（关闭时零开销）
    /// </summary>
    private volatile bool _enabled;

    /// <summary>
    ///     初始化 OpCode 级性能分析器
    /// </summary>
    /// <param name="maxOpcodeValue">最大 OpCode 值。</param>
    public NyarProfiler(int maxOpcodeValue = 256)
    {
        _counts = new long[maxOpcodeValue];
        _total_ticks = new long[maxOpcodeValue];
        _sw = new Stopwatch();
        _enabled = false;
    }

    /// <summary>
    ///     是否启用 Profiling
    /// </summary>
    public bool enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    /// <summary>
    ///     获取总执行指令数
    /// </summary>
    public long total_instructions => _counts.Sum();

    /// <summary>
    ///     记录一次 OpCode 执行（含耗时）
    /// </summary>
    /// <param name="opcode">操作码。</param>
    /// <param name="elapsedTicks">耗时（Stopwatch Ticks）。</param>
    public void record(int opcode, long elapsedTicks)
    {
        if (!_enabled || opcode < 0 || opcode >= _counts.Length) return;

        _counts[opcode]++;
        _total_ticks[opcode] += elapsedTicks;
    }

    /// <summary>
    ///     开始测量（在 Step() 调用前）
    /// </summary>
    public void begin_measure()
    {
        if (_enabled) _sw.Restart();
    }

    /// <summary>
    ///     结束测量并记录（在 Step() 返回后）
    /// </summary>
    /// <param name="opcode">执行的操作码。</param>
    public void end_measure(int opcode)
    {
        if (_enabled && opcode >= 0 && opcode < _counts.Length)
        {
            _sw.Stop();
            _counts[opcode]++;
            _total_ticks[opcode] += _sw.ElapsedTicks;
        }
    }

    /// <summary>
    ///     获取热点分析报告（按耗时降序排列）
    /// </summary>
    /// <returns>热点条目列表。</returns>
    public List<HotspotEntry> get_report()
    {
        var entries = new List<HotspotEntry>();
        for (var i = 0; i < _counts.Length; i++)
            if (_counts[i] > 0)
            {
                var item = new HotspotEntry
                {
                    opcode = i,
                    opcode_name = ((NyarHeadCode)i).ToString(),
                    execution_count = _counts[i],
                    total_ticks = _total_ticks[i],
                    average_ticks = (double)_total_ticks[i] / _counts[i],
                    percentage = 0.0
                };
                entries.Add(item);
            }

        var totalTicks = entries.Sum(e => e.total_ticks);
        foreach (var entry in entries)
            entry.percentage = totalTicks > 0 ? (double)entry.total_ticks / totalTicks * 100.0 : 0.0;

        entries.Sort((a, b) => b.total_ticks.CompareTo(a.total_ticks));

        return entries;
    }

    /// <summary>
    ///     获取某个 OpCode 的执行次数
    /// </summary>
    /// <param name="headCode">操作码。</param>
    /// <returns>执行次数。</returns>
    public long get_count(NyarHeadCode headCode)
    {
        var idx = (int)headCode;
        if (idx < 0 || idx >= _counts.Length) return 0;

        return _counts[idx];
    }

    /// <summary>
    ///     重置所有计数器
    /// </summary>
    public void reset()
    {
        Array.Clear(_counts, 0, _counts.Length);
        Array.Clear(_total_ticks, 0, _total_ticks.Length);
    }
}

/// <summary>
///     热点条目
/// </summary>
public sealed class HotspotEntry
{
    /// <summary>
    ///     操作码值
    /// </summary>
    public int opcode { get; init; }

    /// <summary>
    ///     操作码名称
    /// </summary>
    public string opcode_name { get; init; } = string.Empty;

    /// <summary>
    ///     执行次数
    /// </summary>
    public long execution_count { get; init; }

    /// <summary>
    ///     累积耗时（Stopwatch Ticks）
    /// </summary>
    public long total_ticks { get; init; }

    /// <summary>
    ///     平均耗时（Ticks/次）
    /// </summary>
    public double average_ticks { get; init; }

    /// <summary>
    ///     占总耗时百分比
    /// </summary>
    public double percentage { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return
            $"{opcode_name,-20} 次数={execution_count,10:N0}  总Tick={total_ticks,12:N0}  均Tick={average_ticks,6:F1}  {percentage,5:F1}%";
    }
}