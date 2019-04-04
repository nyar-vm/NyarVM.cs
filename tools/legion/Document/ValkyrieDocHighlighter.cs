using System.Text;
using Nyar.Analyzer.Highlight;
using HighlightKind = Nyar.Analyzer.Highlight.HighlightKind;
using Std.Data.Text.Syntax;
using TextSpan = Std.Text.TextSpan;
using Std.Data.Text.Valkyrie.Lexer;
using ValkyrieTokenKind = Std.Data.Text.Valkyrie.Lexer.ValkyrieTokenKind;

namespace Legion.CLI.Document;

/// <summary>
///     基于 Nyar 高亮基础设施的文档代码着色器
/// </summary>
public sealed class ValkyrieDocHighlighter
{
    #region 常量与字段

    /// <summary>
    ///     基础类型集合
    /// </summary>
    private static readonly HashSet<string> _primitives = new HashSet<string>(StringComparer.Ordinal)
    {
        "i8", "i16", "i32", "i64", "u8", "u16", "u32", "u64",
        "f32", "f64", "string", "bool", "char", "void", "auto", "any",
        "byte", "usize", "utf8", "Self", "List", "Unit"
    };

    /// <summary>
    ///     预配置的语法高亮器
    /// </summary>
    private readonly SyntaxHighlighter _highlighter;

    /// <summary>
    ///     类型索引（用于类型名链接化），键为全限定名
    /// </summary>
    private readonly IReadOnlyDictionary<string, ResolvedTypeRef>? _typeIndex;

    /// <summary>
    ///     短名称索引，键为类型短名称，值为对应的全限定名列表
    /// </summary>
    private readonly Dictionary<string, List<ResolvedTypeRef>> _shortNameIndex = new(StringComparer.Ordinal);

    /// <summary>
    ///     当前项目名称
    /// </summary>
    private readonly string _currentProject;

    /// <summary>
    ///     类型链接生成委托（命名空间, 类型名 → href）
    ///     若为 null 则使用旧的扁平文件名格式
    /// </summary>
    private readonly Func<string, string, string>? _getTypeHref;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建文档代码着色器
    /// </summary>
    /// <param name="typeIndex">类型索引（用于类型名链接化），可为 null</param>
    /// <param name="currentProject">当前项目名称（用于生成相对路径）</param>
    /// <param name="getTypeHref">类型链接生成委托（命名空间, 类型名 → href），null 则使用旧格式</param>
    public ValkyrieDocHighlighter(
        IReadOnlyDictionary<string, ResolvedTypeRef>? typeIndex,
        string currentProject,
        Func<string, string, string>? getTypeHref = null)
    {
        _typeIndex = typeIndex;
        _currentProject = currentProject;
        _getTypeHref = getTypeHref;
        _highlighter = create_highlighter();
        build_short_name_index();
    }

    #endregion

    #region 公开方法

    /// <summary>
    ///     对源码字符串进行 Token 级着色，返回 HTML 片段
    /// </summary>
    /// <param name="source">源码字符串</param>
    /// <param name="currentModule">当前模块命名空间（用于类型链接解析）</param>
    /// <returns>带着色 HTML 标签的片段</returns>
    public string highlight_source(string source, string currentModule)
    {
        // 1. 使用 ValkyrieLexer 进行词法分析
        var lexer = new ValkyrieLexer();
        var tokens = lexer.tokenize(source);

        // 2. 扫描源码，计算每个 token 在源码中的真实位置
        var tokenPositions = new List<(int SourcePos, string Text, int Width, int Kind)>();
        var srcPos = 0;
        foreach (var token in tokens)
        {
            if (token.width <= 0)
            {
                continue;
            }

            if (srcPos >= source.Length)
            {
                break;
            }

            // 获取 token 文本，跳过前导空白找到实际位置
            string text;
            int actualPos;

            if (token.text is not null)
            {
                text = token.text;

                // 字符串 token：源码中引号包裹，文本不含引号，从引号位置开始
                if (token.kind.value == (int)ValkyrieTokenKind.@string && srcPos < source.Length && source[srcPos] == '"')
                {
                    actualPos = srcPos;
                }
                else
                {
                    // 在源码中查找此 token 的实际位置
                    actualPos = source.IndexOf(text, srcPos, StringComparison.Ordinal);
                    if (actualPos < 0 || actualPos > srcPos + 100)
                    {
                        actualPos = srcPos;
                    }
                }
            }
            else
            {
                // 扫描跳过前导空白，找到 token 的实际起始位置
                actualPos = srcPos;
                while (actualPos < source.Length && char.IsWhiteSpace(source[actualPos]))
                {
                    actualPos++;
                }

                if (actualPos >= source.Length)
                {
                    break;
                }

                var actualWidth = Math.Min(token.width, source.Length - actualPos);
                if (actualWidth <= 0)
                {
                    break;
                }

                text = source.Substring(actualPos, actualWidth);
            }

            tokenPositions.Add((actualPos, text, token.width, token.kind.value));
            srcPos = actualPos + token.width;
        }

        // 3. 构建 (Kind, Span) 列表供高亮器使用
        var tokenList = new List<(int Kind, TextSpan Span)>();
        foreach (var (pos, text, width, kind) in tokenPositions)
        {
            tokenList.Add((kind, new TextSpan(pos, width)));
        }

        // 4. 使用 SyntaxHighlighter 分类
        var highlights = _highlighter.HighlightTokens(tokenList);

        // 5. 构建 HighlightKind → Span 的映射
        var spanMap = new Dictionary<TextSpan, HighlightKind>();
        foreach (var hl in highlights)
        {
            spanMap[hl.span] = hl.kind;
        }

        // 6. 渲染 HTML：遍历 token，保留空白，处理 -> 连体
        var sb = new StringBuilder();
        srcPos = 0;
        for (var i = 0; i < tokenPositions.Count; i++)
        {
            var (pos, text, width, _) = tokenPositions[i];

            // 输出 token 之前的空白字符（保留原始格式）
            if (pos > srcPos)
            {
                sb.Append(escape_html(source.Substring(srcPos, pos - srcPos)));
            }

            var span = new TextSpan(pos, width);

            // 处理 arrow token（->），渲染为连体箭头 →
            if (tokenPositions[i].Kind == (int)ValkyrieTokenKind.arrow)
            {
                sb.Append("<span class=\"hl-operator\">→</span>");
                srcPos = pos + width;
                continue;
            }

            // 处理字符串 token：渲染时包含引号
            if (tokenPositions[i].Kind == (int)ValkyrieTokenKind.@string)
            {
                sb.Append("<span class=\"hl-string\">\"");
                sb.Append(escape_html(text));
                sb.Append("\"</span>");
                srcPos = pos + width;
                continue;
            }

            // 检查 -> 连体：如果当前 token 是 - 且下一个 token 是 >
            var isArrow = false;
            if (text == "-" && i + 1 < tokenPositions.Count)
            {
                var (nextPos, nextText, nextWidth, _) = tokenPositions[i + 1];

                // 检查两个 token 之间是否只有空白
                var gapEnd = nextPos;
                var betweenText = nextPos > pos + width
                    ? source.Substring(pos + width, nextPos - pos - width)
                    : string.Empty;

                if (nextText == ">" && string.IsNullOrWhiteSpace(betweenText))
                {
                    sb.Append("<span class=\"hl-operator\">→</span>");
                    i++;
                    srcPos = nextPos + nextWidth;
                    isArrow = true;
                }
            }

            if (isArrow)
            {
                continue;
            }

            if (spanMap.TryGetValue(span, out var kind) && kind != HighlightKind.none)
            {
                sb.Append(render_token(text, kind, currentModule));
            }
            else
            {
                sb.Append(escape_html(text));
            }

            srcPos = pos + width;
        }

        // 输出源代码末尾的剩余文本（如尾部空白）
        if (srcPos < source.Length)
        {
            sb.Append(escape_html(source.Substring(srcPos)));
        }

        // 将 HTML 转义后的 -> 替换为连体箭头 →
        var result = sb.ToString();
        result = result.Replace("-&gt;", "→");
        return result;
    }

    /// <summary>
    ///     对类型字符串进行 Token 级着色
    /// </summary>
    /// <param name="typeString">类型字符串</param>
    /// <param name="currentModule">当前模块命名空间（用于类型链接解析）</param>
    /// <returns>带着色 HTML 标签的片段</returns>
    public string highlight_type(string typeString, string currentModule)
    {
        if (string.IsNullOrEmpty(typeString))
        {
            return string.Empty;
        }

        return highlight_source(typeString, currentModule);
    }

    #endregion

    #region 高亮器创建

    /// <summary>
    ///     创建预配置的语法高亮器，映射 ValkyrieTokenKind 到 HighlightKind
    /// </summary>
    private static SyntaxHighlighter create_highlighter()
    {
        HighlightKind classifier(int tokenType)
        {
            // 先做精确匹配，确保 true/false/null 映射为 keyword
            var vKind = (ValkyrieTokenKind)tokenType;
            switch (vKind)
            {
                case ValkyrieTokenKind.identifier:
                    return HighlightKind.identifier;
                case ValkyrieTokenKind.number:
                    return HighlightKind.number;
                case ValkyrieTokenKind.@string:
                    return HighlightKind.@string;
                case ValkyrieTokenKind.@true:
                case ValkyrieTokenKind.@false:
                case ValkyrieTokenKind.@null:
                    return HighlightKind.keyword;
                case ValkyrieTokenKind.comment_start:
                case ValkyrieTokenKind.comment_content:
                    return HighlightKind.comment;
                case ValkyrieTokenKind.less:
                case ValkyrieTokenKind.greater:
                    return HighlightKind.punctuation;
                default:
                    break;
            }

            // 标点和分隔符范围
            if (vKind >= ValkyrieTokenKind.colon && vKind <= ValkyrieTokenKind.semicolon)
            {
                return HighlightKind.punctuation;
            }

            if (vKind >= ValkyrieTokenKind.brace_l && vKind <= ValkyrieTokenKind.bracket_r)
            {
                return HighlightKind.punctuation;
            }

            // 使用扩展方法进行批量分类
            var kind = new NodeKind(tokenType);
            if (kind.is_keyword())
            {
                return HighlightKind.keyword;
            }

            if (kind.is_operator())
            {
                return HighlightKind.@operator;
            }

            return HighlightKind.none;
        }

        return new SyntaxHighlighter(new TokenKindClassifier(classifier), default!);
    }

    #endregion

    #region 短名称索引

    /// <summary>
    ///     构建短名称索引，用于跨模块类型链接解析
    /// </summary>
    private void build_short_name_index()
    {
        if (_typeIndex is null)
        {
            return;
        }

        foreach (var (fqn, ref_) in _typeIndex)
        {
            var shortName = extract_short_name(fqn);
            if (string.IsNullOrEmpty(shortName))
            {
                continue;
            }

            if (!_shortNameIndex.TryGetValue(shortName, out var list))
            {
                list = [];
                _shortNameIndex[shortName] = list;
            }

            list.Add(ref_);
        }
    }

    /// <summary>
    ///     从全限定名中提取类型短名称
    /// </summary>
    /// <param name="fqn">全限定名，如 "std.collections.HashMap"</param>
    /// <returns>短名称，如 "HashMap"</returns>
    private static string extract_short_name(string fqn)
    {
        var lastDot = fqn.LastIndexOf('.');
        return lastDot >= 0 ? fqn[(lastDot + 1)..] : fqn;
    }

    #endregion

    #region Token 渲染

    /// <summary>
    ///     将单个 Token 渲染为 HTML span 元素
    /// </summary>
    /// <param name="text">Token 文本</param>
    /// <param name="kind">高亮种类</param>
    /// <param name="currentModule">当前模块命名空间</param>
    /// <returns>HTML 片段</returns>
    private string render_token(string text, HighlightKind kind, string currentModule)
    {
        var cssClass = kind switch
        {
            HighlightKind.keyword => "hl-keyword",
            HighlightKind.control_keyword => "hl-keyword",
            HighlightKind.type_identifier => "hl-type",
            HighlightKind.function_identifier => "hl-function",
            HighlightKind.parameter => "hl-parameter",
            HighlightKind.@string => "hl-string",
            HighlightKind.number => "hl-number",
            HighlightKind.comment => "hl-comment",
            HighlightKind.@operator => "hl-operator",
            HighlightKind.punctuation => "hl-punctuation",
            HighlightKind.identifier => "hl-identifier",
            _ => "hl-identifier"
        };

        var escaped = escape_html(text);

        // 类型标识符和普通标识符特殊处理：链接化
        if (kind == HighlightKind.type_identifier || kind == HighlightKind.identifier)
        {
            return render_identifier_token(text, escaped, currentModule);
        }

        return $"<span class=\"{cssClass}\">{escaped}</span>";
    }

    /// <summary>
    ///     对标识符/类型标识符进行链接化处理
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="escaped">HTML 转义后的文本</param>
    /// <param name="currentModule">当前模块命名空间</param>
    /// <returns>HTML 片段</returns>
    private string render_identifier_token(string text, string escaped, string currentModule)
    {
        // 基础类型
        if (_primitives.Contains(text))
        {
            return $"<span class=\"hl-type hl-primitive\">{escaped}</span>";
        }

        // 查询类型索引
        if (_typeIndex is not null)
        {
            // 先尝试当前模块下的短名称匹配
            if (!text.Contains('.'))
            {
                var fqn = string.IsNullOrEmpty(currentModule) ? text : $"{currentModule}.{text}";
                if (_typeIndex.TryGetValue(fqn, out var ref_))
                {
                    return render_type_link(escaped, ref_, currentModule);
                }
            }

            // 再尝试全限定名匹配
            if (_typeIndex.TryGetValue(text, out var ref2))
            {
                return render_type_link(escaped, ref2, currentModule);
            }

            // 跨模块短名称匹配
            if (_shortNameIndex.TryGetValue(text, out var candidates) && candidates.Count > 0)
            {
                if (candidates.Count == 1)
                {
                    return render_type_link(escaped, candidates[0], currentModule);
                }

                // 优先匹配当前模块
                foreach (var candidate in candidates)
                {
                    if (candidate.module == currentModule)
                    {
                        return render_type_link(escaped, candidate, currentModule);
                    }
                }

                // 多个匹配，使用第一个
                return render_type_link(escaped, candidates[0], currentModule);
            }
        }

        // 看起来像用户自定义类型（大写开头且长度 > 1，排除单字符泛型参数）
        if (text.Length > 1 && char.IsUpper(text[0]) && !text.Contains('.'))
        {
            return $"<span class=\"hl-type hl-external\" title=\"外部类型（依赖中定义）\">{escaped}</span>";
        }

        // 普通标识符
        return $"<span class=\"hl-identifier\">{escaped}</span>";
    }

    /// <summary>
    ///     将已知类型渲染为可点击的链接
    /// </summary>
    /// <param name="escaped">HTML 转义后的类型名</param>
    /// <param name="ref_">解析到的类型引用</param>
    /// <param name="currentModule">当前模块命名空间（用于计算相对路径）</param>
    /// <returns>HTML 链接片段</returns>
    private string render_type_link(string escaped, ResolvedTypeRef ref_, string currentModule)
    {
        string href;
        if (_getTypeHref is not null)
        {
            // 使用文件夹层级格式，计算从当前模块到目标类型的相对路径
            var targetHref = _getTypeHref(ref_.module, ref_.typeName);
            href = make_relative_path(currentModule, targetHref);
        }
        else
        {
            // 旧的扁平文件名格式
            var fqn = string.IsNullOrEmpty(ref_.module)
                ? ref_.typeName
                : $"{ref_.module}_{ref_.typeName}";

            var fileName = sanitize_filename(fqn);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = sanitize_filename(ref_.typeName);
            }

            if (_currentProject == string.Empty || ref_.project == _currentProject)
            {
                href = $"{fileName}.html";
            }
            else
            {
                href = $"../{sanitize_filename(ref_.project)}/{fileName}.html";
            }
        }

        return $"<a class=\"hl-type\" href=\"{href}\">{escaped}</a>";
    }

    /// <summary>
    ///     计算从当前模块到目标 href 的相对路径
    ///     如 currentModule="std.collections", targetHref="std/collections/HashMap.html" → "HashMap.html"
    ///     如 currentModule="std.io", targetHref="std/collections/HashMap.html" → "../collections/HashMap.html"
    /// </summary>
    /// <param name="currentModule">当前模块命名空间</param>
    /// <param name="targetHref">目标 href（相对于根目录）</param>
    /// <returns>相对路径</returns>
    private static string make_relative_path(string currentModule, string targetHref)
    {
        if (string.IsNullOrEmpty(currentModule))
        {
            return targetHref;
        }

        var currentParts = currentModule.Split('.');
        var currentDepth = currentParts.Length;

        // 计算当前模块目录路径
        var currentDir = string.Join("/", currentParts);

        // 如果目标 href 在当前模块目录下，只需返回文件名
        if (targetHref.StartsWith(currentDir + "/", StringComparison.Ordinal))
        {
            return targetHref[(currentDir.Length + 1)..];
        }

        // 否则，需要回退到根目录
        var upLevels = string.Concat(Enumerable.Repeat("../", currentDepth));
        return upLevels + targetHref;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     HTML 转义
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转义后的文本</returns>
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
    ///     将名称转换为安全的文件名
    /// </summary>
    /// <param name="name">原始名称</param>
    /// <returns>安全文件名</returns>
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

    #endregion
}
