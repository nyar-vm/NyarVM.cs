using Std.Data.Text.Syntax;

namespace Oak.Wat;

/// <summary>
///     WAT 语法节点类型
/// </summary>
public static class WatNodeKind
{
    public static readonly NodeKind Unknown = 0;

    #region 顶层结构

    public static readonly NodeKind Module = 1;
    public static readonly NodeKind Import = 2;
    public static readonly NodeKind Export = 3;
    public static readonly NodeKind Function = 4;
    public static readonly NodeKind Memory = 5;
    public static readonly NodeKind Table = 6;
    public static readonly NodeKind Global = 7;
    public static readonly NodeKind Data = 8;
    public static readonly NodeKind Type = 9;

    #endregion

    #region 函数体

    public static readonly NodeKind Instruction = 10;
    public static readonly NodeKind Block = 11;
    public static readonly NodeKind Loop = 12;
    public static readonly NodeKind If = 13;

    #endregion

    #region Token 类型

    public static readonly NodeKind Keyword = 20;
    public static readonly NodeKind Opcode = 21;
    public static readonly NodeKind Identifier = 22;
    public static readonly NodeKind Number = 23;
    public static readonly NodeKind StringLiteral = 24;
    public static readonly NodeKind Comment = 25;
    public static readonly NodeKind Punctuation = 26;
    public static readonly NodeKind Eof = 27;

    #endregion
}