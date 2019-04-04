using System.Diagnostics;

namespace Legion.CLI.Runner;

/// <summary>
///     Node.js 外部 Runner，通过启动 node 子进程执行 .js 产物
/// </summary>
public sealed class NodeRunner : IRunner
{
    private const int TimeoutMs = 60_000;

    private readonly string _command;

    public NodeRunner()
    {
        _command = "node";
    }

    public NodeRunner(string command)
    {
        _command = command ?? "node";
    }

    #region IRunner 实现

    /// <summary>
    ///     检查 node 命令是否在 PATH 中可用
    /// </summary>
    /// <returns>node 可用时返回 true，否则返回 false</returns>
    public bool is_available()
    {
        return RunnerAutoDetect.is_command_available(_command);
    }

    /// <summary>
    ///     启动 node 子进程执行 JavaScript 产物
    /// </summary>
    /// <param name="artifactPath">产物文件路径（.js 文件）</param>
    /// <param name="entryPoint">入口点名称，Node 执行一般不使用此参数，保留以备未来扩展</param>
    /// <returns>执行结果，包含退出码、标准输出和错误输出</returns>
    public ExternalRunResult run(string artifactPath, string? entryPoint)
    {
        if (!File.Exists(artifactPath))
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = $"产物文件不存在：{artifactPath}"
            };

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = _command,
                Arguments = $"\"{artifactPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            var readOutputTask = process.StandardOutput.ReadToEndAsync();
            var readErrorTask = process.StandardError.ReadToEndAsync();

            var hasExited = process.WaitForExit(TimeoutMs);
            if (!hasExited)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // 进程可能已自行退出，忽略 Kill 异常
                }

                return new ExternalRunResult
                {
                    exit_code = 1,
                    stderr = $"执行超时（{TimeoutMs / 1000} 秒）：node \"{artifactPath}\""
                };
            }

            var stdout = readOutputTask.Result;
            var stderr = readErrorTask.Result;

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
                stderr = $"启动 node 子进程失败：{ex.Message}"
            };
        }
    }

    #endregion
}