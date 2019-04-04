namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Resume 语句，在异步/协程上下文中恢复执行
/// </summary>
/// <para>示例：</para>
/// <code>
/// resume 42;
/// resume;
/// </code>
public sealed record ResumeStatement : ValkyrieNode
{
    /// <summary>
    ///     恢复时返回的值，可为 <c>null</c>
    /// </summary>
    public ValkyrieNode? value { get; init; }
}