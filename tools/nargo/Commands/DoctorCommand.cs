using Nargo.Cli.ProjectModel;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo doctor 命令：诊断兼容问题与迁移问题
/// </summary>
public static class DoctorCommand
{
    /// <summary>
    ///     执行 doctor 命令
    /// </summary>
    /// <param name="directoryPath">目标目录</param>
    public static void execute(string directoryPath)
    {
        Console.WriteLine("正在诊断工程...");

        var issues = new List<string>();

        // 检查 package.json
        var packageJsonPath = Path.Combine(directoryPath, "package.json");
        if (File.Exists(packageJsonPath))
        {
            Console.WriteLine("  [找到] package.json");
        }
        else
        {
            issues.Add("未找到 package.json — 无法识别 npm 项目");
        }

        // 检查 pnpm-workspace.yaml
        var workspacePath = Path.Combine(directoryPath, "pnpm-workspace.yaml");
        if (File.Exists(workspacePath))
        {
            Console.WriteLine("  [找到] pnpm-workspace.yaml");
        }

        // 检查 lockfile
        var pnpmLockPath = Path.Combine(directoryPath, "pnpm-lock.yaml");
        var npmLockPath = Path.Combine(directoryPath, "package-lock.json");
        if (File.Exists(pnpmLockPath))
        {
            Console.WriteLine("  [找到] pnpm-lock.yaml");
        }
        else if (File.Exists(npmLockPath))
        {
            Console.WriteLine("  [找到] package-lock.json");
        }
        else
        {
            issues.Add("未找到 lockfile — 建议执行 nargo install 生成锁文件");
        }

        // 检查 node_modules
        var nodeModulesPath = Path.Combine(directoryPath, "node_modules");
        if (Directory.Exists(nodeModulesPath))
        {
            Console.WriteLine("  [找到] node_modules/");
        }
        else
        {
            issues.Add("未找到 node_modules/ — 建议执行 nargo install");
        }

        // 检查 nargo.von
        var nargoVonPath = Path.Combine(directoryPath, "nargo.von");
        if (File.Exists(nargoVonPath))
        {
            Console.WriteLine("  [找到] nargo.von (原生配置)");
        }

        // 输出诊断结果
        Console.WriteLine();
        if (issues.Count == 0)
        {
            Console.WriteLine("诊断完成：未发现问题。");
        }
        else
        {
            Console.WriteLine($"诊断完成：发现 {issues.Count} 个问题：");
            foreach (var issue in issues)
            {
                Console.WriteLine($"  - {issue}");
            }
        }
    }
}