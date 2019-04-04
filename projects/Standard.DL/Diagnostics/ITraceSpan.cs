namespace Std.DL.Diagnostics;

/// <summary>追踪跨度句柄</summary>
public interface ITraceSpan : IDisposable
{
    /// <summary>设置属性</summary>
    void SetAttribute(string key, object value);

    /// <summary>记录错误</summary>
    void RecordError(Exception exception);

    /// <summary>结束跨度</summary>
    void End();
}