namespace Std.Data.Text.CSharp;


/// <summary>

///     C# 语言词法单元


/// </summary>
public readonly struct CsToken
{
    public NodeKind Kind { get; }
    public string Text { get; }

    public CsToken(NodeKind kind, string text)
    {
        Kind = kind;
        Text = text;
    }

    public TextSpan ToSourceSpan()
    {
        return default;
    }

    public override string ToString()
    {
        return $"[{Kind}] '{Text}' ";
    }
}
