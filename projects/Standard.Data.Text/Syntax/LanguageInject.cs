namespace Std.Data.Text.Syntax;

/// <summary>
///     ע븨֧ڽעԽ
/// </summary>
public static class LanguageInject
{
    public static GreenNode? inject(string languageId, ISource source, int baseOffset = 0)
    {
        if (!LanguageRegistry.is_registered(languageId)) return null;

        var root = LanguageRegistry.parse(languageId, source);
        return root._green;
    }

    public static GreenNode? inject(string languageId, ISource mainSource, TextSpan range)
    {
        var subText = mainSource.substring(new Range(range.start, range.end));
        var subSource = new StringSource(subText);
        return inject(languageId, subSource, range.start);
    }
}