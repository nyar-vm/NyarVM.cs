namespace Nyar.Language.Valkyrie.Compiler.Meta;

public interface IMacroProvider
{
    string? get(string name);

    IReadOnlyDictionary<string, string> get_all();
}