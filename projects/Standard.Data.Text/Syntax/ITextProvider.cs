namespace Std.Data.Text.Syntax;

public interface ITextProvider
{
    TextProviderId id { get; }
    Language language { get; }
    int version { get; }
    ISource source { get; }
}