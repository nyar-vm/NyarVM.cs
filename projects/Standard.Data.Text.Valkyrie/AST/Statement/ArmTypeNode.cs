using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
/// </summary>
public record ArmTypeNode : ArmNode
{
    public TermNode? guard;
    public TypeNode type;
}