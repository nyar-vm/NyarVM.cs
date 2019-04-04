using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     展开表达式，如 <c>..expr</c> 或 <c>...expr</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// ..my_list        // 展开为列表元素迭代器
/// ...my_dict       // 展开为字典值迭代器
/// ..get_items()    // 对函数返回值展开
/// </code>
public sealed record TermSpreadExpression : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public TermSpreadExpression()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public TermSpreadExpression(ValkyrieNode target, bool isDoubleDot, TextSpan span)
    {
        this.target = target;
        is_double_dot = isDoubleDot;
        this.span = span;
    }

    /// <summary>
    ///     展开目标表达式
    /// </summary>
    public ValkyrieNode target { get; init; } = null!;

    /// <summary>
    ///     是否为两点展开（<c>..</c>），<c>false</c> 则为三点展开（<c>...</c>）
    /// </summary>
    public bool is_double_dot { get; init; }

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.spread_expr;
}