namespace Std.DL.Topos;

/// <summary>分支条件构建器</summary>
public sealed class BranchBuilder
{
    private readonly List<(string Condition, ITopology Branch)> _branches = [];
    private ITopology? _elseBranch;

    /// <summary>添加条件分支</summary>
    public BranchBuilder If(string condition, ITopology branch)
    {
        _branches.Add((condition, branch));
        return this;
    }

    /// <summary>添加默认分支</summary>
    public BranchBuilder Else(ITopology branch)
    {
        _elseBranch = branch;
        return this;
    }

    /// <summary>构建分支定义</summary>
    public BranchDefinition Build()
    {
        return new BranchDefinition
        {
            Branches = _branches.AsReadOnly(),
            ElseBranch = _elseBranch
        };
    }
}