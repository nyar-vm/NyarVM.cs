using System.Collections.Immutable;

namespace Nyar.VM.TextVM;

/// <summary>
/// 文本编码类型，编译期静态已知。
/// </summary>
public enum TextEncoding : byte
{
    /// <summary>
    /// ASCII 编码（单字节）。
    /// </summary>
    Ascii = 0,

    /// <summary>
    /// UTF-8 编码。
    /// </summary>
    Utf8 = 1,

    /// <summary>
    /// UTF-16 小端编码。
    /// </summary>
    Utf16Le = 2,

    /// <summary>
    /// UTF-16 大端编码。
    /// </summary>
    Utf16Be = 3,
}

/// <summary>
/// 字符范围。
/// </summary>
public readonly struct CharRange
{
    /// <summary>范围起始（含）。</summary>
    public Char Lo { get; }

    /// <summary>范围结束（含）。</summary>
    public Char Hi { get; }

    /// <summary>
    /// 创建字符范围。
    /// </summary>
    public CharRange(Char lo, Char hi)
    {
        Lo = lo;
        Hi = hi;
    }
}

/// <summary>
/// 回溯虚拟机指令类型。
/// </summary>
public enum InstKind : byte
{
    /// <summary>匹配指定字符。</summary>
    Char,
    /// <summary>匹配任意码点。</summary>
    Any,
    /// <summary>字符类匹配。</summary>
    CharClass,
    /// <summary>分支：先尝试左分支，失败则尝试右分支。</summary>
    Split,
    /// <summary>保存当前字节偏移到捕获槽。</summary>
    Save,
    /// <summary>无条件跳转。</summary>
    Jump,
    /// <summary>匹配成功。</summary>
    Match,
    /// <summary>匹配失败。</summary>
    Fail,
    /// <summary>反向引用：匹配之前捕获的相同内容。</summary>
    Backref,
    /// <summary>锚点断言。</summary>
    Anchor,
}

/// <summary>
/// 回溯虚拟机指令。
/// </summary>
public readonly struct Inst
{
    /// <summary>指令类型。</summary>
    public InstKind Kind { get; }

    /// <summary>Char 指令的字符值 / Anchor 的锚点类型。</summary>
    public Int32 IntArg1 { get; }

    /// <summary>Split 的右分支 / Jump 的目标。</summary>
    public Int32 IntArg2 { get; }

    /// <summary>字符类指令的区间数组。</summary>
    public ImmutableArray<CharRange> Ranges { get; }

    /// <summary>字符类是否取反。</summary>
    public Boolean Negated { get; }

    private Inst(InstKind kind, Int32 arg1 = 0, Int32 arg2 = 0,
        ImmutableArray<CharRange> ranges = default, Boolean negated = false)
    {
        Kind = kind;
        IntArg1 = arg1;
        IntArg2 = arg2;
        Ranges = ranges.IsDefault ? ImmutableArray<CharRange>.Empty : ranges;
        Negated = negated;
    }

    /// <summary>创建字符匹配指令。</summary>
    public static Inst CreateChar(Char c) => new(InstKind.Char, c);

    /// <summary>任意字符匹配指令。</summary>
    public static Inst Any => new(InstKind.Any);

    /// <summary>创建字符类匹配指令。</summary>
    public static Inst CreateCharClass(ImmutableArray<CharRange> ranges, Boolean negated)
        => new(InstKind.CharClass, ranges: ranges, negated: negated);

    /// <summary>创建分支指令。</summary>
    public static Inst CreateSplit(Int32 left, Int32 right) => new(InstKind.Split, left, right);

    /// <summary>创建保存指令。</summary>
    public static Inst CreateSave(Int32 slot) => new(InstKind.Save, slot);

    /// <summary>创建跳转指令。</summary>
    public static Inst CreateJump(Int32 target) => new(InstKind.Jump, target);

    /// <summary>匹配成功指令。</summary>
    public static Inst Match => new(InstKind.Match);

    /// <summary>匹配失败指令。</summary>
    public static Inst Fail => new(InstKind.Fail);

    /// <summary>创建反向引用指令。</summary>
    public static Inst CreateBackref(Int32 groupId) => new(InstKind.Backref, groupId);

    /// <summary>创建锚点断言指令。</summary>
    public static Inst CreateAnchor(Int32 anchorKind) => new(InstKind.Anchor, anchorKind);

    /// <inheritdoc />
    public override String ToString() => Kind switch
    {
        InstKind.Char => $"Char('{(Char)IntArg1}')",
        InstKind.Any => "Any",
        InstKind.CharClass => $"CharClass(neg={Negated},ranges={Ranges.Length})",
        InstKind.Split => $"Split({IntArg1},{IntArg2})",
        InstKind.Save => $"Save({IntArg1})",
        InstKind.Jump => $"Jump({IntArg1})",
        InstKind.Match => "Match",
        InstKind.Fail => "Fail",
        InstKind.Backref => $"Backref({IntArg1})",
        InstKind.Anchor => $"Anchor({IntArg1})",
        _ => Kind.ToString(),
    };
}
