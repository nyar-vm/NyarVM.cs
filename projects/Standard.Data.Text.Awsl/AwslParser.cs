using System.Text;
using Nyar.Language.Css;
using Std.Data.Text.Awsl.Lexer;

namespace Std.Data.Text.Awsl;

/// <summary>
///     AWSL 语法解析器，基于 AwslLexer 的 Token 流进行递归下降解析。
///     支持组件标签（&lt;widget&gt;, &lt;template&gt;, &lt;script&gt;, &lt;style&gt;）、
///     响应式绑定（@bind, @click）、事件处理、组件声明、模板表达式、条件/循环指令等。
/// </summary>
public sealed class AwslParser
{
    private const int MaxIterations = 100000;

    private readonly DiagnosticSink _diagnostics;
    private int _position;
    private IReadOnlyList<GreenLeafNode> _tokens = [];

    /// <summary>
    ///     创建 AWSL 语法解析器
    /// </summary>
    /// <param name="diagnostics">诊断接收器。</param>
    public AwslParser(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    /// <summary>
    ///     当前 Token
    /// </summary>
    private GreenLeafNode _current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    /// <summary>
    ///     解析 AWSL 源码，返回组件解析结果
    /// </summary>
    /// <param name="source">AWSL 源码。</param>
    /// <param name="filePath">源文件路径（用于提取组件名）。</param>
    /// <returns>解析结果。</returns>
    public AwslParseResult parse(string source, string filePath = "")
    {
        var lexer = new AwslLexer(_diagnostics);
        _tokens = lexer.tokenize(source);
        _position = 0;

        System.Console.Error.WriteLine($"  [DEBUG] Token 数: {_tokens.Count}");

        var result = new AwslParseResult
        {
            name = extract_component_name(filePath),
            properties = new List<AwslProperty>(),
            methods = new List<AwslMethod>(),
            template_nodes = new List<AwslTemplateNode>(),
            styles = new CssStylesheet()
        };

        var properties = (List<AwslProperty>)result.properties;
        var methods = (List<AwslMethod>)result.methods;
        var templateNodes = (List<AwslTemplateNode>)result.template_nodes;
        var styles = result.styles;

        var loopCount = 0;
        while (!is_at_end())
        {
            if (loopCount++ > MaxIterations)
                throw new ParseLoopException("parse", _position, _current.text ?? "", _current.kind);

            if (is_block_start("script"))
                parse_script_block(properties, methods);
            else if (is_block_start("widget") || is_block_start("template"))
                parse_widget_block(templateNodes);
            else if (is_block_start("style"))
                parse_style_block(styles);
            else
                advance();
        }

        return result;
    }

    #region 辅助方法

    /// <summary>
    ///     从文件路径中提取组件名称
    /// </summary>
    private static string extract_component_name(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);

        return string.IsNullOrEmpty(name) ? "AnonymousWidget" : name;
    }

    #endregion

    /// <summary>
    ///     解析迭代上限异常，用于诊断死循环
    /// </summary>
    public sealed class ParseLoopException : Exception
    {
        internal ParseLoopException(string methodName, int position, string tokenText, NodeKind tokenKind)
            : base($"解析死循环: 方法={methodName}, pos={position}, token='{tokenText}'({tokenKind})")
        {
            MethodName = methodName;
            Position = position;
            TokenText = tokenText;
            TokenKind = tokenKind;
        }

        /// <summary>
        ///     死循环发生时的解析位置
        /// </summary>
        public int Position { get; }

        /// <summary>
        ///     当前 Token 文本
        /// </summary>
        public string TokenText { get; }

        /// <summary>
        ///     当前 Token 种类
        /// </summary>
        public NodeKind TokenKind { get; }

        /// <summary>
        ///     当前所在方法的名称
        /// </summary>
        public string MethodName { get; }
    }

    #region Token 流操作

    /// <summary>
    ///     是否到达 Token 流末尾
    /// </summary>
    private bool is_at_end()
    {
        return _current.kind == AwslNodeKind.eof;
    }

    /// <summary>
    ///     前进到下一个 Token 并返回当前 Token
    /// </summary>
    private GreenLeafNode advance()
    {
        if (!is_at_end()) _position++;

        return _tokens[_position - 1];
    }

    /// <summary>
    ///     查看前方第 offset 个 Token
    /// </summary>
    private GreenLeafNode peek(int offset = 0)
    {
        var index = _position + offset;
        return index < _tokens.Count ? _tokens[index] : _tokens[^1];
    }

    /// <summary>
    ///     检查当前 Token 的种类
    /// </summary>
    private bool is_kind(NodeKind kind)
    {
        return _current.kind == kind;
    }

    /// <summary>
    ///     检查当前 Token 的种类是否为给定值之一
    /// </summary>
    private bool is_kind(NodeKind a, NodeKind b)
    {
        return _current.kind == a || _current.kind == b;
    }

    /// <summary>
    ///     检查当前 Token 的文本
    /// </summary>
    private bool is_token(string text)
    {
        return _current.text == text;
    }

    /// <summary>
    ///     检查指定偏移处 Token 的种类是否为给定值之一
    /// </summary>
    private bool is_kind_at(int offset, NodeKind a, NodeKind b)
    {
        var kind = peek(offset).kind;
        return kind == a || kind == b;
    }

    /// <summary>
    ///     尝试匹配指定种类的 Token
    /// </summary>
    private bool match(NodeKind kind)
    {
        if (_current.kind == kind)
        {
            advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     尝试匹配指定文本的 Token
    /// </summary>
    private bool match(string text)
    {
        if (_current.text == text)
        {
            advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     尝试匹配指定种类和文本的 Token
    /// </summary>
    private bool match(NodeKind kind, string text)
    {
        if (_current.kind == kind && _current.text == text)
        {
            advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     期望匹配指定文本的 Token，否则报错
    /// </summary>
    private bool expect(string text)
    {
        if (_current.text == text)
        {
            advance();
            return true;
        }

        _diagnostics.report_error(
            default,
            $"期望 '{text}'，但遇到了 '{_current.text}'");

        return false;
    }

    /// <summary>
    ///     跳过直到遇到指定文本的 Token
    /// </summary>
    private void skip_until(string text)
    {
        while (!is_at_end() && _current.text != text) advance();
    }

    #endregion

    #region 块检测

    /// <summary>
    ///     检查当前位置是否是块起始标签（&lt;tagName&gt; 或 &lt;tagName ... &gt;）
    /// </summary>
    private bool is_block_start(string tagName)
    {
        if (_current.kind != AwslNodeKind.@operator || _current.text != "<") return false;

        return is_kind_at(1, AwslNodeKind.keyword, AwslNodeKind.identifier) && peek(1).text == tagName;
    }

    /// <summary>
    ///     检查是否是块结束标签（&lt;/tagName&gt;）
    /// </summary>
    private bool is_block_end(string tagName)
    {
        if (_current.kind != AwslNodeKind.@operator || _current.text != "</") return false;

        return is_kind_at(1, AwslNodeKind.keyword, AwslNodeKind.identifier) && peek(1).text == tagName;
    }

    /// <summary>
    ///     跳过块起始标签（&lt;tagName ... &gt;）
    /// </summary>
    private void skip_block_start(string tagName)
    {
        expect("<");
        expect(tagName);

        while (!is_at_end() && !(_current.kind == AwslNodeKind.@operator && _current.text == ">")) advance();

        if (_current.kind == AwslNodeKind.@operator && _current.text == ">") advance();
    }

    /// <summary>
    ///     跳过块结束标签（&lt;/tagName&gt;）
    /// </summary>
    private void skip_block_end(string tagName)
    {
        if (_current.kind == AwslNodeKind.@operator && _current.text == "</") advance();

        if (_current.text == tagName) advance();

        if (_current.kind == AwslNodeKind.@operator && _current.text == ">") advance();
    }

    #endregion

    #region Script 块解析

    /// <summary>
    ///     解析 &lt;script&gt; 块
    /// </summary>
    private void parse_script_block(List<AwslProperty> properties, List<AwslMethod> methods)
    {
        skip_block_start("script");

        while (!is_at_end() && !is_block_end("script"))
            if (_current.kind == AwslNodeKind.keyword)
                switch (_current.text)
                {
                    case "import":
                        parse_import_declaration();
                        break;
                    case "let":
                        parse_let_declaration(properties);
                        break;
                    case "const":
                        parse_const_declaration(properties);
                        break;
                    case "micro":
                        parse_micro_declaration(methods);
                        break;
                    default:
                        advance();
                        break;
                }
            else
                advance();

        skip_block_end("script");
    }

    /// <summary>
    ///     解析 import 语句：import { A, B } from "path"
    /// </summary>
    private void parse_import_declaration()
    {
        advance();
    }

    /// <summary>
    ///     解析 let 声明：let name: type = value
    /// </summary>
    private void parse_let_declaration(List<AwslProperty> properties)
    {
        advance();

        if (!is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            skip_until("\n");

            return;
        }

        var name = _current.text ?? string.Empty;
        advance();

        var typeName = "auto";
        if (_current.kind == AwslNodeKind.punctuation && _current.text == ":")
        {
            advance();

            if (is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
            {
                typeName = normalize_type_name(_current.text ?? "auto");
                advance();
            }
        }

        string? defaultValue = null;
        var valueKind = AwslValueKind.none;

        if (_current.kind == AwslNodeKind.@operator && _current.text == "=")
        {
            advance();
            (defaultValue, valueKind) = parse_default_value();
        }

        properties.Add(new AwslProperty
        {
            name = name,
            type_name = typeName,
            is_readonly = false,
            default_value = defaultValue,
            default_value_kind = valueKind
        });
    }

    /// <summary>
    ///     解析 const 声明：const name: type = value
    /// </summary>
    private void parse_const_declaration(List<AwslProperty> properties)
    {
        advance();

        if (!is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            skip_until("\n");

            return;
        }

        var name = _current.text ?? string.Empty;
        advance();

        var typeName = "auto";
        if (_current.kind == AwslNodeKind.punctuation && _current.text == ":")
        {
            advance();

            if (is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
            {
                typeName = normalize_type_name(_current.text ?? "auto");
                advance();
            }
        }

        string? defaultValue = null;
        var valueKind = AwslValueKind.none;

        if (_current.kind == AwslNodeKind.@operator && _current.text == "=")
        {
            advance();
            (defaultValue, valueKind) = parse_default_value();
        }

        properties.Add(new AwslProperty
        {
            name = name,
            type_name = typeName,
            is_readonly = true,
            default_value = defaultValue,
            default_value_kind = valueKind
        });
    }

    /// <summary>
    ///     解析 micro 函数声明：micro name(params): retType { body }
    /// </summary>
    private void parse_micro_declaration(List<AwslMethod> methods)
    {
        advance();

        if (!is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            skip_until("}");

            return;
        }

        var funcName = _current.text ?? string.Empty;
        advance();

        var parameters = parse_micro_parameters();
        var returnType = string.Empty;

        if (_current.kind == AwslNodeKind.punctuation && _current.text == ":")
        {
            advance();

            if (is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
            {
                returnType = normalize_type_name(_current.text ?? string.Empty);
                advance();
            }
        }

        var body = parse_braced_body();

        methods.Add(new AwslMethod
        {
            name = funcName,
            parameters = parameters,
            body = body,
            is_micro = true
        });
    }

    /// <summary>
    ///     解析 micro 函数的参数列表 (a: i32, b: string)
    /// </summary>
    private string parse_micro_parameters()
    {
        if (_current.kind != AwslNodeKind.delimiter || _current.text != "(") return string.Empty;

        var sb = new StringBuilder();
        var depth = 1;
        advance();
        sb.Append('(');

        while (!is_at_end() && depth > 0)
        {
            if (_current.kind == AwslNodeKind.delimiter && _current.text == "(")
            {
                depth++;
            }
            else if (_current.kind == AwslNodeKind.delimiter && _current.text == ")")
            {
                depth--;
                if (depth == 0)
                {
                    sb.Append(')');
                    advance();

                    break;
                }
            }

            if (depth == 1
                && (_current.kind == AwslNodeKind.identifier || _current.kind == AwslNodeKind.keyword)
                && looks_like_parameter_type_token(sb))
            {
                sb.Append(normalize_type_name(_current.text ?? string.Empty));
                advance();
                continue;
            }

            sb.Append(_current.text);
            advance();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     解析花括号包围的代码体 { ... }
    /// </summary>
    private string parse_braced_body()
    {
        if (_current.kind != AwslNodeKind.delimiter || _current.text != "{") return string.Empty;

        var sb = new StringBuilder();
        var depth = 1;
        advance();

        while (!is_at_end() && depth > 0)
        {
            if (_current.kind == AwslNodeKind.delimiter && _current.text == "{")
            {
                depth++;
            }
            else if (_current.kind == AwslNodeKind.delimiter && _current.text == "}")
            {
                depth--;
                if (depth == 0)
                {
                    advance();

                    break;
                }
            }

            sb.Append(_current.text);
            advance();
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    ///     解析默认值表达式
    /// </summary>
    private (string? Value, AwslValueKind Kind) parse_default_value()
    {
        if (is_kind(AwslNodeKind.@string))
        {
            var strVal = _current.text ?? string.Empty;
            advance();

            return (strVal, AwslValueKind.@string);
        }

        if (is_kind(AwslNodeKind.number))
        {
            var numVal = _current.text ?? "0";
            advance();

            return (numVal, AwslValueKind.number);
        }

        if (is_kind(AwslNodeKind.literal))
        {
            var litVal = _current.text ?? string.Empty;
            advance();

            return (litVal.ToLower(), AwslValueKind.boolean);
        }

        if (is_kind(AwslNodeKind.delimiter) && is_token("["))
        {
            var arrSb = new StringBuilder();
            var arrDepth = 1;
            arrSb.Append(advance().text);

            while (!is_at_end() && arrDepth > 0)
            {
                if (is_kind(AwslNodeKind.delimiter))
                {
                    if (is_token("["))
                        arrDepth++;
                    else if (is_token("]")) arrDepth--;
                }

                arrSb.Append(_current.text);
                advance();
            }

            return (arrSb.ToString(), AwslValueKind.array);
        }

        if (is_kind(AwslNodeKind.delimiter) && is_token("{"))
        {
            var exprSb = new StringBuilder();
            var exprDepth = 1;
            advance();

            while (!is_at_end() && exprDepth > 0)
            {
                if (is_kind(AwslNodeKind.delimiter))
                {
                    if (is_token("{"))
                    {
                        exprDepth++;
                    }
                    else if (is_token("}"))
                    {
                        exprDepth--;
                        if (exprDepth == 0)
                        {
                            advance();

                            break;
                        }
                    }
                }

                exprSb.Append(_current.text);
                advance();
            }

            return (exprSb.ToString().Trim(), AwslValueKind.expression);
        }

        if (is_kind(AwslNodeKind.identifier) || is_kind(AwslNodeKind.keyword))
        {
            var idVal = _current.text ?? string.Empty;
            advance();

            return (idVal, AwslValueKind.identifier);
        }

        return (null, AwslValueKind.none);
    }

    /// <summary>
    ///     `AWSL` 只接受 `V/Nyar` 规范类型名，禁止继续兼容 JS 风格别名。
    /// </summary>
    private static string normalize_type_name(string typeName)
    {
        var normalizedTypeName = typeName.Trim();
        return normalizedTypeName switch
        {
            "number" or "boolean" or "string" or "str" or "int" or "long" or "float" or "double"
                or "array" or "object"
                => throw new InvalidOperationException(
                    $"AWSL 禁止使用历史遗留类型别名 `{normalizedTypeName}`，请改用 `f64`、`bool`、`utf8`、`i32`、`i64`、`f32`、`list` 或 `map`。"),
            _ => normalizedTypeName
        };
    }

    /// <summary>
    ///     判断当前参数列表缓冲区是否刚好位于类型注解位置。
    /// </summary>
    private static bool looks_like_parameter_type_token(StringBuilder builder)
    {
        for (var i = builder.Length - 1; i >= 0; i--)
        {
            var ch = builder[i];
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            return ch == ':';
        }

        return false;
    }

    #endregion

    #region Widget/Template 块解析

    /// <summary>
    ///     解析 &lt;widget&gt; 或 &lt;template&gt; 块
    /// </summary>
    private void parse_widget_block(List<AwslTemplateNode> templateNodes)
    {
        var blockTag = peek(1).text ?? "widget";
        skip_block_start(blockTag);

        var guard = 0;
        while (!is_at_end() && !is_block_end(blockTag))
        {
            if (guard++ > MaxIterations)
                throw new ParseLoopException("parse_widget_block", _position, _current.text ?? "", _current.kind);

            parse_template_content(templateNodes);
        }

        skip_block_end(blockTag);
    }

    /// <summary>
    ///     解析模板内容（元素、文本、插值、控制流）
    /// </summary>
    private void parse_template_content(List<AwslTemplateNode> nodes)
    {
        if (_current.kind == AwslNodeKind.@operator && _current.text == "<")
        {
            var next = peek(1);
            if (next.kind == AwslNodeKind.@operator && next.text == "/") return;

            var nextText = next.text ?? string.Empty;
            if (next.kind == AwslNodeKind.keyword || next.kind == AwslNodeKind.identifier)
                switch (nextText)
                {
                    case "if":
                        parse_if_block(nodes);

                        return;
                    case "loop":
                    case "for":
                        parse_loop_block(nodes);

                        return;
                    default:
                        parse_element(nodes);

                        return;
                }

            advance();
        }

        if (_current.kind == AwslNodeKind.delimiter && _current.text == "{")
        {
            var interpNode = parse_interpolation();

            if (interpNode is not null) nodes.Add(interpNode);

            return;
        }

        parse_text_node(nodes);
    }

    /// <summary>
    ///     解析元素节点：&lt;TagName attrs...&gt; children &lt;/TagName&gt; 或 &lt;TagName attrs... /&gt;
    /// </summary>
    private void parse_element(List<AwslTemplateNode> nodes)
    {
        expect("<");

        var tagName = _current.text ?? string.Empty;
        advance();

        var attributes = parse_element_attributes();
        var isSelfClosing = false;

        if (_current.kind == AwslNodeKind.@operator && _current.text == "/>")
        {
            isSelfClosing = true;
            advance();
        }
        else if (_current.kind == AwslNodeKind.@operator && _current.text == ">")
        {
            advance();
        }

        if (isSelfClosing)
        {
            nodes.Add(new AwslElementNode
            {
                tag_name = tagName,
                attributes = attributes,
                is_self_closing = true
            });

            return;
        }

        var children = new List<AwslTemplateNode>();

        var childGuard = 0;
        while (!is_at_end() && !is_block_end(tagName))
        {
            if (childGuard++ > MaxIterations)
                throw new ParseLoopException($"parse_element/{tagName}", _position, _current.text ?? "", _current.kind);

            parse_template_content(children);
        }

        skip_block_end(tagName);

        nodes.Add(new AwslElementNode
        {
            tag_name = tagName,
            attributes = attributes,
            children = children,
            is_self_closing = false
        });
    }

    /// <summary>
    ///     解析元素属性列表
    /// </summary>
    private Dictionary<string, string> parse_element_attributes()
    {
        var attributes = new Dictionary<string, string>();

        while (!is_at_end())
        {
            if (_current.kind == AwslNodeKind.@operator && _current.text is ">" or "/>") break;

            if (_current.kind == AwslNodeKind.at_prefix)
            {
                var atName = "@" + (_current.text ?? string.Empty);
                advance();

                if (_current.kind == AwslNodeKind.@operator && _current.text == "=")
                {
                    advance();
                    var atValue = parse_attribute_value();
                    attributes[atName] = atValue;
                }
                else
                {
                    attributes[atName] = "true";
                }
            }
            else if (is_kind(AwslNodeKind.identifier) || is_kind(AwslNodeKind.keyword))
            {
                var attrName = _current.text ?? string.Empty;
                advance();

                // 处理冒号分隔的属性名，如 on:input、on:focus、on:blur
                if (_current.kind == AwslNodeKind.punctuation && _current.text == ":")
                {
                    advance();
                    if (is_kind(AwslNodeKind.identifier) || is_kind(AwslNodeKind.keyword))
                    {
                        attrName += ":" + _current.text;
                        advance();
                    }
                }

                if (_current.kind == AwslNodeKind.@operator && _current.text == "=")
                {
                    advance();
                    var attrValue = parse_attribute_value();
                    attributes[attrName] = attrValue;
                }
                else
                {
                    attributes[attrName] = "true";
                }
            }
            else
            {
                break;
            }
        }

        return attributes;
    }

    /// <summary>
    ///     解析属性值（字符串、表达式或标识符）
    /// </summary>
    private string parse_attribute_value()
    {
        if (is_kind(AwslNodeKind.@string))
        {
            var strVal = _current.text ?? string.Empty;
            advance();

            return strVal;
        }

        if (is_kind(AwslNodeKind.delimiter) && is_token("{")) return parse_braced_expression();

        if (is_kind(AwslNodeKind.number))
        {
            var num = _current.text ?? string.Empty;
            advance();

            return num;
        }

        if (is_kind(AwslNodeKind.literal))
        {
            var lit = _current.text ?? string.Empty;
            advance();

            return lit.ToLower();
        }

        if (is_kind(AwslNodeKind.identifier) || is_kind(AwslNodeKind.keyword))
        {
            var id = _current.text ?? string.Empty;
            advance();

            return id;
        }

        return string.Empty;
    }

    /// <summary>
    ///     解析花括号表达式 { expr }
    /// </summary>
    private string parse_braced_expression()
    {
        if (_current.kind != AwslNodeKind.delimiter || _current.text != "{") return string.Empty;

        var sb = new StringBuilder();
        var depth = 1;
        advance();

        while (!is_at_end() && depth > 0)
        {
            if (_current.kind == AwslNodeKind.delimiter)
            {
                if (_current.text == "{")
                {
                    depth++;
                }
                else if (_current.text == "}")
                {
                    depth--;
                    if (depth == 0)
                    {
                        advance();

                        break;
                    }
                }
            }

            sb.Append(_current.kind == AwslNodeKind.@string ? $"\"{_current.text}\"" : _current.text);
            advance();
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    ///     解析插值节点：{ expression }
    /// </summary>
    private AwslInterpolationNode? parse_interpolation()
    {
        if (_current.kind != AwslNodeKind.delimiter || _current.text != "{") return null;

        var expr = parse_braced_expression();
        return new AwslInterpolationNode { expression = expr };
    }

    /// <summary>
    ///     解析文本节点
    /// </summary>
    private void parse_text_node(List<AwslTemplateNode> nodes)
    {
        var sb = new StringBuilder();

        while (!is_at_end()
               && !(_current.kind == AwslNodeKind.@operator && _current.text == "<")
               && !(_current.kind == AwslNodeKind.@operator && _current.text == "</")
               && !(_current.kind == AwslNodeKind.@operator && _current.text == "/>")
               && !(_current.kind == AwslNodeKind.delimiter && _current.text == "{"))
        {
            sb.Append(_current.text);
            advance();
        }

        var text = sb.ToString().Trim();

        if (!string.IsNullOrEmpty(text)) nodes.Add(new AwslTextNode { text = text });
    }

    #endregion

    #region 控制流块解析

    /// <summary>
    ///     解析 if 块：&lt;if {condition}&gt; children [&lt;else/&gt; elseChildren] &lt;/if&gt;
    /// </summary>
    private void parse_if_block(List<AwslTemplateNode> nodes)
    {
        expect("<");
        expect("if");

        var condition = parse_if_condition();
        var ifChildren = new List<AwslTemplateNode>();
        var elseChildren = new List<AwslTemplateNode>();

        if (_current.kind == AwslNodeKind.@operator && _current.text == ">") advance();

        var inElse = false;

        while (!is_at_end() && !is_block_end("if"))
        {
            if (!inElse && _current.kind == AwslNodeKind.@operator && _current.text == "<"
                && peek(1).text == "else" && peek(2).kind == AwslNodeKind.@operator && peek(2).text == "/>")
            {
                advance();
                advance();
                advance();
                inElse = true;

                continue;
            }

            var target = inElse ? elseChildren : ifChildren;
            parse_template_content(target);
        }

        skip_block_end("if");

        nodes.Add(new AwslIfNode
        {
            condition = condition,
            children = ifChildren,
            else_children = elseChildren
        });
    }

    /// <summary>
    ///     解析 if 条件表达式。
    ///     支持三种语法：
    ///     &lt;if {condition}&gt; — 花括号表达式
    ///     &lt;if condition={expr}&gt; — 属性语法
    ///     &lt;if showMessage&gt; — 纯标识符
    /// </summary>
    private string parse_if_condition()
    {
        if (_current.kind == AwslNodeKind.delimiter && _current.text == "{") return parse_braced_expression();

        var raw = parse_raw_until(">");

        var eqIdx = raw.IndexOf('=');
        if (eqIdx >= 0)
        {
            var valuePart = raw[(eqIdx + 1)..].Trim();

            if (valuePart.StartsWith("{") && valuePart.EndsWith("}")) return valuePart[1..^1].Trim();

            if ((valuePart.StartsWith("\"") && valuePart.EndsWith("\""))
                || (valuePart.StartsWith("'") && valuePart.EndsWith("'")))
                return valuePart[1..^1];

            return valuePart;
        }

        return raw.Trim();
    }

    /// <summary>
    ///     读取原始 Token 文本直到遇到指定字符串
    /// </summary>
    private string parse_raw_until(string endText)
    {
        var sb = new StringBuilder();

        while (!is_at_end() && !(_current.kind == AwslNodeKind.@operator && _current.text == endText))
        {
            sb.Append(_current.text);
            advance();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     解析 loop/for 块。
    ///     支持多种语法：
    ///     &lt;loop item in {items}&gt; — 标准语法
    ///     &lt;for each={item} in={items}&gt; — 属性语法
    ///     &lt;for item in items&gt; — 简写
    /// </summary>
    private void parse_loop_block(List<AwslTemplateNode> nodes)
    {
        expect("<");
        var loopKeyword = _current.text ?? "loop";
        advance();

        var iterator = "item";
        var iterable = "items";

        if (is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            var first = _current.text ?? string.Empty;

            if (first == "each")
            {
                advance();
                if (_current.kind == AwslNodeKind.@operator && _current.text == "=")
                {
                    advance();
                    iterator = parse_loop_attribute_value();
                }
            }
            else if (first == "in")
            {
                advance();
                iterable = parse_loop_attribute_value();
            }
            else
            {
                iterator = first;
                advance();
            }
        }

        if (_current.kind == AwslNodeKind.keyword && _current.text == "in")
        {
            advance();

            if (_current.kind == AwslNodeKind.@operator && _current.text == "=") advance();

            iterable = parse_iterable_expression();
        }
        else if (is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            var next = _current.text ?? string.Empty;

            if (next is "in")
            {
                advance();
                iterable = parse_iterable_expression();
            }
            else
            {
                iterable = next;
                advance();
            }
        }

        if (_current.kind == AwslNodeKind.@operator && _current.text == ">") advance();

        var children = new List<AwslTemplateNode>();

        while (!is_at_end() && !is_block_end(loopKeyword)) parse_template_content(children);

        skip_block_end(loopKeyword);

        nodes.Add(new AwslForNode
        {
            iterator = iterator,
            iterable = iterable,
            children = children
        });
    }

    /// <summary>
    ///     解析 loop 的遍历表达式
    /// </summary>
    private string parse_iterable_expression()
    {
        if (_current.kind == AwslNodeKind.delimiter && _current.text == "{") return parse_braced_expression();

        if (is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            var id = _current.text ?? string.Empty;
            advance();

            return id;
        }

        return "items";
    }

    /// <summary>
    ///     解析 loop 属性值（{expr}、字符串或标识符）
    /// </summary>
    private string parse_loop_attribute_value()
    {
        if (_current.kind == AwslNodeKind.delimiter && _current.text == "{") return parse_braced_expression();

        if (_current.kind == AwslNodeKind.@string)
        {
            var strVal = _current.text ?? string.Empty;
            advance();

            return strVal;
        }

        if (is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            var id = _current.text ?? string.Empty;
            advance();

            return id;
        }

        return string.Empty;
    }

    #endregion

    #region Style 块解析

    /// <summary>
    ///     解析 &lt;style&gt; 块
    /// </summary>
    private void parse_style_block(CssStylesheet styles)
    {
        skip_block_start("style");

        var guard = 0;
        while (!is_at_end() && !is_block_end("style"))
        {
            if (guard++ > MaxIterations)
                throw new ParseLoopException("parse_style_block", _position, _current.text ?? "", _current.kind);

            parse_css_rule(styles);
        }

        skip_block_end("style");
    }

    /// <summary>
    ///     解析 CSS 规则：.className { properties }
    /// </summary>
    private void parse_css_rule(CssStylesheet styles)
    {
        if (_current.kind == AwslNodeKind.@operator && _current.text == ".") advance();

        if (!is_kind(AwslNodeKind.identifier, AwslNodeKind.keyword))
        {
            advance();

            return;
        }

        var className = _current.text ?? string.Empty;
        advance();

        if (_current.kind == AwslNodeKind.delimiter && _current.text == "{")
        {
            var body = parse_braced_body();

            if (!string.IsNullOrEmpty(body))
            {
                var declarations = new List<CssDeclaration>();
                var parts = body.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var colonIdx = part.IndexOf(':');
                    if (colonIdx > 0)
                    {
                        var prop = part[..colonIdx].Trim();
                        var val = part[(colonIdx + 1)..].Trim();
                        declarations.Add(new CssDeclaration { Property = prop, Value = val });
                    }
                }

                styles.Rules.Add(new CssRule
                {
                    Selector = $".{className}",
                    Declarations = declarations
                });
            }
        }
    }

    #endregion
}
