using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.AST;

#region 类型注解涓庡熀纭€绫诲瀷


/// <summary>
///     类型注解
/// </summary>
public sealed record TsTypeAnnotation(TsAstNode type, TextSpan Span)
    : TsAstNode(Span)
{
    public TsTypeAnnotation(TsAstNode type)
        : this(type, default(TextSpan))
    {
    }
}


/// <summary>

///     鍘熷绫诲瀷


/// </summary>
public sealed record TsPrimitiveType(string name, TextSpan Span)
    : TsAstNode(Span)
{
    public TsPrimitiveType(string name)
        : this(name, default(TextSpan))
    {
    }
}

#endregion

#region 复合类型


/// <summary>

///     联合类型


/// </summary>
public sealed record TsUnionType(IReadOnlyList<TsAstNode> types, TextSpan Span)
    : TsAstNode(Span)
{
    public TsUnionType(IReadOnlyList<TsAstNode> types)
        : this(types, default(TextSpan))
    {
    }
}


/// <summary>

///     数组类型


/// </summary>
public sealed record TsArrayType(TsAstNode element_type, TextSpan Span)
    : TsAstNode(Span)
{
    public TsArrayType(TsAstNode element_type)
        : this(element_type, default(TextSpan))
    {
    }
}

#endregion
