namespace Legion.CLI.Runner;

/// <summary>
///     Runner 自动检测工具，在 PATH 中查找外部命令
/// </summary>
public static class RunnerAutoDetect
{
    /// <summary>
    ///     在 PATH 中查找指定命令，返回完整路径或 null
    /// </summary>
    /// <param name="commandName">命令名（如 dotnet、java、node）</param>
    /// <returns>命令完整路径，未找到返回 null</returns>
    public static string? find_command(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName)) return null;

        // Windows 上需要追加 .exe 后缀
        var searchName = OperatingSystem.IsWindows() &&
                         !commandName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? $"{commandName}.exe"
            : commandName;

        // 如果已经是完整路径，直接检查
        if (Path.IsPathFullyQualified(searchName)) return File.Exists(searchName) ? searchName : null;

        // 在 PATH 环境变量中搜索
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv)) return null;

        var paths = pathEnv.Split(Path.PathSeparator);
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path.Trim(), searchName);
            if (File.Exists(fullPath)) return fullPath;
        }

        return null;
    }

    /// <summary>
    ///     检查指定命令是否在 PATH 中可用
    /// </summary>
    /// <param name="commandName">命令名</param>
    /// <returns>是否可用</returns>
    public static bool is_command_available(string commandName)
    {
        return find_command(commandName) is not null;
    }
}