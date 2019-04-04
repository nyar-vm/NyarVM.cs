namespace Nyar.Language.Julia;

/// <summary>
///     Julia 语言定义
/// </summary>
public sealed class JuliaLanguage : Language
{
    public override string name => "julia";

    public IReadOnlyList<string> extensions { get; init; } = [".jl"];
}