namespace Std.Data.Text.CSharp;


/// <summary>

///     C# 语言解析管道（词法 + 语法）


/// </summary>
public sealed class CsPipeline : IStringParser<CsAstNode>
{
    private readonly CsLexer _lexer;
    private readonly CsParser _parser;

    public CsPipeline()
    {
        _lexer = new CsLexer();
        _parser = new CsParser();
    }

    
/// <summary>
    
///     解析 C# 源代码为 AST
    

/// </summary>
    public CsAstNode Parse(string source)
    {
        var tokens = _lexer.TokenizeAsCsTokens(source);
        return _parser.Parse(tokens);
    }
}
