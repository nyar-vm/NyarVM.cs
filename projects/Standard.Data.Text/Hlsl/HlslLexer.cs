using System.Text;

namespace Std.Data.Text.Hlsl;

public sealed class HlslLexer
{
    private int _column;
    private DiagnosticSink? _diagnostics;
    private int _line;
    private int _position;
    private string _source = string.Empty;

    public IReadOnlyList<HlslToken> Tokenize(string source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _diagnostics = diagnostics;

        var tokens = new List<HlslToken>();

        while (!IsAtEnd())
        {
            SkipWhitespace();

            if (IsAtEnd())
            {
                break;
            }

            var token = ScanToken();

            if (token.type != HlslTokenType.Invalid)
            {
                tokens.Add(token);
            }
        }

        tokens.Add(new HlslToken(HlslTokenType.EndOfFile, string.Empty, _line, _column));
        return tokens;
    }

    private bool IsAtEnd()
    {
        return _position >= _source.Length;
    }

    private char Peek()
    {
        return IsAtEnd() ? '\0' : _source[_position];
    }

    private char PeekNext()
    {
        return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
    }

    private char Advance()
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

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_position] != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Peek())) Advance();
    }

    private HlslToken ScanToken()
    {
        var line = _line;
        var column = _column;
        var c = Peek();

        if (c == '/' && PeekNext() == '/')
        {
            return ScanLineComment(line, column);
        }

        if (c == '/' && PeekNext() == '*')
        {
            return ScanBlockComment(line, column);
        }

        if (c == '#')
        {
            return ScanPreprocessor(line, column);
        }

        if (c == '"')
        {
            return ScanString(line, column);
        }

        if (char.IsDigit(c) || (c == '.' && char.IsDigit(PeekNext())))
        {
            return ScanNumber(line, column);
        }

        if (IsIdentifierStart(c))
        {
            return ScanIdentifier(line, column);
        }

        return ScanSymbol(line, column);
    }

    #region 注释与预处理

    private HlslToken ScanLineComment(int line, int column)
    {
        Advance();
        Advance();

        var sb = new StringBuilder("//");

        while (!IsAtEnd() && Peek() != '\n')
        {
            sb.Append(Advance());
        }

        return new HlslToken(HlslTokenType.LineComment, sb.ToString(), line, column);
    }

    private HlslToken ScanBlockComment(int line, int column)
    {
        Advance();
        Advance();

        var sb = new StringBuilder("/*");

        while (!IsAtEnd())
        {
            if (Peek() == '*' && PeekNext() == '/')
            {
                sb.Append(Advance());
                sb.Append(Advance());
                break;
            }

            sb.Append(Advance());
        }

        return new HlslToken(HlslTokenType.BlockComment, sb.ToString(), line, column);
    }

    private HlslToken ScanPreprocessor(int line, int column)
    {
        Advance();

        var sb = new StringBuilder("#");

        while (!IsAtEnd() && Peek() != '\n')
        {
            sb.Append(Advance());
        }

        return new HlslToken(HlslTokenType.Preprocessor, sb.ToString(), line, column);
    }

    #endregion

    #region 字符串

    private HlslToken ScanString(int line, int column)
    {
        Advance();

        var sb = new StringBuilder();

        while (!IsAtEnd() && Peek() != '"')
        {
            if (Peek() == '\\' && !IsAtEnd())
            {
                Advance();
                if (!IsAtEnd())
                {
                    sb.Append(Advance());
                }

                continue;
            }

            sb.Append(Advance());
        }

        if (!IsAtEnd())
        {
            Advance();
        }

        return new HlslToken(HlslTokenType.StringLiteral, sb.ToString(), line, column);
    }

    #endregion

    #region 数字

    private HlslToken ScanNumber(int line, int column)
    {
        var start = _position;

        if (Peek() == '0' && PeekNext() is 'x' or 'X')
        {
            Advance();
            Advance();

            while (!IsAtEnd() && IsHexDigit(Peek())) Advance();

            if (!IsAtEnd() && char.ToLower(Peek()) == 'l')
            {
                Advance();
                return new HlslToken(HlslTokenType.IntLiteral, _source[start.._position], line, column);
            }

            return new HlslToken(HlslTokenType.IntLiteral, _source[start.._position], line, column);
        }

        var isFloat = false;

        while (!IsAtEnd() && char.IsDigit(Peek())) Advance();

        if (!IsAtEnd() && Peek() == '.')
        {
            isFloat = true;
            Advance();

            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            isFloat = true;
            Advance();

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                Advance();
            }

            while (!IsAtEnd() && char.IsDigit(Peek())) Advance();
        }

        if (!IsAtEnd())
        {
            var suffix = char.ToLower(Peek());

            if (suffix is 'f' or 'h')
            {
                isFloat = true;
                Advance();
            }
            else if (suffix == 'l')
            {
                Advance();
                return new HlslToken(HlslTokenType.DoubleLiteral, _source[start.._position], line, column);
            }
            else if (suffix == 'u')
            {
                Advance();
                return new HlslToken(HlslTokenType.UintLiteral, _source[start.._position], line, column);
            }
        }

        return new HlslToken(
            isFloat ? HlslTokenType.FloatLiteral : HlslTokenType.IntLiteral,
            _source[start.._position], line, column);
    }

    #endregion

    #region 标识符与关键字

    private HlslToken ScanIdentifier(int line, int column)
    {
        var start = _position;

        while (!IsAtEnd() && IsIdentifierChar(Peek())) Advance();

        var text = _source[start.._position];
        var type = ClassifyKeyword(text);

        return new HlslToken(type, text, line, column);
    }

    private static HlslTokenType ClassifyKeyword(string text)
    {
        return text switch
        {
            "void" => HlslTokenType.Void,
            "bool" => HlslTokenType.Bool,
            "int" => HlslTokenType.Int,
            "uint" => HlslTokenType.Uint,
            "float" => HlslTokenType.Float,
            "half" => HlslTokenType.Half,
            "double" => HlslTokenType.Double,
            "min16float" => HlslTokenType.Min16Float,
            "min10float" => HlslTokenType.Min10Float,
            "min16int" => HlslTokenType.Min16Int,
            "min12int" => HlslTokenType.Min12Int,
            "min16uint" => HlslTokenType.Min16Uint,
            "vector" => HlslTokenType.Vector,
            "matrix" => HlslTokenType.Matrix,
            "float1" or "float2" or "float3" or "float4" => HlslTokenType.FloatType,
            "int1" or "int2" or "int3" or "int4" => HlslTokenType.IntType,
            "uint1" or "uint2" or "uint3" or "uint4" => HlslTokenType.UintType,
            "bool1" or "bool2" or "bool3" or "bool4" => HlslTokenType.BoolType,
            "half1" or "half2" or "half3" or "half4" => HlslTokenType.HalfType,
            "double1" or "double2" or "double3" or "double4" => HlslTokenType.DoubleType,
            "float1x1" or "float1x2" or "float1x3" or "float1x4"
                or "float2x1" or "float2x2" or "float2x3" or "float2x4"
                or "float3x1" or "float3x2" or "float3x3" or "float3x4"
                or "float4x1" or "float4x2" or "float4x3" or "float4x4" => HlslTokenType.FloatType,
            "SamplerState" or "sampler" => HlslTokenType.Sampler,
            "SamplerComparisonState" or "SamplerComparisonState" => HlslTokenType.SamplerComparison,
            "Texture2D" => HlslTokenType.Texture2D,
            "Texture3D" => HlslTokenType.Texture3D,
            "TextureCube" => HlslTokenType.TextureCube,
            "Texture2DArray" => HlslTokenType.Texture2DArray,
            "TextureCubeArray" => HlslTokenType.TextureCubeArray,
            "Texture2DMS" => HlslTokenType.Texture2DMS,
            "Texture2DMSArray" => HlslTokenType.Texture2DMSArray,
            "RWTexture1D" => HlslTokenType.RWTexture1D,
            "RWTexture2D" => HlslTokenType.RWTexture2D,
            "RWTexture3D" => HlslTokenType.RWTexture3D,
            "RWBuffer" => HlslTokenType.RWBuffer,
            "ByteAddressBuffer" => HlslTokenType.ByteAddressBuffer,
            "RWByteAddressBuffer" => HlslTokenType.RWByteAddressBuffer,
            "StructuredBuffer" => HlslTokenType.StructuredBuffer,
            "RWStructuredBuffer" => HlslTokenType.RWStructuredBuffer,
            "AppendStructuredBuffer" => HlslTokenType.AppendStructuredBuffer,
            "ConsumeStructuredBuffer" => HlslTokenType.ConsumeStructuredBuffer,
            "InputPatch" => HlslTokenType.InputPatch,
            "OutputPatch" => HlslTokenType.OutputPatch,
            "cbuffer" => HlslTokenType.CBuffer,
            "tbuffer" => HlslTokenType.TBuffer,
            "register" => HlslTokenType.Register,
            "packoffset" => HlslTokenType.PackOffset,
            "in" => HlslTokenType.In,
            "out" => HlslTokenType.Out,
            "inout" => HlslTokenType.Inout,
            "uniform" => HlslTokenType.Uniform,
            "const" => HlslTokenType.Const,
            "static" => HlslTokenType.Static,
            "shared" => HlslTokenType.Shared,
            "groupshared" => HlslTokenType.GroupShared,
            "volatile" => HlslTokenType.Volatile,
            "extern" => HlslTokenType.Extern,
            "precise" => HlslTokenType.Precise,
            "inline" => HlslTokenType.Inline,
            "nointerpolation" => HlslTokenType.Nointerpolation,
            "noperspective" => HlslTokenType.Noperspective,
            "linear" => HlslTokenType.Linear,
            "centroid" => HlslTokenType.Centroid,
            "sample" => HlslTokenType.Sample,
            "row_major" => HlslTokenType.RowMajor,
            "column_major" => HlslTokenType.ColumnMajor,
            "snorm" => HlslTokenType.Snorm,
            "unorm" => HlslTokenType.Unorm,
            "struct" => HlslTokenType.Struct,
            "class" => HlslTokenType.Class,
            "interface" => HlslTokenType.Interface,
            "namespace" => HlslTokenType.Namespace,
            "typedef" => HlslTokenType.Typedef,
            "template" => HlslTokenType.Template,
            "enum" => HlslTokenType.Enum,
            "if" => HlslTokenType.If,
            "else" => HlslTokenType.Else,
            "for" => HlslTokenType.For,
            "while" => HlslTokenType.While,
            "do" => HlslTokenType.Do,
            "return" => HlslTokenType.Return,
            "break" => HlslTokenType.Break,
            "continue" => HlslTokenType.Continue,
            "switch" => HlslTokenType.Switch,
            "case" => HlslTokenType.Case,
            "default" => HlslTokenType.Default,
            "discard" => HlslTokenType.Discard,
            "true" or "false" => HlslTokenType.BoolLiteral,
            _ => HlslTokenType.Identifier
        };
    }

    #endregion

    #region 运算符与符号

    private HlslToken ScanSymbol(int line, int column)
    {
        var c = Advance();

        switch (c)
        {
            case '{': return new HlslToken(HlslTokenType.LeftBrace, "{", line, column);
            case '}': return new HlslToken(HlslTokenType.RightBrace, "}", line, column);
            case '(': return new HlslToken(HlslTokenType.LeftParen, "(", line, column);
            case ')': return new HlslToken(HlslTokenType.RightParen, ")", line, column);
            case '[': return new HlslToken(HlslTokenType.LeftBracket, "[", line, column);
            case ']': return new HlslToken(HlslTokenType.RightBracket, "]", line, column);
            case ';': return new HlslToken(HlslTokenType.Semicolon, ";", line, column);
            case ',': return new HlslToken(HlslTokenType.Comma, ",", line, column);
            case ':': return new HlslToken(HlslTokenType.Colon, ":", line, column);
            case '.': return new HlslToken(HlslTokenType.Dot, ".", line, column);
            case '?': return new HlslToken(HlslTokenType.Question, "?", line, column);
            case '~': return new HlslToken(HlslTokenType.Tilde, "~", line, column);

            case '+':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.PlusEqual, "+=", line, column);
                }

                if (Match('+'))
                {
                    return new HlslToken(HlslTokenType.PlusPlus, "++", line, column);
                }

                return new HlslToken(HlslTokenType.Plus, "+", line, column);

            case '-':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.MinusEqual, "-=", line, column);
                }

                if (Match('-'))
                {
                    return new HlslToken(HlslTokenType.MinusMinus, "--", line, column);
                }

                return new HlslToken(HlslTokenType.Minus, "-", line, column);

            case '*':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.StarEqual, "*=", line, column);
                }

                return new HlslToken(HlslTokenType.Star, "*", line, column);

            case '/':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.SlashEqual, "/=", line, column);
                }

                return new HlslToken(HlslTokenType.Slash, "/", line, column);

            case '%':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.PercentEqual, "%=", line, column);
                }

                return new HlslToken(HlslTokenType.Percent, "%", line, column);

            case '&':
                if (Match('&'))
                {
                    return new HlslToken(HlslTokenType.LogicalAnd, "&&", line, column);
                }

                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.AmpersandEqual, "&=", line, column);
                }

                return new HlslToken(HlslTokenType.Ampersand, "&", line, column);

            case '|':
                if (Match('|'))
                {
                    return new HlslToken(HlslTokenType.LogicalOr, "||", line, column);
                }

                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.PipeEqual, "|=", line, column);
                }

                return new HlslToken(HlslTokenType.Pipe, "|", line, column);

            case '^':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.CaretEqual, "^=", line, column);
                }

                return new HlslToken(HlslTokenType.Caret, "^", line, column);

            case '=':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.EqualEqual, "==", line, column);
                }

                return new HlslToken(HlslTokenType.Equal, "=", line, column);

            case '!':
                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.NotEqual, "!=", line, column);
                }

                return new HlslToken(HlslTokenType.Not, "!", line, column);

            case '<':
                if (Match('<'))
                {
                    if (Match('='))
                    {
                        return new HlslToken(HlslTokenType.LeftShiftEqual, "<<=", line, column);
                    }

                    return new HlslToken(HlslTokenType.LeftShift, "<<", line, column);
                }

                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.LessEqual, "<=", line, column);
                }

                return new HlslToken(HlslTokenType.Less, "<", line, column);

            case '>':
                if (Match('>'))
                {
                    if (Match('='))
                    {
                        return new HlslToken(HlslTokenType.RightShiftEqual, ">>=", line, column);
                    }

                    return new HlslToken(HlslTokenType.RightShift, ">>", line, column);
                }

                if (Match('='))
                {
                    return new HlslToken(HlslTokenType.GreaterEqual, ">=", line, column);
                }

                return new HlslToken(HlslTokenType.Greater, ">", line, column);

            default:
                _diagnostics?.AddError(
                    string.Empty,
                    default,
                    "HLSL001",
                    $"意外的字符 '{c}'");
                return new HlslToken(HlslTokenType.Invalid, c.ToString(), line, column);
        }
    }

    #endregion

    private static bool IsIdentifierStart(char ch) => char.IsLetter(ch) || ch == '_';

    private static bool IsIdentifierChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

    private static bool IsHexDigit(char ch) => char.IsDigit(ch) || ch is >= 'a' and <= 'f' or >= 'A' and <= 'F';
}
