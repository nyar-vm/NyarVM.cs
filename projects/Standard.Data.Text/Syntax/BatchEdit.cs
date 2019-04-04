using System.Collections.Immutable;

namespace Std.Data.Text.Syntax;

public readonly record struct BatchEdit(ImmutableArray<TextEdit> changes) : IEdit;