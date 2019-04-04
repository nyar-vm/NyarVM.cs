namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     链接/图片目标
/// </summary>
public readonly record struct Target
{
    /// <summary>
    ///     创建目标
    /// </summary>
    public Target(string url, string? title = null)
    {
        this.url = url;
        this.title = title ?? string.Empty;
    }

    /// <summary>
    ///     URL 地址
    /// </summary>
    public string url { get; init; }

    /// <summary>
    ///     标题
    /// </summary>
    public string title { get; init; }
}