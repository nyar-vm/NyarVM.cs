using Std.Data.Text.Syntax;

namespace Std.Data.Text.Rust;


/// <summary>

///     Rust 词法单元


/// </summary>
public readonly struct RustToken
{
    public NodeKind kind { get; }
    public string text { get; }
    public int line { get; }
    public int column { get; }

    public RustToken(NodeKind kind, string text, int line = 0, int column = 0)
    {
        this.kind = kind;
        this.text = text;
        this.line = line;
        this.column = column;
    }

    public TextSpan to_source_span()
    {
        return default;
    }
}

