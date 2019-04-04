using Nyar.Analyzer.Semantic;

namespace Nyar.Analyzer.Workspace;

/// <summary>
///     面向工作区的统一语义分析器。
///     它直接基于语法节点上的 Language 信息做分派。
/// </summary>
/// <remarks>
///     TODO: ILanguageService 和 ILanguage&lt;&gt; 类型待定义后恢复实现
/// </remarks>
public sealed class WorkspaceAnalyzer
{
    // TODO: ILanguageService 和 ILanguage&lt;&gt; 类型待定义
    // private readonly ILanguageService _languageService;
    //
    // public WorkspaceAnalyzer(ILanguageService languageService, SemanticIndex? index = null)
    // {
    //     _languageService = languageService;
    //     Index = index ?? new SemanticIndex();
    // }

    public WorkspaceAnalyzer()
    {
        index = new SemanticIndex();
    }

    public SemanticIndex index { get; }

    // TODO: ILanguageService 和 ILanguage&lt;&gt; 类型待定义后恢复
    // public SemanticModel AnalyzeDocument(string filePath, SyntaxRoot syntaxRoot)
    // {
    //     var language = syntaxRoot.Language
    //         ?? _languageService.GetLanguage(syntaxRoot.LanguageId);
    //
    //     return AnalyzeDocument(language, filePath, syntaxRoot);
    // }
    //
    // public SemanticModel AnalyzeDocument(ILanguage<> language, string filePath, object syntaxRoot)
    // {
    //     var semanticProvider = _languageService.GetProvider<ISemanticAnalysisProvider>(language)
    //         ?? throw new InvalidOperationException($"Language '{language.Name}' has no semantic analysis provider.");
    //
    //     var model = semanticProvider.Analyze(filePath, syntaxRoot);
    //     var stubs = semanticProvider.BuildStubs(filePath, syntaxRoot);
    //
    //     Index.IndexFile(filePath, stubs);
    //     Index.IndexFileSymbols(filePath, model);
    //
    //     foreach (var symbol in model.GetAllDeclaredSymbols())
    //     {
    //         Index.AddGlobalSymbol(symbol);
    //
    //         if (symbol is Symbol { HasSourcePosition: true } concreteSymbol)
    //         {
    //             Index.AddDefinition(symbol.Name, filePath, concreteSymbol.DefinitionSourceSpan);
    //         }
    //     }
    //
    //     var referenceProvider = _languageService.GetProvider<IReferenceProvider>(language);
    //     if (referenceProvider is null)
    //     {
    //         return model;
    //     }
    //
    //     foreach (var reference in referenceProvider.CollectReferences(filePath, syntaxRoot, model))
    //     {
    //         Index.AddReference(reference.SymbolName, reference.FilePath, reference.SourceSpan, reference.IsDefinition);
    //     }
    //
    //     return model;
    // }
}