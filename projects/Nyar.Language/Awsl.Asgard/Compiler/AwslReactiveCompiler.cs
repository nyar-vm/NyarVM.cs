using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Std.Data.Text.Awsl;
using Nyar.Language.Css;

namespace Nyar.Language.Awsl.Asgard.Compiler;

/// <summary>
///     AWSL 响应式编译器：将 AwslParseResult AST 编译为使用 voa-runtime.js 的细粒度响应式 JS 代码
///     编译策略：Signal 驱动状态 + Effect 驱动 DOM 更新 + 无 innerHTML
///     设计目标：SolidJS 风格细粒度响应式
/// </summary>
public sealed class AwslReactiveCompiler
{
    private readonly HashSet<string> _component_names = [];
    private HashSet<string> _event_handlers = [];
    private HashSet<string> _memo_names = [];
    private HashSet<string> _method_names = [];
    private HashSet<string> _prop_names = [];
    private HashSet<string> _signal_names = [];
    private int _var_counter;

    public void register_component_names(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            _component_names.Add(name);
            _component_names.Add(to_pascal_case(name));
        }
    }

    public AwslCompileResult compile(AwslParseResult parseResult, string wasmModuleName)
    {
        _var_counter = 0;
        _signal_names = [];
        _memo_names = [];
        _prop_names = [];
        _event_handlers = [];
        _method_names = [.. parseResult.methods.Select(m => m.name)];

        var (islandType, hydrateStrategy) = detect_island_type(parseResult);

        var name = parseResult.name;
        var js = new StringBuilder();
        var css = new StringBuilder();

        js.AppendLine($"// 组件：{name}");

        if (islandType is not null) js.AppendLine($"// Island 类型：{islandType}, 策略：{hydrateStrategy}");

        js.AppendLine($"function {to_pascal_case(name)}(props) {{");

        emit_voa_destructure(js);
        js.AppendLine();

        emit_props(js, parseResult.properties);
        js.AppendLine();

        emit_signals(js, parseResult.properties);
        js.AppendLine();

        emit_memos(js, parseResult.properties);
        js.AppendLine();

        var rootVar = emit_template(js, parseResult.template_nodes);
        js.AppendLine();

        emit_lifecycle_hooks(js, parseResult);
        js.AppendLine();

        emit_event_handlers(js, parseResult);
        js.AppendLine();

        js.AppendLine($"    return {rootVar};");
        js.AppendLine("}");

        if (islandType is not null)
        {
            js.AppendLine();
            js.AppendLine($"Voa.registerIsland('{name}', {{");
            js.AppendLine($"    factory: {to_pascal_case(name)},");
            js.AppendLine($"    strategy: Voa.IslandStrategy.{get_strategy_enum(hydrateStrategy!)},");
            js.AppendLine($"    islandType: '{islandType}'");
            js.AppendLine("});");
        }

        if (!parseResult.styles.IsEmpty)
        {
            css.Append(CssScoper.scope_css_stylesheet(parseResult.styles, name, "voa-"));
        }

        return new AwslCompileResult
        {
            component_name = name,
            java_script = js.ToString(),
            css = css.ToString(),
            island_type = islandType,
            hydrate_strategy = hydrateStrategy
        };
    }

    private static (string? IslandType, string? Strategy) detect_island_type(AwslParseResult parseResult)
    {
        var hasMutableProps = parseResult.properties.Any(p =>
            !p.is_readonly || p.default_value is not null);
        var hasMethods = parseResult.methods.Count > 0;
        var hasScript = hasMutableProps || hasMethods;

        if (!hasScript) return ("static", null);

        var hasInteractiveMethods = parseResult.methods.Any(m =>
            m.name.Contains("click", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("submit", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("toggle", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("select", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("hover", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("drag", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("scroll", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("input", StringComparison.OrdinalIgnoreCase) ||
            m.name.Contains("change", StringComparison.OrdinalIgnoreCase));

        if (hasInteractiveMethods) return ("hydrated", "interaction");

        if (hasMutableProps) return ("hydrated", "visible");

        return ("hydrated", "idle");
    }

    private static string get_strategy_enum(string strategy)
    {
        return strategy switch
        {
            "idle" => "Idle",
            "visible" => "Visible",
            "interaction" => "Interaction",
            "media" => "Media",
            _ => "Load"
        };
    }

    private static void emit_voa_destructure(StringBuilder js)
    {
        js.AppendLine("    const { createSignal, createEffect, createMemo, createElement,");
        js.AppendLine("            createTextNode, dynamicText, dynamicAttribute, conditional,");
        js.AppendLine("            listMap, insertNode, removeNode, setAttribute, setProperty,");
        js.AppendLine("            createComponent, onMount, onCleanup } = Voa;");
    }

    #region Props 生成

    private void emit_props(StringBuilder js, IReadOnlyList<AwslProperty> properties)
    {
        var propDecls = properties.Where(p => p.is_readonly && p.default_value == null).ToList();
        if (propDecls.Count == 0) return;

        js.AppendLine("    // Props");

        foreach (var prop in propDecls)
        {
            _prop_names.Add(prop.name);
            js.AppendLine(
                $"    const [get_{to_snake_case(prop.name)}, set_{to_snake_case(prop.name)}] = createSignal(props?.{to_snake_case(prop.name)});");
        }
    }

    #endregion

    #region Signal 生成

    private void emit_signals(StringBuilder js, IReadOnlyList<AwslProperty> properties)
    {
        var signals = properties
            .Where(p => !_prop_names.Contains(p.name))
            .Where(p => !is_computed_property(p))
            .ToList();

        if (signals.Count == 0) return;

        js.AppendLine("    // 响应式状态");

        foreach (var prop in signals)
        {
            _signal_names.Add(prop.name);
            var defaultValue = format_default_value(prop);
            js.AppendLine(
                $"    const [get_{to_snake_case(prop.name)}, set_{to_snake_case(prop.name)}] = createSignal({defaultValue});");
        }
    }

    private void emit_memos(StringBuilder js, IReadOnlyList<AwslProperty> properties)
    {
        var memos = properties
            .Where(p => !_prop_names.Contains(p.name))
            .Where(is_computed_property)
            .ToList();

        if (memos.Count == 0) return;

        js.AppendLine("    // 派生状态");

        foreach (var prop in memos)
        {
            _memo_names.Add(prop.name);
            var expr = resolve_value_expr(prop.default_value ?? "undefined");
            js.AppendLine($"    const get_{to_snake_case(prop.name)} = createMemo(() => {expr});");
        }
    }

    private static bool is_computed_property(AwslProperty prop)
    {
        if (prop.default_value_kind == AwslValueKind.expression && prop.default_value != null) return true;

        if (prop.is_readonly && prop.default_value != null && prop.default_value_kind != AwslValueKind.none)
        {
            var val = prop.default_value.Trim();
            if (val.Contains('+') || val.Contains('-') || val.Contains('*') ||
                val.Contains('/') || val.Contains('?') || val.Contains(':') ||
                val.Contains("&&") || val.Contains("||") || val.Contains("==") ||
                val.Contains("!=") || val.Contains('>') || val.Contains('<') ||
                val.Contains(".length") || val.Contains(".filter") || val.Contains(".map") ||
                val.Contains(".reduce") || val.Contains(".join"))
                return true;
        }

        return false;
    }

    #endregion

    #region 模板 → DOM 创建

    private string emit_template(StringBuilder js, IReadOnlyList<AwslTemplateNode> nodes)
    {
        js.AppendLine("    // DOM 构建");

        if (nodes.Count == 0)
        {
            js.AppendLine("    const __root = createElement('div');");
            return "__root";
        }

        if (nodes.Count == 1) return emit_node(js, nodes[0], "    ");

        js.AppendLine("    const __root = createElement('div');");
        foreach (var node in nodes)
        {
            var childVar = emit_node(js, node, "    ");
            if (childVar != "null") js.AppendLine($"    insertNode(__root, {childVar});");
        }

        return "__root";
    }

    private string emit_node(StringBuilder js, AwslTemplateNode node, string indent)
    {
        return node switch
        {
            AwslTextNode textNode => emit_text(js, textNode, indent),
            AwslInterpolationNode interpNode => emit_interpolation(js, interpNode, indent),
            AwslElementNode elementNode => emit_element(js, elementNode, indent),
            AwslIfNode ifNode => emit_conditional(js, ifNode, indent),
            AwslForNode forNode => emit_list(js, forNode, indent),
            _ => emit_placeholder(js, indent)
        };
    }

    private string emit_text(StringBuilder js, AwslTextNode node, string indent)
    {
        if (string.IsNullOrWhiteSpace(node.text)) return "null";

        var varName = next_var();
        js.AppendLine($"{indent}const {varName} = createTextNode({format_js_string(node.text)});");
        return varName;
    }

    private string emit_interpolation(StringBuilder js, AwslInterpolationNode node, string indent)
    {
        var expr = node.expression.Trim();
        var varName = next_var();

        if (expr.Contains('.') && !expr.StartsWith("Math.") && !expr.StartsWith("Date."))
        {
            var resolved = replace_identifiers_with_getters(expr);
            if (is_reactive_expr(expr))
                js.AppendLine($"{indent}const {varName} = dynamicText(() => String({resolved}));");
            else
                js.AppendLine($"{indent}const {varName} = createTextNode(String({resolved}));");
        }
        else
        {
            var resolved = resolve_value_expr(expr);

            if (is_reactive_expr(expr))
                js.AppendLine($"{indent}const {varName} = dynamicText(() => String({resolved}));");
            else
                js.AppendLine($"{indent}const {varName} = createTextNode(String({resolved}));");
        }

        return varName;
    }

    private string emit_element(StringBuilder js, AwslElementNode node, string indent)
    {
        if (is_component_tag(node.tag_name)) return emit_component(js, node, indent);

        var varName = next_var();
        js.AppendLine($"{indent}const {varName} = createElement('{node.tag_name}');");

        foreach (var attr in node.attributes)
        {
            emit_attribute(js, varName, attr.Key, attr.Value, indent);
        }

        foreach (var child in node.children)
        {
            var childVar = emit_node(js, child, indent);
            if (childVar != "null") js.AppendLine($"{indent}insertNode({varName}, {childVar});");
        }

        return varName;
    }

    private string emit_component(StringBuilder js, AwslElementNode node, string indent)
    {
        var varName = next_var();
        var componentName = node.tag_name;

        var propsObj = new StringBuilder();
        propsObj.Append("{");

        var first = true;
        foreach (var attr in node.attributes)
        {
            if (!first) propsObj.Append(", ");
            first = false;

            var propName = to_snake_case(attr.Key);
            var propValue = is_signal_name(attr.Value) || is_reactive_expr(attr.Value)
                ? resolve_value_expr(attr.Value)
                : format_js_string(attr.Value);

            propsObj.Append($"{propName}: {propValue}");
        }

        propsObj.Append("}");

        js.AppendLine($"{indent}const {varName} = createComponent({componentName}, {propsObj});");

        return varName;
    }

    private void emit_attribute(StringBuilder js, string elVar, string name, string value, string indent)
    {
        if (name is "v-model" or "vModel")
        {
            emit_v_model(js, elVar, value, indent);
            return;
        }

        if (name.StartsWith("on:") || name.StartsWith("on"))
        {
            var eventName = name.StartsWith("on:") ? name[3..] : name[2..].ToLowerInvariant();
            _event_handlers.Add(value);
            js.AppendLine($"{indent}{elVar}.addEventListener('{eventName}', {value});");
            return;
        }

        if (is_signal_name(value))
        {
            var getter = $"get_{to_snake_case(value)}()";

            if (name == "value")
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'value', {getter}); }});");
            else if (name == "checked")
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'checked', {getter}); }});");
            else if (name is "class" or "className")
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'class', () => {getter});");
            else if (name == "style")
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'style', () => {getter});");
            else
                js.AppendLine($"{indent}dynamicAttribute({elVar}, '{name}', () => String({getter}));");

            return;
        }

        if (is_reactive_expr(value))
        {
            var resolved = resolve_value_expr(value);

            if (name == "value")
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'value', {resolved}); }});");
            else if (name == "checked")
                js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'checked', {resolved}); }});");
            else if (name is "class" or "className")
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'class', () => {resolved});");
            else if (name == "style")
                js.AppendLine($"{indent}dynamicAttribute({elVar}, 'style', () => {resolved});");
            else
                js.AppendLine($"{indent}dynamicAttribute({elVar}, '{name}', () => String({resolved}));");

            return;
        }

        if (name is "class" or "className")
            js.AppendLine($"{indent}setAttribute({elVar}, 'class', {format_js_string(value)});");
        else if (name == "value")
            js.AppendLine($"{indent}setProperty({elVar}, 'value', {format_js_string(value)});");
        else if (name == "checked")
            js.AppendLine($"{indent}setProperty({elVar}, 'checked', true);");
        else if (value == name)
            js.AppendLine($"{indent}setAttribute({elVar}, '{name}', true);");
        else
            js.AppendLine($"{indent}setAttribute({elVar}, '{name}', {format_js_string(value)});");
    }

    private void emit_v_model(StringBuilder js, string elVar, string signalName, string indent)
    {
        if (!is_signal_name(signalName)) return;

        var pascal = to_snake_case(signalName);
        js.AppendLine($"{indent}createEffect(() => {{ setProperty({elVar}, 'value', get_{pascal}()); }});");
        js.AppendLine($"{indent}{elVar}.addEventListener('input', (e) => {{ set_{pascal}(e.target.value); }});");
    }

    private string emit_conditional(StringBuilder js, AwslIfNode node, string indent = "    ")
    {
        var inner = indent + "    ";
        var elVar = next_var();

        var condition = replace_identifiers_with_getters(node.condition);

        js.AppendLine($"{indent}let {elVar};");

        js.AppendLine($"{indent}createEffect(() => {{");
        js.AppendLine($"{inner}if ({condition} != null && {condition} !== false) {{");

        var thenEl = emit_children(js, node.children, inner + "    ");
        js.AppendLine($"{inner}    {elVar} = conditional({elVar}, {thenEl});");
        js.AppendLine($"{inner}}} else {{");

        var elseChildren = node.else_children;
        if (elseChildren.Count == 1 && elseChildren[0] is AwslIfNode elif)
        {
            var elifEl = emit_conditional(js, elif, inner + "    ");
            js.AppendLine($"{inner}    {elVar} = conditional({elVar}, {elifEl});");
        }
        else
        {
            var elseEl = emit_children(js, elseChildren, inner + "    ");
            js.AppendLine($"{inner}    {elVar} = conditional({elVar}, {elseEl});");
        }

        js.AppendLine($"{inner}}}");
        js.AppendLine($"{indent}}});");

        return elVar;
    }

    private string emit_children(StringBuilder js, IReadOnlyList<AwslTemplateNode> children, string indent)
    {
        if (children.Count == 0) return "null";
        if (children.Count == 1) return emit_node(js, children[0], indent);

        var rootVar = next_var();
        js.AppendLine($"{indent}const {rootVar} = createElement('div');");

        foreach (var child in children)
        {
            var childVar = emit_node(js, child, indent);
            if (childVar != "null") js.AppendLine($"{indent}insertNode({rootVar}, {childVar});");
        }

        return rootVar;
    }

    private string emit_list(StringBuilder js, AwslForNode node, string indent)
    {
        var iterable = replace_identifiers_with_getters(node.iterable);
        var varName = next_var();

        var mapFn = build_map_fn(js, node.iterator, node.children, indent + "    ");

        js.AppendLine($"{indent}const {varName} = listMap(() => {iterable}, {mapFn});");

        return varName;
    }

    private string build_factory(StringBuilder js, IReadOnlyList<AwslTemplateNode> nodes, string indent)
    {
        if (nodes.Count == 0) return "() => createElement('span')";
        if (nodes.Count == 1)
        {
            var childVar = emit_node(js, nodes[0], indent);
            return $"() => {childVar}";
        }

        var rootVar = next_var();
        js.AppendLine($"{indent}const {rootVar} = createElement('div');");

        foreach (var node in nodes)
        {
            var childVar = emit_node(js, node, indent);
            if (childVar != "null") js.AppendLine($"{indent}insertNode({rootVar}, {childVar});");
        }

        return $"() => {rootVar}";
    }

    private string build_map_fn(StringBuilder js, string iterator, IReadOnlyList<AwslTemplateNode> nodes,
        string indent)
    {
        js.AppendLine($"{indent}const __item = ({iterator}, __idx) => {{");

        if (nodes.Count == 0)
        {
            js.AppendLine($"{indent}    return createElement('span');");
        }
        else if (nodes.Count == 1)
        {
            var childVar = emit_node_with_iterator(js, nodes[0], indent + "    ", iterator);
            js.AppendLine($"{indent}    return {childVar};");
        }
        else
        {
            var rootVar = next_var();
            js.AppendLine($"{indent}    const {rootVar} = createElement('div');");

            foreach (var childNode in nodes)
            {
                var childVar = emit_node_with_iterator(js, childNode, indent + "    ", iterator);
                if (childVar != "null") js.AppendLine($"{indent}    insertNode({rootVar}, {childVar});");
            }

            js.AppendLine($"{indent}    return {rootVar};");
        }

        js.AppendLine($"{indent}}};");
        return "__item";
    }

    private string emit_node_with_iterator(StringBuilder js, AwslTemplateNode node, string indent, string iterator)
    {
        return node switch
        {
            AwslElementNode elementNode => emit_element_with_iterator(js, elementNode, indent, iterator),
            AwslInterpolationNode interpNode => emit_interpolation_with_iterator(js, interpNode, indent, iterator),
            _ => emit_node(js, node, indent)
        };
    }

    private string emit_interpolation_with_iterator(StringBuilder js, AwslInterpolationNode node, string indent,
        string iterator)
    {
        var expr = node.expression.Trim();
        var varName = next_var();

        if (expr.Contains('.') && expr.StartsWith(iterator))
        {
            js.AppendLine($"{indent}const {varName} = createTextNode(String({expr}));");
        }
        else
        {
            var resolved = resolve_value_expr(expr);
            if (is_reactive_expr(expr))
                js.AppendLine($"{indent}const {varName} = dynamicText(() => String({resolved}));");
            else
                js.AppendLine($"{indent}const {varName} = createTextNode(String({resolved}));");
        }

        return varName;
    }

    private string emit_element_with_iterator(StringBuilder js, AwslElementNode node, string indent, string iterator)
    {
        var varName = next_var();
        js.AppendLine($"{indent}const {varName} = createElement('{node.tag_name}');");
        js.AppendLine($"{indent}{varName}.__voa_idx = __idx;");

        foreach (var attr in node.attributes)
        {
            emit_attribute_with_iterator(js, varName, attr.Key, attr.Value, indent, iterator);
        }

        foreach (var child in node.children)
        {
            var childVar = emit_node_with_iterator(js, child, indent, iterator);
            if (childVar != "null") js.AppendLine($"{indent}insertNode({varName}, {childVar});");
        }

        return varName;
    }

    private void emit_attribute_with_iterator(StringBuilder js, string elVar, string name, string value, string indent,
        string iterator)
    {
        if (name is "v-model" or "vModel")
        {
            emit_v_model(js, elVar, value, indent);
            return;
        }

        if (name.StartsWith("on:") || name.StartsWith("on"))
        {
            var eventName = name.StartsWith("on:") ? name[3..] : name[2..].ToLowerInvariant();
            _event_handlers.Add(value);
            js.AppendLine($"{indent}{elVar}.addEventListener('{eventName}', {value});");
            return;
        }

        if (value.Contains('.') && value.StartsWith(iterator))
        {
            var prop = value[(iterator.Length + 1)..];

            if (name == "checked")
                js.AppendLine($"{indent}setProperty({elVar}, 'checked', {iterator}.{prop});");
            else if (name == "value")
                js.AppendLine($"{indent}setProperty({elVar}, 'value', {iterator}.{prop});");
            else
                js.AppendLine($"{indent}setAttribute({elVar}, '{name}', {iterator}.{prop});");

            return;
        }

        emit_attribute(js, elVar, name, value, indent);
    }

    #endregion

    #region 事件处理器生成

    private void emit_lifecycle_hooks(StringBuilder js, AwslParseResult parseResult)
    {
        var hasMount = parseResult.methods.Any(m => m.name is "onMount" or "mounted");
        var hasDestroy =
            parseResult.methods.Any(m => m.name is "onDestroy" or "destroyed" or "onCleanup");

        var needsLifecycle = hasMount || hasDestroy || _event_handlers.Count > 0;

        if (!needsLifecycle) return;

        js.AppendLine("    // 生命周期");

        if (hasMount)
        {
            var mountMethod = parseResult.methods.First(m => m.name is "onMount" or "mounted");
            var body = translate_method_body(mountMethod.body);
            js.AppendLine($"    onMount(() => {{ {body} }});");
        }

        if (hasDestroy)
        {
            var destroyMethod = parseResult.methods.First(m =>
                m.name is "onDestroy" or "destroyed" or "onCleanup");
            var body = translate_method_body(destroyMethod.body);
            js.AppendLine($"    onCleanup(() => {{ {body} }});");
        }
    }

    private void emit_event_handlers(StringBuilder js, AwslParseResult parseResult)
    {
        if (parseResult.methods.Count == 0 && _event_handlers.Count == 0) return;

        js.AppendLine("    // 微过程");

        foreach (var method in parseResult.methods)
        {
            if (method.name is "onMount" or "mounted" or "onDestroy" or "destroyed" or "onCleanup") continue;

            var body = translate_method_body(method.body);
            var paramsStr = string.IsNullOrEmpty(method.parameters) ? "event" : method.parameters;

            js.AppendLine($"    function {method.name}({paramsStr}) {{");

            if (!string.IsNullOrWhiteSpace(body))
                js.AppendLine($"        {body}");
            else
                js.AppendLine($"        console.debug('事件触发: {method.name}', event);");

            js.AppendLine("    }");
        }

        foreach (var handler in _event_handlers)
        {
            var alreadyDefined = parseResult.methods.Any(m => m.name == handler);
            if (alreadyDefined) continue;

            js.AppendLine($"    function {handler}(event) {{");
            js.AppendLine($"        console.warn('事件处理桩: {handler}', event);");
            js.AppendLine("    }");
        }
    }

    private string translate_method_body(string body)
    {
        var lines = body.Split('\n');
        var result = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var translated = translate_statement(trimmed);
            result.AppendLine($"        {translated}");
        }

        return result.ToString().TrimEnd();
    }

    private string translate_statement(string stmt)
    {
        var s = stmt.Trim();

        if (s.StartsWith("return "))
        {
            var expr = s[7..].TrimEnd(';');
            return $"return {translate_expr(expr)};";
        }

        if (s.StartsWith("let ") || s.StartsWith("const ") || s.StartsWith("var ")) return translate_var_declaration(s);

        if (s.StartsWith("if ")) return translate_expr(s);

        var assignMatch = System.Text.RegularExpressions.Regex.Match(s, @"^(\w+(?:\.\w+)*)\s*=\s*(.+);?$");
        if (assignMatch.Success)
        {
            var target = assignMatch.Groups[1].Value;
            var value = assignMatch.Groups[2].Value.TrimEnd(';');
            return translate_assignment(target, value);
        }

        if (s.EndsWith("++"))
        {
            var name = s[..^2].Trim();
            if (is_signal_name(name)) return $"set{to_snake_case(name)}(get{to_snake_case(name)}() + 1);";

            return s;
        }

        if (s.EndsWith("--"))
        {
            var name = s[..^2].Trim();
            if (is_signal_name(name)) return $"set_{to_snake_case(name)}(get_{to_snake_case(name)}() - 1);";

            return s;
        }

        return translate_expr(s);
    }

    private string translate_var_declaration(string s)
    {
        var declMatch = System.Text.RegularExpressions.Regex.Match(s, @"^(let|const|var)\s+(\w+)(?::\s*\w+)?\s*=\s*(.+?);?$");
        if (!declMatch.Success) return s;

        var keyword = declMatch.Groups[1].Value;
        var name = declMatch.Groups[2].Value;
        var value = declMatch.Groups[3].Value.TrimEnd(';');

        return $"{keyword} {name} = {translate_expr(value)};";
    }

    private string translate_assignment(string target, string valueExpr)
    {
        if (target.Contains('.')) return $"{target} = {translate_expr(valueExpr)};";

        if (is_signal_name(target))
        {
            var translated = translate_expr(valueExpr);
            return $"set_{to_snake_case(target)}({translated});";
        }

        return $"{target} = {translate_expr(valueExpr)};";
    }

    private string translate_expr(string expr)
    {
        return replace_identifiers_with_getters(expr);
    }

    #endregion

    #region 表达式解析

    private bool is_signal_name(string name)
    {
        return _signal_names.Contains(name) || _memo_names.Contains(name) || _prop_names.Contains(name);
    }

    private bool is_component_tag(string tagName)
    {
        if (tagName.Length > 0 && char.IsUpper(tagName[0])) return true;

        return _component_names.Contains(tagName);
    }

    private string resolve_value_expr(string expr)
    {
        var trimmed = expr.Trim();

        if (trimmed.StartsWith("state.") || trimmed.StartsWith("props."))
            return replace_identifiers_with_getters(trimmed);

        if (CssStringUtils.is_js_keyword(trimmed) || CssStringUtils.is_literal(trimmed)) return trimmed;

        if (trimmed.EndsWith("()")) return trimmed;

        if (is_signal_name(trimmed)) return $"get_{to_snake_case(trimmed)}()";

        if (_method_names.Contains(trimmed)) return $"{trimmed}()";

        if (trimmed.Contains(".") && !trimmed.StartsWith("Math.") && !trimmed.StartsWith("Date."))
            return replace_identifiers_with_getters(trimmed);

        if (trimmed.Contains("&&") || trimmed.Contains("||") || trimmed.Contains("==") ||
            trimmed.Contains("!=") || trimmed.Contains(">") || trimmed.Contains("<") ||
            trimmed.Contains("+") || trimmed.Contains("-") || trimmed.Contains("*") ||
            trimmed.Contains("/") || trimmed.Contains("?") || trimmed.Contains(":"))
            return replace_identifiers_with_getters(trimmed);

        if (trimmed.StartsWith("!") || trimmed.StartsWith("(") || trimmed.StartsWith("["))
            return replace_identifiers_with_getters(trimmed);

        return replace_identifiers_with_getters(trimmed);
    }

    private string replace_identifiers_with_getters(string expr)
    {
        var result = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in System.Text.RegularExpressions.Regex.Matches(expr, @"\b([a-zA-Z_]\w*)\b"))
        {
            result.Append(expr[lastIndex..match.Index]);
            var name = match.Groups[1].Value;

            if (CssStringUtils.is_js_keyword(name) || CssStringUtils.is_literal(name) ||
                name is "Math" or "Date" or "JSON" or "console" or "window" or "document"
                    or "this" or "undefined" or "NaN" or "Infinity" or "String" or "Number"
                    or "Boolean" or "Array" or "Object" or "parseInt" or "parseFloat"
                    or "props")
                result.Append(name);
            else if (is_signal_name(name))
                result.Append($"get_{to_snake_case(name)}()");
            else
                result.Append(name);

            lastIndex = match.Index + match.Length;
        }

        result.Append(expr[lastIndex..]);
        return result.ToString();
    }

    private bool is_reactive_expr(string expr)
    {
        var trimmed = expr.Trim();

        if (CssStringUtils.is_js_keyword(trimmed) || CssStringUtils.is_literal(trimmed)) return false;
        if (trimmed.StartsWith("'") || trimmed.StartsWith("\"") || trimmed.StartsWith("`")) return false;
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return false;

        foreach (Match match in System.Text.RegularExpressions.Regex.Matches(expr, @"\b([a-zA-Z_]\w*)\b"))
        {
            var name = match.Groups[1].Value;
            if (is_signal_name(name)) return true;
        }

        return false;
    }

    #endregion

    #region 工具方法

    private string next_var()
    {
        return $"__v{_var_counter++}";
    }

    private static string format_default_value(AwslProperty prop)
    {
        if (prop.default_value_kind == AwslValueKind.expression) return "null";

        if (prop.default_value_kind == AwslValueKind.none)
            return prop.type_name switch
            {
                "bool" => "false",
                "i8" or "i16" or "i32" or "i64" => "0",
                "u8" or "u16" or "u32" or "u64" => "0",
                "f32" or "f64" => "0.0",
                "utf8" => "\"\"",
                "list" or "list<" => "[]",
                "map" or "map<" => "{}",
                _ => "null"
            };

        return prop.default_value_kind switch
        {
            AwslValueKind.@string => $"\"{prop.default_value}\"",
            AwslValueKind.boolean => prop.default_value?.ToLower() ?? "false",
            AwslValueKind.number => prop.default_value ?? "0",
            AwslValueKind.array => prop.default_value ?? "[]",
            AwslValueKind.@object => prop.default_value ?? "{}",
            _ => prop.default_value ?? "null"
        };
    }

    private static string format_js_string(string text)
    {
        if (string.IsNullOrEmpty(text)) return "''";

        var escaped = text
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");

        return $"'{escaped}'";
    }

    private static string emit_placeholder(StringBuilder js, string indent)
    {
        var varName = "__ph";
        js.AppendLine($"{indent}const {varName} = createElement('span');");
        return varName;
    }

    private static string to_pascal_case(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();
        sb.Append(char.ToUpperInvariant(name[0]));

        for (var i = 1; i < name.Length; i++)
        {
            var c = name[i];
            if (c is '-' or '_' or ' ' or '.')
            {
                if (i + 1 < name.Length)
                {
                    sb.Append(char.ToUpperInvariant(name[i + 1]));
                    i++;
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string to_snake_case(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is '-' or ' ' or '.')
            {
                sb.Append('_');
            }
            else if (c == '_')
            {
                sb.Append('_');
            }
            else if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]))) sb.Append('_');
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
