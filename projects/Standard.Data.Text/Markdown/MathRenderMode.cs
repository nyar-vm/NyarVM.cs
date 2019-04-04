namespace Std.Data.Text.Markdown;

/// <summary>
///     数学公式渲染模式
/// </summary>
public enum MathRenderMode
{
    /// <summary>
    ///     KaTeX 渲染
    /// </summary>
    ka_te_x,


    /// <summary>
    ///     MathJax 渲染
    /// </summary>
    math_jax,


    /// <summary>
    ///     原始 LaTeX 输出
    /// </summary>
    raw
}