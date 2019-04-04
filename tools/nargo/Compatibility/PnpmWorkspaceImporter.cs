using Nargo.Cli.ProjectModel;

namespace Nargo.Cli.Compatibility;

/// <summary>
///     pnpm-workspace.yaml 兼容导入器：读取 workspace 配置并生成 NargoWorkspace 模型
/// </summary>
public static class PnpmWorkspaceImporter
{
    /// <summary>
    ///     从指定目录导入 pnpm-workspace.yaml
    /// </summary>
    /// <param name="directoryPath">包含 pnpm-workspace.yaml 的目录</param>
    /// <param name="report">兼容性报告（可为 null）</param>
    /// <returns>导入后的 NargoWorkspace，无 workspace 文件时返回 null</returns>
    public static NargoWorkspace? import(string directoryPath, NargoCompatibilityReport? report = null)
    {
        var workspacePath = Path.Combine(directoryPath, "pnpm-workspace.yaml");
        if (!File.Exists(workspacePath))
        {
            return null;
        }

        report?.DetectedSources.Add("pnpm-workspace.yaml");

        try
        {
            var content = File.ReadAllText(workspacePath);
            var members = parse_workspace_members(content);

            var workspace = new NargoWorkspace
            {
                Name = Path.GetFileName(directoryPath),
                RootDir = Path.GetFullPath(directoryPath),
                CompatibilitySource = "pnpm-workspace.yaml",
                Members = []
            };

            // 为每个 workspace 成员导入项目
            foreach (var memberPattern in members)
            {
                var memberDirs = resolve_glob(directoryPath, memberPattern);
                foreach (var memberDir in memberDirs)
                {
                    var project = PackageJsonImporter.import(memberDir, report);
                    if (project is not null)
                    {
                        workspace.Members.Add(project with { WorkspaceName = workspace.Name });
                    }
                }
            }

            report?.add_issue(CompatibilitySeverity.Info, "pnpm-workspace.yaml",
                $"已导入 workspace，包含 {workspace.Members.Count} 个成员项目",
                "后续可使用 nargo.workspace.von 作为正式配置");

            return workspace;
        }
        catch (Exception ex)
        {
            report?.add_issue(CompatibilitySeverity.Error, "pnpm-workspace.yaml",
                $"导入失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     解析 workspace.yaml 中的成员模式列表
    /// </summary>
    private static List<string> parse_workspace_members(string content)
    {
        var members = new List<string>();

        // 简单 YAML 解析：提取 packages 列表
        var inPackages = false;
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("packages:") || trimmed.StartsWith("packages :"))
            {
                inPackages = true;
                continue;
            }

            if (inPackages)
            {
                if (trimmed.StartsWith("- "))
                {
                    var pattern = trimmed[2..].Trim().Trim('\'', '"');
                    members.Add(pattern);
                }
                else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#"))
                {
                    inPackages = false;
                }
            }
        }

        return members;
    }

    /// <summary>
    ///     解析 glob 模式为实际目录列表
    /// </summary>
    private static List<string> resolve_glob(string rootDir, string pattern)
    {
        var result = new List<string>();

        // pnpm workspace 常见模式：packages/* 或 apps/*
        if (pattern.EndsWith("/*") || pattern.EndsWith("\\*"))
        {
            var dirName = pattern[..^2];
            var baseDir = Path.Combine(rootDir, dirName);
            if (Directory.Exists(baseDir))
            {
                foreach (var dir in Directory.GetDirectories(baseDir))
                {
                    if (File.Exists(Path.Combine(dir, "package.json")))
                    {
                        result.Add(dir);
                    }
                }
            }
        }
        else
        {
            var fullPath = Path.Combine(rootDir, pattern);
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "package.json")))
            {
                result.Add(fullPath);
            }
        }

        return result;
    }
}