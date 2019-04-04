using Nyar.IR.Intent;

namespace Nyar.Language.Valkyrie.TypeSystem;

/// <summary>
///     带效应类型标注的类型，包含正常类型和效应集合
/// </summary>
public sealed class Typed
{
    public ValkyrieType normal { get; }
    public EffectSet effects { get; }

    public Typed(ValkyrieType normal, EffectSet effects)
    {
        this.normal = normal;
        this.effects = effects;
    }

    public override string ToString()
    {
        return effects.is_empty
            ? normal.ToString()
            : $"{normal} / {effects}";
    }
}