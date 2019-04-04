namespace Nyar.Analyzer.Format;

public interface IFormatterProvider
{
    IFormatter create_formatter();
}