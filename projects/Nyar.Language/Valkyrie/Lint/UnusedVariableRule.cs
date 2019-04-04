using Nyar.Lint;

namespace Nyar.Language.Valkyrie.Lint;

public sealed class UnusedVariableRule : ILintRule
{
    public string Code => "V001";

    public string Description => "检测声明后未被使用的变量";

    public LintSeverity Severity => LintSeverity.Warning;

    public IEnumerable<LintDiagnostic> Diagnose(AstNode node)
    {
        if (node is LetDeclaration { name: { } nameNode })
        {
            yield return new LintDiagnostic(
                LintSeverity.Warning,
                $"变量 '{nameNode.name}' 声明后未被使用",
                string.Empty,
                node.span.start,
                1,
                Code);
        }
    }
}