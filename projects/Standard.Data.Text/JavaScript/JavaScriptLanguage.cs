using Std.Data.Text.JavaScript.Lexer;
using Std.Data.Text.JavaScript.Parser;
using Std.Data.Text.Syntax;
using Std.Data.Text.Typescript.AST;

namespace Std.Data.Text.JavaScript;


/// <summary>

///     JavaScript 璇█鍓嶇锛屽皝瑁呰瘝娉曞垎鏋愩€佽娉曞垎鏋愮绾?

///     澶嶇敤 Oak.Typescript 鍩虹璁炬柦锛孞avaScript 鏄?TypeScript 鐨勫瓙闆?


/// </summary>
public sealed class JavaScriptLanguage : Language
{
    private readonly JsLexer _lexer = new();
    private readonly JsParser _parser = new();

    public override string name => "JavaScript";

    
/// <summary>
    
///     将 JavaScript 源代码解析为 AST
    

/// </summary>
    
/// <param name="source">JavaScript 婧愪唬鐮併€?/param>
    
/// <returns>AST 根节点（TsCompilationUnit）。</returns>
    public TsAstNode parse(string source)
    {
        var tokens = _lexer.tokenize(source);
        return _parser.parse(tokens);
    }
}
