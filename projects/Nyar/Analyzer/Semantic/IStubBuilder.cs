using TextSpan = Std.Data.Text.Syntax.TextSpan;

namespace Nyar.Analyzer.Semantic;

public interface IStubBuilder
{
    IReadOnlyList<SymbolStub> build_stubs(int nodeKind, TextSpan span);
}