namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     列表编号样式
/// </summary>
public enum ListNumberStyle
{
    /// <summary>默认样式</summary>
    @default,

    /// <summary>示例样式</summary>
    example,

    /// <summary>十进制</summary>
    @decimal,

    /// <summary>小写罗马数字</summary>
    lower_roman,

    /// <summary>大写罗马数字</summary>
    upper_roman,

    /// <summary>小写字母</summary>
    lower_alpha,

    /// <summary>大写字母</summary>
    upper_alpha
}