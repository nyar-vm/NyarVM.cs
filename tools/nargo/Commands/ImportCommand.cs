using Nargo.Cli.Compatibility;
using Nargo.Cli.ProjectModel;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo import 命令：导入现有 npm / pnpm 项目
/// </summary>
public static class ImportCommand
{
    /// <summary>
    ///     执行 import 命令
    /// </summary>
    /// <param name="directoryPath">目标目录</param>
    /// <returns>导入结果（NargoProject 或 NargoWorkspace）和兼容性报告</returns>
    public static (object? Result, NargoCompatibilityReport Report) execute(string directoryPath)
    {
        var report = new NargoCompatibilityReport();

        // 优先检测 pnpm workspace
        var workspace = PnpmWorkspaceImporter.import(directoryPath, report);
        if (workspace is not null)
        {
            Console.WriteLine($"已导入 pnpm workspace: {workspace.Name} ({workspace.Members.Count} 个成员)");
            return (workspace, report);
        }

        // 其次检测 package.json
        var project = PackageJsonImporter.import(directoryPath, report);
        if (project is not null)
        {
            Console.WriteLine($"已导入 npm 项目: {project.Name}@{project.Version}");
            return (project, report);
        }

        report.add_issue(CompatibilitySeverity.Error, "import",
            "未找到 package.json 或 pnpm-workspace.yaml",
            "请确保在 Node / Web 项目目录中执行 nargo import");

        return (null, report);
    }
}