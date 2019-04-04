using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Statement;

public record ArmWhenNode : ArmNode
{
    public TermNode? guard;
    public TermNode term;
}