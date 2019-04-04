namespace Nyar.Analyzer.Highlight;

public interface IHighlighterProvider
{
    IHighlighter create_highlighter();
}