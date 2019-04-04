namespace Std.Data.Text.Awsl;

/// <summary>
///     JSX 型插值节点，表示模板中的 {expression}
/// </summary>
public sealed class AwslInterpolationNode : AwslTemplateNode
{
    /// <summary>
    ///     插值表达式（花括号内的内容）
    /// </summary>
    public string expression { get; init; } = string.Empty;
}