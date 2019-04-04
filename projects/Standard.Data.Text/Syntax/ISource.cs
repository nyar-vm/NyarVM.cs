namespace Std.Data.Text.Syntax;

public interface ISource
{
    char this[int index] { get; }

    int length { get; }

    string substring(Range range);
}