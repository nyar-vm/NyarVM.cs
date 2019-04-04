using System.Diagnostics;

namespace Legion.CLI.Runner;

/// <summary>
///     JVM 外部 Runner，通过子进程执行 Java 字节码（.class 文件）
/// </summary>
public sealed class JvmRunner : IRunner
{
    /// <summary>
    ///     子进程超时时间（毫秒）
    /// </summary>
    private const int ProcessTimeoutMs = 60_000;

    private readonly string _command;

    public JvmRunner()
    {
        _command = "java";
    }

    public JvmRunner(string command)
    {
        _command = command ?? "java";
    }

    #region IRunner 实现

    /// <summary>
    ///     检查 JVM Runner 是否可用（java 命令是否在 PATH 上）
    /// </summary>
    /// <returns>java 命令可用时返回 true</returns>
    public bool is_available()
    {
        return RunnerAutoDetect.is_command_available(_command);
    }

    /// <summary>
    ///     启动外部 Java 子进程执行编译产物
    /// </summary>
    /// <param name="artifactPath">产物路径，支持目录、`.class` 文件或 `.jar` 文件</param>
    /// <param name="entryPoint">JVM 入口类名，为 null 时返回错误</param>
    /// <returns>执行结果，包含退出码、标准输出和错误输出</returns>
    public ExternalRunResult run(string artifactPath, string? entryPoint)
    {
        if (string.IsNullOrWhiteSpace(artifactPath))
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = "JVM Runner 需要有效的产物路径"
            };

        var isArtifactDirectory = Directory.Exists(artifactPath);
        var isArtifactFile = File.Exists(artifactPath);
        if (!isArtifactDirectory && !isArtifactFile)
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = $"产物不存在：{artifactPath}"
            };

        var artifactExtension = isArtifactFile ? Path.GetExtension(artifactPath).ToLowerInvariant() : string.Empty;
        var isJarArtifact = string.Equals(artifactExtension, ".jar", StringComparison.Ordinal);
        if (!isJarArtifact && string.IsNullOrWhiteSpace(entryPoint))
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = "JVM Runner 需要指定入口类名（entryPoint）"
            };

        var javaPath = RunnerAutoDetect.find_command(_command);
        if (javaPath is null)
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = $"未找到 {_command} 命令，请确保 JDK 已安装并在 PATH 中"
            };

        var arguments = isJarArtifact
            ? $"-jar \"{artifactPath}\""
            : $"-cp \"{resolve_class_path(artifactPath, isArtifactDirectory)}\" {entryPoint}";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new ExternalRunResult
            {
                exit_code = 1,
                stderr = $"启动 JVM 进程失败：{ex.Message}"
            };
        }

        var stdoutTask = Task.Run(() => process.StandardOutput.ReadToEnd());
        var stderrTask = Task.Run(() => process.StandardError.ReadToEnd());

        var completedInTime = process.WaitForExit(ProcessTimeoutMs);

        if (!completedInTime)
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
                // 进程可能已经自行结束
            }

            return new ExternalRunResult
            {
                exit_code = -1,
                stderr = "JVM 进程执行超时（60 秒）"
            };
        }

        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        return new ExternalRunResult
        {
            exit_code = process.ExitCode,
            stdout = stdout,
            stderr = stderr
        };
    }

    #endregion

    /// <summary>
    ///     将目录或 `.class` 文件路径统一转换为 JVM `-cp` 所需的类路径目录。
    /// </summary>
    private static string resolve_class_path(string artifactPath, bool isArtifactDirectory)
    {
        if (isArtifactDirectory)
        {
            return artifactPath;
        }

        return Path.GetDirectoryName(artifactPath) ?? artifactPath;
    }
}
