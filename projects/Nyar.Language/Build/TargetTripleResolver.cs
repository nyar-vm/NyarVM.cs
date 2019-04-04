using Nyar.Types.Targets;

namespace Nyar.Language.Build;

/// <summary>
///     目标三元组解析器：支持短别名到完整四段式 canonical triple 的映射。
/// </summary>
public sealed class TargetTripleResolver
{
    private readonly CanonicalTargetRegistry _registry = new();

    /// <summary>
    ///     解析短别名为完整四段式 canonical triple。
    /// </summary>
    public string resolve_canonical_name(string targetName)
    {
        if (CanonicalTarget.try_parse(targetName, out var canonicalTarget))
        {
            return canonicalTarget.ToString();
        }

        return targetName;
    }

    /// <summary>
    ///     解析目标名称为目标配置，支持短别名和完整四段式。
    /// </summary>
    public TargetProfile? resolve_target(string targetName)
    {
        var canonical = resolve_canonical_name(targetName);

        try
        {
            return _registry.resolve(canonical);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    ///     解析目标名称为编译目标配置，兼容旧接口。
    /// </summary>
    public CompilationTarget? resolve_compilation_target(string targetName)
    {
        var profile = resolve_target(targetName);
        if (profile is null)
        {
            return null;
        }

        return CanonicalTarget.parse(profile.canonical_triple).to_compilation_target();
    }
}
