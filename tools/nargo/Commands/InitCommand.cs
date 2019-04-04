using Nargo.Cli.Compatibility;
using Nargo.Cli.ProjectModel;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo init 命令：初始化 nargo 原生项目
/// </summary>
public static class InitCommand
{
    /// <summary>
    ///     执行 init 命令
    /// </summary>
    /// <param name="directoryPath">目标目录</param>
    /// <param name="targetKind">工程目标类型</param>
    /// <returns>初始化后的 NargoProject</returns>
    public static NargoProject execute(string directoryPath, NargoTargetKind targetKind = NargoTargetKind.BrowserApp)
    {
        var projectName = Path.GetFileName(directoryPath) ?? "my-project";

        var project = new NargoProject
        {
            Name = projectName,
            Version = "0.0.0",
            RootDir = Path.GetFullPath(directoryPath),
            PrimaryTarget = targetKind,
            CompatibilitySource = "nargo",
            Entries = generate_default_entries(targetKind),
            Targets =
            [
                new NargoTarget
                {
                    Name = "default",
                    Kind = targetKind,
                    OutputDir = "dist"
                }
            ]
        };

        Console.WriteLine($"已初始化 nargo 项目: {projectName} ({targetKind})");
        return project;
    }

    /// <summary>
    ///     根据目标类型生成默认入口
    /// </summary>
    private static List<NargoEntry> generate_default_entries(NargoTargetKind targetKind)
    {
        return targetKind switch
        {
            NargoTargetKind.BrowserApp =>
            [
                new NargoEntry { Name = "index", Path = "index.html", Kind = NargoEntryKind.Html },
                new NargoEntry { Name = "main", Path = "src/main.ts", Kind = NargoEntryKind.Script }
            ],
            NargoTargetKind.SsrApp =>
            [
                new NargoEntry { Name = "index", Path = "index.html", Kind = NargoEntryKind.Html },
                new NargoEntry { Name = "main", Path = "src/main.ts", Kind = NargoEntryKind.Script },
                new NargoEntry { Name = "server", Path = "src/server.ts", Kind = NargoEntryKind.Ssr }
            ],
            NargoTargetKind.NodeApp =>
            [
                new NargoEntry { Name = "main", Path = "src/main.ts", Kind = NargoEntryKind.Script }
            ],
            NargoTargetKind.NodeLib =>
            [
                new NargoEntry { Name = "index", Path = "src/index.ts", Kind = NargoEntryKind.Script }
            ],
            NargoTargetKind.Cli =>
            [
                new NargoEntry { Name = "cli", Path = "src/cli.ts", Kind = NargoEntryKind.Script }
            ],
            NargoTargetKind.Worker =>
            [
                new NargoEntry { Name = "worker", Path = "src/worker.ts", Kind = NargoEntryKind.Worker }
            ],
            _ =>
            [
                new NargoEntry { Name = "index", Path = "src/index.ts", Kind = NargoEntryKind.Script }
            ]
        };
    }
}