using System.Diagnostics;

namespace Std.Template.Deploy;

/// <summary>
///     GitHub Pages 部署器，通过 git 推送站点到 gh-pages 分支
/// </summary>
public class GitHubPagesDeployer : IDeployer
{
    /// <inheritdoc />
    public async Task DeployAsync(string sourceDir, DeployOptions options, CancellationToken ct = default)
    {
        var repoUrl = options.RepoUrl;
        var branch = options.Branch;
        var message = options.Message;

        if (string.IsNullOrWhiteSpace(repoUrl)) throw new InvalidOperationException("仓库地址 (--repo) 不能为空");

        var tempDir = Path.Combine(Path.GetTempPath(), "dejavu-deploy-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            Console.WriteLine($"正在克隆仓库 {repoUrl} (分支: {branch})...");
            await RunGitAsync("clone", $"--depth 1 --branch {branch} {repoUrl} \"{tempDir}\"", ct);

            Console.WriteLine("正在清理旧文件...");
            DeleteDirectoryContents(tempDir, ".git");

            Console.WriteLine("正在复制站点文件...");
            CopyDirectoryRecursive(sourceDir, tempDir);

            Console.WriteLine("正在提交...");
            await RunGitAsync("-C", $"\"{tempDir}\" add --all", ct);

            try
            {
                await RunGitAsync("-C", $"\"{tempDir}\" commit -m \"{message}\"", ct);
            }
            catch (Exception)
            {
                Console.WriteLine("没有需要提交的变更，跳过提交。");
                return;
            }

            Console.WriteLine("正在推送...");
            await RunGitAsync("-C", $"\"{tempDir}\" push origin {branch}", ct);

            Console.WriteLine("部署成功！");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                try
                {
                    DeleteReadOnlyDirectory(tempDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"清理临时目录失败: {ex.Message}");
                }
        }
    }

    /// <summary>
    ///     执行 git 命令
    /// </summary>
    private static async Task RunGitAsync(string args, string command, CancellationToken ct)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"{args} {command}",
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
            throw new InvalidOperationException($"Git 命令执行失败 (exit code {process.ExitCode}): {errMsg}");
        }
    }

    /// <summary>
    ///     删除目录中除保留项外的所有内容
    /// </summary>
    private static void DeleteDirectoryContents(string dir, string keepItem)
    {
        foreach (var entry in Directory.GetFileSystemEntries(dir))
        {
            var entryName = Path.GetFileName(entry);
            if (string.Equals(entryName, keepItem, StringComparison.OrdinalIgnoreCase)) continue;

            if (Directory.Exists(entry))
            {
                DeleteReadOnlyDirectory(entry);
            }
            else
            {
                File.SetAttributes(entry, FileAttributes.Normal);
                File.Delete(entry);
            }
        }
    }

    /// <summary>
    ///     递归复制目录
    /// </summary>
    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectoryRecursive(dir, destSubDir);
        }
    }

    /// <summary>
    ///     递归删除只读目录
    /// </summary>
    private static void DeleteReadOnlyDirectory(string dir)
    {
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        foreach (var subDir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
            File.SetAttributes(subDir, FileAttributes.Normal);

        Directory.Delete(dir, true);
    }
}