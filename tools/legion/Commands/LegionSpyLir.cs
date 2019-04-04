using ExitCode = Core.Terminal.ExitCode;

namespace Legion.CLI.Commands;

/// <summary>
///     legion spy lir 子模式：dump 指定项目/函数的 LIR
/// </summary>
internal static partial class LegionSpyLir
{
    /// <summary>
    ///     执行 LIR dump
    /// </summary>
    /// <param name="project">项目路径，为 null 时使用当前目录</param>
    /// <param name="func">函数名过滤器，为 null 时 dump 所有函数</param>
    /// <param name="json">是否以 JSON 格式输出（当前未实现，保留参数）</param>
    /// <param name="targetPlatform">编译目标平台：wasm / jvm / clr，默认 wasm</param>
    /// <returns>退出码</returns>
    public static Task<ExitCode> run(string? project, string? func, bool json, string? targetPlatform = null)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：无法解析项目目录：{project}");
            return Task.FromResult(ExitCode.Error);
        }

        if (!Directory.Exists(projectDir))
        {
            Console.Error.WriteLine($"错误：项目目录不存在：{projectDir}");
            return Task.FromResult(ExitCode.Error);
        }

        // 解析目标平台，默认 wasm
        var platform = string.IsNullOrEmpty(targetPlatform) ? "wasm" : targetPlatform.ToLowerInvariant();
        if (platform is not ("wasm" or "jvm" or "clr"))
        {
            Console.Error.WriteLine($"错误：不支持的目标平台 '{targetPlatform}'，可用值：wasm / jvm / clr");
            return Task.FromResult(ExitCode.Error);
        }

        // 设置环境变量，触发后端的 LIR dump（WasmBackend 和 JvmBackend 均遵循此协议）
        Environment.SetEnvironmentVariable("LEGION_SPY_LIR_DUMP", "1");
        if (!string.IsNullOrEmpty(func))
        {
            Environment.SetEnvironmentVariable("LEGION_SPY_LIR_FUNC", func);
            Console.Error.WriteLine($"LIR dump：仅 dump 名称包含 `{func}` 的函数");
        }
        else
        {
            Environment.SetEnvironmentVariable("LEGION_SPY_LIR_FUNC", null);
            Console.Error.WriteLine("LIR dump：dump 所有函数");
        }

        if (json)
        {
            Console.Error.WriteLine("提示：JSON 输出格式当前未实现，将以默认文本格式输出");
        }

        Console.Error.WriteLine($"正在构建项目：{projectDir}（target={platform}，verbose=true）");
        Console.Error.WriteLine(new string('-', 60));

        var exitCode = LegionHelper.build_single_project(projectDir, platform, null, true);

        Console.Error.WriteLine(new string('-', 60));

        if (exitCode == 0)
        {
            Console.Error.WriteLine("LIR dump 完成");
            return Task.FromResult(ExitCode.Success);
        }

        Console.Error.WriteLine($"LIR dump 失败：构建返回退出码 {exitCode}");
        return Task.FromResult(ExitCode.Error);
    }
}
