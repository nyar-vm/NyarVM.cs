namespace Std.DL.Diagnostics;

/// <summary>全局追踪器入口</summary>
public interface ITracer
{
    /// <summary>开始追踪跨度</summary>
    ITraceSpan StartSpan(string name, TraceAttributes? attributes = null);

    /// <summary>记录指标</summary>
    void RecordMetric(string name, double value);

    /// <summary>记录事件</summary>
    void RecordEvent(string name, TraceAttributes? attributes = null);
}