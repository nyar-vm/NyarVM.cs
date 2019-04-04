using Std.Data.Text.Syntax;

namespace Nyar.Analyzer.Format;

public interface IFormatter
{
    IEdit format(ITextProvider textProvider);
}