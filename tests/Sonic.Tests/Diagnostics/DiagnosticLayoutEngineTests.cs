using Std.Data.Text.Diagnostics;

namespace Sonic.Tests.Diagnostics;

public sealed class DiagnosticLayoutEngineTests
{
    [Fact]
    public void CreatePrettyLayout_ShouldEmitHeadingLocationSnippetMetadataAndSpacer()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                1,
                5,
                1,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "let name = foo;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    1,
                    5,
                    1,
                    8,
                    "unresolved name")
            ],
            ["define `name` before use", "compiler: test"]);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);

        Assert.Equal(
            [
                DiagnosticLayoutBlockKind.Heading,
                DiagnosticLayoutBlockKind.Location,
                DiagnosticLayoutBlockKind.Snippet,
                DiagnosticLayoutBlockKind.Metadata,
                DiagnosticLayoutBlockKind.Spacer
            ],
            layout.blocks.Select(block => block.kind).ToArray());

        Assert.Equal("[E0001]: unresolved name", layout.blocks[0].lines[0].text);
        Assert.Equal("  ┌─ src\\main.v:1:5", layout.blocks[1].lines[0].text);
        Assert.Contains("let name = foo;", layout.blocks[2].lines[1].text);
        Assert.Contains("^^^^", layout.blocks[2].lines[2].text);
        Assert.Equal("   = hint: define `name` before use", layout.blocks[3].lines[0].text);
        Assert.Equal(string.Empty, layout.blocks[4].lines[0].text);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldRenderSecondaryAnnotationsAfterPrimary()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                1,
                5,
                1,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "let name = foo;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    1,
                    1,
                    1,
                    4,
                    "binding"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    1,
                    5,
                    1,
                    8,
                    "unresolved name")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines;

        Assert.Equal(3, snippetLines.Count);
        Assert.Contains("--- binding", snippetLines[2].text);
        Assert.Contains("^^^^ unresolved name", snippetLines[2].text);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldRenderMultiLinePrimaryAnnotation()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                1,
                5,
                2,
                4),
            [
                new DiagnosticSourceLineViewModel(1, "let name ="),
                new DiagnosticSourceLineViewModel(2, "    foo;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    1,
                    5,
                    2,
                    4,
                    "unresolved name")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines;

        Assert.Equal(5, snippetLines.Count);
        Assert.Contains("1", snippetLines[1].text);
        Assert.Contains("let name =", snippetLines[1].text);
        Assert.DoesNotContain("unresolved name", snippetLines[2].text);
        Assert.Contains("2", snippetLines[3].text);
        Assert.Contains("foo;", snippetLines[3].text);
        Assert.Contains("unresolved name", snippetLines[4].text);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldRenderMultiLineSecondaryAnnotation()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                2,
                5,
                2,
                8),
            [
                new DiagnosticSourceLineViewModel(2, "let name ="),
                new DiagnosticSourceLineViewModel(3, "    foo;"),
                new DiagnosticSourceLineViewModel(4, "let name = bar;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    2,
                    5,
                    2,
                    8,
                    "unresolved name"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    3,
                    1,
                    4,
                    5,
                    "previous binding")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines;

        Assert.Contains("----", snippetLines[4].text);
        Assert.DoesNotContain("previous binding", snippetLines[4].text);
        Assert.Contains("---- previous binding", snippetLines[6].text);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldInsertFoldLineForOmittedContext()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                2,
                5,
                2,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "line 1"),
                new DiagnosticSourceLineViewModel(2, "line 2"),
                new DiagnosticSourceLineViewModel(3, "line 3"),
                new DiagnosticSourceLineViewModel(6, "line 6"),
                new DiagnosticSourceLineViewModel(7, "line 7")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    2,
                    1,
                    2,
                    4,
                    "unresolved name"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    6,
                    1,
                    7,
                    4,
                    "previous binding")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines;

        Assert.Contains("... 2 lines omitted", snippetLines.Select(line => line.text));
    }

    [Fact]
    public void CreatePrettyLayout_ShouldSplitOverlappingAnnotationsIntoSeparateLayers()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                1,
                5,
                1,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "let name = foo;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    1,
                    5,
                    1,
                    8,
                    "unresolved name"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    1,
                    6,
                    1,
                    9,
                    "previous binding")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines;

        Assert.Equal(4, snippetLines.Count);
        Assert.Contains("^^^^ unresolved name", snippetLines[2].text);
        Assert.Contains("--- previous binding", snippetLines[3].text);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldKeepPrimaryClosestWhenMultipleOverlapsExist()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                2,
                5,
                2,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "let a = first;"),
                new DiagnosticSourceLineViewModel(2, "let a = foo;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    1,
                    5,
                    1,
                    10,
                    "outer binding"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    2,
                    5,
                    2,
                    8,
                    "unresolved name"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    2,
                    6,
                    2,
                    9,
                    "inner binding")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines;

        Assert.Contains("^^^^ unresolved name", snippetLines[4].text);
        Assert.Contains("--- inner binding", snippetLines[5].text);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldKeepOverlappingSecondaryContinuationsSplitAcrossAdjacentLines()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                2,
                8,
                2,
                12),
            [
                new DiagnosticSourceLineViewModel(1, "alpha = first;"),
                new DiagnosticSourceLineViewModel(2, "beta = middle;"),
                new DiagnosticSourceLineViewModel(3, "gamma = last;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    1,
                    1,
                    3,
                    6,
                    "outer binding"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    2,
                    8,
                    2,
                    12,
                    "unresolved name"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    2,
                    1,
                    3,
                    4,
                    "inner binding")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines.Select(line => line.text).ToArray();
        var betaIndex = Array.FindIndex(snippetLines, line => line.Contains("beta = middle;"));
        var gammaIndex = Array.FindIndex(snippetLines, line => line.Contains("gamma = last;"));
        var betaMarkers = snippetLines[(betaIndex + 1)..gammaIndex];
        var gammaMarkers = snippetLines[(gammaIndex + 1)..];

        Assert.Equal(3, betaMarkers.Length);
        Assert.Contains(betaMarkers, line => line.Contains("^^^^ unresolved name"));
        Assert.Equal(2, gammaMarkers.Length);
        Assert.Contains(gammaMarkers, line => line.Contains("----- outer binding"));
        Assert.Contains(gammaMarkers, line => line.Contains("--- inner binding"));
    }

    [Fact]
    public void CreatePrettyLayout_ShouldNotAppendTrailingSpaceForEmptyAnnotationLabel()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                2,
                5,
                2,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "let name = bar;"),
                new DiagnosticSourceLineViewModel(2, "let name = foo;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    1,
                    5,
                    1,
                    8,
                    string.Empty),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    2,
                    5,
                    2,
                    8,
                    "unresolved name")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines.Select(line => line.text).ToArray();
        var secondaryMarkerLine = snippetLines.Single(line => line.Contains("---") && !line.Contains("unresolved name"));

        Assert.False(char.IsWhiteSpace(secondaryMarkerLine[^1]));
        Assert.DoesNotContain("binding", secondaryMarkerLine);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldCompressSecondaryMiddleContinuationMarkerWidth()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                2,
                5,
                2,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "line 1"),
                new DiagnosticSourceLineViewModel(2, "line 2"),
                new DiagnosticSourceLineViewModel(3, "line 3"),
                new DiagnosticSourceLineViewModel(4, "line 4")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    2,
                    5,
                    2,
                    8,
                    "unresolved name"),
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Secondary,
                    1,
                    1,
                    4,
                    5,
                    "previous binding")
            ],
            []);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var snippetLines = layout.blocks[2].lines.Select(line => line.text).ToArray();
        var line2Index = Array.FindIndex(snippetLines, line => line.Contains("line 2"));
        var line3Index = Array.FindIndex(snippetLines, line => line.Contains("line 3"));
        var line4Index = Array.FindIndex(snippetLines, line => line.Contains("line 4"));
        var line2Markers = snippetLines[(line2Index + 1)..line3Index];
        var line3Markers = snippetLines[(line3Index + 1)..line4Index];
        var line4Markers = snippetLines[(line4Index + 1)..];

        Assert.Contains(line2Markers, line => line.Contains("^^^^ unresolved name"));
        Assert.Contains(line2Markers, line => line.TrimEnd().EndsWith("---"));
        Assert.Single(line3Markers);
        Assert.Equal("---", line3Markers[0].Trim());
        Assert.Contains(line4Markers, line => line.Contains("---- previous binding"));
    }

    [Fact]
    public void CreatePrettyLayout_ShouldRenderRelatedInformationInMetadataGroup()
    {
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                1,
                5,
                1,
                8),
            [
                new DiagnosticSourceLineViewModel(1, "let name = foo;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    1,
                    5,
                    1,
                    8,
                    "unresolved name")
            ],
            [],
            [
                new DiagnosticRelatedInformationViewModel(
                    new DiagnosticLocationViewModel(
                        @"E:\temp\src\other.v",
                        @"src\other.v",
                        3,
                        2,
                        3,
                        6),
                    "previous binding")
            ]);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);
        var metadataLines = layout.blocks[3].lines;

        Assert.Equal(2, metadataLines.Count);
        Assert.Equal("   = related:", metadataLines[0].text);
        Assert.Equal("     - previous binding at src\\other.v:3:2", metadataLines[1].text);
    }

    [Fact]
    public void CreatePrettyLayout_ShouldOmitMetadataWhenHintsHidden()
    {
        var viewModel = new DiagnosticViewModel(
            false,
            DiagnosticSeverity.warning,
            "warning",
            "W0001",
            "unused binding",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                0,
                0,
                0,
                0),
            [],
            [],
            ["remove the binding"]);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning,
            show_hints: false);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);

        Assert.Equal(
            [
                DiagnosticLayoutBlockKind.Heading,
                DiagnosticLayoutBlockKind.Spacer
            ],
            layout.blocks.Select(block => block.kind).ToArray());
    }

    [Fact]
    public void CreatePrettyLayout_ShouldKeepRelatedMetadataWhenHintsHidden()
    {
        var viewModel = new DiagnosticViewModel(
            false,
            DiagnosticSeverity.warning,
            "warning",
            "W0001",
            "unused binding",
            new DiagnosticLocationViewModel(
                @"E:\temp\src\main.v",
                @"src\main.v",
                1,
                2,
                1,
                5),
            [
                new DiagnosticSourceLineViewModel(1, "let a = b;")
            ],
            [
                new DiagnosticAnnotationViewModel(
                    DiagnosticAnnotationKind.Primary,
                    1,
                    2,
                    1,
                    5,
                    "unused binding")
            ],
            ["remove the binding"],
            [
                new DiagnosticRelatedInformationViewModel(
                    new DiagnosticLocationViewModel(
                        @"E:\temp\src\other.v",
                        @"src\other.v",
                        4,
                        1,
                        4,
                        3),
                    "declared here")
            ]);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning,
            show_hints: false);

        var layout = DiagnosticLayoutEngine.create_pretty_layout(viewModel, options);

        Assert.Equal(
            [
                DiagnosticLayoutBlockKind.Heading,
                DiagnosticLayoutBlockKind.Location,
                DiagnosticLayoutBlockKind.Snippet,
                DiagnosticLayoutBlockKind.Metadata,
                DiagnosticLayoutBlockKind.Spacer
            ],
            layout.blocks.Select(block => block.kind).ToArray());
        Assert.Equal("   = related:", layout.blocks[3].lines[0].text);
        Assert.Equal("     - declared here at src\\other.v:4:1", layout.blocks[3].lines[1].text);
    }
}
