namespace Std.Data.Text.Haskell.Syntax;

public static class HsNodeKind
{
    public static readonly NodeKind Eof = 0;
    public static readonly NodeKind Identifier = 1;
    public static readonly NodeKind Keyword = 2;
    public static readonly NodeKind Number = 3;
    public static readonly NodeKind String = 4;
    public static readonly NodeKind Char = 5;
    public static readonly NodeKind Operator = 6;
    public static readonly NodeKind Delimiter = 7;
    public static readonly NodeKind Punctuation = 8;
    public static readonly NodeKind Comment = 9;
    public static readonly NodeKind Qualified = 10;
}
