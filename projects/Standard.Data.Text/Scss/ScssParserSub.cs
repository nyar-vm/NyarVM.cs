namespace Std.Data.Text.Scss;

/// <summary>
///     SCSS 子解析器，用于解析 Mixin 体
/// </summary>
internal sealed class ScssParserSub
{
    private readonly Dictionary<string, ScssMixin> _mixins;
    private readonly ScssVariableScope _scope;
    private readonly string _source;
    private int _pos;

    public ScssParserSub(string source, ScssVariableScope scope, Dictionary<string, ScssMixin> mixins)
    {
        _source = source;
        _pos = 0;
        _scope = scope;
        _mixins = mixins;
    }


    /// <summary>
    ///     解析声明列表
    /// </summary>
    public List<StyleDeclaration> parse_declarations()
    {
        var declarations = new List<StyleDeclaration>();

        while (_pos < _source.Length)
        {
            skip_whitespace_and_comments();

            if (_pos >= _source.Length) break;

            if (_source[_pos] == '}') break;

            if (try_parse_variable_declaration(_scope)) continue;

            if (try_parse_include(declarations)) continue;

            var decl = parse_single_declaration();
            if (decl != null)
                declarations.Add(decl);
            else
                _pos++;
        }

        return declarations;
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

    private bool try_parse_include(List<StyleDeclaration> declarations)
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

        var mixinScope = _scope.push();
        for (var i = 0; i < mixin.parameters.Count && i < args.Count; i++)
            mixinScope.define(mixin.parameters[i], args[i]);

        var subParser = new ScssParserSub(mixin.body, mixinScope, _mixins);
        var mixinDecls = subParser.parse_declarations();

        var evaluator = new ScssEvaluator(mixinScope);
        foreach (var decl in mixinDecls)
        {
            var evaluated = new StyleDeclaration(decl.property, evaluator.evaluate(decl.value), decl.specificity,
                decl.important);
            declarations.Add(evaluated);
        }

        return true;
    }

    private StyleDeclaration? parse_single_declaration()
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

    private static bool is_ident_char(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '-';
    }
}