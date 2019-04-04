namespace Std.Data.Text.Verse;


/// <summary>

///     Verse 词法节点类型


/// </summary>
public static class VerseNodeKind
{
    public static readonly NodeKind Unknown = 0;
    public static readonly NodeKind Keyword = 1;
    public static readonly NodeKind Identifier = 2;
    public static readonly NodeKind Number = 3;
    public static readonly NodeKind String = 4;
    public static readonly NodeKind Literal = 5;
    public static readonly NodeKind Operator = 6;
    public static readonly NodeKind Punctuation = 7;
    public static readonly NodeKind Delimiter = 8;
    public static readonly NodeKind CommandPrefix = 9;
    public static readonly NodeKind LabelMarker = 10;
    public static readonly NodeKind ChoiceMarker = 11;
    public static readonly NodeKind Comment = 12;
    public static readonly NodeKind Eof = 13;
}
