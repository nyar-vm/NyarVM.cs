using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.LanguageTests;

public sealed class AssignmentTests
{
    [Fact]
    public void Parse_SimpleAssignment_ShouldWork()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() {
                var x = 1;
                x = 2;
            }
            """,
            "assignment.v").Value!;

        // Let's see what this produces
        Assert.NotNull(ast);
    }
}

