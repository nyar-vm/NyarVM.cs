using Nyar.Language.WebStyle;

namespace Legion.CLI.Document;

/// <summary>
///     SCSS 主题引擎：负责发现和编译 SCSS 主题文件。
///     主题发现（文件查找）保留在下游，SCSS 编译委托给上游 <see cref="WebStylePipeline" />。
/// </summary>
public sealed class ThemeEngine
{
    /// <summary>
    ///     编译后的主题文件信息
    /// </summary>
    public sealed class CompiledTheme
    {
        /// <summary>
        ///     主题名称（文件名不含扩展名）
        /// </summary>
        public string name { get; init; } = string.Empty;

        /// <summary>
        ///     输出 CSS 文件路径（相对于 outputDir）
        /// </summary>
        public string cssPath { get; init; } = string.Empty;

        /// <summary>
        ///     编译后的 CSS 内容
        /// </summary>
        public string cssContent { get; init; } = string.Empty;

        /// <summary>
        ///     主题目标：api 或 user-doc
        /// </summary>
        public string target { get; init; } = "all";
    }

    /// <summary>
    ///     发现并编译所有 SCSS 主题文件。
    ///     文件发现逻辑保留在 legion 中，SCSS 编译委托给上游 <see cref="WebStylePipeline.compile_scss" />。
    /// </summary>
    /// <param name="projectDir">项目根目录</param>
    /// <param name="outputDir">文档输出目录</param>
    /// <returns>编译后的主题列表</returns>
    public List<CompiledTheme> compile_themes(string projectDir, string outputDir)
    {
        var themes = new List<CompiledTheme>();

        // 主题目录候选：documentation/themes/ 和 projects/*/documentation/themes/
        var themeDirs = new List<string>();

        var mainThemeDir = Path.Combine(projectDir, "documentation", "themes");
        if (Directory.Exists(mainThemeDir))
        {
            themeDirs.Add(mainThemeDir);
        }

        // 检查各子项目的 themes 目录
        var projectsDir = Path.Combine(projectDir, "projects");
        if (Directory.Exists(projectsDir))
        {
            foreach (var projDir in Directory.GetDirectories(projectsDir))
            {
                var projThemeDir = Path.Combine(projDir, "documentation", "themes");
                if (Directory.Exists(projThemeDir))
                {
                    themeDirs.Add(projThemeDir);
                }
            }
        }

        foreach (var themeDir in themeDirs)
        {
            var scssFiles = Directory.GetFiles(themeDir, "*.scss", SearchOption.AllDirectories);
            foreach (var scssFile in scssFiles)
            {
                try
                {
                    var compiled = compile_single_theme(scssFile, themeDir, outputDir);
                    if (compiled is not null)
                    {
                        themes.Add(compiled);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"编译 SCSS 主题失败: {scssFile}: {ex.Message}");
                }
            }
        }

        return themes;
    }

    /// <summary>
    ///     编译单个 SCSS 主题文件，委托给上游 <see cref="WebStylePipeline.compile_scss" />。
    /// </summary>
    private static CompiledTheme? compile_single_theme(string scssFile, string themeDir, string outputDir)
    {
        var relativePath = Path.GetRelativePath(themeDir, scssFile);
        var source = File.ReadAllText(scssFile);

        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        // 委托上游 WebStylePipeline 进行 SCSS → CSS 编译
        var cssContent = WebStylePipeline.compile_scss(source);

        if (string.IsNullOrWhiteSpace(cssContent))
        {
            return null;
        }

        var name = Path.GetFileNameWithoutExtension(scssFile);
        var cssFileName = $"{name}.css";
        var target = determine_target(relativePath);

        // 所有主题 CSS 统一写入根目录 themes/ 子目录
        var cssDir = Path.Combine(outputDir, "themes");
        Directory.CreateDirectory(cssDir);
        var cssFullPath = Path.Combine(cssDir, cssFileName);
        File.WriteAllText(cssFullPath, cssContent);

        // 相对于引用页面的 CSS 路径
        // API 页面在 api/ 下，需要 ../themes/；用户文档在 doc/ 下，也需要 ../themes/
        var cssRelPath = $"../themes/{cssFileName}";

        return new CompiledTheme
        {
            name = name,
            cssPath = cssRelPath,
            cssContent = cssContent,
            target = target
        };
    }

    /// <summary>
    ///     根据文件路径判断主题目标
    /// </summary>
    private static string determine_target(string relativePath)
    {
        var lower = relativePath.ToLowerInvariant().Replace('\\', '/');
        if (lower.StartsWith("api") || lower.Contains("/api/"))
        {
            return "api";
        }
        if (lower.StartsWith("user-doc") || lower.Contains("/user-doc/"))
        {
            return "user-doc";
        }
        return "all";
    }
}