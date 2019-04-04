namespace Std.Data.Text.Rust;


/// <summary>

///     Rust 璇█瑙ｆ瀽绠￠亾锛堣瘝娉?+ 璇硶锛?


/// </summary>
public sealed class RustPipeline : IStringParser<RustAstNode>
{
    private readonly RustLexer _lexer;
    private readonly RustParser _parser;

    
/// <summary>
    
///     创建 Rust 解析管道
    

/// </summary>
    public RustPipeline()
    {
        _lexer = new RustLexer();
        _parser = new RustParser();
    }

    
/// <summary>
    
///     解析 Rust 源代码为 AST
    

/// </summary>
    public RustAstNode Parse(string source)
    {
        var tokens = _lexer.TokenizeAsRustTokens(source);
        return _parser.Parse(tokens);
    }
}
