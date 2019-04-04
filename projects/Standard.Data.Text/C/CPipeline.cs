namespace Std.Data.Text.C;


/// <summary>
///     C 语言解析管道（词法 + 语法）
/// </summary>
public sealed class CPipeline : IStringParser<CAstNode>
{
    private readonly CLexer _lexer;
    private readonly CParser _parser;

    
/// <summary>
    
///     创建 C 解析管道
    

/// </summary>
    public CPipeline()
    {
        _lexer = new CLexer();
        _parser = new CParser();
    }

    
/// <summary>
    
///     解析 C 源代码为 AST
    

/// </summary>
    public CAstNode Parse(string source)
    {
        var tokens = _lexer.TokenizeAsCTokens(source);
        return _parser.Parse(tokens);
    }
}