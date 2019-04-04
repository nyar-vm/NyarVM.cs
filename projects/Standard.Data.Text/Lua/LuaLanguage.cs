using Std.Data.Text.Syntax;

namespace Std.Data.Text.Lua;

/// <summary>
///     Lua 语言配置。
/// </summary>
public sealed class LuaLanguage : Language
{
    /// <summary>
    ///     语言名称。
    /// </summary>
    public override string name => "Lua";


    /// <summary>
    ///     是否启用 Lua 5.1 兼容模式。
    /// </summary>
    public bool lua51_compat { get; init; }


    /// <summary>
    ///     是否启用整数字面量（Lua 5.3+）。
    /// </summary>
    public bool integer_literals { get; init; } = true;


    /// <summary>
    ///     是否启用位运算符（Lua 5.3+）。
    /// </summary>
    public bool bitwise_operators { get; init; } = true;


    /// <summary>
    ///     是否启用 goto 语句。
    /// </summary>
    public bool goto_statement { get; init; } = true;
}