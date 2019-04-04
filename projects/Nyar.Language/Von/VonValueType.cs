namespace Nyar.Language.Von;

/// <summary>
///     Gon 值类型枚举（已弃用，请使用 Oak.Data.DataValueType）
/// </summary>
[Obsolete("请使用 Oak.Data.DataValueType")]
public enum VonValueType
{
    @null = 0,
    boolean = 1,
    integer = 2,
    @decimal = 3,
    @string = 4,
    array = 5,
    @object = 6
}