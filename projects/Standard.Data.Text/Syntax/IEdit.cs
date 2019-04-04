using System.Collections.Immutable;

namespace Std.Data.Text.Syntax;

public interface IEdit
{
    ImmutableArray<TextEdit> changes { get; }
}