namespace Std.DL.Topos;

/// <summary>分支定义</summary>
public sealed class BranchDefinition
{
    /// <summary>条件分支列表</summary>
    public IReadOnlyList<(string Condition, ITopology Branch)> Branches { get; init; } = [];

    /// <summary>默认分支</summary>
    public ITopology? ElseBranch { get; init; }
}