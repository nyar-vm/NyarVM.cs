namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     元 for 循环语句，在 meta 代码中进行数字范围迭代
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% loop pattern in expression %>
///         body
///         <% end loop %>
/// </code>
public sealed record TemplateLoopNode : ValkyrieNode
{
    /// <summary>
    ///     带参数的构造函数
    /// </summary>
    /// <param name="variableName">循环变量名。</param>
    /// <param name="rangeStart">范围起始值。</param>
    /// <param name="rangeEnd">范围结束值。</param>
    /// <param name="body">循环体 AST。</param>
    public TemplateLoopNode(string variableName, string rangeStart, string rangeEnd, IReadOnlyList<ValkyrieNode> body)
    {
        variable_name = variableName;
        range_start = rangeStart;
        range_end = rangeEnd;
        this.body = body;
    }

    /// <summary>
    ///     循环变量名
    /// </summary>
    public string variable_name { get; }

    /// <summary>
    ///     范围起始值
    /// </summary>
    public string range_start { get; }

    /// <summary>
    ///     范围结束值
    /// </summary>
    public string range_end { get; }

    /// <summary>
    ///     循环体代码 AST
    /// </summary>
    public IReadOnlyList<ValkyrieNode> body { get; }
}