namespace Std.DL.Diagnostics;

internal sealed class TraceSpan : ITraceSpan
{
    private readonly string _name;

    public TraceSpan(string name)
    {
        _name = name;
    }

    public void SetAttribute(string key, object value)
    {
    }

    public void RecordError(Exception exception)
    {
    }

    public void End()
    {
    }

    public void Dispose()
    {
        End();
    }
}