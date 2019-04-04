namespace Asgard.CLI.Commands;

/// <summary>
///     VOA 生产服务器启动命令。
///     暂不可用：VoaConfigLoader 的 VonParser API 尚未对齐。
/// </summary>
public static class StartCommand
{
    /// <summary>
    ///     启动生产服务器（暂未实现）。
    /// </summary>
    public static async Task<int> execute(
        string project,
        string env = "production",
        int? portNumber = null,
        string? hostAddr = null,
        bool ssl = false,
        string cache = "long"
    )
    {
        await Console.Error.WriteLineAsync("start 命令暂不可用：VoaConfigLoader 的 VonParser API 尚未对齐。");
        return 2;
    }
}