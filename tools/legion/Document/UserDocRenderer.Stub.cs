namespace Legion.CLI.Document;

/// <summary>
///     用户文档页面信息
/// </summary>
public sealed class UserDocPage
{
    /// <summary>
    ///     页面标题
    /// </summary>
    public string title { get; init; } = string.Empty;

    /// <summary>
    ///     相对于输出目录的 HTML 文件路径
    /// </summary>
    public string relativePath { get; init; } = string.Empty;

    /// <summary>
    ///     所属分区名称
    /// </summary>
    public string sectionName { get; init; } = string.Empty;

    /// <summary>
    ///     源 Markdown 文件路径
    /// </summary>
    public string sourceFile { get; init; } = string.Empty;
}

/// <summary>
///     用户文档分区（一个文档目录对应一个分区）
/// </summary>
public sealed class UserDocSection
{
    /// <summary>
    ///     分区名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     分区内的页面列表
    /// </summary>
    public List<UserDocPage> pages { get; init; } = [];
}

/// <summary>
///     将用户文档（Markdown 格式）渲染为 HTML 页面的存根实现。
///     UserDocRenderer 的完整实现依赖 build_css() 方法，
///     当前 CSS 内容内嵌在 write_css_file 局部变量中无法提取，
///     待重构后删除此存根。
/// </summary>
public sealed class UserDocRenderer
{
    /// <summary>
    ///     获取用户文档 CSS 内容（存根实现，返回空字符串）
    /// </summary>
    /// <returns>用户文档 CSS 原始文本</returns>
    public static string get_css() => string.Empty;

    /// <summary>
    ///     渲染用户文档目录中的所有 Markdown 文件为 HTML（存根实现，返回空列表）
    /// </summary>
    /// <param name="docDir">文档源目录</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="sectionName">文档分区名称</param>
    /// <param name="sidebarTitle">侧边栏显示标题</param>
    /// <returns>渲染的页面路径列表（当前始终返回空列表）</returns>
    public List<UserDocPage> render(string docDir, string outputDir, string sectionName, string sidebarTitle)
    {
        // TODO: 待 UserDocRenderer 完整实现恢复编译后，替换此存根
        return [];
    }

    /// <summary>
    ///     生成用户文档索引页面（存根实现，无操作）
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    /// <param name="sections">文档分区列表</param>
    public void generate_index(string outputDir, List<UserDocSection> sections)
    {
        // TODO: 待 UserDocRenderer 完整实现恢复编译后，替换此存根
    }
}
