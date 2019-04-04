namespace Nyar.Language.Valkyrie.TypeSystem;

public sealed class ParameterType
{
    public ParameterType(string name, ValkyrieType type, bool isMutable = false)
    {
        this.name = name;
        this.type = type;
        is_mutable = isMutable;
    }

    public string name { get; }
    public ValkyrieType type { get; }
    public bool is_mutable { get; }
}