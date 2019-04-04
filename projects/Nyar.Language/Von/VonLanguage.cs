namespace Nyar.Language.Von;

public sealed class VonLanguage : Language
{
    public override string name => "Von";

    public IReadOnlyList<string> extensions { get; init; } = [".von", ".gon"];
}