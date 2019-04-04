using Std.Data.Text.Verse.AST;

namespace Std.Data.Text.Verse.Semantic;

/// <summary>
///     Verse 类型检查器实现
/// </summary>
public sealed class VerseTypeCheckerImpl {
    public VerseTypeCheckerImpl() {
        Bridge = new VerseSemanticBridge();
    }

    public VerseSemanticBridge Bridge { get; }

    public SemanticModel CheckCompilationUnit(CompilationUnit compilationUnit, string? filePath = null) {
        return Bridge.BuildSemanticModel(compilationUnit, filePath ?? "");
    }
}
