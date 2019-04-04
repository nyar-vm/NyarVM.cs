namespace Std.Data.Text.Regex;


/// <summary>

///     极简正则表达式解析器

///     支持：字面量、连接、|、*、+、?、括号


/// </summary>
public sealed class RegexParser
{
    private readonly string _pattern;
    private int _position;

    
/// <summary>
    
///     创建正则表达式解析器
    

/// </summary>
    
/// <param name="pattern">正则表达式模式字符串。</param>
    public RegexParser(string pattern)
    {
        _pattern = pattern;
        _position = 0;
    }

    
/// <summary>
    
///     解析正则表达式为 AST
    

/// </summary>
    
/// <returns>解析后的 AST 根节点。</returns>
    public RegexAstNode Parse()
    {
        if (string.IsNullOrEmpty(_pattern))
        {
            return new RegexAstEmpty();
        }

        var result = ParseAlt();
        return result;
    }

    
/// <summary>
    
///     解析选择表达式：a|b
    

/// </summary>
    private RegexAstNode ParseAlt()
    {
        var left = ParseConcat();

        while (Peek() == '|')
        {
            Advance();
            var right = ParseConcat();
            left = new RegexAstAlt(left, right);
        }

        return left;
    }

    
/// <summary>
    
///     解析连接序列：ab
    

/// </summary>
    private RegexAstNode ParseConcat()
    {
        var children = new List<RegexAstNode>();

        while (_position < _pattern.Length && Peek() != '|' && Peek() != ')')
        {
            children.Add(ParsePostfix());
        }

        if (children.Count == 0)
        {
            return new RegexAstEmpty();
        }

        if (children.Count == 1)
        {
            return children[0];
        }

        return new RegexAstConcat(children);
    }

    
/// <summary>
    
///     解析后缀操作：* + ?
    

/// </summary>
    private RegexAstNode ParsePostfix()
    {
        var node = ParsePrimary();

        while (true)
        {
            var c = Peek();
            if (c == '*')
            {
                Advance();
                node = new RegexAstStar(node);
            }
            else if (c == '+')
            {
                Advance();
                node = new RegexAstPlus(node);
            }
            else if (c == '?')
            {
                Advance();
                node = new RegexAstQuestion(node);
            }
            else
            {
                break;
            }
        }

        return node;
    }

    
/// <summary>
    
///     解析基本元素：字面量、括号
    

/// </summary>
    private RegexAstNode ParsePrimary()
    {
        if (_position >= _pattern.Length)
        {
            return new RegexAstEmpty();
        }

        var c = Advance();

        if (c == '(')
        {
            var node = ParseAlt();
            if (Peek() == ')')
            {
                Advance();
            }

            return node;
        }

        if (c == '.')
        {
            return new RegexAstCharClass(".");
        }

        if (c == '[')
        {
            return ParseCharClass();
        }

        if (c == '\\' && _position < _pattern.Length)
        {
            c = Advance();
        }

        return new RegexAstLiteral(c);
    }

    
/// <summary>
    
///     解析字符类：[abc]
    

/// </summary>
    private RegexAstNode ParseCharClass()
    {
        var chars = new List<char>();

        while (_position < _pattern.Length && Peek() != ']')
        {
            chars.Add(Advance());
        }

        if (Peek() == ']')
        {
            Advance();
        }

        return new RegexAstCharClass(new string(chars.ToArray()));
    }

    private char Peek()
    {
        return _position < _pattern.Length ? _pattern[_position] : '\0';
    }

    private char Advance()
    {
        return _position < _pattern.Length ? _pattern[_position++] : '\0';
    }
}
