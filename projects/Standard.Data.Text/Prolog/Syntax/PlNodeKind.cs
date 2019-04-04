namespace Std.Data.Text.Prolog.Syntax;

public static class PlNodeKind
{
    public static readonly NodeKind Eof = 0;
    public static readonly NodeKind Atom = 1;
    public static readonly NodeKind Variable = 2;
    public static readonly NodeKind Number = 3;
    public static readonly NodeKind String = 4;
    public static readonly NodeKind Operator = 5;
    public static readonly NodeKind Delimiter = 6;
    public static readonly NodeKind Punctuation = 7;
    public static readonly NodeKind Comment = 8;
    public static readonly NodeKind Functor = 9;
}
