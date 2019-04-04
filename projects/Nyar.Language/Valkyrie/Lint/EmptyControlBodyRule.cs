using Nyar.Lint;
using Std.Data.Text.Valkyrie.AST.Statement;

namespace Nyar.Language.Valkyrie.Lint;

public sealed class EmptyControlBodyRule : ILintRule
{
    public string Code => "V003";

    public string Description => "检测空的 if/while/for 体";

    public LintSeverity Severity => LintSeverity.Warning;

    public IEnumerable<LintDiagnostic> Diagnose(AstNode node)
    {
        switch (node)
        {
            case IfStatement { then_block.statements.Count: 0 }:
                yield return new LintDiagnostic(
                    LintSeverity.Warning,
                    "if 语句体为空",
                    string.Empty,
                    node.span.start,
                    1,
                    Code);
                break;

            case WhileStatement { body.statements.Count: 0 }:
                yield return new LintDiagnostic(
                    LintSeverity.Warning,
                    "while 循环体为空",
                    string.Empty,
                    node.span.start,
                    1,
                    Code);
                break;

            case UntilStatement { body.statements.Count: 0 }:
                yield return new LintDiagnostic(
                    LintSeverity.Warning,
                    "until 循环体为空",
                    string.Empty,
                    node.span.start,
                    1,
                    Code);
                break;

            case LoopStatement { body.statements.Count: 0 }:
                yield return new LintDiagnostic(
                    LintSeverity.Warning,
                    "for 循环体为空",
                    string.Empty,
                    node.span.start,
                    1,
                    Code);
                break;
        }
    }
}
