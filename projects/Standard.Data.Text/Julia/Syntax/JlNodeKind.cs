using Std.Data.Text.Syntax;

namespace Std.Data.Text.Julia.Syntax;

public static class JlNodeKind
{
    public static readonly NodeKind eof = 0;
    public static readonly NodeKind identifier = 1;
    public static readonly NodeKind keyword = 2;
    public static readonly NodeKind number = 3;
    public static readonly NodeKind @string = 4;
    public static readonly NodeKind @char = 5;
    public static readonly NodeKind @operator = 6;
    public static readonly NodeKind delimiter = 7;
    public static readonly NodeKind punctuation = 8;
    public static readonly NodeKind comment = 9;
    public static readonly NodeKind macro_name = 10;
    public static readonly NodeKind command_type = 11;
    public static readonly NodeKind symbol = 12;
}
