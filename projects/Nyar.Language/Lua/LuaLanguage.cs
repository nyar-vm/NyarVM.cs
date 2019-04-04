namespace Nyar.Language.Lua;

/// <summary>
///     Lua 语言定义
/// </summary>
public sealed class LuaLanguage : Language
{
    public override string name => "lua";

    public IReadOnlyList<string> extensions { get; init; } = [".lua"];
}