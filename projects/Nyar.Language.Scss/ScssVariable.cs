namespace Nyar.Language.Scss;

/// <summary>
///     SCSS 变量定义
/// </summary>
public sealed class ScssVariable
{
    /// <summary>
    ///     变量名（含 $ 前缀，如 "$primary-color"）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     变量值
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    ///     是否有默认标记（!default）
    /// </summary>
    public bool IsDefault { get; init; }
}