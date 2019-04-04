using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;


/// <summary>

///     C 语言词法单元


/// </summary>
public readonly struct CToken
{
    public NodeKind kind { get; }
    public string text { get; }

    public CToken(NodeKind kind, string text)
    {
        this.kind = kind;
        this.text = text;
    }

    public TextSpan to_source_span()
    {
        return default;
    }

    public override string ToString()
    {
        return $"[{kind}] '{text}' ";
    }
}
