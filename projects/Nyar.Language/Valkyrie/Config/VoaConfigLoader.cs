using Nyar.Language.Von;
using Std.Config;
using Std.Config.Node;
using Std.Data.Text.Diagnostics;

namespace Nyar.Language.Valkyrie.Config;

/// <summary>
///     VOA 项目配置加载器，使用 Oak.Von（VonParser）解析 VON 格式的 voa.config.v
///     配置文件使用 VON 格式：{ key: value, nested: { ... } }
/// </summary>
public sealed class VoaConfigLoader
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly VonParser _parser;

    public VoaConfigLoader()
    {
        _parser = new VonParser(_diagnostics);
    }

    /// <summary>
    ///     从项目目录加载配置
    /// </summary>
    public VoaProjectConfig load(string projectDir)
    {
        var configPath = Path.Combine(projectDir, "voa.config.v");

        if (!File.Exists(configPath)) return new VoaProjectConfig();

        var content = File.ReadAllText(configPath);
        return parse_config(content);
    }

    /// <summary>
    ///     查找工作区根目录（包含 voa.workspace.v 的目录）
    /// </summary>
    public string? find_workspace_root(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "voa.workspace.v"))) return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    ///     查找项目目录（包含 voa.config.v 的目录）
    /// </summary>
    public string? find_project_dir(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "voa.config.v"))) return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    ///     解析项目目录路径
    ///     先检查直接路径是否存在，再查找工作区子目录，最后回退到向上搜索
    /// </summary>
    /// <param name="project">项目名或路径</param>
    /// <param name="configLoader">配置加载器实例</param>
    /// <returns>项目绝对路径，找不到返回 <see langword="null" /></returns>
    public static string? resolve_project_dir(string project, VoaConfigLoader configLoader)
    {
        if (Directory.Exists(project)) return project;

        var workspaceRoot = configLoader.find_workspace_root(Directory.GetCurrentDirectory());
        if (workspaceRoot is not null)
        {
            var candidate = Path.Combine(workspaceRoot, "projects", project);
            if (Directory.Exists(candidate)) return candidate;
        }

        return configLoader.find_project_dir(Directory.GetCurrentDirectory());
    }

    /// <summary>
    ///     解析 voa.config.v 内容（VON 格式）
    ///     语法：{ key: value, nested: { ... } }
    ///     VonParser 直接解析 VON 对象，无需 define_config(voa) 包装
    /// </summary>
    private VoaProjectConfig parse_config(string content)
    {
        try
        {
            if (SerdeConfigNodeAdapter.from_serde(_parser.parse(content).inner) is ObjectConfigNode root)
            {
                return ConfigProjector.project<VoaProjectConfig>(root);
            }
        }
        catch (Exception)
        {
            // 解析失败时使用默认配置
        }

        return new VoaProjectConfig();
    }
}
