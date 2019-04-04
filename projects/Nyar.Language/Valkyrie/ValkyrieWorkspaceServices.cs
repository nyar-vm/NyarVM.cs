using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Parsing;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie;

public sealed class ValkyrieWorkspaceServices
{
    private readonly ValkyrieSemanticBridge _semantic_bridge;

    public ValkyrieWorkspaceServices()
    {
        language = ValkyrieLanguage.standard;
        source_analyzer = new SourceAnalyzer();
        _semantic_bridge = new ValkyrieSemanticBridge();
    }

    public ValkyrieLanguage language { get; }

    public SourceAnalyzer source_analyzer { get; }

    public ParseResult<CompilationUnit> parse(string source, string filePath = "")
    {
        return source_analyzer.parse(source, filePath);
    }

    public SemanticModel analyze(CompilationUnit compilationUnit, string filePath)
    {
        return _semantic_bridge.analyze(filePath, compilationUnit);
    }
}