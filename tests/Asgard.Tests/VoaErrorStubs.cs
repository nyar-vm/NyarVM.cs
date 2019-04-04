namespace VOA.ToolChain.Tests;

/// <summary>
///     堆栈帧信息（测试桩）
/// </summary>
public sealed class VoaStackFrame
{
    /// <summary>
    ///     文件路径
    /// </summary>
    public string FilePath { get; init; } = "";

    /// <summary>
    ///     行号
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    ///     列号
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    ///     方法名
    /// </summary>
    public string MethodName { get; init; } = "";
}

/// <summary>
///     错误信息（测试桩）
/// </summary>
public sealed class VoaErrorInfo
{
    /// <summary>
    ///     错误消息
    /// </summary>
    public string Message { get; init; } = "";

    /// <summary>
    ///     文件路径
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    ///     HTTP 状态码
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    ///     修复建议列表
    /// </summary>
    public List<string> Suggestions { get; init; } = [];

    /// <summary>
    ///     文档链接
    /// </summary>
    public string DocumentationUrl { get; init; } = "";

    /// <summary>
    ///     堆栈帧列表
    /// </summary>
    public List<VoaStackFrame> StackFrames { get; init; } = [];
}

/// <summary>
///     错误覆盖层（测试桩）
/// </summary>
public sealed class VoaErrorOverlay
{
    /// <summary>
    ///     渲染错误页面
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="statusCode">状态码</param>
    /// <returns>HTML 内容</returns>
    public string RenderErrorPage(string message, string? filePath, int statusCode)
    {
        var filePart = filePath is not null ? $"文件: {filePath}" : "文件: (unknown)";
        return $@"
<!DOCTYPE html>
<html>
<head><title>Error {statusCode}</title></head>
<body>
    <h1>Error {statusCode}</h1>
    <p>{message}</p>
    <p>{filePart}</p>
</body>
</html>";
    }

    /// <summary>
    ///     渲染包含完整信息的错误页面
    /// </summary>
    /// <param name="info">错误信息</param>
    /// <returns>HTML 内容</returns>
    public string RenderErrorPageWithInfo(VoaErrorInfo info)
    {
        var framesHtml = string.Join("\n", info.StackFrames.Select(f =>
            $"<li>{f.FilePath}:{f.Line}:{f.Column} in {f.MethodName}</li>"));

        var suggestionsHtml = string.Join("\n", info.Suggestions.Select(s => $"<li>{s}</li>"));

        return $@"
<!DOCTYPE html>
<html>
<head><title>Error {info.StatusCode}</title></head>
<body>
    <h1>Error {info.StatusCode}</h1>
    <p>{info.Message}</p>
    <p>文件: {info.FilePath}</p>
    <ul>{framesHtml}</ul>
    <ul>{suggestionsHtml}</ul>
    <a href=""{info.DocumentationUrl}"">文档</a>
</body>
</html>";
    }
}