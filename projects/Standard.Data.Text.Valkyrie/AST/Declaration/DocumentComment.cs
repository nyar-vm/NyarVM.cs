namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     文档注释声明，表示 <c>///</c> 或 <c>/** */</c> 格式的文档注释
/// </summary>
/// <para>示例：</para>
/// <code>
/// 
/// 计算两点之间的欧几里得距离
/// fn distance(a: Point, b: Point) -> f32 { ... }
/// </code>
public sealed record DocumentComment : ValkyrieNode
{
    /// <summary>
    ///     文档注释的文本内容
    /// </summary>
    public string content { get; init; } = string.Empty;
}