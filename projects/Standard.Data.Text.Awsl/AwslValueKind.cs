namespace Std.Data.Text.Awsl;

/// <summary>
///     Awsl 值种类
/// </summary>
public enum AwslValueKind
{
    none,
    boolean,
    number,
    @string,
    identifier,
    array,
    @object,
    expression
}