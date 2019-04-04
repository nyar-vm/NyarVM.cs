namespace Std.Data.Text.Scss;

/// <summary>
///     样式声明
/// </summary>
public sealed class StyleDeclaration
{
    public StyleDeclaration(string property, string value, int specificity = 0, string? important = null)
    {
        this.property = property;
        this.value = value;
        this.specificity = specificity;
        this.important = important;
    }


    /// <summary>
    ///     CSS 属性名
    /// </summary>
    public string property { get; }


    /// <summary>
    ///     CSS 属性值
    /// </summary>
    public string value { get; }


    /// <summary>
    ///     是否 !important
    /// </summary>
    public string? important { get; }


    /// <summary>
    ///     特异性权重
    /// </summary>
    public int specificity { get; }
}