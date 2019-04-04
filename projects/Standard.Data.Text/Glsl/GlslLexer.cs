using System.Text;
using Std.Data.Text.Diagnostics;

namespace Std.Data.Text.Glsl;

public sealed class GlslLexer
{
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<GlslToken> tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<GlslToken>();

        while (!is_at_end())
        {
            skip_whitespace();

            if (is_at_end()) break;

            var token = scan_token();

            if (token.type != GlslTokenType.invalid) tokens.Add(token);
        }

        tokens.Add(new GlslToken(GlslTokenType.end_of_file, string.Empty, _line, _column));
        return tokens;
    }

    private bool is_at_end()
    {
        return _position >= _source.Length;
    }

    private char peek()
    {
        return is_at_end() ? '\0' : _source[_position];
    }

    private char peek_next()
    {
        return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
    }

    private char advance()
    {
        var c = _source[_position];
        _position++;

        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        return c;
    }

    private bool match(char expected)
    {
        if (is_at_end() || _source[_position] != expected) return false;

        advance();
        return true;
    }

    private void skip_whitespace()
    {
        while (!is_at_end() && char.IsWhiteSpace(peek())) advance();
    }

    private GlslToken scan_token()
    {
        var line = _line;
        var column = _column;
        var c = peek();

        if (c == '/' && peek_next() == '/') return scan_line_comment(line, column);

        if (c == '/' && peek_next() == '*') return scan_block_comment(line, column);

        if (c == '#') return scan_preprocessor(line, column);

        if (c == '"') return scan_string(line, column);

        if (char.IsDigit(c) || (c == '.' && char.IsDigit(peek_next()))) return scan_number(line, column);

        if (is_identifier_start(c)) return scan_identifier(line, column);

        return scan_symbol(line, column);
    }

    #region 字符串

    private GlslToken scan_string(int line, int column)
    {
        advance();

        var sb = new StringBuilder();

        while (!is_at_end() && peek() != '"')
        {
            if (peek() == '\\' && !is_at_end())
            {
                advance();
                if (!is_at_end()) sb.Append(advance());

                continue;
            }

            sb.Append(advance());
        }

        if (!is_at_end()) advance();

        return new GlslToken(GlslTokenType.@string, sb.ToString(), line, column);
    }

    #endregion

    #region 数字

    private GlslToken scan_number(int line, int column)
    {
        var start = _position;

        if (peek() == '0' && peek_next() is 'x' or 'X')
        {
            advance();
            advance();

            while (!is_at_end() && is_hex_digit(peek())) advance();

            return new GlslToken(GlslTokenType.int_constant, _source[start.._position], line, column);
        }

        var isFloat = false;

        if (peek() != '.')
            while (!is_at_end() && char.IsDigit(peek()))
                advance();

        if (!is_at_end() && peek() == '.' && char.IsDigit(peek_next()))
        {
            isFloat = true;
            advance();

            while (!is_at_end() && char.IsDigit(peek())) advance();
        }

        if (!is_at_end() && (peek() == 'e' || peek() == 'E'))
        {
            isFloat = true;
            advance();

            if (!is_at_end() && (peek() == '+' || peek() == '-')) advance();

            while (!is_at_end() && char.IsDigit(peek())) advance();
        }

        if (!is_at_end())
        {
            var suffix = char.ToLower(peek());

            if (suffix == 'f')
            {
                isFloat = true;
                advance();
            }
            else if (suffix == 'l')
            {
                advance();

                return new GlslToken(GlslTokenType.double_constant, _source[start.._position], line, column);
            }
            else if (suffix == 'u')
            {
                advance();

                return new GlslToken(GlslTokenType.uint_constant, _source[start.._position], line, column);
            }
        }

        return new GlslToken(
            isFloat ? GlslTokenType.float_constant : GlslTokenType.int_constant,
            _source[start.._position], line, column);
    }

    #endregion

    #region 运算符与符号

    private GlslToken scan_symbol(int line, int column)
    {
        var c = advance();

        switch (c)
        {
            case '{': return new GlslToken(GlslTokenType.left_brace, "{", line, column);
            case '}': return new GlslToken(GlslTokenType.right_brace, "}", line, column);
            case '(': return new GlslToken(GlslTokenType.left_paren, "(", line, column);
            case ')': return new GlslToken(GlslTokenType.right_paren, ")", line, column);
            case '[': return new GlslToken(GlslTokenType.left_bracket, "[", line, column);
            case ']': return new GlslToken(GlslTokenType.right_bracket, "]", line, column);
            case ';': return new GlslToken(GlslTokenType.semicolon, ";", line, column);
            case ',': return new GlslToken(GlslTokenType.comma, ",", line, column);
            case ':': return new GlslToken(GlslTokenType.colon, ":", line, column);
            case '.': return new GlslToken(GlslTokenType.dot, ".", line, column);
            case '?': return new GlslToken(GlslTokenType.question, "?", line, column);
            case '~': return new GlslToken(GlslTokenType.tilde, "~", line, column);

            case '+':
                if (match('=')) return new GlslToken(GlslTokenType.plus_equal, "+=", line, column);

                if (match('+')) return new GlslToken(GlslTokenType.plus_plus, "++", line, column);

                return new GlslToken(GlslTokenType.plus, "+", line, column);

            case '-':
                if (match('=')) return new GlslToken(GlslTokenType.minus_equal, "-=", line, column);

                if (match('-')) return new GlslToken(GlslTokenType.minus_minus, "--", line, column);

                return new GlslToken(GlslTokenType.minus, "-", line, column);

            case '*':
                if (match('=')) return new GlslToken(GlslTokenType.star_equal, "*=", line, column);

                return new GlslToken(GlslTokenType.star, "*", line, column);

            case '/':
                if (match('=')) return new GlslToken(GlslTokenType.slash_equal, "/=", line, column);

                return new GlslToken(GlslTokenType.slash, "/", line, column);

            case '%':
                if (match('=')) return new GlslToken(GlslTokenType.percent_equal, "%=", line, column);

                return new GlslToken(GlslTokenType.percent, "%", line, column);

            case '&':
                if (match('&')) return new GlslToken(GlslTokenType.logical_and, "&&", line, column);

                if (match('=')) return new GlslToken(GlslTokenType.ampersand_equal, "&=", line, column);

                return new GlslToken(GlslTokenType.ampersand, "&", line, column);

            case '|':
                if (match('|')) return new GlslToken(GlslTokenType.logical_or, "||", line, column);

                if (match('=')) return new GlslToken(GlslTokenType.pipe_equal, "|=", line, column);

                return new GlslToken(GlslTokenType.pipe, "|", line, column);

            case '^':
                if (match('=')) return new GlslToken(GlslTokenType.caret_equal, "^=", line, column);

                return new GlslToken(GlslTokenType.caret, "^", line, column);

            case '=':
                if (match('=')) return new GlslToken(GlslTokenType.equal_equal, "==", line, column);

                return new GlslToken(GlslTokenType.equal, "=", line, column);

            case '!':
                if (match('=')) return new GlslToken(GlslTokenType.not_equal, "!=", line, column);

                return new GlslToken(GlslTokenType.not, "!", line, column);

            case '<':
                if (match('<'))
                {
                    if (match('=')) return new GlslToken(GlslTokenType.left_shift_equal, "<<=", line, column);

                    return new GlslToken(GlslTokenType.left_shift, "<<", line, column);
                }

                if (match('=')) return new GlslToken(GlslTokenType.less_equal, "<=", line, column);

                return new GlslToken(GlslTokenType.less, "<", line, column);

            case '>':
                if (match('>'))
                {
                    if (match('=')) return new GlslToken(GlslTokenType.right_shift_equal, ">>=", line, column);

                    return new GlslToken(GlslTokenType.right_shift, ">>", line, column);
                }

                if (match('=')) return new GlslToken(GlslTokenType.greater_equal, ">=", line, column);

                return new GlslToken(GlslTokenType.greater, ">", line, column);

            default:
                _diagnostics?.report_error(
                    string.Empty,
                    default,
                    "GLSL001",
                    $"意外的字符 '{c}'");
                return new GlslToken(GlslTokenType.invalid, c.ToString(), line, column);
        }
    }

    #endregion

    private static bool is_identifier_start(char ch)
    {
        return char.IsLetter(ch) || ch == '_';
    }

    private static bool is_identifier_char(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static bool is_hex_digit(char ch)
    {
        return char.IsDigit(ch) || ch is >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    #region 注释与预处理

    private GlslToken scan_line_comment(int line, int column)
    {
        advance();
        advance();

        var sb = new StringBuilder("//");

        while (!is_at_end() && peek() != '\n') sb.Append(advance());

        return new GlslToken(GlslTokenType.line_comment, sb.ToString(), line, column);
    }

    private GlslToken scan_block_comment(int line, int column)
    {
        advance();
        advance();

        var sb = new StringBuilder("/*");

        while (!is_at_end())
        {
            if (peek() == '*' && peek_next() == '/')
            {
                sb.Append(advance());
                sb.Append(advance());
                break;
            }

            sb.Append(advance());
        }

        return new GlslToken(GlslTokenType.block_comment, sb.ToString(), line, column);
    }

    private GlslToken scan_preprocessor(int line, int column)
    {
        advance();

        var sb = new StringBuilder("#");

        while (!is_at_end() && peek() != '\n')
        {
            if (peek() == '\\' && peek_next() == '\n')
            {
                sb.Append(advance());
                sb.Append(advance());
                continue;
            }

            sb.Append(advance());
        }

        return new GlslToken(GlslTokenType.preprocessor, sb.ToString(), line, column);
    }

    #endregion

    #region 标识符与关键字

    private GlslToken scan_identifier(int line, int column)
    {
        var start = _position;

        while (!is_at_end() && is_identifier_char(peek())) advance();

        var text = _source[start.._position];
        var type = classify_keyword(text);

        return new GlslToken(type, text, line, column);
    }

    private static GlslTokenType classify_keyword(string text)
    {
        return text switch
        {
            "void" => GlslTokenType.@void,
            "float" => GlslTokenType.@float,
            "double" => GlslTokenType.@double,
            "int" => GlslTokenType.@int,
            "uint" => GlslTokenType.@uint,
            "bool" => GlslTokenType.@bool,
            "vec2" or "vec3" or "vec4" => GlslTokenType.vec,
            "mat2" or "mat3" or "mat4" => GlslTokenType.mat,
            "dmat2" or "dmat3" or "dmat4" => GlslTokenType.d_mat,
            "ivec2" or "ivec3" or "ivec4" => GlslTokenType.i_vec,
            "uvec2" or "uvec3" or "uvec4" => GlslTokenType.u_vec,
            "bvec2" or "bvec3" or "bvec4" => GlslTokenType.b_vec,
            "sampler2D" or "samplerCube" or "sampler2DShadow"
                or "samplerCubeShadow" or "sampler2DArray"
                or "sampler2DArrayShadow" or "sampler3D" or "sampler2DRect" => GlslTokenType.sampler,
            "isampler2D" or "isamplerCube" or "isampler2DArray"
                or "isampler3D" or "isampler2DRect" or "isampler2DShadow"
                or "isamplerCubeShadow" or "isampler2DArrayShadow" => GlslTokenType.i_sampler,
            "usampler2D" or "usamplerCube" or "usampler2DArray"
                or "usampler3D" or "usampler2DRect" or "usampler2DShadow"
                or "usamplerCubeShadow" or "usampler2DArrayShadow" => GlslTokenType.u_sampler,
            "image2D" or "image3D" or "imageCube" or "image2DArray"
                or "imageBuffer" or "image2DRect" or "iimage2D"
                or "uimage2D" or "image2DMS" => GlslTokenType.image,
            "attribute" => GlslTokenType.attribute,
            "varying" => GlslTokenType.varying,
            "uniform" => GlslTokenType.uniform,
            "in" => GlslTokenType.@in,
            "out" => GlslTokenType.@out,
            "inout" => GlslTokenType.inout,
            "const" => GlslTokenType.@const,
            "layout" => GlslTokenType.layout,
            "struct" => GlslTokenType.@struct,
            "precision" => GlslTokenType.precision,
            "highp" => GlslTokenType.highp,
            "mediump" => GlslTokenType.mediump,
            "lowp" => GlslTokenType.lowp,
            "flat" => GlslTokenType.flat,
            "smooth" => GlslTokenType.smooth,
            "noperspective" => GlslTokenType.noperspective,
            "centroid" => GlslTokenType.centroid,
            "patch" => GlslTokenType.patch,
            "sample" => GlslTokenType.sample,
            "subroutine" => GlslTokenType.subroutine,
            "coherent" => GlslTokenType.coherent,
            "volatile" => GlslTokenType.@volatile,
            "restrict" => GlslTokenType.restrict,
            "readonly" => GlslTokenType.@readonly,
            "writeonly" => GlslTokenType.writeonly,
            "buffer" => GlslTokenType.buffer,
            "if" => GlslTokenType.@if,
            "else" => GlslTokenType.@else,
            "for" => GlslTokenType.@for,
            "while" => GlslTokenType.@while,
            "do" => GlslTokenType.@do,
            "return" => GlslTokenType.@return,
            "discard" => GlslTokenType.discard,
            "break" => GlslTokenType.@break,
            "continue" => GlslTokenType.@continue,
            "switch" => GlslTokenType.@switch,
            "case" => GlslTokenType.@case,
            "default" => GlslTokenType.@default,
            "true" or "false" => GlslTokenType.bool_constant,
            _ => GlslTokenType.identifier
        };
    }

    #endregion
}