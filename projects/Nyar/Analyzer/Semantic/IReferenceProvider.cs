namespace Nyar.Analyzer.Semantic;

/// <summary>
///     语言侧引用收集入口，供工作区索引统一汇总引用关系。
/// </summary>
public interface IReferenceProvider
{
    IReadOnlyList<ReferenceEntry> collect_references(string filePath, object syntaxRoot, SemanticModel model);
}