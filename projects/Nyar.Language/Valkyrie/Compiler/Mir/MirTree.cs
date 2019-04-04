using Nyar.IR.Intent;

namespace Nyar.Language.Valkyrie.Compiler.Mir;

public abstract record MirTree
{
    public T accept<T>(Func<AlgebraNode, IReadOnlyList<T>, T> nodeVisitor, Func<AlgebraNode, T> leafVisitor, Func<T> noneVisitor)
    {
        return this switch
        {
            Node(var value, var children) => nodeVisitor(value,
                [.. children.Select(c => c.accept(nodeVisitor, leafVisitor, noneVisitor))]),
            Leaf(var value) => leafVisitor(value),
            None => noneVisitor(),
            _ => noneVisitor()
        };
    }

    public sealed record Node(AlgebraNode value, IReadOnlyList<MirTree> children) : MirTree;

    public sealed record Leaf(AlgebraNode value) : MirTree;

    public sealed record None : MirTree;
}