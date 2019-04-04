using System.Diagnostics;

namespace Std.Template.Deploy;

/// <summary>
///     Rsync 部署器，通过 SSH + rsync 同步站点文件到远程服务器
/// </summary>
public class RsyncDeployer : IDeployer
{
    /// <inheritdoc />
    public async Task DeployAsync(string sourceDir, DeployOptions options, CancellationToken ct = default)
    {
        var host = options.Host;
        var remotePath = options.RemotePath;

        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("SSH 主机地址 (--host) 不能为空");

        if (string.IsNullOrWhiteSpace(remotePath)) throw new InvalidOperationException("远程路径 (--path) 不能为空");

        var sourcePath = sourceDir.TrimEnd('/', '\\') + "/";
        var destination = $"{host}:{remotePath}";

        Console.WriteLine($"正在同步 {sourcePath} -> {destination} ...");

        var args = $"-avz --delete \"{sourcePath}\" \"{destination}\"";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "rsync",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine(output);

        if (process.ExitCode != 0)
        {
            var errMsg = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException($"Rsync 执行失败 (exit code {process.ExitCode}): {errMsg}");
        }

        Console.WriteLine("部署成功！");
    }
}