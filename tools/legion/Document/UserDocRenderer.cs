using System.Text;

namespace Legion.CLI.Document;

/// <summary>
///     将用户文档（Markdown 格式）渲染为 HTML 页面，参考 VuePress 风格
/// </summary>
public sealed class UserDocRenderer
{
    /// <summary>
    ///     主题 CSS 链接标签（来自 SCSS 编译的主题）
    /// </summary>
    private string _themeLinkTags = string.Empty;

    /// <summary>
    ///     设置主题 CSS 链接（来自 SCSS 编译的自定义主题）
    ///     在 render 或 generate_index 之前调用以注入自定义主题样式
    /// </summary>
    /// <param name="themeCssHrefs">主题 CSS 文件的 href 列表（相对于输出目录的 user-doc/ 子目录）</param>
    public void set_theme_css(List<string> themeCssHrefs)
    {
        if (themeCssHrefs.Count == 0)
        {
            _themeLinkTags = string.Empty;
            return;
        }

        var sb = new StringBuilder();
        foreach (var href in themeCssHrefs)
        {
            sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{href}\">");
        }
        _themeLinkTags = sb.ToString();
    }

    /// <summary>
    ///     获取用户文档 CSS 内容（供统一 CSS 管线合并使用）
    /// </summary>
    /// <returns>用户文档 CSS 原始文本</returns>
    public static string get_css() => string.Empty;

    /// <summary>
    ///     渲染用户文档目录中的所有 Markdown 文件为 HTML，输出到目标目录
    /// </summary>
    /// <param name="docDir">文档源目录（如 documentation/pages/zh-hans/）</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="sectionName">文档分区名称（如 "语言参考"、"用户指南"）</param>
    /// <param name="sidebarTitle">侧边栏显示标题</param>
    /// <returns>渲染的页面路径列表（相对于 outputDir）</returns>
    public List<UserDocPage> render(string docDir, string outputDir, string sectionName, string sidebarTitle)
    {
        var pages = new List<UserDocPage>();

        if (!Directory.Exists(docDir))
        {
            return pages;
        }

        var targetDir = Path.Combine(outputDir, sanitize_dir_name(sectionName));
        Directory.CreateDirectory(targetDir);

        var mdFiles = Directory.GetFiles(docDir, "*.md", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        // 构建导航树
        var navTree = build_nav_tree(mdFiles, docDir, sectionName);

        foreach (var mdFile in mdFiles)
        {
            try
            {
                var relativePath = Path.GetRelativePath(docDir, mdFile);
                var htmlContent = render_markdown_file(mdFile, sectionName, sidebarTitle, docDir, navTree, pages);

                var htmlFileName = Path.ChangeExtension(relativePath, ".html");
                var htmlFilePath = Path.Combine(targetDir, htmlFileName);
                var htmlFileDir = Path.GetDirectoryName(htmlFilePath)!;
                Directory.CreateDirectory(htmlFileDir);

                File.WriteAllText(htmlFilePath, htmlContent);

                pages.Add(new UserDocPage
                {
                    title = extract_title_from_file(mdFile),
                    relativePath = $"doc/{sanitize_dir_name(sectionName)}/{htmlFileName.Replace('\\', '/')}",
                    sectionName = sectionName,
                    sourceFile = mdFile
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"渲染用户文档失败: {mdFile}: {ex.Message}");
            }
        }

        return pages;
    }

    /// <summary>
    ///     渲染单个 Markdown 文件为 HTML 页面
    /// </summary>
    private string render_markdown_file(string filePath, string sectionName, string sidebarTitle, string docRoot,
        List<NavTreeNode> navTree, List<UserDocPage> pages)
    {
        var source = File.ReadAllText(filePath);
        var relativePath = Path.GetRelativePath(docRoot, filePath);
        var title = extract_title_from_content(source) ?? Path.GetFileNameWithoutExtension(filePath);

        var bodyHtml = render_markdown_to_html(source, docRoot, Path.GetDirectoryName(filePath)!);

        var depth = relativePath.Count(c => c is '/' or '\\');
        var linkPrefix = depth == 0 ? string.Empty : string.Concat(Enumerable.Repeat("../", depth));
        // CSS/JS 在根目录，doc/ 下需要多一层 ../
        var assetPrefix = string.Concat(Enumerable.Repeat("../", depth + 1));

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{escape_html(title)} - {escape_html(sectionName)}</title>");
        sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{assetPrefix}legion-document.css\">");
        if (!string.IsNullOrEmpty(_themeLinkTags))
        {
            sb.Append(_themeLinkTags);
        }
        sb.AppendLine($"  <script src=\"{assetPrefix}legion-document.js\" defer></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"vp-page\">");
        sb.AppendLine("    <aside class=\"vp-sidebar\">");
        sb.AppendLine($"      <div class=\"vp-sidebar-header\">");
        sb.AppendLine($"        <a href=\"{linkPrefix}index.html\" class=\"vp-sidebar-logo\">{escape_html(sidebarTitle)}</a>");
        sb.AppendLine("      </div>");
        render_sidebar_nav(sb, navTree, relativePath, 0, linkPrefix);
        sb.AppendLine("    </aside>");
        sb.AppendLine("    <main class=\"vp-content\">");
        sb.AppendLine($"      <div class=\"vp-breadcrumb\">");
        sb.AppendLine($"        <a href=\"{linkPrefix}index.html\">文档首页</a>");
        sb.AppendLine($"        <span>/</span>");
        sb.AppendLine($"        <span>{escape_html(sectionName)}</span>");
        sb.AppendLine("      </div>");
        sb.AppendLine($"      <article class=\"vp-article\">");
        sb.AppendLine(bodyHtml);
        sb.AppendLine("      </article>");
        sb.AppendLine("    </main>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <footer class=\"vp-footer\">");
        sb.AppendLine("    <p>由 <strong>legion doc</strong> 生成</p>");
        sb.AppendLine("  </footer>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    ///     将 Markdown 文本转换为 HTML
    /// </summary>
    private string render_markdown_to_html(string markdown, string docRoot, string currentDir)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // 代码块
            if (line.TrimStart().StartsWith("```"))
            {
                var lang = line.TrimStart()[3..].Trim();
                sb.AppendLine("<div class=\"vp-code-block\">");
                if (!string.IsNullOrEmpty(lang))
                {
                    sb.AppendLine($"  <div class=\"vp-code-lang\">{escape_html(lang)}</div>");
                }
                sb.AppendLine($"  <pre><code class=\"language-{escape_html(lang)}\">");
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```"))
                {
                    sb.AppendLine(escape_html(lines[i]));
                    i++;
                }
                sb.AppendLine("  </code></pre>");
                sb.AppendLine("</div>");
                i++;
                continue;
            }

            // 标题
            if (line.StartsWith("#### "))
            {
                var id = slugify(line[5..]);
                sb.AppendLine($"<h4 id=\"{id}\">{render_inline(line[5..], docRoot, currentDir)}</h4>");
                i++;
                continue;
            }
            if (line.StartsWith("### "))
            {
                var id = slugify(line[4..]);
                sb.AppendLine($"<h3 id=\"{id}\">{render_inline(line[3..], docRoot, currentDir)}</h3>");
                i++;
                continue;
            }
            if (line.StartsWith("## "))
            {
                var id = slugify(line[3..]);
                sb.AppendLine($"<h2 id=\"{id}\">{render_inline(line[2..], docRoot, currentDir)}</h2>");
                i++;
                continue;
            }
            if (line.StartsWith("# "))
            {
                var id = slugify(line[2..]);
                sb.AppendLine($"<h1 id=\"{id}\">{render_inline(line[1..], docRoot, currentDir)}</h1>");
                i++;
                continue;
            }

            // 水平线
            if (line.Trim() is "---" or "***" or "___")
            {
                sb.AppendLine("<hr>");
                i++;
                continue;
            }

            // 引用块
            if (line.StartsWith("> "))
            {
                sb.Append("<blockquote>");
                while (i < lines.Length && (lines[i].StartsWith("> ") || lines[i] == ">"))
                {
                    var content = lines[i].StartsWith("> ") ? lines[i][2..] : lines[i][1..];
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        i++;
                        continue;
                    }
                    sb.Append($"<p>{render_inline(content, docRoot, currentDir)}</p>");
                    i++;
                }
                sb.AppendLine("</blockquote>");
                continue;
            }

            // 表格
            if (line.Contains('|') && i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith('|') &&
                lines[i + 1].Contains("---"))
            {
                sb.AppendLine("<div class=\"vp-table-wrapper\"><table>");
                sb.AppendLine("<thead><tr>");
                var headerCells = split_table_cells(line);
                foreach (var cell in headerCells)
                {
                    sb.AppendLine($"<th>{render_inline(cell.Trim(), docRoot, currentDir)}</th>");
                }
                sb.AppendLine("</tr></thead>");
                i += 2;

                sb.AppendLine("<tbody>");
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    sb.AppendLine("<tr>");
                    var cells = split_table_cells(lines[i]);
                    foreach (var cell in cells)
                    {
                        sb.AppendLine($"<td>{render_inline(cell.Trim(), docRoot, currentDir)}</td>");
                    }
                    sb.AppendLine("</tr>");
                    i++;
                }
                sb.AppendLine("</tbody>");
                sb.AppendLine("</table></div>");
                continue;
            }

            // 无序列表
            if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* ") ||
                line.TrimStart().StartsWith("+ "))
            {
                sb.AppendLine("<ul>");
                while (i < lines.Length &&
                       (lines[i].TrimStart().StartsWith("- ") || lines[i].TrimStart().StartsWith("* ") ||
                        lines[i].TrimStart().StartsWith("+ ")))
                {
                    var content = lines[i].TrimStart()[2..];
                    sb.AppendLine($"<li>{render_inline(content, docRoot, currentDir)}</li>");
                    i++;
                }
                sb.AppendLine("</ul>");
                continue;
            }

            // 有序列表
            if (System.Text.RegularExpressions.Regex.IsMatch(line.TrimStart(), @"^\d+\.\s"))
            {
                sb.AppendLine("<ol>");
                while (i < lines.Length &&
                       System.Text.RegularExpressions.Regex.IsMatch(lines[i].TrimStart(), @"^\d+\.\s"))
                {
                    var content =
                        System.Text.RegularExpressions.Regex.Replace(lines[i].TrimStart(), @"^\d+\.\s", "");
                    sb.AppendLine($"<li>{render_inline(content, docRoot, currentDir)}</li>");
                    i++;
                }
                sb.AppendLine("</ol>");
                continue;
            }

            // 普通段落
            sb.Append("<p>");
            sb.Append(render_inline(line, docRoot, currentDir));
            i++;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]) && !is_block_start(lines[i]))
            {
                sb.Append(' ');
                sb.Append(render_inline(lines[i], docRoot, currentDir));
                i++;
            }
            sb.AppendLine("</p>");
        }

        return sb.ToString();
    }

    /// <summary>
    ///     判断一行是否为新块元素的开始
    /// </summary>
    private static bool is_block_start(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith('#') ||
               trimmed.StartsWith("```") ||
               trimmed.StartsWith("> ") ||
               trimmed.StartsWith("- ") ||
               trimmed.StartsWith("* ") ||
               trimmed.StartsWith("+ ") ||
               System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s") ||
               (trimmed.Contains('|') && trimmed.Contains("---")) ||
               trimmed is "---" or "***" or "___";
    }

    /// <summary>
    ///     拆分表格行单元格
    /// </summary>
    private static List<string> split_table_cells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }
        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        return [.. trimmed.Split('|')];
    }

    /// <summary>
    ///     渲染行内元素（链接、代码、粗体、斜体等）
    /// </summary>
    private string render_inline(string text, string docRoot, string currentDir)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var result = escape_html(text);

        // 行内代码 `` 或 `
        result = System.Text.RegularExpressions.Regex.Replace(result, @"``([^`]+)``", "<code>$1</code>");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"`([^`]+)`", "<code>$1</code>");

        // 图片 ![alt](url)
        result = System.Text.RegularExpressions.Regex.Replace(result, @"!\[([^\]]*)\]\(([^)]+)\)",
            "<img src=\"$2\" alt=\"$1\">");

        // 链接 [text](url)
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\[([^\]]+)\]\(([^)]+)\)", match =>
        {
            var linkText = match.Groups[1].Value;
            var linkUrl = match.Groups[2].Value;

            if (linkUrl.EndsWith(".md") && !linkUrl.StartsWith("http"))
            {
                linkUrl = Path.ChangeExtension(linkUrl, ".html");
            }

            return $"<a href=\"{linkUrl}\">{linkText}</a>";
        });

        // 粗体 **text** 或 __text__
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"__([^_]+)__", "<strong>$1</strong>");

        // 斜体 *text* 或 _text_
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\*([^*]+)\*", "<em>$1</em>");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"_([^_]+)_", "<em>$1</em>");

        return result;
    }

    /// <summary>
    ///     从 Markdown 文件中提取标题（第一个 # 标题）
    /// </summary>
    private static string extract_title_from_file(string filePath)
    {
        try
        {
            var firstLine = File.ReadLines(filePath).FirstOrDefault() ?? "";
            if (firstLine.StartsWith("# "))
            {
                return firstLine[2..].Trim();
            }
        }
        catch
        {
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    /// <summary>
    ///     从 Markdown 内容中提取标题
    /// </summary>
    private static string? extract_title_from_content(string content)
    {
        var firstLine = content.Split('\n').FirstOrDefault()?.Trim();
        if (firstLine is not null && firstLine.StartsWith("# "))
        {
            return firstLine[2..].Trim();
        }

        return null;
    }

    /// <summary>
    ///     生成 slug 用于锚点 ID
    /// </summary>
    private static string slugify(string heading)
    {
        var sb = new StringBuilder();
        var lastWasDash = false;
        foreach (var c in heading.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                sb.Append(c);
                lastWasDash = c == '-';
            }
            else if (c is ' ' or '.' or '/' or '\\' or ':' or '(' or ')' or ',' or ';' or '!' or '?')
            {
                if (!lastWasDash)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }
            }
        }

        var result = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(result) ? "section" : result;
    }

    /// <summary>
    ///     生成用户文档索引页面
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    /// <param name="sections">文档分区列表</param>
    public void generate_index(string outputDir, List<UserDocSection> sections)
    {
        var targetDir = outputDir;
        Directory.CreateDirectory(targetDir);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>用户文档</title>");
        sb.AppendLine("  <link rel=\"stylesheet\" href=\"../legion-document.css\">");
        if (!string.IsNullOrEmpty(_themeLinkTags))
        {
            sb.Append(_themeLinkTags);
        }
        sb.AppendLine("  <script src=\"../legion-document.js\" defer></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"vp-page\">");
        sb.AppendLine("    <aside class=\"vp-sidebar\">");
        sb.AppendLine("      <div class=\"vp-sidebar-header\">");
        sb.AppendLine("        <a href=\"index.html\" class=\"vp-sidebar-logo\">用户文档</a>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <nav class=\"vp-sidebar-nav\">");
        foreach (var section in sections)
        {
            sb.AppendLine($"        <div class=\"vp-nav-section\">");
            sb.AppendLine(
                $"          <span class=\"vp-nav-section-title\">{escape_html(section.name)}</span>");
            sb.AppendLine("          <ul class=\"vp-nav-list\">");
            foreach (var page in section.pages)
            {
                var href = page.relativePath.StartsWith("doc/")
                    ? page.relativePath["doc/".Length..]
                    : page.relativePath;
                sb.AppendLine($"            <li><a href=\"{href}\">{escape_html(page.title)}</a></li>");
            }
            sb.AppendLine("          </ul>");
            sb.AppendLine("        </div>");
        }
        sb.AppendLine("      </nav>");
        sb.AppendLine("    </aside>");
        sb.AppendLine("    <main class=\"vp-content\">");
        sb.AppendLine("      <div class=\"vp-breadcrumb\">");
        sb.AppendLine("        <a href=\"../api/index.html\">API 文档首页</a>");
        sb.AppendLine("        <span>/</span>");
        sb.AppendLine("        <span>用户文档</span>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <article class=\"vp-article\">");
        sb.AppendLine("        <h1 id=\"用户文档\">用户文档</h1>");
        sb.AppendLine("        <p>欢迎查阅用户文档，以下为各分区的文档目录。</p>");
        foreach (var section in sections)
        {
            sb.AppendLine($"        <h2 id=\"{slugify(section.name)}\">{escape_html(section.name)}</h2>");
            sb.AppendLine("        <ul>");
            foreach (var page in section.pages)
            {
                var href = page.relativePath.StartsWith("user-doc/")
                    ? page.relativePath["user-doc/".Length..]
                    : page.relativePath;
                sb.AppendLine($"          <li><a href=\"{href}\">{escape_html(page.title)}</a></li>");
            }
            sb.AppendLine("        </ul>");
        }
        sb.AppendLine("      </article>");
        sb.AppendLine("    </main>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <footer class=\"vp-footer\">");
        sb.AppendLine("    <p>由 <strong>legion doc</strong> 生成</p>");
        sb.AppendLine("  </footer>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        File.WriteAllText(Path.Combine(targetDir, "index.html"), sb.ToString());
    }

    #region 导航树

    /// <summary>
    ///     导航树节点
    /// </summary>
    private sealed class NavTreeNode
    {
        /// <summary>
        ///     显示名称
        /// </summary>
        public string title = string.Empty;

        /// <summary>
        ///     相对于文档根目录的路径
        /// </summary>
        public string relativePath = string.Empty;

        /// <summary>
        ///     HTML 链接 href
        /// </summary>
        public string href = string.Empty;

        /// <summary>
        ///     是否为当前页面
        /// </summary>
        public bool isActive;

        /// <summary>
        ///     子节点
        /// </summary>
        public List<NavTreeNode> children = [];
    }

    /// <summary>
    ///     从 Markdown 文件列表构建导航树
    /// </summary>
    private static List<NavTreeNode> build_nav_tree(List<string> mdFiles, string docRoot, string sectionName)
    {
        var root = new List<NavTreeNode>();

        foreach (var mdFile in mdFiles)
        {
            var relativePath = Path.GetRelativePath(docRoot, mdFile);
            var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var title = extract_title_from_file(mdFile);

            var current = root;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var isLast = i == parts.Length - 1;

                if (isLast)
                {
                    // 叶子节点：文件
                    var htmlFileName = Path.ChangeExtension(part, ".html");
                    var currentPath = string.Join("/", parts.Take(i + 1));
                    var href = sanitize_dir_name(sectionName) + "/" +
                               Path.ChangeExtension(currentPath, ".html").Replace('\\', '/');

                    current.Add(new NavTreeNode
                    {
                        title = title,
                        relativePath = currentPath,
                        href = href
                    });
                }
                else
                {
                    // 目录节点
                    var existing = current.Find(n =>
                        n.title == part && n.children.Count > 0);
                    if (existing is null)
                    {
                        existing = new NavTreeNode
                        {
                            title = part,
                            relativePath = string.Join("/", parts.Take(i + 1)),
                            href = string.Empty
                        };
                        current.Add(existing);
                    }
                    current = existing.children;
                }
            }
        }

        return root;
    }

    /// <summary>
    ///     渲染侧边栏导航
    /// </summary>
    private static void render_sidebar_nav(StringBuilder sb, List<NavTreeNode> nodes, string currentRelativePath,
        int depth, string cssPrefix)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        sb.AppendLine("      <nav class=\"vp-sidebar-nav\">");
        foreach (var node in nodes)
        {
            var indent = new string(' ', depth * 2);
            var hasChildren = node.children.Count > 0;
            var isActive = node.relativePath == currentRelativePath;
            var activeClass = isActive ? " active" : string.Empty;

            if (hasChildren)
            {
                sb.AppendLine(
                    $"{indent}        <div class=\"vp-nav-section\">");
                sb.AppendLine(
                    $"{indent}          <span class=\"vp-nav-section-title\">{escape_html(node.title)}</span>");
                render_sidebar_nav_children(sb, node.children, currentRelativePath, depth + 1, cssPrefix);
                sb.AppendLine($"{indent}        </div>");
            }
            else
            {
                sb.AppendLine(
                    $"{indent}        <div class=\"vp-nav-item\">");
                sb.AppendLine(
                    $"{indent}          <a href=\"{cssPrefix}{node.href}\" class=\"vp-nav-link{activeClass}\">{escape_html(node.title)}</a>");
                sb.AppendLine($"{indent}        </div>");
            }
        }
        sb.AppendLine("      </nav>");
    }

    /// <summary>
    ///     渲染侧边栏子导航项
    /// </summary>
    private static void render_sidebar_nav_children(StringBuilder sb, List<NavTreeNode> nodes,
        string currentRelativePath, int depth, string cssPrefix)
    {
        sb.AppendLine("          <ul class=\"vp-nav-list\">");
        foreach (var node in nodes)
        {
            var isActive = node.relativePath == currentRelativePath;
            var activeClass = isActive ? " active" : string.Empty;
            sb.AppendLine(
                $"            <li><a href=\"{cssPrefix}{node.href}\" class=\"vp-nav-link{activeClass}\">{escape_html(node.title)}</a></li>");
        }
        sb.AppendLine("          </ul>");
    }

    #endregion

    #region CSS 与辅助

    /// <summary>
    ///     将 CSS 写入独立文件（仅在不存在时创建）
    /// </summary>
    private static void write_css_file(string outputDir)
    {
        var cssPath = Path.Combine(outputDir, "legion-document.css");
        if (File.Exists(cssPath))
        {
            return;
        }

        var css = @":root {
    --vp-c-bg: #ffffff;
    --vp-c-bg-soft: #f6f6f7;
    --vp-c-bg-mute: #f1f1f1;
    --vp-c-text: #2c3e50;
    --vp-c-text-light: #476582;
    --vp-c-text-lighter: #90a4ae;
    --vp-c-brand: #3eaf7c;
    --vp-c-brand-light: #4abf8a;
    --vp-c-brand-dark: #2d9465;
    --vp-c-border: #eaecef;
    --vp-c-divider: #eaecef;
    --vp-c-code-bg: #f8f8f8;
    --vp-c-code-border: #e1e4e8;
    --vp-code-font: ""Source Code Pro"", ""Fira Code"", ""Consolas"", ""Courier New"", monospace;
    --vp-font: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, ""Fira Sans"", ""Droid Sans"", ""Noto Sans SC"", ""Helvetica Neue"", sans-serif;
    --vp-shadow-1: 0 1px 2px rgba(0,0,0,0.04), 0 1px 2px rgba(0,0,0,0.06);
    --vp-shadow-2: 0 3px 12px rgba(0,0,0,0.07), 0 1px 4px rgba(0,0,0,0.07);
}

*, *::before, *::after { box-sizing: border-box; }

body {
    margin: 0; padding: 0;
    font-family: var(--vp-font);
    font-size: 16px; line-height: 1.7;
    color: var(--vp-c-text);
    background: var(--vp-c-bg);
    -webkit-font-smoothing: antialiased;
}

/* ── 页面布局 ── */
.vp-page {
    display: flex; min-height: calc(100vh - 72px);
    max-width: 1440px; margin: 0 auto;
}

/* ── 侧边栏 ── */
.vp-sidebar {
    position: sticky; top: 0;
    width: 260px; height: 100vh;
    overflow-y: auto; flex-shrink: 0;
    background: var(--vp-c-bg);
    border-right: 1px solid var(--vp-c-border);
    padding: 0; font-size: 0.875rem;
    scrollbar-width: thin;
}

.vp-sidebar::-webkit-scrollbar { width: 4px; }
.vp-sidebar::-webkit-scrollbar-thumb { background: #ccc; border-radius: 2px; }
.vp-sidebar::-webkit-scrollbar-thumb:hover { background: #aaa; }

.vp-sidebar-header {
    padding: 1.5rem 1.5rem 1rem;
    border-bottom: 1px solid var(--vp-c-border);
    margin-bottom: 0.5rem;
}

.vp-sidebar-logo {
    font-size: 1.05rem; font-weight: 700;
    color: var(--vp-c-text); text-decoration: none;
    display: flex; align-items: center; gap: 0.5rem;
}

.vp-sidebar-logo:hover { color: var(--vp-c-brand); }

.vp-sidebar-nav {
    padding: 0.5rem 0;
}

.vp-nav-section {
    margin-bottom: 0.25rem;
}

.vp-nav-section-title {
    display: block; padding: 0.35rem 1.5rem;
    font-size: 0.75rem; font-weight: 700;
    color: var(--vp-c-text-lighter);
    text-transform: uppercase; letter-spacing: 0.5px;
}

.vp-nav-list {
    list-style: none; padding: 0; margin: 0;
}

.vp-nav-list li {
    padding: 0;
}

.vp-nav-item {
    padding: 0;
}

.vp-nav-link {
    display: block; padding: 0.35rem 1.5rem;
    color: var(--vp-c-text-light); text-decoration: none;
    font-size: 0.875rem; line-height: 1.5;
    border-left: 2px solid transparent;
    transition: all 0.15s;
}

.vp-nav-link:hover {
    color: var(--vp-c-brand);
    background: var(--vp-c-bg-soft);
}

.vp-nav-link.active {
    color: var(--vp-c-brand);
    border-left-color: var(--vp-c-brand);
    background: var(--vp-c-bg-soft);
    font-weight: 500;
}

/* ── 主内容区 ── */
.vp-content {
    flex: 1; padding: 2rem 3rem;
    max-width: 860px; min-width: 0;
}

.vp-breadcrumb {
    display: flex; align-items: center; gap: 0.4rem;
    font-size: 0.8rem; color: var(--vp-c-text-lighter);
    margin-bottom: 1.5rem; padding-bottom: 0.75rem;
    border-bottom: 1px solid var(--vp-c-divider);
}

.vp-breadcrumb a {
    color: var(--vp-c-brand); text-decoration: none;
}

.vp-breadcrumb a:hover { text-decoration: underline; }

/* ── 文章内容 ── */
.vp-article { }

.vp-article h1 {
    font-size: 2rem; font-weight: 700;
    color: var(--vp-c-text);
    margin: 0 0 1rem; padding-bottom: 0.5rem;
    border-bottom: 1px solid var(--vp-c-divider);
}

.vp-article h2 {
    font-size: 1.55rem; font-weight: 600;
    color: var(--vp-c-text);
    margin: 2.5rem 0 1rem; padding-bottom: 0.3rem;
    border-bottom: 1px solid var(--vp-c-divider);
}

.vp-article h3 {
    font-size: 1.25rem; font-weight: 600;
    color: var(--vp-c-text);
    margin: 1.8rem 0 0.75rem;
}

.vp-article h4 {
    font-size: 1.05rem; font-weight: 600;
    color: var(--vp-c-text);
    margin: 1.2rem 0 0.5rem;
}

.vp-article p {
    margin: 0.75rem 0;
    line-height: 1.8;
}

.vp-article a {
    color: var(--vp-c-brand); text-decoration: none;
    font-weight: 500;
}

.vp-article a:hover { text-decoration: underline; }

.vp-article strong {
    font-weight: 600; color: var(--vp-c-text);
}

.vp-article ul, .vp-article ol {
    padding-left: 1.5rem; margin: 0.5rem 0;
}

.vp-article li {
    line-height: 1.8; margin: 0.25rem 0;
}

.vp-article li > p { margin: 0; }

/* ── 引用块 ── */
.vp-article blockquote {
    margin: 1rem 0; padding: 0.75rem 1.25rem;
    border-left: 4px solid var(--vp-c-brand);
    background: var(--vp-c-bg-soft);
    border-radius: 0 4px 4px 0;
    color: var(--vp-c-text-light);
}

.vp-article blockquote p {
    margin: 0.25rem 0;
}

/* ── 代码块 ── */
.vp-code-block {
    margin: 1rem 0; border-radius: 6px;
    overflow: hidden; border: 1px solid var(--vp-c-code-border);
    box-shadow: var(--vp-shadow-1);
}

.vp-code-lang {
    background: var(--vp-c-bg-mute);
    color: var(--vp-c-text-lighter);
    font-size: 0.75rem; padding: 0.35rem 1rem;
    font-family: var(--vp-code-font);
    border-bottom: 1px solid var(--vp-c-code-border);
}

.vp-code-block pre {
    margin: 0; padding: 1rem 1.25rem;
    background: var(--vp-c-code-bg);
    overflow-x: auto; font-size: 0.875rem;
    line-height: 1.65;
}

.vp-code-block pre code {
    font-family: var(--vp-code-font);
    background: none; padding: 0;
    color: var(--vp-c-text);
}

/* ── 行内代码 ── */
.vp-article code {
    font-family: var(--vp-code-font);
    font-size: 0.875em;
    background: var(--vp-c-code-bg);
    color: var(--vp-c-brand-dark);
    padding: 0.15rem 0.4rem;
    border-radius: 4px;
    border: 1px solid var(--vp-c-code-border);
}

.vp-article pre code {
    background: none; border: none;
    padding: 0; color: inherit;
}

/* ── 表格 ── */
.vp-table-wrapper {
    overflow-x: auto; margin: 1rem 0;
}

.vp-article table {
    width: 100%; border-collapse: collapse;
    font-size: 0.9rem;
}

.vp-article thead {
    background: var(--vp-c-bg-soft);
}

.vp-article th {
    font-weight: 600; color: var(--vp-c-text);
    padding: 0.6rem 1rem; text-align: left;
    border-bottom: 2px solid var(--vp-c-border);
    font-size: 0.85rem;
    white-space: nowrap;
}

.vp-article td {
    padding: 0.5rem 1rem;
    border-bottom: 1px solid var(--vp-c-divider);
}

.vp-article tbody tr:nth-child(even) {
    background: var(--vp-c-bg-soft);
}

.vp-article tbody tr:hover {
    background: var(--vp-c-bg-mute);
}

/* ── 水平线 ── */
.vp-article hr {
    border: none; border-top: 1px solid var(--vp-c-divider);
    margin: 2rem 0;
}

/* ── 图片 ── */
.vp-article img {
    max-width: 100%; border-radius: 4px;
}

/* ── 页脚 ── */
.vp-footer {
    text-align: center; padding: 2rem;
    color: var(--vp-c-text-lighter);
    font-size: 0.8rem;
    border-top: 1px solid var(--vp-c-divider);
}

/* ── 响应式 ── */
@media (max-width: 768px) {
    .vp-sidebar {
        position: fixed; z-index: 50;
        transform: translateX(-100%);
        transition: transform 0.25s;
    }

    .vp-sidebar.open {
        transform: translateX(0);
    }

    .vp-content {
        padding: 1.5rem;
    }

    .vp-article h1 { font-size: 1.5rem; }
    .vp-article h2 { font-size: 1.25rem; }
    .vp-article h3 { font-size: 1.1rem; }
}

/* ── 大屏幕优化 ── */
@media (min-width: 1440px) {
    .vp-sidebar { width: 280px; }
    .vp-content { max-width: 960px; }
}

/* ── 打印样式 ── */
@media print {
    .vp-sidebar { display: none; }
    .vp-content { max-width: none; padding: 0; }
    .vp-footer { display: none; }
}
";

        File.WriteAllText(cssPath, css);
    }

    /// <summary>
    ///     目录名安全化
    /// </summary>
    private static string sanitize_dir_name(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                sb.Append(c);
            }
            else if (c is ' ' or '/' or '\\')
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }

    /// <summary>
    ///     HTML 转义
    /// </summary>
    private static string escape_html(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    #endregion
}

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