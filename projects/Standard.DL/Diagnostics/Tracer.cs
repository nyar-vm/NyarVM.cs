namespace Std.DL.Diagnostics;

/// <summary>默认追踪器实现</summary>
public sealed class Tracer : ITracer
{
    /// <summary>开始追踪跨度</summary>
    public ITraceSpan StartSpan(string name, TraceAttributes? attributes = null)
    {
        return new TraceSpan(name);
    }

    /// <summary>记录指标</summary>
    public void RecordMetric(string name, double value)
    {
    }

    /// <summary>记录事件</summary>
    public void RecordEvent(string name, TraceAttributes? attributes = null)
    {
    }
}