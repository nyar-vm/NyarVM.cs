namespace Std.App.Server.Systems;

/// <summary>
///     空日志器实现，用于测试或禁用日志场景。
///     所有日志操作为空操作。
/// </summary>
public sealed class NullAtlasSystemLogger : IAtlasSystemLogger
{
    /// <inheritdoc />
    public void Log(string action, params object[] keyValues)
    {
    }

    /// <inheritdoc />
    public void LogWarning(string action, params object[] keyValues)
    {
    }

    /// <inheritdoc />
    public void LogError(string action, Exception? ex, params object[] keyValues)
    {
    }
}