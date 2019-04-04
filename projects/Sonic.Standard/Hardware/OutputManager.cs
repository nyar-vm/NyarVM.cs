using Core.Hardware;

namespace Std.Hardware;

/// <summary>
///     输出管理器，实现 IOutputSink 接口，用于呈现内容
/// </summary>
public sealed class OutputManager : IOutputSink
{
    /// <summary>
    ///     呈现内容
    /// </summary>
    /// <param name="content">要呈现的内容</param>
    public void present(object content)
    {
        System.Console.WriteLine(content?.ToString() ?? "");
    }
}