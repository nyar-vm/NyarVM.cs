namespace Nyar.Analyzer.Semantic;

/// <summary>
///     语言侧语义分析入口，负责从语法结果构建语义模型并导出工作区可复用存根。
/// </summary>
public interface ISemanticAnalysisProvider
{
    SemanticModel analyze(string filePath, object syntaxRoot);

    IReadOnlyList<SymbolStub> build_stubs(string filePath, object syntaxRoot);
}