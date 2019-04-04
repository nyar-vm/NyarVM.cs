namespace Legion.CLI.Document;

/// <summary>
///     SCSS 主题引擎存根：ThemeEngine 需要 WebStylePipeline.compile_scss，
///     而 WebStylePipeline 当前被 WebStyle.csproj 排除编译，
///     待 WebStylePipeline 恢复编译后删除此存根。
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
    ///     发现并编译所有 SCSS 主题文件（存根实现）
    /// </summary>
    /// <param name="projectDir">项目根目录</param>
    /// <param name="outputDir">文档输出目录</param>
    /// <returns>编译后的主题列表（当前始终返回空列表）</returns>
    public List<CompiledTheme> compile_themes(string projectDir, string outputDir)
    {
        // TODO: 待 WebStylePipeline.compile_scss 恢复编译后，实现 SCSS 主题编译逻辑
        return [];
    }
}
