using System.Diagnostics;

namespace Legion.CLI.Runner;

/// <summary>
///     CLR 外部 Runner，通过 <c>dotnet exec</c> 启动子进程执行 .NET 程序集。
///     .NET 程序集自带入口点，无需外部指定。
/// </summary>
public sealed class ClrRunner : IRunner
{
    /// <summary>
    ///     子进程执行超时时间（毫秒）
    /// </summary>
    private const int TimeoutMs = 60_000;

    private readonly string _command;

    public ClrRunner()
    {
        _command = "dotnet";
    }

    public ClrRunner(string command)
    {
        _command = command ?? "dotnet";
    }

    #region IRunner 实现

    /// <summary>
    ///     检查 dotnet 命令是否在 PATH 上可用
    /// </summary>
    /// <returns>dotnet 命令可用返回 true，否则返回 false</returns>
    public bool is_available()
    {
        return RunnerAutoDetect.is_command_available(_command);
    }

    /// <summary>
    ///     通过 <c>dotnet exec</c> 执行 .NET 程序集。
    ///     程序集自带入口点，entryPoint 参数被忽略。
    /// </summary>
    /// <param name="artifactPath">产物文件路径（.dll 程序集）</param>
    /// <param name="entryPoint">入口点名称，CLR 程序集自带入口点，此参数被忽略</param>
    /// <returns>执行结果，包含退出码、标准输出和错误输出</returns>
    public ExternalRunResult run(string artifactPath, string? entryPoint)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _command,
            Arguments = $"exec \"{artifactPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(processStartInfo);

            if (process is null)
                return new ExternalRunResult
                {
                    exit_code = 1,
                    stderr = $"无法启动 {_command} 子进程"
                };

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            var completed = process.WaitForExit(TimeoutMs);

            if (!completed)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // 进程可能已退出，忽略 kill 异常
                }

                process.WaitForExit();

                return new ExternalRunResult
                {
                    exit_code = 1,
                    stdout = stdout,
                    stderr = $"执行超时（{TimeoutMs / 1000} 秒），已终止进程\n{stderr}"
                };
            }

            return new ExternalRunResult
            {
                exit_code = process.ExitCode,
                stdout = stdout,
                stderr = stderr
            };
        }
        catch (Exception ex)
        {
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = $"启动 dotnet 子进程失败：{ex.Message}"
            };
        }
    }

    #endregion
}