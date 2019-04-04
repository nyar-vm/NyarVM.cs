using System.Collections.Immutable;
using Std.Data.Text.Diagnostics;

namespace Nyar.Analyzer.Check;

public interface IChecker
{
    ImmutableArray<Diagnostic> check();
}