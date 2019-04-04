namespace Std.Data.Text.Scss;

/// <summary>
///     SCSS 解析器
/// </summary>
public sealed class ScssParser
{
    private readonly ScssVariableScope _global_scope;
    private readonly Dictionary<string, ScssMixin> _mixins = new();
    private readonly List<StyleRule> _output_rules = [];
    private readonly string _source;
    private int _pos;

    public ScssParser(string source)
    {
        _source = source;
        _pos = 0;
        _global_scope = new ScssVariableScope();
    }


    /// <summary>
    ///     解析 SCSS 到样式表
    /// </summary>
    public void parse(StyleSheet sheet)
    {
        parse_block(_global_scope, null);

        foreach (var rule in _output_rules) sheet.add_rule(rule);
    }

    private void parse_block(ScssVariableScope scope, List<StyleSelector>? parentSelectors)
    {
        while (_pos < _source.Length)
        {
            skip_whitespace_and_comments();

            if (_pos >= _source.Length) break;

            if (_source[_pos] == '}') break;

            if (try_parse_variable_declaration(scope)) continue;

            if (try_parse_mixin_definition(scope)) continue;

            if (try_parse_include(scope, parentSelectors)) continue;

            if (try_parse_at_rule(scope, parentSelectors)) continue;

            var selectors = parse_selectors();
            if (selectors.Count == 0)
            {
                _pos++;
                continue;
            }

            skip_whitespace();

            if (_pos >= _source.Length) break;

            if (_source[_pos] != '{') continue;

            _pos++;

            var resolvedSelectors = resolve_nested_selectors(parentSelectors, selectors);

            var childScope = scope.push();
            var declarations = new List<StyleDeclaration>();
            var hasNestedContent = false;

            parse_block_content(childScope, resolvedSelectors, declarations, ref hasNestedContent);

            skip_whitespace();

            if (_pos < _source.Length && _source[_pos] == '}') _pos++;

            if (declarations.Count > 0)
            {
                var evaluator = new ScssEvaluator(childScope);
                var evaluatedDecls = declarations.Select(d =>
                    new StyleDeclaration(d.property, evaluator.evaluate(d.value), d.specificity, d.important)).ToList();

                _output_rules.Add(new StyleRule(resolvedSelectors, evaluatedDecls));
            }
        }
    }

    private void parse_block_content(ScssVariableScope scope, List<StyleSelector> currentSelectors,
        List<StyleDeclaration> declarations, ref bool hasNestedContent)
    {
        while (_pos < _source.Length)
        {
            skip_whitespace_and_comments();

            if (_pos >= _source.Length || _source[_pos] == '}') break;

            if (try_parse_variable_declaration(scope)) continue;

            if (try_parse_include_in_declarations(scope, declarations)) continue;

            var peekSelectors = peek_selectors();
            if (peekSelectors is { Count: > 0 })
            {
                hasNestedContent = true;
                parse_block(scope, currentSelectors);
                continue;
            }

            var decl = parse_single_declaration(scope);
            if (decl != null)
                declarations.Add(decl);
            else
                _pos++;
        }
    }

    private bool try_parse_variable_declaration(ScssVariableScope scope)
    {
        if (_pos >= _source.Length || _source[_pos] != '$') return false;

        var savePos = _pos;
        _pos++;

        var name = read_ident();
        if (name.Length == 0)
        {
            _pos = savePos;
            return false;
        }

        skip_whitespace();
        if (_pos >= _source.Length || _source[_pos] != ':')
        {
            _pos = savePos;
            return false;
        }

        _pos++;
        skip_whitespace();

        var value = read_until_semicolon();

        if (_pos < _source.Length && _source[_pos] == ';') _pos++;

        scope.define(name, value.Trim());
        return true;
    }

    private bool try_parse_mixin_definition(ScssVariableScope scope)
    {
        if (!peek_keyword("@mixin")) return false;

        _pos += 6;
        skip_whitespace();

        var name = read_ident();
        if (name.Length == 0) return true;

        skip_whitespace();

        var parameters = new List<string>();
        if (_pos < _source.Length && _source[_pos] == '(')
        {
            _pos++;
            parameters = parse_mixin_parameters();
            skip_whitespace();

            if (_pos < _source.Length && _source[_pos] == ')') _pos++;
        }

        skip_whitespace();
        if (_pos >= _source.Length || _source[_pos] != '{') return true;

        _pos++;
        var bodyStart = _pos;
        var depth = 1;

        while (_pos < _source.Length && depth > 0)
        {
            if (_source[_pos] == '{')
                depth++;
            else if (_source[_pos] == '}') depth--;

            if (depth > 0) _pos++;
        }

        var body = _source.Substring(bodyStart, _pos - bodyStart);

        if (_pos < _source.Length && _source[_pos] == '}') _pos++;

        _mixins[name] = new ScssMixin(name, parameters, body);
        return true;
    }

    private bool try_parse_include(ScssVariableScope scope, List<StyleSelector>? parentSelectors)
    {
        return try_parse_include_core(scope, parentSelectors, null);
    }

    private bool try_parse_include_in_declarations(ScssVariableScope scope, List<StyleDeclaration> declarations)
    {
        return try_parse_include_core(scope, null, declarations);
    }

    private bool try_parse_include_core(ScssVariableScope scope, List<StyleSelector>? parentSelectors,
        List<StyleDeclaration>? declarations)
    {
        if (!peek_keyword("@include")) return false;

        _pos += 8;
        skip_whitespace();

        var name = read_ident();
        if (name.Length == 0) return true;

        skip_whitespace();

        var args = new List<string>();
        if (_pos < _source.Length && _source[_pos] == '(')
        {
            _pos++;
            args = parse_mixin_arguments();
            skip_whitespace();

            if (_pos < _source.Length && _source[_pos] == ')') _pos++;
        }

        skip_whitespace();
        if (_pos < _source.Length && _source[_pos] == ';') _pos++;

        if (!_mixins.TryGetValue(name, out var mixin)) return true;

        var mixinScope = scope.push();
        for (var i = 0; i < mixin.parameters.Count && i < args.Count; i++)
            mixinScope.define(mixin.parameters[i], args[i]);

        var subParser = new ScssParserSub(mixin.body, mixinScope, _mixins);
        var mixinDecls = subParser.parse_declarations();

        var evaluator = new ScssEvaluator(mixinScope);
        foreach (var decl in mixinDecls)
        {
            var evaluated = new StyleDeclaration(decl.property, evaluator.evaluate(decl.value), decl.specificity,
                decl.important);
            declarations?.Add(evaluated);
        }

        return true;
    }

    private bool try_parse_at_rule(ScssVariableScope scope, List<StyleSelector>? parentSelectors)
    {
        if (_pos >= _source.Length || _source[_pos] != '@') return false;

        if (peek_keyword("@mixin") || peek_keyword("@include")) return false;

        var ruleName = read_at_rule_name();

        if (ruleName == "extend")
        {
            skip_whitespace();
            var extendSelector = read_until_semicolon().Trim();

            if (_pos < _source.Length && _source[_pos] == ';') _pos++;

            return true;
        }

        if (ruleName is "if" or "each" or "for")
        {
            skip_block();
            return true;
        }

        skip_whitespace();
        if (_pos < _source.Length && _source[_pos] == '{')
        {
            _pos++;
            skip_block();

            if (_pos < _source.Length && _source[_pos] == '}') _pos++;
        }
        else
        {
            read_until_semicolon();

            if (_pos < _source.Length && _source[_pos] == ';') _pos++;
        }

        return true;
    }

    private List<StyleSelector> resolve_nested_selectors(List<StyleSelector>? parent, List<StyleSelector> child)
    {
        if (parent == null || parent.Count == 0) return child;

        var result = new List<StyleSelector>();

        foreach (var p in parent)
        foreach (var c in child)
            if (c.value == "&")
            {
                result.Add(p);
            }
            else if (c.value.StartsWith("&"))
            {
                var suffix = c.value[1..];
                result.Add(new StyleSelector(p.type, p.value + suffix));
            }
            else
            {
                result.Add(p);
                result.Add(c);
            }

        return result;
    }

    private List<StyleSelector> parse_selectors()
    {
        var selectors = new List<StyleSelector>();

        while (_pos < _source.Length)
        {
            skip_whitespace();

            if (_pos >= _source.Length || _source[_pos] == '{' || _source[_pos] == '}') break;

            if (_source[_pos] == ';')
            {
                _pos++;
                break;
            }

            if (_source[_pos] == ',')
            {
                _pos++;
                continue;
            }

            var selector = parse_single_selector();
            if (selector != null)
                selectors.Add(selector.Value);
            else
                break;
        }

        return selectors;
    }

    private List<StyleSelector>? peek_selectors()
    {
        var savePos = _pos;
        var selectors = new List<StyleSelector>();
        var foundBrace = false;

        while (_pos < _source.Length)
        {
            skip_whitespace();

            if (_pos >= _source.Length) break;

            if (_source[_pos] == '{')
            {
                foundBrace = true;
                break;
            }

            if (_source[_pos] == ':' || _source[_pos] == ';') break;

            var selector = parse_single_selector();
            if (selector != null)
                selectors.Add(selector.Value);
            else
                break;
        }

        _pos = savePos;
        return foundBrace && selectors.Count > 0 ? selectors : null;
    }

    private StyleSelector? parse_single_selector()
    {
        if (_pos >= _source.Length) return null;

        var ch = _source[_pos];

        if (ch == '&')
        {
            _pos++;
            var suffix = read_ident();
            return new StyleSelector(StyleSelectorType.parent_ref, "&" + suffix);
        }

        if (ch == '#')
        {
            _pos++;
            var id = read_ident();
            return StyleSelector.by_id(id);
        }

        if (ch == '.')
        {
            _pos++;
            var className = read_ident();
            return StyleSelector.by_class(className);
        }

        if (ch == ':')
        {
            _pos++;
            var name = read_ident();
            return StyleSelector.by_pseudo_class(name);
        }

        if (is_ident_start(ch))
        {
            var typeName = read_ident();
            return StyleSelector.by_type(typeName);
        }

        return null;
    }

    private StyleDeclaration? parse_single_declaration(ScssVariableScope scope)
    {
        var property = read_ident();
        if (property.Length == 0) return null;

        skip_whitespace();
        if (_pos >= _source.Length || _source[_pos] != ':') return null;

        _pos++;
        skip_whitespace();

        var value = read_until_semicolon_or_brace();
        skip_whitespace();

        string? important = null;
        var trimmedValue = value.Trim();
        if (trimmedValue.EndsWith("!important", StringComparison.OrdinalIgnoreCase))
        {
            important = "!important";
            trimmedValue = trimmedValue[..^"!important".Length].Trim();
        }

        if (_pos < _source.Length && _source[_pos] == ';') _pos++;

        return new StyleDeclaration(property, trimmedValue, 0, important);
    }

    private List<string> parse_mixin_parameters()
    {
        var parameters = new List<string>();

        while (_pos < _source.Length)
        {
            skip_whitespace();

            if (_pos >= _source.Length || _source[_pos] == ')') break;

            if (_source[_pos] == ',')
            {
                _pos++;
                continue;
            }

            if (_source[_pos] == '$')
            {
                _pos++;
                var name = read_ident();
                if (name.Length > 0) parameters.Add(name);
            }
            else
            {
                _pos++;
            }
        }

        return parameters;
    }

    private List<string> parse_mixin_arguments()
    {
        var args = new List<string>();
        var depth = 0;
        var start = _pos;

        while (_pos < _source.Length)
        {
            var ch = _source[_pos];

            if (ch == '(')
            {
                depth++;
                _pos++;
                continue;
            }

            if (ch == ')')
            {
                if (depth == 0) break;

                depth--;
                _pos++;
                continue;
            }

            if (ch == ',' && depth == 0)
            {
                args.Add(_source.Substring(start, _pos - start).Trim());
                _pos++;
                start = _pos;
                continue;
            }

            _pos++;
        }

        if (_pos > start) args.Add(_source.Substring(start, _pos - start).Trim());

        return args;
    }

    private string read_at_rule_name()
    {
        if (_pos < _source.Length && _source[_pos] == '@') _pos++;

        return read_ident();
    }

    private string read_ident()
    {
        var start = _pos;

        while (_pos < _source.Length && is_ident_char(_source[_pos])) _pos++;

        return _source.Substring(start, _pos - start);
    }

    private string read_until_semicolon()
    {
        var start = _pos;
        var depth = 0;

        while (_pos < _source.Length)
        {
            if (_source[_pos] == '(')
                depth++;
            else if (_source[_pos] == ')')
                depth--;
            else if (_source[_pos] == ';' && depth == 0) break;

            _pos++;
        }

        return _source.Substring(start, _pos - start);
    }

    private string read_until_semicolon_or_brace()
    {
        var start = _pos;
        var depth = 0;

        while (_pos < _source.Length)
        {
            if (_source[_pos] == '(')
                depth++;
            else if (_source[_pos] == ')')
                depth--;
            else if ((_source[_pos] == ';' || _source[_pos] == '}') && depth == 0) break;

            _pos++;
        }

        return _source.Substring(start, _pos - start);
    }

    private void skip_block()
    {
        var depth = 0;

        while (_pos < _source.Length)
        {
            if (_source[_pos] == '{')
            {
                depth++;
            }
            else if (_source[_pos] == '}')
            {
                depth--;
                if (depth <= 0) return;
            }

            _pos++;
        }
    }

    private bool peek_keyword(string keyword)
    {
        if (_pos + keyword.Length > _source.Length) return false;

        if (_source.Substring(_pos, keyword.Length) != keyword) return false;

        if (_pos + keyword.Length < _source.Length && is_ident_char(_source[_pos + keyword.Length])) return false;

        return true;
    }

    private void skip_whitespace_and_comments()
    {
        while (_pos < _source.Length)
        {
            if (char.IsWhiteSpace(_source[_pos]))
            {
                _pos++;
                continue;
            }

            if (_pos + 1 < _source.Length && _source[_pos] == '/' && _source[_pos + 1] == '/')
            {
                while (_pos < _source.Length && _source[_pos] != '\n') _pos++;

                continue;
            }

            if (_pos + 1 < _source.Length && _source[_pos] == '/' && _source[_pos + 1] == '*')
            {
                _pos += 2;
                while (_pos + 1 < _source.Length && !(_source[_pos] == '*' && _source[_pos + 1] == '/')) _pos++;

                if (_pos + 1 < _source.Length) _pos += 2;

                continue;
            }

            break;
        }
    }

    private void skip_whitespace()
    {
        while (_pos < _source.Length && char.IsWhiteSpace(_source[_pos])) _pos++;
    }

    private static bool is_ident_start(char ch)
    {
        return char.IsLetter(ch) || ch == '_' || ch == '-';
    }

    private static bool is_ident_char(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '-';
    }
}