using Std.Data.Text.Syntax;

namespace Std.Data.Text.Rust;


/// <summary>

///     Rust 词法节点类型


/// </summary>
public static class RustNodeKind
{
    public static readonly NodeKind unknown = 0;
    public static readonly NodeKind identifier = 1;
    public static readonly NodeKind keyword = 2;
    public static readonly NodeKind number = 3;
    public static readonly NodeKind @string = 4;
    public static readonly NodeKind @char = 5;
    public static readonly NodeKind @operator = 6;
    public static readonly NodeKind delimiter = 7;
    public static readonly NodeKind comment = 8;
    public static readonly NodeKind new_line = 9;
    public static readonly NodeKind eof = 10;
}

