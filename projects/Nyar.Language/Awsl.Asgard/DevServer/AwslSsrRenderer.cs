using System.Globalization;
using System.Text;
using Nyar.Language.Awsl.Asgard.Compiler;
using Std.Data.Text.Awsl;
using Nyar.Language.Css;

namespace Valkyrie.Asgard.DevServer;

/// <summary>
///     AWSL 服务端渲染器，将组件渲染为静态 HTML 字符串。
///     用于 SSR 模式：服务端输出初始 HTML，客户端 hydration 后变为可交互。
///     与 AwslRenderer（innerHTML 模式）不同，SSR 渲染器：
///     - 输出纯静态 HTML（无 JS 渲染逻辑）
///     - 为每个组件添加 data-voa-ssr 属性用于 hydration 定位
///     - 内联初始状态为 JSON（data-voa-state）供客户端 hydration 读取
///     - 不生成事件处理器（hydration 阶段由客户端绑定）
/// </summary>
public sealed class AwslSsrRenderer
{
    private readonly AwslParser _parser = new();

    private Dictionary<string, object>? _ssr_data_context;

    /// <summary>
    ///     请求级 SSR 渲染：注入 get_data() 返回值作为组件的 data prop
    /// </summary>
    public AwslSsrRenderResult render_ssr(string source, string filePath,
        Dictionary<string, object>? requestData = null)
    {
        if (requestData != null && requestData.Count > 0)
        {
            _ssr_data_context = requestData;
        }

        return render_ssr_internal(source, filePath);
    }

    /// <summary>
    ///     SSR 渲染内部实现：解析 AWSL → 调用已存在的 RenderParsedSsr
    /// </summary>
    private AwslSsrRenderResult render_ssr_internal(string source, string filePath)
    {
        AwslParseResult parseResult = _parser.parse(source, filePath);
        return render_parsed_ssr(parseResult);
    }

    /// <summary>
    ///     将已解析的 AWsl 组件渲染为 SSR HTML
    /// </summary>
    public AwslSsrRenderResult render_parsed_ssr(AwslParseResult parseResult)
    {
        var componentName = parseResult.name;
        var scope = $"voa-ssr-{to_kebab_case(componentName)}";

        var htmlBuilder = new StringBuilder();
        var cssBuilder = new StringBuilder();

        var componentStyles = render_styles(parseResult.styles, componentName);
        if (!string.IsNullOrEmpty(componentStyles))
        {
            cssBuilder.AppendLine(componentStyles);
        }

        var initialState = build_initial_state_json(parseResult.properties);

        htmlBuilder.AppendLine(
            $"<div class=\"{scope}\" data-voa-ssr=\"{componentName}\" data-voa-state=\"{escape_attr(initialState)}\">");

        htmlBuilder.AppendLine("</div>");

        var headTags = new List<string>();
        var scriptTags = new List<string>();
        var isSuspense = false;
        var fallbackHtml = (string?)null;

        foreach (var node in parseResult.template_nodes)
        {
            var (nodeHtml, nodeHeadTags, nodeScriptTags, nodeSuspense, nodeFallback) =
                render_node_to_ssr_html_with_meta(node, parseResult.properties);
            htmlBuilder.Append(nodeHtml);
            headTags.AddRange(nodeHeadTags);
            scriptTags.AddRange(nodeScriptTags);
            if (nodeSuspense)
            {
                isSuspense = true;
                fallbackHtml = nodeFallback;
            }
        }

        return new AwslSsrRenderResult
        {
            html = htmlBuilder.ToString(),
            css = cssBuilder.ToString(),
            component_name = componentName,
            initial_state_json = initialState,
            scope = scope,
            head_tags = headTags,
            script_tags = scriptTags,
            is_suspense = isSuspense,
            fallback_html = fallbackHtml
        };
    }

    /// <summary>
    ///     生成客户端 hydration 脚本
    /// </summary>
    public static string
        generate_hydration_script(AwslSsrRenderResult[] results, string moduleName, string? wasmFileName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("(function() {");
        sb.AppendLine("  'use strict';");
        sb.AppendLine();
        sb.AppendLine("  var Voa = window.Voa || {};");
        sb.AppendLine("  var hydrated = new Set();");
        sb.AppendLine();
        sb.AppendLine("  function toPascal(name) {");
        sb.AppendLine("    return name.replace(/[-_]\\w/g, function(m) {");
        sb.AppendLine("      return m.charAt(1).toUpperCase();");
        sb.AppendLine("    }).replace(/^./, function(m) {");
        sb.AppendLine("      return m.toUpperCase();");
        sb.AppendLine("    });");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function hydrateComponent(el) {");
        sb.AppendLine("    var name = el.getAttribute('data-voa-ssr');");
        sb.AppendLine("    var stateJson = el.getAttribute('data-voa-state');");
        sb.AppendLine("    if (!name || hydrated.has(name)) return;");
        sb.AppendLine("    hydrated.add(name);");
        sb.AppendLine();
        sb.AppendLine("    try {");
        sb.AppendLine("      var pascalName = toPascal(name);");
        sb.AppendLine("      var state = stateJson ? JSON.parse(stateJson) : {};");
        sb.AppendLine("      console.log('[VOA SSR] hydrating: ' + name, state);");
        sb.AppendLine();
        sb.AppendLine("      var componentFn = window[pascalName];");
        sb.AppendLine("      if (!componentFn && Voa['_' + name]) {");
        sb.AppendLine("        componentFn = Voa['_' + name];");
        sb.AppendLine("      }");
        sb.AppendLine();
        sb.AppendLine("      if (typeof componentFn === 'function') {");
        sb.AppendLine("        var newEl = componentFn(state);");
        sb.AppendLine("        if (newEl) {");
        sb.AppendLine("          el.innerHTML = '';");
        sb.AppendLine("          while (newEl.firstChild) {");
        sb.AppendLine("            el.appendChild(newEl.firstChild);");
        sb.AppendLine("          }");
        sb.AppendLine("          el.removeAttribute('data-voa-state');");
        sb.AppendLine("          el.setAttribute('data-voa-hydrated', 'true');");
        sb.AppendLine("        }");
        sb.AppendLine("      } else {");
        sb.AppendLine("        console.warn('[VOA SSR] component not found: ' + name + ' (' + pascalName + ')');");
        sb.AppendLine("        el.setAttribute('data-voa-hydrated', 'fallback');");
        sb.AppendLine("      }");
        sb.AppendLine("    } catch(e) {");
        sb.AppendLine("      console.error('[VOA SSR] hydration failed: ' + name, e);");
        sb.AppendLine("      el.setAttribute('data-voa-hydrated', 'error');");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  function hydrateAll() {");
        sb.AppendLine("    var elements = document.querySelectorAll('[data-voa-ssr]');");
        sb.AppendLine("    for (var i = 0; i < elements.length; i++) {");
        sb.AppendLine("      hydrateComponent(elements[i]);");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  if (document.readyState === 'loading') {");
        sb.AppendLine("    document.addEventListener('DOMContentLoaded', hydrateAll);");
        sb.AppendLine("  } else {");
        sb.AppendLine("    hydrateAll();");
        sb.AppendLine("  }");
        sb.AppendLine("})();");

        return sb.ToString();
    }

    /// <summary>
    ///     生成完整的 SSR HTML 页面
    /// </summary>
    public static string generate_ssr_page(string moduleName, AwslSsrRenderResult[] results, string? wasmFileName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"  <title>{moduleName} - VOA</title>");

        var allCss = new StringBuilder();
        var allHeadTags = new List<string>();
        var allScriptTags = new List<string>();
        var hasSuspense = false;

        foreach (var result in results)
        {
            if (!string.IsNullOrEmpty(result.css))
            {
                allCss.AppendLine(result.css.TrimEnd());
            }

            allHeadTags.AddRange(result.head_tags);
            allScriptTags.AddRange(result.script_tags);

            if (result.is_suspense)
            {
                hasSuspense = true;
            }
        }

        foreach (var headTag in allHeadTags)
        {
            sb.AppendLine($"  {headTag}");
        }

        if (allCss.Length > 0)
        {
            sb.AppendLine("  <style>");
            sb.AppendLine(allCss.ToString().TrimEnd());
            sb.AppendLine("  </style>");
        }

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div id=\"app\">");

        foreach (var result in results)
        {
            if (result.is_suspense && !string.IsNullOrEmpty(result.fallback_html))
            {
                sb.AppendLine("  <div class=\"voa-suspense-wrapper\" data-suspense=\"loading\">");
                sb.AppendLine($"    {result.fallback_html}");
                sb.AppendLine("  </div>");
            }
            else
            {
                sb.AppendLine(result.html);
            }
        }

        sb.AppendLine("  </div>");

        if (hasSuspense)
        {
            sb.AppendLine("  <script>");
            sb.AppendLine("    (function() {");
            sb.AppendLine("      var suspenseObserver = new MutationObserver(function(mutations) {");
            sb.AppendLine("        mutations.forEach(function(m) {");
            sb.AppendLine("          if (m.type === 'attributes' && m.attributeName === 'data-suspense') {");
            sb.AppendLine("            var el = m.target;");
            sb.AppendLine("            if (el.dataset.suspense === 'resolved') {");
            sb.AppendLine("              el.classList.add('voa-suspense-resolved');");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            sb.AppendLine("        });");
            sb.AppendLine("      });");
            sb.AppendLine("      document.querySelectorAll('.voa-suspense-wrapper').forEach(function(el) {");
            sb.AppendLine("        suspenseObserver.observe(el, { attributes: true });");
            sb.AppendLine("      });");
            sb.AppendLine("    })();");
            sb.AppendLine("  </script>");
        }

        foreach (var scriptTag in allScriptTags)
        {
            sb.AppendLine($"  {scriptTag}");
        }

        sb.AppendLine("  <script src=\"voa-runtime.js\"></script>");

        if (wasmFileName is not null)
        {
            sb.AppendLine($"  <script src=\"{wasmFileName.Replace(".wasm", ".js")}\"></script>");
        }

        sb.AppendLine($"  <script src=\"{moduleName}.js\"></script>");

        sb.AppendLine("  <script>");
        sb.AppendLine(generate_hydration_script(results, moduleName, wasmFileName));
        sb.AppendLine("  </script>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    #region Styles

    private static string render_styles(CssStylesheet styles, string componentName)
    {
        return CssScoper.scope_css_stylesheet(styles, componentName, "voa-ssr-");
    }

    #endregion

    #region Template → SSR HTML

    private (string html, List<string> headTags, List<string> scriptTags, bool isSuspense, string? fallback)
        render_node_to_ssr_html_with_meta(
            AwslTemplateNode node, IReadOnlyList<AwslProperty> properties)
    {
        var headTags = new List<string>();
        var scriptTags = new List<string>();
        var isSuspense = false;
        var fallback = (string?)null;

        var html = node switch
        {
            AwslTextNode textNode => textNode.text,
            AwslInterpolationNode interpNode => render_interpolation_ssr(interpNode, properties),
            AwslElementNode elementNode => render_element_ssr_with_meta(elementNode, properties, headTags, scriptTags,
                ref isSuspense, ref fallback),
            AwslIfNode ifNode => render_if_ssr(ifNode, properties),
            AwslForNode forNode => render_for_ssr(forNode, properties),
            _ => string.Empty
        };

        return (html, headTags, scriptTags, isSuspense, fallback);
    }

    private string render_node_to_ssr_html(AwslTemplateNode node, IReadOnlyList<AwslProperty> properties)
    {
        return node switch
        {
            AwslTextNode textNode => textNode.text,
            AwslInterpolationNode interpNode => render_interpolation_ssr(interpNode, properties),
            AwslElementNode elementNode => render_element_ssr(elementNode, properties),
            AwslIfNode ifNode => render_if_ssr(ifNode, properties),
            AwslForNode forNode => render_for_ssr(forNode, properties),
            _ => string.Empty
        };
    }

    private string render_element_ssr_with_meta(AwslElementNode node, IReadOnlyList<AwslProperty> properties,
        List<string> headTags, List<string> scriptTags, ref bool isSuspense, ref string? fallback)
    {
        if (node.tag_name == "Head")
        {
            var inner = render_children_ssr(node.children, properties);
            headTags.Add(inner);
            return string.Empty;
        }

        if (node.tag_name == "Script")
        {
            var src = node.attributes.GetValueOrDefault("src", "");
            var content = render_children_ssr(node.children, properties);
            if (!string.IsNullOrEmpty(src))
            {
                scriptTags.Add($"<script src=\"{src}\"></script>");
            }
            else if (!string.IsNullOrEmpty(content))
            {
                scriptTags.Add($"<script>{content}</script>");
            }

            return string.Empty;
        }

        if (node.tag_name == "Suspense")
        {
            return render_suspense_ssr(node, properties, ref isSuspense, ref fallback);
        }

        return render_element_ssr(node, properties);
    }

    private string render_suspense_ssr(AwslElementNode node, IReadOnlyList<AwslProperty> properties,
        ref bool isSuspense, ref string? fallback)
    {
        var fallbackNode = node.children.FirstOrDefault(c => c is AwslElementNode e && e.tag_name == "Fallback");
        var contentNode = node.children.FirstOrDefault(c => c is AwslElementNode e && e.tag_name != "Fallback");

        if (fallbackNode is AwslElementNode fn)
        {
            fallback = $"<div class=\"voa-suspense-fallback\">{render_children_ssr(fn.children, properties)}</div>";
        }
        else
        {
            fallback = "<div class=\"voa-suspense-fallback\">Loading...</div>";
        }

        if (contentNode is AwslElementNode cn)
        {
            return render_children_ssr(cn.children, properties);
        }

        return fallback;
    }

    private string render_children_ssr(IReadOnlyList<AwslTemplateNode> children,
        IReadOnlyList<AwslProperty> properties)
    {
        var sb = new StringBuilder();
        foreach (var child in children)
        {
            sb.Append(render_node_to_ssr_html(child, properties));
        }

        return sb.ToString();
    }

    private static string
        render_interpolation_ssr(AwslInterpolationNode node, IReadOnlyList<AwslProperty> properties)
    {
        var expr = node.expression.Trim();
        var value = resolve_ssr_expression(expr, properties);
        return escape_html(value);
    }

    private string render_element_ssr(AwslElementNode node, IReadOnlyList<AwslProperty> properties)
    {
        var sb = new StringBuilder();
        sb.Append($"<{node.tag_name}");

        foreach (var attr in node.attributes)
        {
            if (attr.Key.StartsWith("on"))
            {
                continue;
            }

            if (is_expression_binding(attr.Value))
            {
                var expr = extract_expression(attr.Value);
                var value = resolve_ssr_expression(expr, properties);
                sb.Append($" {attr.Key}=\"{escape_attr(value)}\"");
            }
            else
            {
                sb.Append($" {attr.Key}=\"{escape_attr(attr.Value)}\"");
            }
        }

        if (node.is_self_closing)
        {
            sb.Append(" />");
            return sb.ToString();
        }

        sb.Append(">");

        if (node.children.Count > 0)
        {
            foreach (var child in node.children)
            {
                sb.Append(render_node_to_ssr_html(child, properties));
            }
        }

        sb.Append($"</{node.tag_name}>");
        return sb.ToString();
    }

    private string render_if_ssr(AwslIfNode node, IReadOnlyList<AwslProperty> properties)
    {
        var condition = resolve_ssr_expression(node.condition, properties);

        if (is_truthy(condition))
        {
            var sb = new StringBuilder();
            foreach (var child in node.children)
            {
                sb.Append(render_node_to_ssr_html(child, properties));
            }

            return sb.ToString();
        }

        if (node.else_children.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var child in node.else_children)
            {
                sb.Append(render_node_to_ssr_html(child, properties));
            }

            return sb.ToString();
        }

        return string.Empty;
    }

    private string render_for_ssr(AwslForNode node, IReadOnlyList<AwslProperty> properties)
    {
        var iterableValue = resolve_ssr_expression(node.iterable, properties);
        var items = parse_array_value(iterableValue);

        if (items is null || items.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < items.Count; i++)
        {
            var itemProps = create_iteration_properties(properties, node.iterator, items[i], i);
            foreach (var child in node.children)
            {
                sb.Append(render_node_to_ssr_html(child, itemProps));
            }
        }

        return sb.ToString();
    }

    #endregion

    #region SSR 表达式求值

    private static string resolve_ssr_expression(string expr, IReadOnlyList<AwslProperty> properties)
    {
        var trimmed = expr.Trim();

        foreach (var prop in properties)
        {
            if (trimmed == prop.name || trimmed == $"state.{prop.name}")
            {
                return prop.default_value ?? "";
            }

            if (trimmed.StartsWith($"{prop.name}.") || trimmed.StartsWith($"state.{prop.name}."))
            {
                var accessor = trimmed.Contains("state.") ? trimmed["state.".Length..] : trimmed;
                return resolve_property_accessor(accessor, prop.default_value);
            }
        }

        if (trimmed.StartsWith("Math.") || trimmed.StartsWith("Date.") || trimmed.StartsWith("JSON."))
        {
            return "";
        }

        return eval_ssr_expression(trimmed, properties);
    }

    private static string eval_ssr_expression(string expr, IReadOnlyList<AwslProperty> properties)
    {
        var tokens = tokenize_expression(expr);
        if (tokens.Count == 0)
        {
            return "";
        }

        if (tokens.Count == 1)
        {
            return resolve_literal_or_prop(tokens[0].value, properties);
        }

        var ternaryResult = try_eval_ternary(tokens, properties);
        if (ternaryResult is not null)
        {
            return ternaryResult;
        }

        return eval_binary_expression(tokens, properties);
    }

    private static string resolve_literal_or_prop(string token, IReadOnlyList<AwslProperty> properties)
    {
        var trimmed = token.Trim();

        if (trimmed == "true")
        {
            return "true";
        }

        if (trimmed == "false")
        {
            return "false";
        }

        if (trimmed is "null" or "undefined")
        {
            return "";
        }

        if (double.TryParse(trimmed, NumberStyles.Float,
                CultureInfo.InvariantCulture, out _))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
        {
            return trimmed[1..^1];
        }

        if (trimmed.StartsWith("'") && trimmed.EndsWith("'"))
        {
            return trimmed[1..^1];
        }

        foreach (var prop in properties)
        {
            if (trimmed == prop.name || trimmed == $"state.{prop.name}")
            {
                return prop.default_value ?? "";
            }

            if (trimmed.StartsWith($"state.{prop.name}.") || trimmed.StartsWith($"{prop.name}."))
            {
                var accessor = trimmed.StartsWith("state.")
                    ? trimmed["state.".Length..]
                    : trimmed;
                return resolve_property_accessor(accessor, prop.default_value);
            }
        }

        return "";
    }

    private static string? try_eval_ternary(List<ExpressionToken> tokens, IReadOnlyList<AwslProperty> properties)
    {
        var questionIdx = find_top_level_operator(tokens, "?");
        if (questionIdx < 0)
        {
            return null;
        }

        var colonIdx = find_top_level_operator(tokens, ":", questionIdx + 1);
        if (colonIdx < 0)
        {
            return null;
        }

        var condition = eval_binary_expression(
            tokens.GetRange(0, questionIdx), properties);
        var isTruthy = is_truthy(condition);

        if (isTruthy)
        {
            return eval_binary_expression(
                tokens.GetRange(questionIdx + 1, colonIdx - questionIdx - 1), properties);
        }

        return eval_binary_expression(
            tokens.GetRange(colonIdx + 1, tokens.Count - colonIdx - 1), properties);
    }

    private static string eval_binary_expression(List<ExpressionToken> tokens, IReadOnlyList<AwslProperty> properties)
    {
        if (tokens.Count == 0)
        {
            return "";
        }

        if (tokens.Count == 1)
        {
            return resolve_literal_or_prop(tokens[0].value, properties);
        }

        for (var precedence = 0; precedence <= 8; precedence++)
        {
            var matchIdx = find_operator_with_precedence(tokens, precedence);
            if (matchIdx < 0)
            {
                continue;
            }

            var op = tokens[matchIdx].value;

            if (op == "!")
            {
                var operand = eval_binary_expression(
                    tokens.GetRange(matchIdx + 1, tokens.Count - matchIdx - 1), properties);
                return is_truthy(operand) ? "false" : "true";
            }

            var left = eval_binary_expression(
                tokens.GetRange(0, matchIdx), properties);
            var right = eval_binary_expression(
                tokens.GetRange(matchIdx + 1, tokens.Count - matchIdx - 1), properties);

            return eval_binary_op(left, op, right);
        }

        return "";
    }

    private static string eval_binary_op(string left, string op, string right)
    {
        var leftNum = parse_number(left);
        var rightNum = parse_number(right);

        switch (op)
        {
            case "+":
                if (leftNum is not null && rightNum is not null)
                {
                    return (leftNum.Value + rightNum.Value).ToString(
                        CultureInfo.InvariantCulture);
                }

                return left + right;

            case "-":
                if (leftNum is not null && rightNum is not null)
                {
                    return (leftNum.Value - rightNum.Value).ToString(
                        CultureInfo.InvariantCulture);
                }

                return left;

            case "*":
                if (leftNum is not null && rightNum is not null)
                {
                    return (leftNum.Value * rightNum.Value).ToString(
                        CultureInfo.InvariantCulture);
                }

                return left;

            case "/":
                if (leftNum is not null && rightNum is not null && rightNum.Value != 0)
                {
                    return (leftNum.Value / rightNum.Value).ToString(
                        CultureInfo.InvariantCulture);
                }

                return left;

            case "==":
                return left == right ? "true" : "false";

            case "!=":
                return left != right ? "true" : "false";

            case ">":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value > rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) > 0 ? "true" : "false";

            case "<":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value < rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) < 0 ? "true" : "false";

            case ">=":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value >= rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) >= 0 ? "true" : "false";

            case "<=":
                if (leftNum is not null && rightNum is not null)
                {
                    return leftNum.Value <= rightNum.Value ? "true" : "false";
                }

                return string.Compare(left, right, StringComparison.Ordinal) <= 0 ? "true" : "false";

            case "&&":
                return is_truthy(left) && is_truthy(right) ? "true" : "false";

            case "||":
                return is_truthy(left) || is_truthy(right) ? "true" : "false";

            default:
                return left;
        }
    }

    private static double? parse_number(string value)
    {
        if (double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var num))
        {
            return num;
        }

        return null;
    }

    private static int find_operator_with_precedence(List<ExpressionToken> tokens, int precedence)
    {
        for (var i = 0; i < tokens.Count; i++)
            if (tokens[i].is_operator && operator_precedence(tokens[i].value) == precedence
                                      && tokens[i].depth == tokens[^1].depth)
            {
                return i;
            }

        return -1;
    }

    private static int find_top_level_operator(List<ExpressionToken> tokens, string op, int startIdx = 0)
    {
        for (var i = startIdx; i < tokens.Count; i++)
            if (tokens[i].value == op && tokens[i].depth == tokens[0].depth
                                      && tokens[i].is_operator)
            {
                return i;
            }

        return -1;
    }

    private static List<ExpressionToken> tokenize_expression(string expr)
    {
        var tokens = new List<ExpressionToken>();
        var depth = 0;
        var i = 0;

        while (i < expr.Length)
        {
            var c = expr[i];

            if (c == '(')
            {
                depth++;
                var innerExpr = read_parenthesized(expr, ref i);
                innerExpr = innerExpr[1..^1];
                var innerTokens = tokenize_expression(innerExpr);
                tokens.Add(new ExpressionToken($"({innerExpr})", false, depth - 1));
                depth--;
            }
            else if (c is '"' or '\'')
            {
                var strVal = read_quoted_string(expr, ref i);
                tokens.Add(new ExpressionToken(strVal, false, depth));
            }
            else if (c is ' ' or '\t' or '\n' or '\r')
            {
                i++;
            }
            else if (c == '!' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken("!=", true, depth));
                i += 2;
            }
            else if (c == '=' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken("==", true, depth));
                i += 2;
            }
            else if (c == '>' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken(">=", true, depth));
                i += 2;
            }
            else if (c == '<' && i + 1 < expr.Length && expr[i + 1] == '=')
            {
                tokens.Add(new ExpressionToken("<=", true, depth));
                i += 2;
            }
            else if (c == '&' && i + 1 < expr.Length && expr[i + 1] == '&')
            {
                tokens.Add(new ExpressionToken("&&", true, depth));
                i += 2;
            }
            else if (c == '|' && i + 1 < expr.Length && expr[i + 1] == '|')
            {
                tokens.Add(new ExpressionToken("||", true, depth));
                i += 2;
            }
            else if (c is '+' or '-' or '*' or '/' or '>' or '<' or '!' or '?' or ':')
            {
                tokens.Add(new ExpressionToken(c.ToString(), true, depth));
                i++;
            }
            else
            {
                var ident = read_identifier(expr, ref i);
                tokens.Add(new ExpressionToken(ident, false, depth));
            }
        }

        return tokens;
    }

    private static string read_parenthesized(string expr, ref int i)
    {
        var start = i;
        var depth = 1;
        i++;

        while (i < expr.Length && depth > 0)
        {
            var c = expr[i];

            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
            }
            else if (c is '"' or '\'')
            {
                read_quoted_string(expr, ref i);
            }

            i++;
        }

        return expr[start..i];
    }

    private static string read_quoted_string(string expr, ref int i)
    {
        var quote = expr[i];
        var start = i;
        i++;

        while (i < expr.Length && expr[i] != quote)
        {
            if (expr[i] == '\\')
            {
                i++;
            }

            i++;
        }

        i++;
        return expr[start..i];
    }

    private static string read_identifier(string expr, ref int i)
    {
        var start = i;

        while (i < expr.Length && (char.IsLetterOrDigit(expr[i]) || expr[i] is '.' or '_' or '$')) i++;

        return expr[start..i];
    }

    private static int operator_precedence(string op)
    {
        return op switch
        {
            "!" => 8,
            "*" or "/" => 7,
            "+" or "-" => 6,
            ">" or "<" or ">=" or "<=" => 5,
            "==" or "!=" => 4,
            "&&" => 3,
            "||" => 2,
            "?" or ":" => 1,
            _ => 0
        };
    }

    private sealed class ExpressionToken
    {
        public ExpressionToken(string value, bool isOperator, int depth)
        {
            this.value = value;
            is_operator = isOperator;
            this.depth = depth;
        }

        public string value { get; }
        public bool is_operator { get; }
        public int depth { get; }
    }

    private static string resolve_property_accessor(string accessor, string? defaultValue)
    {
        if (string.IsNullOrEmpty(defaultValue))
        {
            return "";
        }

        var parts = accessor.Split('.');
        if (parts.Length <= 1)
        {
            return defaultValue;
        }

        return defaultValue;
    }

    private static bool is_truthy(string value)
    {
        return value is not ("false" or "0" or "" or "null" or "undefined" or "NaN");
    }

    private static List<string>? parse_array_value(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value.StartsWith("[") && value.EndsWith("]"))
        {
            var inner = value[1..^1].Trim();
            if (string.IsNullOrEmpty(inner))
            {
                return [];
            }

            return
            [
                .. inner.Split(',')
                    .Select(s => s.Trim().Trim('"', '\''))
                    .Where(s => !string.IsNullOrEmpty(s))
            ];
        }

        return null;
    }

    private static List<AwslProperty> create_iteration_properties(
        IReadOnlyList<AwslProperty> baseProps, string iteratorName, string itemValue, int index)
    {
        var props = baseProps.ToList();
        props.Add(new AwslProperty
        {
            name = iteratorName,
            default_value = itemValue,
            default_value_kind = AwslValueKind.@string
        });
        props.Add(new AwslProperty
        {
            name = "__index",
            default_value = index.ToString(),
            default_value_kind = AwslValueKind.number
        });
        return props;
    }

    #endregion

    #region 初始状态 JSON

    private static string build_initial_state_json(IReadOnlyList<AwslProperty> properties)
    {
        if (properties.Count == 0)
        {
            return "{}";
        }

        var sb = new StringBuilder();
        sb.Append('{');

        var first = true;
        foreach (var prop in properties)
        {
            if (!first)
            {
                sb.Append(',');
            }

            first = false;

            sb.Append($"\"{prop.name}\":");

            sb.Append(prop.default_value_kind switch
            {
                AwslValueKind.@string => $"\"{escape_json_string(prop.default_value ?? "")}\"",
                AwslValueKind.boolean => (prop.default_value ?? "false").ToLowerInvariant(),
                AwslValueKind.number => prop.default_value ?? "0",
                AwslValueKind.array => prop.default_value ?? "[]",
                AwslValueKind.@object => prop.default_value ?? "{}",
                AwslValueKind.expression => format_expression_default_value(prop.default_value, prop.type_name),
                AwslValueKind.none => format_none_default_value(prop.type_name),
                _ => $"\"{escape_json_string(prop.default_value ?? "")}\""
            });
        }

        sb.Append('}');
        return sb.ToString();
    }

    private static string format_expression_default_value(string? defaultValue, string typeName)
    {
        if (string.IsNullOrEmpty(defaultValue))
        {
            return "null";
        }

        var trimmed = defaultValue.Trim();

        if (trimmed == "true")
        {
            return "true";
        }

        if (trimmed == "false")
        {
            return "false";
        }

        if (trimmed is "null" or "undefined")
        {
            return "null";
        }

        if (double.TryParse(trimmed, NumberStyles.Float,
                CultureInfo.InvariantCulture, out _))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            return trimmed;
        }

        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
        {
            return trimmed;
        }

        return typeName switch
        {
            "bool" => trimmed == "true" ? "true" : "false",
            "i8" or "i16" or "i32" or "i64" or "u8" or "u16" or "u32" or "u64" or "f32" or "f64" => "0",
            _ => $"\"{escape_json_string(trimmed)}\""
        };
    }

    private static string format_none_default_value(string typeName)
    {
        return typeName switch
        {
            "bool" => "false",
            "i8" or "i16" or "i32" or "i64" or "u8" or "u16" or "u32" or "u64" or "f32" or "f64" => "0",
            "utf8" => "\"\"",
            "list" => "[]",
            "map" => "{}",
            _ => "null"
        };
    }

    #endregion

    #region Utility

    private static bool is_expression_binding(string value)
    {
        return value.StartsWith("{") && value.EndsWith("}");
    }

    private static string extract_expression(string binding)
    {
        return binding[1..^1].Trim();
    }

    private static string escape_html(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private static string escape_attr(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string escape_json_string(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string to_kebab_case(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    #endregion
}
