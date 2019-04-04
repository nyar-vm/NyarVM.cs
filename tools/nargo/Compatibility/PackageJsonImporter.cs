using System.Text.Json;
using Nargo.Cli.ProjectModel;

namespace Nargo.Cli.Compatibility;

/// <summary>
///     package.json 兼容导入器：读取 package.json 并生成 NargoProject 模型
/// </summary>
public static class PackageJsonImporter
{
    /// <summary>
    ///     从指定目录导入 package.json
    /// </summary>
    /// <param name="directoryPath">包含 package.json 的目录</param>
    /// <param name="report">兼容性报告（可为 null）</param>
    /// <returns>导入后的 NargoProject，无 package.json 时返回 null</returns>
    public static NargoProject? import(string directoryPath, NargoCompatibilityReport? report = null)
    {
        var packageJsonPath = Path.Combine(directoryPath, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            return null;
        }

        report?.DetectedSources.Add("package.json");

        try
        {
            var json = File.ReadAllText(packageJsonPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.try_get_string("name") ?? Path.GetFileName(directoryPath);
            var version = root.try_get_string("version") ?? "0.0.0";

            var project = new NargoProject
            {
                Name = name,
                Version = version,
                RootDir = Path.GetFullPath(directoryPath),
                PrimaryTarget = infer_target_kind(root, report),
                CompatibilitySource = "package.json",
                Dependencies = read_dependencies(root, "dependencies"),
                DevDependencies = read_dependencies(root, "devDependencies"),
                PeerDependencies = read_dependencies(root, "peerDependencies"),
                OptionalDependencies = read_dependencies(root, "optionalDependencies"),
                Scripts = read_scripts(root, report),
                Entries = discover_entries(directoryPath, root),
            };

            report?.add_issue(CompatibilitySeverity.Info, "package.json",
                $"已导入项目 {name}@{version}",
                "后续可使用 nargo.von 作为正式配置");

            return project;
        }
        catch (JsonException ex)
        {
            report?.add_issue(CompatibilitySeverity.Error, "package.json",
                $"JSON 解析失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     推断工程目标类型
    /// </summary>
    private static NargoTargetKind infer_target_kind(JsonElement root, NargoCompatibilityReport? report)
    {
        // 检查是否有 bin 字段 → CLI 工具
        if (root.TryGetProperty("bin", out _))
        {
            return NargoTargetKind.Cli;
        }

        // 检查是否有浏览器入口 → 浏览器应用
        if (root.try_get_string("browser") is not null)
        {
            return NargoTargetKind.BrowserApp;
        }

        // 检查 main / module / exports → 库
        if (root.try_get_string("main") is not null || root.try_get_string("module") is not null)
        {
            // 如果有 type: module 且无 browser，可能是 Node 库
            var packageType = root.try_get_string("type");
            if (packageType == "module")
            {
                return NargoTargetKind.NodeLib;
            }

            return NargoTargetKind.BrowserLib;
        }

        report?.add_issue(CompatibilitySeverity.Warning, "package.json",
            "无法自动推断工程目标类型",
            "建议在 nargo.von 中显式指定 target");

        return NargoTargetKind.BrowserApp;
    }

    /// <summary>
    ///     读取依赖字典
    /// </summary>
    private static Dictionary<string, string> read_dependencies(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, string>();

        if (!root.TryGetProperty(propertyName, out var deps))
        {
            return result;
        }

        foreach (var prop in deps.EnumerateObject())
        {
            result[prop.Name] = prop.Value.GetString() ?? "*";
        }

        return result;
    }

    /// <summary>
    ///     读取 scripts 字典
    /// </summary>
    private static Dictionary<string, string> read_scripts(JsonElement root, NargoCompatibilityReport? report)
    {
        var result = new Dictionary<string, string>();

        if (!root.TryGetProperty("scripts", out var scripts))
        {
            return result;
        }

        foreach (var prop in scripts.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;
            result[prop.Name] = value;
        }

        if (result.Count > 0)
        {
            report?.add_issue(CompatibilitySeverity.Info, "package.json",
                $"发现 {result.Count} 个 scripts，已桥接为兼容层",
                "scripts 为兼容层，建议逐步迁移到 nargo 正式任务模型");
        }

        return result;
    }

    /// <summary>
    ///     发现入口点
    /// </summary>
    private static List<NargoEntry> discover_entries(string directoryPath, JsonElement root)
    {
        var entries = new List<NargoEntry>();

        // 从 package.json 字段发现入口
        var main = root.try_get_string("main");
        if (main is not null)
        {
            entries.Add(new NargoEntry
            {
                Name = "main",
                Path = main,
                Kind = NargoEntryKind.Script,
                AutoDiscovered = false
            });
        }

        var module = root.try_get_string("module");
        if (module is not null)
        {
            entries.Add(new NargoEntry
            {
                Name = "module",
                Path = module,
                Kind = NargoEntryKind.Script,
                AutoDiscovered = false
            });
        }

        // 自动发现常见入口文件
        var autoEntryFiles = new (string FileName, string Name, NargoEntryKind Kind)[]
        {
            ("index.html", "index-html", NargoEntryKind.Html),
            ("src/main.ts", "main-ts", NargoEntryKind.Script),
            ("src/main.tsx", "main-tsx", NargoEntryKind.Script),
            ("src/main.js", "main-js", NargoEntryKind.Script),
            ("src/index.ts", "index-ts", NargoEntryKind.Script),
            ("src/server.ts", "server-ts", NargoEntryKind.Ssr),
            ("src/server.tsx", "server-tsx", NargoEntryKind.Ssr),
            ("worker.ts", "worker-ts", NargoEntryKind.Worker),
            ("src/style.css", "style-css", NargoEntryKind.Css),
            ("src/app.css", "app-css", NargoEntryKind.Css),
        };

        foreach (var (fileName, name, kind) in autoEntryFiles)
        {
            var fullPath = Path.Combine(directoryPath, fileName);
            if (File.Exists(fullPath))
            {
                entries.Add(new NargoEntry
                {
                    Name = name,
                    Path = fileName,
                    Kind = kind,
                    AutoDiscovered = true
                });
            }
        }

        return entries;
    }
}

/// <summary>
///     JsonElement 扩展方法
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>
    ///     尝试获取字符串属性值
    /// </summary>
    public static string? try_get_string(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }
}