namespace Std.Data.Text.Verse.AST;


/// <summary>

///     AST 节点基类


/// </summary>
public abstract record AstNode(TextSpan Span = default(TextSpan))
{
    
/// <summary>
    
///     节点类型
    

/// </summary>
    public abstract NodeType Kind { get; }
}