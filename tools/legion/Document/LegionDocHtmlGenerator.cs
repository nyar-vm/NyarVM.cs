using System.Text;

namespace Legion.CLI.Document;

/// <summary>
///     将 DocProject 数据模型渲染为静态 HTML 文档站点
/// </summary>
public sealed class LegionDocHtmlGenerator
{
    #region 原子写辅助

    /// <summary>
    ///     原子写入文本文件：先写入临时文件再原子替换，多进程并发写入时读者不会读到半写入状态。
    /// </summary>
    private static void atomic_write_all_text(string filePath, string content)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = filePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            throw;
        }
    }

    #endregion

    #region 常量与字段

    /// <summary>
    ///     基础类型（不渲染为链接）
    /// </summary>
    private static readonly HashSet<string> _primitives = new HashSet<string>(StringComparer.Ordinal)
    {
        "i8", "i16", "i32", "i64", "u8", "u16", "u32", "u64",
        "f32", "f64", "string", "bool", "char", "void", "auto", "any",
        "byte", "usize"
    };

    /// <summary>
    ///     当前类型索引（workspace 模式为全局索引，包模式为本地索引）
    /// </summary>
    private IReadOnlyDictionary<string, ResolvedTypeRef>? _typeIndex;

    /// <summary>
    ///     当前项目名称（用于生成相对路径）
    /// </summary>
    private string _currentProject = string.Empty;

    /// <summary>
    ///     是否为统一 workspace 模式（所有文件输出到单个目录）
    /// </summary>
    private bool _isUnifiedWorkspace;

    /// <summary>
    ///     workspace 模式下的所有项目列表（用于统一侧边栏）
    /// </summary>
    private List<DocProject>? _allProjects;

    /// <summary>
    ///     基于 Nyar 高亮基础设施的文档代码着色器
    /// </summary>
    private ValkyrieDocHighlighter _highlighter = null!;

    /// <summary>
    ///     主题 CSS 链接标签（来自 SCSS 编译的主题）
    /// </summary>
    private string _themeLinkTags = string.Empty;

    /// <summary>
    ///     根目录深度偏移（api 子目录为 1，表示 CSS/JS 文件在上层目录）
    /// </summary>
    private int _rootDepth;

    #endregion

    #region 公开方法

    /// <summary>
    ///     设置主题 CSS 链接（来自 SCSS 编译的自定义主题）
    ///     在 generate 之前调用以注入自定义主题样式
    /// </summary>
    /// <param name="themeCssHrefs">主题 CSS 文件的 href 列表（相对于输出目录）</param>
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
    ///     获取 API 文档 CSS 内容（供统一 CSS 管线合并使用）
    /// </summary>
    /// <returns>API 文档 CSS 原始文本</returns>
    public string get_css() => build_css();

    /// <summary>
    ///     获取 JS 内容（供统一写入）
    /// </summary>
    /// <returns>legion-document.js 原始文本</returns>
    public static string get_js() => build_js();

    /// <summary>
    ///     生成项目文档生成静态 HTML 文档（单项目模式）
    /// </summary>
    /// <param name="project">文档数据模型</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="isWorkspace">是否为 workspace 模式</param>
    /// <param name="allProjects">workspace 模式下的所有成员项目</param>
    /// <param name="typeIndex">类型索引（workspace 为全局，包模式为本地，null 则无链接）</param>
    public void generate(DocProject project, string outputDir, bool isWorkspace = false,
        List<DocProject>? allProjects = null,
        IReadOnlyDictionary<string, ResolvedTypeRef>? typeIndex = null)
    {
        _typeIndex = typeIndex;
        _currentProject = project.name;
        _allProjects = allProjects;
        _isUnifiedWorkspace = false;
        _rootDepth = 1; // API 文档在 api/ 子目录下，CSS/JS 在上层
        _highlighter = new ValkyrieDocHighlighter(_typeIndex, _currentProject, get_type_href);

        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        Directory.CreateDirectory(outputDir);

        // 共享资源（CSS/JS）写入上层根目录
        var rootDir = Path.GetDirectoryName(outputDir)!;
        Directory.CreateDirectory(rootDir);

        var indexHtml = generate_index_html(project, isWorkspace, allProjects);
        File.WriteAllText(Path.Combine(outputDir, "index.html"), indexHtml);

        foreach (var module in project.modules)
        {
            var modulePath = get_module_path(module.namespace_name);
            var fullPath = Path.Combine(outputDir, modulePath);
            var moduleDir = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(moduleDir);

            var moduleHtml = generate_module_html(project, module, modulePath);
            File.WriteAllText(fullPath, moduleHtml);

            foreach (var type in module.types)
            {
                if (type.kind == "external")
                {
                    continue;
                }

                var typePath = get_type_path(module.namespace_name, type.name);
                var typeFullPath = Path.Combine(outputDir, typePath);
                var typeDir = Path.GetDirectoryName(typeFullPath)!;
                Directory.CreateDirectory(typeDir);

                var typeHtml = generate_type_html(project, module, type, typePath);
                File.WriteAllText(typeFullPath, typeHtml);
            }
        }

        generate_source_pages(project, outputDir);
    }

    /// <summary>
    ///     生成统一 workspace 文档（所有项目输出到单个目录，消除 package 区分，按命名空间合并）
    /// </summary>
    /// <param name="allProjects">所有成员项目</param>
    /// <param name="outputDir">统一输出目录</param>
    /// <param name="typeIndex">全局类型索引</param>
    public void generate_unified_workspace(List<DocProject> allProjects, string outputDir,
        IReadOnlyDictionary<string, ResolvedTypeRef> typeIndex)
    {
        _typeIndex = typeIndex;
        _isUnifiedWorkspace = true;
        _allProjects = allProjects;
        _rootDepth = 1; // API 文档在 api/ 子目录下，CSS/JS 在上层

        // 按命名空间合并所有项目的模块（类似 C# 中 package 可在任意 namespace 定义 symbol）
        var mergedModules = merge_modules_by_namespace(allProjects);

        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        Directory.CreateDirectory(outputDir);

        // 共享资源根目录
        var rootDir = Path.GetDirectoryName(outputDir)!;
        Directory.CreateDirectory(rootDir);

        // 使用虚拟的合并项目（消除 package 区分）
        var mergedProject = new DocProject { name = "Workspace", modules = mergedModules };

        var indexHtml = generate_unified_index_html(mergedModules);
        File.WriteAllText(Path.Combine(outputDir, "index.html"), indexHtml);

        _highlighter = new ValkyrieDocHighlighter(_typeIndex, string.Empty, get_type_href);

        foreach (var module in mergedModules)
        {
            var modulePath = get_module_path(module.namespace_name);
            var fullPath = Path.Combine(outputDir, modulePath);
            var moduleDir = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(moduleDir);

            var moduleHtml = generate_module_html(mergedProject, module, modulePath);
            File.WriteAllText(fullPath, moduleHtml);

            foreach (var type in module.types)
            {
                if (type.kind == "external")
                {
                    continue;
                }

                var typePath = get_type_path(module.namespace_name, type.name);
                var typeFullPath = Path.Combine(outputDir, typePath);
                var typeDir = Path.GetDirectoryName(typeFullPath)!;
                Directory.CreateDirectory(typeDir);

                var typeHtml = generate_type_html(mergedProject, module, type, typePath);
                File.WriteAllText(typeFullPath, typeHtml);
            }
        }

        // 收集所有源文件并生成源代码页面
        generate_source_pages_unified(allProjects, outputDir);
    }

    /// <summary>
    ///     按命名空间合并所有项目的模块（消除 package 区分）
    ///     类似 C# 中多个 project 可在同一 namespace 中定义 symbol
    /// </summary>
    /// <param name="projects">所有成员项目</param>
    /// <returns>合并后的模块列表</returns>
    private static List<DocModule> merge_modules_by_namespace(List<DocProject> projects)
    {
        var merged = new Dictionary<string, DocModule>(StringComparer.Ordinal);

        foreach (var project in projects)
        {
            foreach (var module in project.modules)
            {
                if (merged.TryGetValue(module.namespace_name, out var existing))
                {
                    // 合并：同名 namespace 的类型和函数合并到一个模块
                    var mergedTypes = existing.types
                        .Concat(module.types)
                        .GroupBy(t => t.name)
                        .Select(g => g.First()) // 同名类型保留第一个（避免重复）
                        .ToList();
                    var mergedFunctions = existing.functions
                        .Concat(module.functions)
                        .GroupBy(f => f.name)
                        .Select(g => g.First())
                        .ToList();

                    merged[module.namespace_name] = existing with
                    {
                        types = mergedTypes,
                        functions = mergedFunctions
                    };
                }
                else
                {
                    merged[module.namespace_name] = module;
                }
            }
        }

        return
        [
            .. merged.Values
                .OrderBy(m => m.namespace_name, StringComparer.Ordinal)
        ];
    }

    /// <summary>
    ///     workspace 模式下收集所有项目的源文件并生成源代码页面
    /// </summary>
    private void generate_source_pages_unified(List<DocProject> allProjects, string outputDir)
    {
        var sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in allProjects)
        {
            foreach (var module in project.modules)
            {
                foreach (var type in module.types)
                {
                    if (!string.IsNullOrEmpty(type.source_file) && File.Exists(type.source_file))
                    {
                        sourceFiles.Add(type.source_file);
                    }

                    foreach (var method in type.methods)
                    {
                        if (!string.IsNullOrEmpty(method.source_file) && File.Exists(method.source_file))
                        {
                            sourceFiles.Add(method.source_file);
                        }
                    }
                }

                foreach (var func in module.functions)
                {
                    if (!string.IsNullOrEmpty(func.source_file) && File.Exists(func.source_file))
                    {
                        sourceFiles.Add(func.source_file);
                    }
                }
            }
        }

        foreach (var absolutePath in sourceFiles)
        {
            try
            {
                var srcHtml = generate_source_page(absolutePath);
                var href = src_href(absolutePath);
                File.WriteAllText(Path.Combine(outputDir, href), srcHtml);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"生成源代码页面失败: {absolutePath}: {ex.Message}");
            }
        }
    }

    #endregion

    #region 页面生成

    /// <summary>
    ///     生成首页 HTML
    /// </summary>
    private string generate_index_html(DocProject project, bool isWorkspace, List<DocProject>? allProjects)
    {
        var sb = new StringBuilder();
        write_html_head(sb, $"{project.name} - 文档", get_css_href(""));
        sb.AppendLine("<body>");
        write_header(sb, project.name, project.description);

        sb.AppendLine("  <div class=\"layout\">");
        write_sidebar(sb, project, string.Empty, string.Empty);

        sb.AppendLine("    <main class=\"main-content\">");

        if (isWorkspace && allProjects is not null && allProjects.Count > 0)
        {
            sb.AppendLine("      <h2>Workspace 成员</h2>");
            sb.AppendLine("      <ul class=\"member-list\">");
            foreach (var member in allProjects)
            {
                var memberLink = string.IsNullOrEmpty(member.name) ? "未命名项目" : member.name;
                sb.AppendLine($"        <li><a href=\"../{sanitize_filename(member.name)}/index.html\">{escape_html(memberLink)}</a>");
                if (!string.IsNullOrEmpty(member.description))
                {
                    sb.AppendLine($"          <span class=\"member-desc\">{escape_html(member.description)}</span>");
                }

                sb.AppendLine("        </li>");
            }

            sb.AppendLine("      </ul>");
        }

        sb.AppendLine("      <h2>模块</h2>");

        if (project.modules.Count == 0)
        {
            sb.AppendLine("      <p>暂无模块文档。</p>");
        }
        else
        {
            foreach (var module in project.modules)
            {
                var moduleName = string.IsNullOrEmpty(module.namespace_name) ? "(默认模块)" : module.namespace_name;
                var moduleHref = get_module_href(module.namespace_name);

                sb.AppendLine("      <div class=\"module-card\">");
                sb.AppendLine($"        <h3><a href=\"{moduleHref}\">{escape_html(moduleName)}</a></h3>");

                if (!string.IsNullOrEmpty(module.doc_comment))
                {
                    sb.AppendLine($"        <p class=\"module-desc\">{escape_html(module.doc_comment)}</p>");
                }

                sb.AppendLine($"        <p class=\"module-stats\">{module.types.Count} 类型, {module.functions.Count} 函数</p>");
                sb.AppendLine("      </div>");
            }
        }

        sb.AppendLine("    </main>");
        sb.AppendLine("  </div>");

        write_footer(sb);
        sb.AppendLine("</body>");
        write_html_tail(sb);
        return sb.ToString();
    }

    /// <summary>
    ///     生成统一 workspace 首页（消除 package 区分，只显示合并后的模块）
    /// </summary>
    /// <param name="mergedModules">合并后的模块列表</param>
    private string generate_unified_index_html(List<DocModule> mergedModules)
    {
        var sb = new StringBuilder();
        var mergedProject = new DocProject { name = "Workspace", modules = mergedModules };

        write_html_head(sb, "Workspace - 文档", get_css_href(""));
        sb.AppendLine("<body>");
        write_header(sb, "Workspace", string.Empty);

        sb.AppendLine("  <div class=\"layout\">");
        write_sidebar(sb, mergedProject, string.Empty, string.Empty);

        sb.AppendLine("    <main class=\"main-content\">");

        var totalTypes = mergedModules.Sum(m => m.types.Count(t => t.kind != "external"));
        var totalFuncs = mergedModules.Sum(m => m.functions.Count);
        var totalModules = mergedModules.Count(m => !string.IsNullOrEmpty(m.namespace_name));

        sb.AppendLine($"      <p>共 {totalTypes} 个类型, {totalFuncs} 个函数, 分布在 {totalModules} 个命名空间中</p>");

        if (mergedModules.Count > 0)
        {
            sb.AppendLine("      <h2>模块</h2>");
            foreach (var module in mergedModules)
            {
                var moduleName = string.IsNullOrEmpty(module.namespace_name) ? "(默认模块)" : module.namespace_name;
                var moduleHref = get_module_href(module.namespace_name);

                sb.AppendLine("      <div class=\"module-card\">");
                sb.AppendLine($"        <h3><a href=\"{moduleHref}\">{escape_html(moduleName)}</a></h3>");
                if (!string.IsNullOrEmpty(module.doc_comment))
                {
                    sb.AppendLine($"        <p class=\"module-desc\">{escape_html(module.doc_comment)}</p>");
                }

                sb.AppendLine($"        <p class=\"module-stats\">{module.types.Count(t => t.kind != "external")} 类型, {module.functions.Count} 函数</p>");
                sb.AppendLine("      </div>");
            }
        }

        sb.AppendLine("    </main>");
        sb.AppendLine("  </div>");

        write_footer(sb);
        sb.AppendLine("</body>");
        write_html_tail(sb);
        return sb.ToString();
    }

    /// <summary>
    ///     生成模块（命名空间）页面 HTML
    /// </summary>
    private string generate_module_html(DocProject project, DocModule module, string filePath)
    {
        var sb = new StringBuilder();
        var moduleDisplayName = string.IsNullOrEmpty(module.namespace_name) ? "(默认模块)" : module.namespace_name;
        var cssHref = get_css_href(filePath);
        write_html_head(sb, $"{moduleDisplayName} - {project.name} 文档", cssHref);
        sb.AppendLine("<body>");
        write_header(sb, _isUnifiedWorkspace ? "Workspace" : project.name, _isUnifiedWorkspace ? string.Empty : project.description);

        sb.AppendLine("  <div class=\"layout\">");
        write_sidebar(sb, project, module.namespace_name, string.Empty, filePath);

        sb.AppendLine("    <main class=\"main-content\">");
        sb.AppendLine($"      <p class=\"breadcrumb\"><a href=\"{get_index_href(filePath)}\">{escape_html(_isUnifiedWorkspace ? "Workspace" : project.name)}</a> / {escape_html(moduleDisplayName)}</p>");
        sb.AppendLine($"      <h2>模块 {escape_html(moduleDisplayName)}</h2>");

        if (!string.IsNullOrEmpty(module.doc_comment))
        {
            sb.AppendLine($"      <p class=\"module-doc\">{escape_html(module.doc_comment)}</p>");
        }

        var typeGroups = module.types
            .GroupBy(t => t.kind)
            .OrderBy(g => type_kind_order(g.Key))
            .ToList();

        foreach (var group in typeGroups)
        {
            var kindLabel = get_kind_label(group.Key);
            sb.AppendLine($"      <h3>{escape_html(kindLabel)}</h3>");
            sb.AppendLine("      <ul class=\"type-list\">");

            foreach (var type in group)
            {
                // 外部类型不生成独立页面，链接到当前模块页面
                string typeHref;
                if (type.kind == "external")
                {
                    typeHref = "#";
                }
                else
                {
                    typeHref = get_type_href(module.namespace_name, type.name);
                }

                var genericPart = type.generic_params.Count > 0
                    ? $"&lt;{string.Join(", ", type.generic_params.Select(escape_html))}&gt;"
                    : string.Empty;

                sb.AppendLine($"        <li>");
                sb.AppendLine($"          <span class=\"kind-badge kind-{escape_html(type.kind)}\">{escape_html(type.kind)}</span>");
                sb.AppendLine($"          <a href=\"{typeHref}\">{escape_html(type.name)}{genericPart}</a>");
                if (!string.IsNullOrEmpty(type.doc_comment))
                {
                    var shortDoc = type.doc_comment.Length > 100
                        ? type.doc_comment[..100] + "..."
                        : type.doc_comment;
                    sb.AppendLine($"          <span class=\"type-summary\">{escape_html(shortDoc)}</span>");
                }

                sb.AppendLine("        </li>");
            }

            sb.AppendLine("      </ul>");
        }

        if (module.functions.Count > 0)
        {
            sb.AppendLine("      <h3>函数</h3>");
            sb.AppendLine("      <div class=\"function-list\">");

            foreach (var func in module.functions)
            {
                sb.AppendLine("        <div class=\"function-item\">");
                sb.AppendLine($"          <div class=\"item-header\">");
                sb.AppendLine($"          <pre class=\"item-decl\"><code>{_highlighter.highlight_source(func.signature, module.namespace_name)}</code></pre>");
                sb.AppendLine($"          {src_link(func.source_file)}");
                sb.AppendLine($"          </div>");
                if (!string.IsNullOrEmpty(func.doc_comment))
                {
                    sb.AppendLine($"          <p class=\"func-doc\">{escape_html(func.doc_comment)}</p>");
                }

                sb.AppendLine("        </div>");
            }

            sb.AppendLine("      </div>");
        }

        // 外部类型的 imply 扩展块（如基础类型的 Hash 实现）
        var externalTypes = module.types.Where(t => t.kind == "external").ToList();
        foreach (var extType in externalTypes)
        {
            if (extType.imply_blocks.Count > 0)
            {
                sb.AppendLine($"      <h3>扩展 {escape_html(extType.name)}</h3>");
                foreach (var imply in extType.imply_blocks)
                {
                    sb.AppendLine("      <div class=\"imply-block\">");
                    var contractLabel = string.IsNullOrEmpty(imply.contract_type)
                        ? _highlighter.highlight_type(imply.target_type, module.namespace_name)
                        : $"{_highlighter.highlight_type(imply.target_type, module.namespace_name)}: {_highlighter.highlight_type(imply.contract_type, module.namespace_name)}";
                    sb.AppendLine($"        <h4><span class=\"hl-keyword\">imply</span> {contractLabel}</h4>");
                    foreach (var method in imply.methods)
                    {
                        sb.AppendLine($"        <div class=\"item-header\">");
                        sb.AppendLine($"        <pre class=\"item-decl\"><code>{_highlighter.highlight_source(method.signature, module.namespace_name)}</code></pre>");
                        sb.AppendLine($"        {src_link(method.source_file)}");
                        sb.AppendLine($"        </div>");
                        if (!string.IsNullOrEmpty(method.doc_comment))
                        {
                            sb.AppendLine($"        <div class=\"doc-comment\">{format_doc_comment(method.doc_comment)}</div>");
                        }
                    }

                    sb.AppendLine("      </div>");
                }
            }
        }

        sb.AppendLine("    </main>");
        sb.AppendLine("  </div>");

        write_footer(sb);
        sb.AppendLine("</body>");
        write_html_tail(sb);
        return sb.ToString();
    }

    /// <summary>
    ///     生成类型详情页面 HTML
    /// </summary>
    private string generate_type_html(DocProject project, DocModule module, DocType type, string filePath)
    {
        var sb = new StringBuilder();
        var title = $"{type.name} - {project.name} 文档";
        var cssHref = get_css_href(filePath);
        write_html_head(sb, title, cssHref);
        sb.AppendLine("<body>");
        write_header(sb, _isUnifiedWorkspace ? "Workspace" : project.name, _isUnifiedWorkspace ? string.Empty : project.description);

        sb.AppendLine("  <div class=\"layout\">");
        write_sidebar(sb, project, module.namespace_name, type.name, filePath);

        sb.AppendLine("    <main class=\"main-content\">");

        var moduleDisplayName = string.IsNullOrEmpty(module.namespace_name) ? "(默认模块)" : module.namespace_name;
        var indexHref = get_index_href(filePath);

        sb.AppendLine($"      <p class=\"breadcrumb\"><a href=\"{indexHref}\">{escape_html(_isUnifiedWorkspace ? "Workspace" : project.name)}</a> / <a href=\"index.html\">{escape_html(moduleDisplayName)}</a> / {escape_html(type.name)}</p>");

        var genericPart = type.generic_params.Count > 0
            ? $"&lt;{string.Join(", ", type.generic_params.Select(escape_html))}&gt;"
            : string.Empty;

        sb.AppendLine($"      <h2><span class=\"kind-badge kind-{escape_html(type.kind)}\">{escape_html(type.kind)}</span> {escape_html(type.name)}{genericPart}{src_link(type.source_file)}</h2>");

        if (!string.IsNullOrEmpty(type.doc_comment))
        {
            sb.AppendLine($"      <div class=\"doc-comment\">{format_doc_comment(type.doc_comment)}</div>");
        }

        if (type.generic_params.Count > 0)
        {
            sb.AppendLine("      <h3>泛型参数</h3>");
            sb.AppendLine("      <ul class=\"generic-params\">");
            foreach (var gp in type.generic_params)
            {
                sb.AppendLine($"        <li>{escape_html(gp)}</li>");
            }

            sb.AppendLine("      </ul>");
        }

        if (type.fields.Count > 0)
        {
            sb.AppendLine("      <h3>字段</h3>");
            sb.AppendLine("      <table class=\"fields-table\">");
            sb.AppendLine("        <thead><tr><th>名称</th><th>类型</th><th>描述</th></tr></thead>");
            sb.AppendLine("        <tbody>");
            foreach (var field in type.fields)
            {
                sb.AppendLine("          <tr>");
                sb.AppendLine($"            <td><code>{escape_html(field.name)}</code></td>");
                sb.AppendLine($"            <td><code>{_highlighter.highlight_type(field.field_type, module.namespace_name)}</code></td>");
                sb.AppendLine($"            <td>{escape_html(field.doc_comment)}</td>");
                sb.AppendLine("          </tr>");
            }

            sb.AppendLine("        </tbody>");
            sb.AppendLine("      </table>");
        }

        if (type.methods.Count > 0)
        {
            sb.AppendLine("      <h3>方法</h3>");
            foreach (var method in type.methods)
            {
                sb.AppendLine("      <div class=\"method-item\">");
                sb.AppendLine($"        <div class=\"item-header\">");
                sb.AppendLine($"        <pre class=\"item-decl\"><code>{_highlighter.highlight_source(method.signature, module.namespace_name)}</code></pre>");
                sb.AppendLine($"        {src_link(method.source_file)}");
                sb.AppendLine($"        </div>");
                if (!string.IsNullOrEmpty(method.doc_comment))
                {
                    sb.AppendLine($"        <div class=\"doc-comment\">{format_doc_comment(method.doc_comment)}</div>");
                }

                if (method.parameters.Count > 0)
                {
                    sb.AppendLine("        <table class=\"params-table\">");
                    sb.AppendLine("          <thead><tr><th>参数</th><th>类型</th></tr></thead>");
                    sb.AppendLine("          <tbody>");
                    foreach (var param in method.parameters)
                    {
                        sb.AppendLine("            <tr>");
                        sb.AppendLine($"              <td><code><span class=\"hl-parameter\">{escape_html(param.name)}</span></code></td>");
                        sb.AppendLine($"              <td><code>{_highlighter.highlight_type(param.param_type, module.namespace_name)}</code></td>");
                        sb.AppendLine("            </tr>");
                    }

                    sb.AppendLine("          </tbody>");
                    sb.AppendLine("        </table>");
                }

                sb.AppendLine("      </div>");
            }
        }

        if (type.imply_blocks.Count > 0)
        {
            sb.AppendLine("      <h3>Imply 扩展块</h3>");
            foreach (var imply in type.imply_blocks)
            {
                sb.AppendLine("      <div class=\"imply-block\">");
                var contractLabel = string.IsNullOrEmpty(imply.contract_type)
                    ? _highlighter.highlight_type(imply.target_type, module.namespace_name)
                    : $"{_highlighter.highlight_type(imply.target_type, module.namespace_name)}: {_highlighter.highlight_type(imply.contract_type, module.namespace_name)}";
                sb.AppendLine($"        <h4><span class=\"hl-keyword\">imply</span> {contractLabel}</h4>");
                foreach (var method in imply.methods)
                {
                    sb.AppendLine($"        <div class=\"item-header\">");
                    sb.AppendLine($"        <pre class=\"item-decl\"><code>{_highlighter.highlight_source(method.signature, module.namespace_name)}</code></pre>");
                    sb.AppendLine($"        {src_link(method.source_file)}");
                    sb.AppendLine($"        </div>");
                    if (!string.IsNullOrEmpty(method.doc_comment))
                    {
                        sb.AppendLine($"        <div class=\"doc-comment\">{format_doc_comment(method.doc_comment)}</div>");
                    }
                }

                sb.AppendLine("      </div>");
            }
        }

        sb.AppendLine("    </main>");
        sb.AppendLine("  </div>");

        write_footer(sb);
        sb.AppendLine("</body>");
        write_html_tail(sb);
        return sb.ToString();
    }

    #endregion

    #region 源代码页面

    /// <summary>
    ///     收集所有唯一源文件并生成带语法高亮的源代码页面
    /// </summary>
    private void generate_source_pages(DocProject project, string outputDir)
    {
        // 收集所有唯一的源文件路径
        var sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in project.modules)
        {
            foreach (var type in module.types)
            {
                if (!string.IsNullOrEmpty(type.source_file) && File.Exists(type.source_file))
                {
                    sourceFiles.Add(type.source_file);
                }

                foreach (var method in type.methods)
                {
                    if (!string.IsNullOrEmpty(method.source_file) && File.Exists(method.source_file))
                    {
                        sourceFiles.Add(method.source_file);
                    }
                }
            }

            foreach (var func in module.functions)
            {
                if (!string.IsNullOrEmpty(func.source_file) && File.Exists(func.source_file))
                {
                    sourceFiles.Add(func.source_file);
                }
            }
        }

        foreach (var absolutePath in sourceFiles)
        {
            try
            {
                var srcHtml = generate_source_page(absolutePath);
                var href = src_href(absolutePath);
                File.WriteAllText(Path.Combine(outputDir, href), srcHtml);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"生成源代码页面失败: {absolutePath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     生成单个源代码页面
    /// </summary>
    private string generate_source_page(string absolutePath)
    {
        var source = File.ReadAllText(absolutePath);
        var relativePath = get_relative_source_path(absolutePath);

        var sb = new StringBuilder();
        write_html_head(sb, $"{relativePath} - 源代码", get_css_href(""));
        sb.AppendLine("<body class=\"source-page\">");

        sb.AppendLine("  <div class=\"source-header\">");
        sb.AppendLine($"    <span class=\"source-file-path\">{escape_html(relativePath)}</span>");
        sb.AppendLine("    <a class=\"back-link\" href=\"javascript:history.back()\">← 返回</a>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <div class=\"source-content\">");
        sb.AppendLine("    <pre class=\"source-code\"><code>");

        // 应用语法高亮
        var highlighted = _highlighter.highlight_source(source, string.Empty);

        // 添加行号
        var lines = highlighted.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var lineNum = i + 1;
            sb.Append($"<a class=\"line-number\" id=\"L{lineNum}\" href=\"#L{lineNum}\">{lineNum}</a>");
            sb.Append($"<span class=\"line-content\">{lines[i]}</span>");
            if (i < lines.Length - 1)
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("</code></pre>");
        sb.AppendLine("  </div>");

        sb.AppendLine("</body>");
        write_html_tail(sb);
        return sb.ToString();
    }

    /// <summary>
    ///     获取源代码页面的 HTML 文件名
    /// </summary>
    private static string src_href(string absolutePath)
    {
        var relative = get_relative_source_path(absolutePath);
        return $"src_{sanitize_filename(relative)}.html";
    }

    /// <summary>
    ///     生成 [src] 跳转链接的 HTML
    /// </summary>
    private string src_link(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
        {
            return string.Empty;
        }

        var href = src_href(absolutePath);
        return $"<a class=\"src-link\" href=\"{href}\" target=\"_blank\" title=\"查看源代码\">[src]</a>";
    }

    #endregion

    #region 侧边栏

    /// <summary>
    ///     写入侧边栏 HTML（按 kind 分组：Modules / Structures / Classes / Traits 等并列为顶级分组）
    ///     workspace 模式消除 package 区分，所有模块已按命名空间合并
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="project">当前项目（workspace 模式为合并后的虚拟项目）</param>
    /// <param name="currentModule">当前模块命名空间（用于高亮）</param>
    /// <param name="currentType">当前类型名称（用于高亮）</param>
    /// <param name="currentFilePath">当前页面文件路径（用于计算相对链接）</param>
    private void write_sidebar(StringBuilder sb, DocProject project, string currentModule, string currentType, string currentFilePath = "")
    {
        // workspace 模式：project 已是合并后的模块，直接使用
        // 单项目模式：使用 project 本身
        var modules = project.modules;

        // 计算从当前页面到根目录的路径前缀
        var depth = string.IsNullOrEmpty(currentFilePath) ? 0 : currentFilePath.Count(c => c == '/');
        var prefix = depth == 0 ? string.Empty : string.Concat(Enumerable.Repeat("../", depth));

        // 收集所有命名空间和类型
        var allNamespaces = new List<string>();
        var typesByKind = new Dictionary<string, List<(string name, string ns)>>(StringComparer.Ordinal);

        foreach (var module in modules)
        {
            if (!string.IsNullOrEmpty(module.namespace_name))
            {
                allNamespaces.Add(module.namespace_name);
            }

            foreach (var type in module.types)
            {
                if (type.kind == "external")
                {
                    continue;
                }

                if (!typesByKind.TryGetValue(type.kind, out var list))
                {
                    list = [];
                    typesByKind[type.kind] = list;
                }

                list.Add((type.name, module.namespace_name));
            }
        }

        sb.AppendLine("    <nav class=\"sidebar\">");
        sb.AppendLine("      <div class=\"sidebar-header\">");
        var sidebarTitle = _isUnifiedWorkspace ? "Workspace" : project.name;
        sb.AppendLine($"        <a href=\"{prefix}index.html\" class=\"sidebar-project-name\">{escape_html(sidebarTitle)}</a>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <ul class=\"sidebar-nav\">");

        // Modules 分区（树形结构）
        if (allNamespaces.Count > 0)
        {
            var moduleTree = build_module_tree(allNamespaces);
            sb.AppendLine("        <li class=\"nav-kind-group\">");
            sb.AppendLine("          <span class=\"nav-kind-label\">Modules</span>");
            render_module_tree(sb, moduleTree, currentModule, string.Empty, prefix);
            sb.AppendLine("        </li>");
        }

        // 各类型分区（按固定顺序）
        var kindOrder = new[] { "structure", "class", "trait", "unite", "union", "enums", "flags" };
        foreach (var kind in kindOrder)
        {
            if (!typesByKind.TryGetValue(kind, out var typeList) || typeList.Count == 0)
            {
                continue;
            }

            var kindLabel = get_kind_label(kind);
            var items = typeList
                .OrderBy(t => t.name, StringComparer.Ordinal)
                .Select(t =>
                {
                    var href = prefix + get_type_href(t.ns, t.name);
                    var isActive = t.ns == currentModule && t.name == currentType;
                    return (t.name, href, isActive);
                })
                .ToList();

            render_sidebar_section(sb, kindLabel, items);
        }

        sb.AppendLine("      </ul>");
        sb.AppendLine("    </nav>");
    }

    /// <summary>
    ///     模块树节点
    /// </summary>
    private sealed class ModuleTreeNode
    {
        /// <summary>
        ///     段名称
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        ///     完整命名空间
        /// </summary>
        public string fullNs = string.Empty;

        /// <summary>
        ///     子节点
        /// </summary>
        public List<ModuleTreeNode> children = [];
    }

    /// <summary>
    ///     从命名空间列表构建树形结构
    /// </summary>
    private static ModuleTreeNode build_module_tree(List<string> namespaces)
    {
        var root = new ModuleTreeNode();

        foreach (var ns in namespaces.OrderBy(n => n, StringComparer.Ordinal))
        {
            var parts = ns.Split('.');
            var current = root;
            var path = string.Empty;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                path = string.IsNullOrEmpty(path) ? part : $"{path}.{part}";

                var child = current.children.Find(c => c.name == part);
                if (child is null)
                {
                    child = new ModuleTreeNode { name = part, fullNs = path };
                    current.children.Add(child);
                }

                current = child;
            }
        }

        return root;
    }

    /// <summary>
    ///     递归渲染模块树
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="node">当前树节点</param>
    /// <param name="currentModule">当前模块命名空间（用于高亮）</param>
    /// <param name="indent">缩进字符串</param>
    /// <param name="prefix">从当前页面到根目录的相对路径前缀</param>
    private static void render_module_tree(StringBuilder sb, ModuleTreeNode node, string currentModule, string indent, string prefix = "")
    {
        foreach (var child in node.children)
        {
            var hasChildren = child.children.Count > 0;
            var moduleHref = prefix + get_module_href(child.fullNs);
            var isActive = child.fullNs == currentModule;
            var activeClass = isActive ? " active" : string.Empty;

            if (hasChildren)
            {
                // 有子节点的模块：可折叠
                sb.AppendLine($"          <div class=\"nav-tree-item\">");
                sb.AppendLine($"            <span class=\"nav-indent\">{indent}</span>");
                sb.AppendLine($"            <span class=\"nav-toggle\" onclick=\"toggleNav(this)\">▸</span>");
                sb.AppendLine($"            <a href=\"{moduleHref}\" class=\"nav-link{activeClass}\">{escape_html(child.name)}</a>");
                sb.AppendLine("          </div>");
                sb.AppendLine("          <div class=\"nav-children\">");
                render_module_tree(sb, child, currentModule, indent + "  ", prefix);
                sb.AppendLine("          </div>");
            }
            else
            {
                // 叶子节点：简单链接
                sb.AppendLine($"          <div class=\"nav-tree-item\">");
                sb.AppendLine($"            <span class=\"nav-indent\">{indent}</span>");
                sb.AppendLine($"            <span class=\"nav-tree-leaf\"></span>");
                sb.AppendLine($"            <a href=\"{moduleHref}\" class=\"nav-link{activeClass}\">{escape_html(child.name)}</a>");
                sb.AppendLine("          </div>");
            }
        }
    }

    /// <summary>
    ///     渲染侧边栏中一个分组（标题 + 链接列表）
    /// </summary>
    /// <param name="sb">字符串构建器</param>
    /// <param name="heading">分组标题</param>
    /// <param name="items">分组事项（显示名、链接、是否激活）</param>
    private static void render_sidebar_section(StringBuilder sb, string heading, List<(string display, string href, bool active)> items)
    {
        sb.AppendLine($"        <li class=\"nav-kind-group\">");
        sb.AppendLine($"          <span class=\"nav-kind-label\">{escape_html(heading)}</span>");
        foreach (var (display, href, active) in items)
        {
            var activeClass = active ? " active" : string.Empty;
            sb.AppendLine($"          <div class=\"nav-type\"><a href=\"{href}\" class=\"nav-link{activeClass}\">{escape_html(display)}</a></div>");
        }

        sb.AppendLine("        </li>");
    }

    #endregion

    #region 类型解析与渲染

    /// <summary>
    ///     尝试在类型索引中解析类型全限定名
    /// </summary>
    /// <param name="typeString">类型字符串（可能包含泛型参数等）</param>
    /// <param name="currentModule">当前模块命名空间（用于解析短名称）</param>
    /// <returns>解析到的类型引用，或 null</returns>
    private ResolvedTypeRef? try_resolve_type(string typeString, string currentModule)
    {
        if (_typeIndex is null || string.IsNullOrEmpty(typeString))
        {
            return null;
        }

        var baseName = extract_base_type_name(typeString);
        if (string.IsNullOrEmpty(baseName))
        {
            return null;
        }

        if (_primitives.Contains(baseName))
        {
            return null;
        }

        if (_typeIndex.TryGetValue(baseName, out var ref_))
        {
            return ref_;
        }

        if (!baseName.Contains('.') && !string.IsNullOrEmpty(currentModule))
        {
            var fqn = $"{currentModule}.{baseName}";
            if (_typeIndex.TryGetValue(fqn, out var ref2))
            {
                return ref2;
            }
        }

        return null;
    }

    /// <summary>
    ///     渲染类型引用：已知类型生成链接，外部类型生成着色 span
    /// </summary>
    /// <param name="typeString">类型字符串</param>
    /// <param name="currentModule">当前模块命名空间</param>
    /// <returns>HTML 片段</returns>
    private string render_type_reference(string typeString, string currentModule)
    {
        var display = escape_html(typeString);
        var resolved = try_resolve_type(typeString, currentModule);

        if (resolved is not null)
        {
            var href = get_type_href(resolved.module ?? string.Empty, resolved.typeName);

            return $"<a class=\"hl-type\" href=\"{href}\">{display}</a>";
        }

        if (is_user_defined_type(typeString))
        {
            return $"<span class=\"hl-external\" title=\"外部类型（依赖中定义）\">{display}</span>";
        }

        return display;
    }

    /// <summary>
    ///     判断类型字符串是否看起来是用户自定义类型（非基础类型）
    /// </summary>
    private static bool is_user_defined_type(string typeString)
    {
        if (string.IsNullOrEmpty(typeString))
        {
            return false;
        }

        var baseName = extract_base_type_name(typeString);
        if (string.IsNullOrEmpty(baseName))
        {
            return false;
        }

        if (_primitives.Contains(baseName))
        {
            return false;
        }

        if (baseName.StartsWith("micro(", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     从复杂类型字符串中提取基础类型名
    /// </summary>
    /// <remarks>
    ///     如 "std.collections.HashMap&lt;i32, string&gt;?" → "std.collections.HashMap"
    ///     如 "i32 | string" → 返回空（联合类型不解析）
    /// </remarks>
    private static string extract_base_type_name(string typeString)
    {
        if (string.IsNullOrEmpty(typeString))
        {
            return string.Empty;
        }

        var span = typeString.AsSpan().Trim();

        if (span.Contains('|') || span.Contains('&'))
        {
            return string.Empty;
        }

        if (span.StartsWith("micro(", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (span.EndsWith("?"))
        {
            span = span[..^1];
        }

        var angleIdx = span.IndexOf('<');
        if (angleIdx > 0)
        {
            span = span[..angleIdx];
        }

        return span.Trim().ToString();
    }

    #endregion

    #region HTML 结构

    /// <summary>
    ///     写入 HTML head 部分
    /// </summary>
    private void write_html_head(StringBuilder sb, string title, string cssHref)
    {
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{escape_html(title)}</title>");
        sb.AppendLine($"  <link rel=\"stylesheet\" href=\"{cssHref}\">");
        if (!string.IsNullOrEmpty(_themeLinkTags))
        {
            sb.Append(_themeLinkTags);
        }
        sb.AppendLine($"  <script src=\"{get_js_href("")}\" defer></script>");
        sb.AppendLine("</head>");
    }

    /// <summary>
    ///     写入 HTML tail 部分
    /// </summary>
    private static void write_html_tail(StringBuilder sb)
    {
        sb.AppendLine("</html>");
    }

    /// <summary>
    ///     写入页面头部
    /// </summary>
    private static void write_header(StringBuilder sb, string projectName, string projectDescription)
    {
        sb.AppendLine("  <header class=\"site-header\">");
        sb.AppendLine("    <div class=\"header-content\">");
        sb.AppendLine("      <button class=\"menu-toggle\" aria-label=\"切换导航菜单\">☰</button>");
        sb.AppendLine($"      <h1>{escape_html(projectName)}</h1>");
        if (!string.IsNullOrEmpty(projectDescription))
        {
            sb.AppendLine($"      <p class=\"header-desc\">{escape_html(projectDescription)}</p>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine("  </header>");
    }

    /// <summary>
    ///     写入页面底部（含侧边栏切换脚本）
    /// </summary>
    private static void write_footer(StringBuilder sb)
    {
        sb.AppendLine("  <footer class=\"site-footer\">");
        sb.AppendLine("    <p>由 <strong>legion doc</strong> 生成</p>");
        sb.AppendLine("  </footer>");
        sb.AppendLine("  <script>");
        sb.AppendLine("    function toggleNav(el) {");
        sb.AppendLine("      var parent = el.parentElement;");
        sb.AppendLine("      var children = parent.nextElementSibling;");
        sb.AppendLine("      if (children && children.classList.contains('nav-children')) {");
        sb.AppendLine("        var isVisible = children.style.display === 'block';");
        sb.AppendLine("        children.style.display = isVisible ? 'none' : 'block';");
        sb.AppendLine("        el.classList.toggle('expanded', !isVisible);");
        sb.AppendLine("        var folder = parent.querySelector('.nav-folder');");
        sb.AppendLine("        if (folder) folder.classList.toggle('expanded', !isVisible);");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("    (function() {");
        sb.AppendLine("      var toggle = document.querySelector('.menu-toggle');");
        sb.AppendLine("      var sidebar = document.querySelector('.sidebar');");
        sb.AppendLine("      if (toggle && sidebar) {");
        sb.AppendLine("        toggle.addEventListener('click', function() { sidebar.classList.toggle('open'); });");
        sb.AppendLine("        document.addEventListener('click', function(e) {");
        sb.AppendLine("          if (!sidebar.contains(e.target) && !toggle.contains(e.target)) { sidebar.classList.remove('open'); }");
        sb.AppendLine("        });");
        sb.AppendLine("      }");
        sb.AppendLine("    })();");
        sb.AppendLine("  </script>");
    }

    #endregion

    #region CSS 输出

    /// <summary>
    ///     将 CSS 写入独立的 legion-document.css 文件
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    private static void write_css_file(string outputDir)
    {
        var css = build_css();
        atomic_write_all_text(Path.Combine(outputDir, "legion-document.css"), css);
    }

    /// <summary>
    ///     将 JS 写入独立的 legion-document.js 文件
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    private static void write_js_file(string outputDir)
    {
        var js = build_js();
        var jsPath = Path.Combine(outputDir, "legion-document.js");
        if (!File.Exists(jsPath))
        {
            atomic_write_all_text(jsPath, js);
        }
    }

    /// <summary>
    ///     构建 JS 内容（动态功能：侧边栏、语言、主题）
    /// </summary>
    private static string build_js()
    {
        return @"(function() {
    'use strict';

    // ── 语言偏好持久化 ──
    const LS_KEY = 'legion-doc-lang';

    function getLang() {
        return localStorage.getItem(LS_KEY) || 'zh-hans';
    }

    function setLang(lang) {
        localStorage.setItem(LS_KEY, lang);
        switchLang(lang);
    }

    function switchLang(lang) {
        // 尝试跳转到对应语言版本的文档
        var currentPath = window.location.pathname;
        var langMap = { 'zh-hans': 'zh-hans', 'en': 'en' };
        var targetLang = langMap[lang] || 'zh-hans';

        // 查找页面上的语言切换链接
        var langLinks = document.querySelectorAll('[data-lang]');
        langLinks.forEach(function(link) {
            var linkLang = link.getAttribute('data-lang');
            if (linkLang === targetLang) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
    }

    // ── 侧边栏切换 ──
    function initSidebar() {
        var toggle = document.querySelector('.menu-toggle');
        var sidebar = document.querySelector('.sidebar');
        if (!toggle || !sidebar) return;

        toggle.addEventListener('click', function() {
            sidebar.classList.toggle('open');
        });

        // 点击主内容区关闭侧边栏
        var mainContent = document.querySelector('.main-content');
        if (mainContent) {
            mainContent.addEventListener('click', function() {
                sidebar.classList.remove('open');
            });
        }
    }

    // ── 侧边栏树展开/折叠 ──
    function initTreeToggle() {
        document.querySelectorAll('.nav-toggle').forEach(function(toggle) {
            toggle.addEventListener('click', function() {
                this.classList.toggle('expanded');
                var children = this.parentElement.querySelector('.nav-children');
                if (children) {
                    if (children.style.display === 'block') {
                        children.style.display = 'none';
                    } else {
                        children.style.display = 'block';
                    }
                }
            });
        });
    }

    // ── 主题切换（亮色/暗色） ──
    const THEME_KEY = 'legion-doc-theme';

    function getTheme() {
        return localStorage.getItem(THEME_KEY) || 'light';
    }

    function setTheme(theme) {
        localStorage.setItem(THEME_KEY, theme);
        document.documentElement.setAttribute('data-theme', theme);
    }

    function initTheme() {
        var saved = getTheme();
        document.documentElement.setAttribute('data-theme', saved);

        // 监听系统主题变化
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function(e) {
                if (!localStorage.getItem(THEME_KEY)) {
                    document.documentElement.setAttribute('data-theme', e.matches ? 'dark' : 'light');
                }
            });
        }
    }

    // ── 初始化 ──
    document.addEventListener('DOMContentLoaded', function() {
        initSidebar();
        initTreeToggle();
        initTheme();
        switchLang(getLang());
    });

    // 暴露全局 API
    window.LegionDoc = {
        getLang: getLang,
        setLang: setLang,
        getTheme: getTheme,
        setTheme: setTheme
    };
})();
";
    }

    /// <summary>
    ///     构建 CSS 内容（rustdoc 风格）
    /// </summary>
    private static string build_css()
    {
        return @":root {
    --main-bg: #ffffff;
    --main-color: #1a1a1a;
    --sidebar-bg: #f5f5f5;
    --sidebar-color: #444;
    --sidebar-hover: #e0e0e0;
    --sidebar-active: #d4d4d4;
    --code-bg: #fafafa;
    --code-inline-bg: #f0f0f0;
    --code-border: #e5e5e5;
    --link-color: #3873ad;
    --link-hover: #2b5c8a;
    --border-color: #ddd;
    --heading-color: #333;
    --topbar-bg: #f5f2f0;
    --topbar-border: #e5e0dc;
    --rust-brown: #c67b34;
    --rust-dark: #573b21;
    --item-bg: #fcfcfc;
    --table-stripe: #f9f9f9;
    --shadow-sm: 0 1px 3px rgba(0,0,0,0.06);
    --shadow-md: 0 2px 8px rgba(0,0,0,0.08);
    --doc-comment-bg: #fcfcfc;
    --doc-comment-border: #eee;
}

*, *::before, *::after { box-sizing: border-box; }

body {
    margin: 0; padding: 0;
    font-family: ""Fira Sans"", ""Noto Sans SC"", system-ui, -apple-system, sans-serif;
    font-size: 15px; line-height: 1.65;
    color: var(--main-color);
    background: var(--main-bg);
    -webkit-font-smoothing: antialiased;
}

/* ── 顶部导航栏 ── */
.site-header {
    background: var(--topbar-bg);
    border-bottom: 1px solid var(--topbar-border);
    padding: 0;
    position: sticky; top: 0; z-index: 100;
}

.header-content {
    max-width: 1200px; margin: 0 auto;
    display: flex; align-items: center;
    padding: 0.6rem 1.5rem; gap: 1rem;
}

.site-header h1 {
    margin: 0; font-size: 1.2rem; font-weight: 600;
    color: var(--rust-dark); letter-spacing: -0.3px;
}

.header-desc {
    margin: 0; font-size: 0.82rem; color: #888;
}

.menu-toggle {
    display: none; background: none;
    border: 1px solid var(--border-color); color: var(--main-color);
    font-size: 1.2rem; cursor: pointer;
    padding: 0.2rem 0.5rem; border-radius: 4px;
    line-height: 1;
}

.menu-toggle:hover { background: var(--sidebar-hover); }

/* ── 左右布局 ── */
.layout {
    display: flex; min-height: calc(100vh - 48px);
    max-width: 1200px; margin: 0 auto;
}

/* ── 侧边栏 ── */
.sidebar {
    position: sticky; top: 48px;
    width: 240px; height: calc(100vh - 48px);
    overflow-y: auto; flex-shrink: 0;
    background: var(--sidebar-bg);
    border-right: 1px solid var(--border-color);
    padding: 16px 12px; font-size: 0.85rem;
    scrollbar-width: thin;
}

.sidebar::-webkit-scrollbar { width: 6px; }
.sidebar::-webkit-scrollbar-thumb { background: #ccc; border-radius: 3px; }

.sidebar-header {
    padding: 0 0 12px 0;
    border-bottom: 1px solid var(--border-color);
    margin-bottom: 12px;
}

.sidebar-project-name {
    font-size: 1.05rem; font-weight: 700;
    color: var(--rust-dark); text-decoration: none;
    display: flex; align-items: center; gap: 6px;
}

.sidebar-project-name::before {
    content: '🦀'; font-size: 1rem;
}

.sidebar-project-name:hover { opacity: 0.8; text-decoration: none; }

/* 侧边栏导航 */
.sidebar-nav { list-style: none; padding: 0; margin: 0; }

.nav-kind-group { margin-top: 14px; }

.nav-kind-label {
    display: block; padding: 3px 8px 5px 8px;
    font-size: 0.7rem; color: #999;
    text-transform: uppercase; letter-spacing: 0.8px;
    font-weight: 700; border-top: 1px solid var(--border-color);
    padding-top: 8px; margin-top: 4px;
}

.nav-kind-group:first-child .nav-kind-label {
    border-top: none; margin-top: 0; padding-top: 3px;
}

.nav-type { padding: 2px 8px; font-size: 0.84rem; }

.nav-type a, .nav-link {
    color: var(--link-color); text-decoration: none;
    display: block; padding: 2px 4px; border-radius: 3px;
    transition: background 0.1s;
}

.nav-type a:hover, .nav-link:hover {
    background: var(--sidebar-hover); text-decoration: none;
}

.nav-link.active {
    background: var(--sidebar-active); border-radius: 3px; font-weight: 600; color: #333;
}

/* 模块树 */
.nav-tree-item {
    display: flex; align-items: center; padding: 1px 0; font-size: 0.84rem;
}

.nav-indent { display: inline-block; white-space: pre; color: transparent; }

.nav-toggle {
    font-size: 0.55rem; color: #999; cursor: pointer;
    width: 14px; flex-shrink: 0; transition: transform 0.15s;
    user-select: none; text-align: center;
}

.nav-toggle:hover { color: #555; }
.nav-toggle.expanded { transform: rotate(90deg); }
.nav-tree-leaf { width: 14px; flex-shrink: 0; }
.nav-children { display: none; }
.nav-children[style*=""block""], .nav-children.expanded { display: block; }

/* ── 主内容区 ── */
.main-content {
    flex: 1; padding: 28px 36px; max-width: 860px; min-width: 0;
}

.breadcrumb {
    font-size: 0.82rem; color: #999; margin-bottom: 1.2rem;
    display: flex; align-items: center; gap: 4px; flex-wrap: wrap;
}

.breadcrumb a { color: var(--link-color); text-decoration: none; }
.breadcrumb a:hover { text-decoration: underline; }
.breadcrumb a + a::before { content: '/'; margin: 0 4px; color: #ccc; }

/* ── 标题 ── */
.main-content h2 {
    font-size: 1.5rem; color: var(--heading-color);
    border-bottom: 1px solid var(--border-color);
    padding-bottom: 0.4rem; margin: 2.2rem 0 1rem;
    font-weight: 600;
}

.main-content h3 {
    font-size: 1.2rem; color: var(--heading-color);
    margin: 1.8rem 0 0.8rem; font-weight: 600;
}

.main-content h4 {
    font-size: 1rem; color: #555;
    margin: 1.2rem 0 0.5rem; font-weight: 600;
}

.main-content > p:first-of-type {
    font-size: 0.95rem; color: #666;
}

a { color: var(--link-color); text-decoration: none; }
a:hover { color: var(--link-hover); text-decoration: underline; }

/* ── 语法高亮 ── */
.hl-keyword { color: #8959a8; font-weight: 500; }
.hl-type { color: #4271ae; }
.hl-function { color: #4271ae; font-weight: 500; }
.hl-string { color: #718c00; }
.hl-number { color: #c76c29; }
.hl-comment { color: #8e908c; font-style: italic; }
.hl-operator { color: #333; }
.hl-punctuation { color: #666; }
.hl-identifier { color: #333; }
.hl-parameter { color: #c76c29; }
.hl-external { color: #c76c29; border-bottom: 1px dashed #c76c29; cursor: help; }
.hl-primitive { color: #4271ae; }

/* ── 代码签名块 (rustdoc style) ── */
.item-decl {
    background: var(--item-bg);
    border: 1px solid var(--code-border);
    border-left: 3px solid var(--rust-brown);
    border-radius: 0 4px 4px 0;
    padding: 12px 16px; overflow-x: auto;
    margin: 10px 0; white-space: pre;
    font-size: 0.9rem; line-height: 1.6;
    box-shadow: var(--shadow-sm);
}

.item-decl code {
    font-family: ""Source Code Pro"", ""Fira Code"", ""Consolas"", monospace;
    font-size: 0.9rem; background: none; padding: 0; border-radius: 0;
}

/* ── 模块卡片 (首页) ── */
.module-card {
    background: var(--item-bg);
    border: 1px solid var(--border-color);
    border-radius: 6px; padding: 1.2rem 1.5rem;
    margin-bottom: 1rem;
    transition: box-shadow 0.15s;
}

.module-card:hover { box-shadow: var(--shadow-md); }

.module-card h3 { margin: 0 0 0.2rem; font-size: 1.1rem; }

.module-card h3 a {
    color: var(--link-color);
}

.module-card h3 a::before {
    content: '📦 '; font-size: 0.9rem;
}

.module-desc { color: #777; margin: 0.3rem 0 0; font-size: 0.88rem; }
.module-stats { color: #aaa; font-size: 0.78rem; margin: 0.4rem 0 0; }
.module-doc { color: #666; margin-bottom: 1.5rem; }

.module-card > p:first-of-type {
    font-size: 0.9rem; color: #777;
}

/* ── 成员列表 ── */
.member-list { list-style: none; padding: 0; }
.member-list li {
    padding: 0.8rem 0; border-bottom: 1px solid #f0f0f0;
}
.member-list li:last-child { border-bottom: none; }
.member-list a { font-size: 1.05rem; font-weight: 500; }
.member-desc { display: block; font-size: 0.82rem; color: #999; margin-top: 0.15rem; }

/* ── 类型列表 ── */
.type-list { list-style: none; padding: 0; }
.type-list li {
    padding: 0.6rem 0; border-bottom: 1px solid #f5f5f5;
    display: flex; align-items: baseline; flex-wrap: wrap; gap: 6px;
}
.type-list li:last-child { border-bottom: none; }
.type-list li a { font-weight: 500; }
.type-summary { font-size: 0.82rem; color: #aaa; }

/* ── 类型标签 rustdoc style ── */
.kind-badge {
    display: inline-block; padding: 1px 8px; border-radius: 10px;
    font-size: 0.7rem; font-weight: 700; text-transform: uppercase;
    letter-spacing: 0.3px; vertical-align: middle;
}

.kind-structure { background: #e8f0fe; color: #1967d2; }
.kind-class { background: #e6f4ea; color: #137333; }
.kind-trait { background: #fef7e0; color: #b06000; }
.kind-unite { background: #f3e8fd; color: #7627bb; }
.kind-union { background: #fce8e6; color: #c5221f; }
.kind-enums { background: #e8f0fe; color: #1967d2; }
.kind-flags { background: #e6f4ea; color: #137333; }
.kind-external { background: #f1f3f4; color: #5f6368; }

/* ── 函数/方法项 ── */
.function-item, .method-item {
    background: var(--item-bg);
    border: 1px solid var(--border-color);
    border-radius: 6px; padding: 1rem 1.2rem;
    margin-bottom: 0.8rem;
    box-shadow: var(--shadow-sm);
}

.func-doc { color: #777; font-size: 0.88rem; margin: 0.5rem 0 0; line-height: 1.6; }
.doc-comment { color: #555; margin: 0.75rem 0; line-height: 1.7; }

.source-file { color: #aaa; font-size: 0.82rem; }
.source-file code {
    background: var(--code-inline-bg); padding: 0.12rem 0.35rem;
    border-radius: 3px; font-size: 0.82rem;
}

/* 源码跳转链接 */
.src-link {
    font-size: 0.75rem; color: #aaa; text-decoration: none;
    margin-left: 0.5rem; font-weight: normal;
    padding: 1px 6px; border: 1px solid transparent; border-radius: 3px;
    transition: all 0.15s;
}

.src-link:hover {
    color: var(--link-color); border-color: var(--border-color);
    text-decoration: none; background: #fafafa;
}

.item-header {
    display: flex; align-items: flex-start; gap: 0.5rem;
}

.item-header .item-decl { flex: 1; margin: 0; }

/* ── 泛型参数 ── */
.generic-params { list-style: none; padding: 0; margin: 0.5rem 0; }
.generic-params li {
    display: inline-block; background: var(--code-inline-bg);
    border: 1px solid var(--border-color); border-radius: 4px;
    padding: 0.15rem 0.6rem; margin-right: 0.4rem;
    font-family: ""Source Code Pro"", monospace; font-size: 0.84rem;
}

/* ── 表格 ── */
.fields-table, .params-table {
    width: 100%; border-collapse: collapse; margin: 0.8rem 0;
    font-size: 0.9rem; border-radius: 6px; overflow: hidden;
    border: 1px solid var(--border-color);
}

.fields-table th, .params-table th {
    text-align: left; background: #f5f5f5;
    padding: 0.6rem 1rem; font-weight: 600; font-size: 0.82rem;
    color: #555; text-transform: uppercase; letter-spacing: 0.3px;
    border-bottom: 2px solid var(--border-color);
}

.fields-table td, .params-table td {
    padding: 0.6rem 1rem; border-bottom: 1px solid #f0f0f0;
}

.fields-table tr:last-child td, .params-table tr:last-child td { border-bottom: none; }
.fields-table tr:nth-child(even), .params-table tr:nth-child(even) { background: var(--table-stripe); }

/* ── 行内代码 ── */
code {
    font-family: ""Source Code Pro"", ""Fira Code"", ""Consolas"", monospace;
    font-size: 0.88em; background: var(--code-inline-bg);
    padding: 0.1rem 0.35rem; border-radius: 3px;
    color: #333;
}

pre code { background: none; padding: 0; border-radius: 0; font-size: 0.85rem; }

/* ── Imply 扩展块 ── */
.imply-block {
    background: var(--item-bg); border: 1px solid var(--border-color);
    border-radius: 6px; padding: 1rem 1.2rem; margin-bottom: 0.8rem;
    box-shadow: var(--shadow-sm);
}

.imply-block h4 {
    margin: 0 0 0.8rem;
    font-family: ""Source Code Pro"", monospace; font-size: 0.92rem;
    color: var(--main-color);
}

/* ── 页脚 ── */
.site-footer {
    text-align: center; padding: 2.5rem 1.5rem;
    color: #bbb; font-size: 0.8rem;
    border-top: 1px solid var(--border-color); margin-top: 4rem;
}

/* ── 用户文档横幅 ── */
.user-doc-banner {
    background: linear-gradient(135deg, #f0f5fa 0%, #e8f0fe 100%);
    border: 1px solid #90b4db; border-radius: 8px;
    padding: 1rem 1.5rem; margin-bottom: 1.5rem;
    display: flex; align-items: center; gap: 1rem;
}

.user-doc-banner a {
    font-size: 1rem; font-weight: 600; color: #3873ad;
    text-decoration: none; padding: 0.35rem 1.2rem;
    border: 1px solid #3873ad; border-radius: 4px;
    white-space: nowrap; transition: all 0.15s;
}

.user-doc-banner a:hover {
    background: #3873ad; color: #fff; text-decoration: none;
}

.user-doc-banner span { color: #777; font-size: 0.85rem; }

/* ── 响应式 ── */
@media (max-width: 700px) {
    .menu-toggle { display: block; }
    .layout { flex-direction: column; }
    .sidebar {
        position: relative; top: 0; width: 100%; height: auto;
        max-height: 50vh; border-right: none;
        border-bottom: 1px solid var(--border-color); display: none;
    }
    .sidebar.open { display: block; }
    .main-content { padding: 16px; }
    .site-header .header-content { padding: 0.5rem 1rem; }
    .site-header h1 { font-size: 1.1rem; }
    .main-content h2 { font-size: 1.25rem; }
}

/* ── 源代码页面 ── */
.source-page .site-header { display: none; }
.source-header {
    display: flex; justify-content: space-between; align-items: center;
    padding: 0.8rem 1.5rem; background: var(--sidebar-bg);
    border-bottom: 1px solid var(--border-color);
}

.source-file-path {
    font-family: ""Source Code Pro"", monospace; font-size: 0.85rem;
    color: var(--main-color);
}

.back-link { color: var(--link-color); text-decoration: none; font-size: 0.85rem; }
.back-link:hover { text-decoration: underline; }
.source-content { padding: 1rem 1.5rem; }

.source-code {
    font-family: ""Source Code Pro"", monospace;
    font-size: 0.84rem; line-height: 1.6;
    background: var(--code-bg); border-radius: 6px;
    padding: 1rem; overflow-x: auto;
    border: 1px solid var(--border-color);
}

.source-code .line-number {
    display: inline-block; width: 44px; color: #bbb;
    text-align: right; padding-right: 1rem;
    user-select: none; text-decoration: none; font-size: 0.78rem;
}

.source-code .line-number:hover { color: var(--link-color); text-decoration: underline; }
.source-code .line-number:target { background: #fff3cd; color: #333; border-radius: 2px; }
.source-code .line-content { }

/* ── 大屏幕优化 ── */
@media (min-width: 1400px) {
    .layout { max-width: 1400px; }
    .main-content { max-width: 960px; }
    .sidebar { width: 260px; }
}

/* ── 打印样式 ── */
@media print {
    .sidebar, .site-header { display: none; }
    .layout { display: block; }
    .main-content { max-width: none; padding: 0; }
}
";
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     获取模块（命名空间）页面的相对路径
    ///     如 "std.collections" → "std/collections/index.html"
    ///     如 "" → "default/index.html"
    /// </summary>
    private static string get_module_path(string namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
        {
            return "default/index.html";
        }

        var parts = namespaceName.Split('.');
        return string.Join("/", parts) + "/index.html";
    }

    /// <summary>
    ///     获取模块页面的 href（用于链接生成）
    /// </summary>
    private static string get_module_href(string namespaceName)
    {
        return get_module_path(namespaceName);
    }

    /// <summary>
    ///     获取类型页面的相对路径
    ///     如 ("std.collections", "HashMap") → "std/collections/HashMap.html"
    ///     如 ("", "Foo") → "default/Foo.html"
    /// </summary>
    private static string get_type_path(string namespaceName, string typeName)
    {
        var ns = string.IsNullOrEmpty(namespaceName) ? "default" : namespaceName.Replace('.', '/');
        return $"{ns}/{typeName}.html";
    }

    /// <summary>
    ///     获取类型页面的 href（用于链接生成）
    /// </summary>
    private static string get_type_href(string namespaceName, string typeName)
    {
        return get_type_path(namespaceName, typeName);
    }

    /// <summary>
    ///     计算 CSS 文件的相对路径（根据当前文件深度）
    ///     根目录文件 → "legion-document.css"
    ///     一级子目录 → "../legion-document.css"
    ///     二级子目录 → "../../legion-document.css"
    /// </summary>
    private string get_css_href(string filePath)
    {
        var depth = filePath.Count(c => c == '/') + _rootDepth;
        if (depth == 0)
        {
            return "legion-document.css";
        }

        return string.Concat(Enumerable.Repeat("../", depth)) + "legion-document.css";
    }

    /// <summary>
    ///     计算从给定文件路径到 legion-document.js 的相对路径
    /// </summary>
    private string get_js_href(string filePath)
    {
        var depth = filePath.Count(c => c == '/') + _rootDepth;
        if (depth == 0)
        {
            return "legion-document.js";
        }

        return string.Concat(Enumerable.Repeat("../", depth)) + "legion-document.js";
    }

    /// <summary>
    ///     计算从给定文件路径到 index.html 的相对路径
    ///     如 "std/collections/HashMap.html" → "../../index.html"
    /// </summary>
    private static string get_index_href(string filePath)
    {
        var depth = filePath.Count(c => c == '/');
        if (depth == 0)
        {
            return "index.html";
        }

        return string.Concat(Enumerable.Repeat("../", depth)) + "index.html";
    }

    /// <summary>
    ///     获取类型种类的中文显示名
    /// </summary>
    private static string get_kind_label(string kind)
    {
        return kind switch
        {
            "class" => "类 (Class)",
            "structure" => "结构体 (Structure)",
            "trait" => "Trait",
            "unite" => "联合类型 (Unite)",
            "union" => "Union",
            "enums" => "枚举 (Enums)",
            "flags" => "位标志 (Flags)",
            "external" => "外部类型",
            _ => kind
        };
    }

    /// <summary>
    ///     获取类型种类的排序权重
    /// </summary>
    private static int type_kind_order(string kind)
    {
        return kind switch
        {
            "class" => 0,
            "structure" => 1,
            "trait" => 2,
            "unite" => 3,
            "union" => 4,
            "enums" => 5,
            "flags" => 6,
            _ => 7
        };
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

    /// <summary>
    ///     格式化文档注释（支持简单换行转 &lt;br&gt;）
    /// </summary>
    private static string format_doc_comment(string docComment)
    {
        if (string.IsNullOrEmpty(docComment))
        {
            return string.Empty;
        }

        return escape_html(docComment).Replace("\n", "<br>");
    }

    /// <summary>
    ///     将命名空间名称转换为安全的文件名
    /// </summary>
    private static string sanitize_filename(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                sb.Append(c);
            }
            else if (c == '.')
            {
                sb.Append('_');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     从绝对路径中提取相对于 source 目录的路径
    /// </summary>
    /// <param name="absolutePath">绝对文件路径</param>
    /// <returns>相对路径，如 "source/collection/hashmap.v"</returns>
    private static string get_relative_source_path(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
        {
            return string.Empty;
        }

        // 查找 source 目录在路径中的位置
        var normalized = absolutePath.Replace('\\', '/');
        var sourceIdx = normalized.LastIndexOf("/source/", StringComparison.OrdinalIgnoreCase);
        if (sourceIdx >= 0)
        {
            return normalized[(sourceIdx + 1)..];
        }

        // 回退：只用文件名
        return Path.GetFileName(absolutePath);
    }

    #endregion
}
