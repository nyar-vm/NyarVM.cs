namespace Std.Data.Text.Verse.AST;


/// <summary>

///     二元表达式


/// </summary>
public sealed record BinaryExpr : AstNode
{
    public BinaryExpr(AstNode left, string op, AstNode right, TextSpan span = default(TextSpan))
        : base(span)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override NodeType Kind => NodeType.BinaryExpr;

    public AstNode Left { get; }
    public string Operator { get; }
    public AstNode Right { get; }
}


/// <summary>

///     一元表达式


/// </summary>
public sealed record UnaryExpr : AstNode
{
    public UnaryExpr(string op, AstNode operand, TextSpan span = default(TextSpan))
        : base(span)
    {
        Operator = op;
        Operand = operand;
    }

    public override NodeType Kind => NodeType.UnaryExpr;

    public string Operator { get; }
    public AstNode Operand { get; }
}


/// <summary>

///     字面量类型


/// </summary>
public enum LiteralType
{
    Number,
    String,
    Boolean,
    Null
}


/// <summary>

///     字面量表达式


/// </summary>
public sealed record LiteralExpr : AstNode
{
    public LiteralExpr(LiteralType kind, string value, TextSpan span = default(TextSpan))
        : base(span)
    {
        LiteralKind = kind;
        Value = value;
    }

    public override NodeType Kind => NodeType.LiteralExpr;

    public LiteralType LiteralKind { get; }
    public string Value { get; }
}


/// <summary>

///     标识符表达式


/// </summary>
public sealed record IdentifierExpr : AstNode
{
    public IdentifierExpr(string name, TextSpan span = default(TextSpan))
        : base(span)
    {
        Name = name;
    }

    public override NodeType Kind => NodeType.IdentifierExpr;

    public string Name { get; }
}


/// <summary>

///     成员访问表达式


/// </summary>
public sealed record MemberAccessExpr : AstNode
{
    public MemberAccessExpr(AstNode obj, string member, TextSpan span = default(TextSpan))
        : base(span)
    {
        Object = obj;
        Member = member;
    }

    public override NodeType Kind => NodeType.MemberAccessExpr;

    public AstNode Object { get; }
    public string Member { get; }
}


/// <summary>

///     赋值表达式


/// </summary>
public sealed record AssignmentExpr : AstNode
{
    public AssignmentExpr(AstNode target, string op, AstNode value, TextSpan span = default(TextSpan))
        : base(span)
    {
        Target = target;
        Operator = op;
        Value = value;
    }

    public override NodeType Kind => NodeType.AssignmentExpr;

    public AstNode Target { get; }
    public string Operator { get; }
    public AstNode Value { get; }
}