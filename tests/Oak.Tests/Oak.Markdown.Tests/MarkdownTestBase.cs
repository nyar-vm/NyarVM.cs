using Oak.Markdown.Syntax;
using Oak.Testing;

namespace Oak.Markdown.Tests;

public abstract class MarkdownTestBase : TestBase
{
    protected MarkdownDocument ParseWithTimeout(string source)
    {
        return ExecuteWithTimeout(() =>
        {
            var lang = new MarkdownLanguage();
            return lang.Parse(source);
        }, "Markdown 解析器");
    }
}
