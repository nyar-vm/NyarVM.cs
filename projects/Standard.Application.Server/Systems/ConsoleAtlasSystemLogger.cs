using System.Text;

namespace Std.App.Server.Systems;

/// <summary>
///     控制台日志器实现，输出 [系统名:动作:级别] Key=Value 格式日志。
/// </summary>
public sealed class ConsoleAtlasSystemLogger : IAtlasSystemLogger
{
    private readonly string _systemName;

    /// <summary>
    ///     初始化控制台日志器
    /// </summary>
    /// <param name="systemName">系统名称</param>
    public ConsoleAtlasSystemLogger(string systemName)
    {
        _systemName = systemName;
    }

    /// <inheritdoc />
    public void Log(string action, params object[] keyValues)
    {
        Console.WriteLine(Format("INFO", action, keyValues));
    }

    /// <inheritdoc />
    public void LogWarning(string action, params object[] keyValues)
    {
        Console.WriteLine(Format("WARN", action, keyValues));
    }

    /// <inheritdoc />
    public void LogError(string action, Exception? ex, params object[] keyValues)
    {
        var message = Format("ERROR", action, keyValues);
        if (ex is not null) message += $"; Exception={ex.Message}";
        Console.WriteLine(message);
    }

    /// <summary>
    ///     格式化日志消息
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="action">动作名称</param>
    /// <param name="keyValues">键值对参数</param>
    /// <returns>格式化后的日志字符串</returns>
    private string Format(string level, string action, object[] keyValues)
    {
        var sb = new StringBuilder();
        sb.Append($"[{_systemName}:{action}:{level}]");
        for (var i = 0; i + 1 < keyValues.Length; i += 2)
            sb.Append($" {keyValues[i]}={FormatValue(keyValues[i + 1])};");
        return sb.ToString();
    }

    /// <summary>
    ///     格式化键值对中的值
    /// </summary>
    /// <param name="value">待格式化的值</param>
    /// <returns>格式化后的字符串</returns>
    private static string FormatValue(object? value)
    {
        if (value is null) return "null";
        var s = value.ToString() ?? "";
        if (s.Contains(' ')) return $"\"{s}\"";
        return s;
    }
}