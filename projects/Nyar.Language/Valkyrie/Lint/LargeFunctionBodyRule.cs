using Nyar.Lint;

namespace Nyar.Language.Valkyrie.Lint;

public sealed class LargeFunctionBodyRule : ILintRule
{
    private const int Threshold = 50;

    public string Code => "V002";

    public string Description => $"检测函数体行数过多的函数（阈值：{Threshold} 行）";

    public LintSeverity Severity => LintSeverity.Suggestion;

    public IEnumerable<LintDiagnostic> Diagnose(AstNode node)
    {
        if (node is FunctionDecl { body: { } body } && body.statements.Count > Threshold)
        {
            yield return new LintDiagnostic(
                LintSeverity.Suggestion,
                $"函数体包含 {body.statements.Count} 条语句，超过建议阈值 {Threshold}",
                string.Empty,
                node.span.start,
                1,
                Code);
        }
    }
}