using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Nyar.Language.Awsl.Asgard.Compiler;
using Std.Data.Text.Awsl;
using Nyar.Language.Css;

namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     AWSL 组件渲染器，将 AwslParseResult AST 渲染为 HTML/CSS/JS
/// </summary>
public sealed class AwslRenderer
{
    private readonly AwslParser _parser = new();

    /// <summary>
    ///     渲染 AWSL 源码为完整的 HTML 页面
    /// </summary>
    public AwslRenderResult render(string source, string filePath = "")
    {
        AwslParseResult parseResult = _parser.parse(source, filePath);
        return render_parsed(parseResult);
    }

    /// <summary>
    ///     渲染已解析的 AWsl 组件为完整 HTML 页面
    /// </summary>
    public AwslRenderResult render_parsed(AwslParseResult parseResult)
    {
        var componentName = parseResult.name;
        var componentVar = to_camel_case(componentName);

        var htmlBuilder = new StringBuilder();
        var cssBuilder = new StringBuilder();
        var jsBuilder = new StringBuilder();

        var componentStyles = render_styles(parseResult.styles, componentName);
        if (!string.IsNullOrEmpty(componentStyles))
        {
            cssBuilder.AppendLine(componentStyles);
        }

        var templateJs = render_template_to_js(parseResult.template_nodes, componentVar);
        jsBuilder.AppendLine($"const {componentVar} = (() => {{");

        jsBuilder.AppendLine("  const state = {};");
        foreach (var prop in parseResult.properties)
        {
            var defaultValue = prop.default_value_kind switch
            {
                AwslValueKind.@string => $"\"{prop.default_value}\"",
                AwslValueKind.boolean => prop.default_value?.ToLower() ?? "false",
                AwslValueKind.number => prop.default_value ?? "0",
                AwslValueKind.array => prop.default_value ?? "[]",
                _ => prop.default_value ?? "null"
            };
            jsBuilder.AppendLine($"  state.{prop.name} = {defaultValue};");
        }

        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  function render() {");
        jsBuilder.AppendLine("    const root = document.getElementById('voa-app');");
        jsBuilder.AppendLine("    if (!root) return;");
        jsBuilder.AppendLine($"    root.innerHTML = {templateJs};");
        jsBuilder.AppendLine("  }");
        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  function setState(updates) {");
        jsBuilder.AppendLine("    Object.assign(state, updates);");
        jsBuilder.AppendLine("    render();");
        jsBuilder.AppendLine("  }");
        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  render();");
        jsBuilder.AppendLine();
        jsBuilder.AppendLine("  return { state, setState, render };");
        jsBuilder.AppendLine("})();");

        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html lang=\"zh-CN\">");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("  <meta charset=\"utf-8\">");
        htmlBuilder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        htmlBuilder.AppendLine($"  <title>{componentName}</title>");

        if (cssBuilder.Length > 0)
        {
            htmlBuilder.AppendLine("  <style>");
            htmlBuilder.AppendLine(cssBuilder.ToString().TrimEnd());
            htmlBuilder.AppendLine("  </style>");
        }

        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");
        htmlBuilder.AppendLine("  <div id=\"voa-app\"></div>");

        if (jsBuilder.Length > 0)
        {
            htmlBuilder.AppendLine("  <script>");
            htmlBuilder.AppendLine(jsBuilder.ToString().TrimEnd());
            htmlBuilder.AppendLine("  </script>");
        }

        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return new AwslRenderResult
        {
            html = htmlBuilder.ToString(),
            css = cssBuilder.ToString(),
            java_script = jsBuilder.ToString(),
            component_name = componentName
        };
    }

    #region Styles

    private static string render_styles(CssStylesheet styles, string componentName)
    {
        return CssScoper.scope_css_stylesheet(styles, componentName, "voa-");
    }

    #endregion

    #region Template → JS

    private string render_template_to_js(IReadOnlyList<AwslTemplateNode> nodes, string componentVar)
    {
        if (nodes.Count == 0)
        {
            return "''";
        }

        var parts = new List<string>();

        foreach (var node in nodes)
        {
            var js = render_node_to_js(node, componentVar);
            if (!string.IsNullOrEmpty(js))
            {
                parts.Add(js);
            }
        }

        if (parts.Count == 0)
        {
            return "''";
        }

        if (parts.Count == 1)
        {
            return parts[0];
        }

        var sb = new StringBuilder();
        sb.Append('[');
        sb.Append(string.Join(", ", parts));
        sb.Append("].join('')");
        return sb.ToString();
    }

    private string render_node_to_js(AwslTemplateNode node, string componentVar)
    {
        return node switch
        {
            AwslTextNode textNode => escape_js_string(textNode.text),
            AwslInterpolationNode interpNode => render_interpolation(interpNode, componentVar),
            AwslElementNode elementNode => render_element(elementNode, componentVar),
            AwslIfNode ifNode => render_if(ifNode, componentVar),
            AwslForNode forNode => render_for(forNode, componentVar),
            _ => "''"
        };
    }

    private static string render_interpolation(AwslInterpolationNode node, string componentVar)
    {
        var expr = node.expression.Trim();
        return $"String({resolve_expression(expr, componentVar)})";
    }

    private string render_element(AwslElementNode node, string componentVar)
    {
        var sb = new StringBuilder();
        sb.Append($"'<{node.tag_name}'");

        foreach (var attr in node.attributes)
        {
            if (attr.Key.StartsWith("on"))
            {
                var eventName = attr.Key[2..].ToLower();
                var handler = attr.Value;
                sb.Append($" + ' {attr.Key}=\"{escape_html_attr(handler)}\"'");
            }
            else if (is_expression_binding(attr.Value))
            {
                var expr = extract_expression(attr.Value);
                sb.Append($" + ' {attr.Key}=\"' + String({resolve_expression(expr, componentVar)}) + '\"'");
            }
            else
            {
                sb.Append($" + ' {attr.Key}=\"{escape_html_attr(attr.Value)}\"'");
            }
        }

        if (node.is_self_closing)
        {
            sb.Append(" + ' />'");
            return sb.ToString();
        }

        sb.Append(" + '>'");

        if (node.children.Count > 0)
        {
            sb.Append(" + ");
            sb.Append(render_template_to_js(node.children, componentVar));
        }

        sb.Append($" + '</{node.tag_name}>'");
        return sb.ToString();
    }

    private string render_if(AwslIfNode node, string componentVar)
    {
        var condition = resolve_expression(node.condition, componentVar);
        var thenJs = render_template_to_js(node.children, componentVar);
        return $"({condition} ? {thenJs} : '')";
    }

    private string render_for(AwslForNode node, string componentVar)
    {
        var iterator = node.iterator;
        var iterable = resolve_expression(node.iterable, componentVar);
        var bodyJs = render_template_to_js(node.children, componentVar);

        return $"({iterable}.map(({iterator}, __index) => {bodyJs}).join(''))";
    }

    private static string resolve_expression(string expr, string componentVar)
    {
        var trimmed = expr.Trim();

        if (trimmed.StartsWith("state.") || trimmed.StartsWith("Math.") || trimmed.StartsWith("Date.") ||
            trimmed.StartsWith("JSON.") || trimmed.StartsWith("console.") || trimmed.StartsWith("window.") ||
            trimmed.StartsWith("document."))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("!") || trimmed.StartsWith("(") || trimmed.StartsWith("[") ||
            trimmed.Contains("&&") || trimmed.Contains("||") || trimmed.Contains("==") ||
            trimmed.Contains("!=") || trimmed.Contains(">") || trimmed.Contains("<") ||
            trimmed.Contains("+") || trimmed.Contains("-") || trimmed.Contains("*") ||
            trimmed.Contains("/"))
        {
            return replace_state_references(trimmed, componentVar);
        }

        if (CssStringUtils.is_js_keyword(trimmed) || CssStringUtils.is_literal(trimmed))
        {
            return trimmed;
        }

        return $"{componentVar}.state.{trimmed}";
    }

    private static string replace_state_references(string expr, string componentVar)
    {
        var pattern = @"\b([a-zA-Z_]\w*)\b";
        return Regex.Replace(expr, pattern, match =>
        {
            var name = match.Groups[1].Value;
            if (CssStringUtils.is_js_keyword(name) || CssStringUtils.is_literal(name) || name.StartsWith("state.") ||
                name == "Math" || name == "Date" || name == "JSON" || name == "console" ||
                name == "window" || name == "document" || name == "true" || name == "false" ||
                name == "null" || name == "undefined" || name == "this")
            {
                return name;
            }

            return $"{componentVar}.state.{name}";
        });
    }

    private static bool is_expression_binding(string value)
    {
        return value.StartsWith("{") && value.EndsWith("}");
    }

    private static string extract_expression(string binding)
    {
        return binding[1..^1].Trim();
    }

    #endregion

    #region Utility

    private static string escape_js_string(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "''";
        }

        var escaped = text
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");

        return $"'{escaped}'";
    }

    private static string escape_html_attr(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string to_camel_case(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var parts = name.Split('-', '_', ' ');
        var sb = new StringBuilder();

        for (var i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
            {
                continue;
            }

            if (i == 0)
            {
                sb.Append(parts[i].ToLowerInvariant());
            }
            else
            {
                sb.Append(char.ToUpperInvariant(parts[i][0]));
                sb.Append(parts[i][1..].ToLowerInvariant());
            }
        }

        return sb.ToString();
    }

    #endregion
}
