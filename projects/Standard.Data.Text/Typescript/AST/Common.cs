using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.AST;

#region 参数与属性


/// <summary>

///     鍑芥暟鍙傛暟


/// </summary>
public sealed record TsParameter(
    string name,
    TsAstNode? type_annotation,
    TsAstNode? default_value,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsParameter(string name, TsAstNode? type_annotation, TsAstNode? default_value)
        : this(name, type_annotation, default_value, default(TextSpan))
    {
    }
}


/// <summary>

///     瀵硅薄灞炴€?


/// </summary>
public sealed record TsProperty(string key, TsAstNode value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsProperty(string key, TsAstNode value)
        : this(key, value, default(TextSpan))
    {
    }
}

#endregion
