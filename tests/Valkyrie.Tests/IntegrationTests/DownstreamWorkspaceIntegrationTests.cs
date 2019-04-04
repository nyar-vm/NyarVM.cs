using Nyar.Language.Valkyrie.Formatter;
using Nyar.Language.Valkyrie.Linter;

namespace Valkyrie.Tests.IntegrationTests;

public sealed class DownstreamWorkspaceIntegrationTests
{
    [Fact]
    public void Formatter_FormatSource_ShouldUseWorkspaceParser()
    {
        var source = """
                     using Gameplay.Core;
                     component Position {
                     x: f32;
                     y: f32;
                     }
                     """;

        var formatter = new CodeFormatter();
        var result = formatter.format(source, "Position.v");

        Assert.Empty(result.diagnostics);
        Assert.True(result.changed);
        Assert.Contains("component Position", result.formatted_text);
        Assert.Contains("    x: f32;", result.formatted_text);
    }

    [Fact]
    public void Linter_CheckSource_ShouldReturnParseDiagnostics()
    {
        var source = "using Gameplay.Core";
        var engine = new LinterEngine();

        var result = engine.check(source, "Broken.v");

        Assert.NotEmpty(result.diagnostics);
        Assert.Contains(result.diagnostics, diagnostic => diagnostic.rule_id == "VALK_PARSE");
    }
}
