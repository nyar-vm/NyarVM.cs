namespace Valkyrie.CLI.Diag;

/// <summary>
///     诊断工具（桩实现）
/// </summary>
public static class DiagTool
{
    /// <summary>
    ///     运行诊断
    /// </summary>
    public static int Run(string file)
    {
        Console.Error.WriteLine($"诊断功能尚未实现。文件：{file}");
        return 1;
    }
}