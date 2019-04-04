using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;

namespace Sonic.Tests.Diagnostics;

public sealed class DiagnosticSourceResolverTests
{
    [Fact]
    public void ResolveCheckLocation_ShouldPreferSourceSpanAndNormalizeDisplayPath()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "let name = foo;\n");

            var diagnostic = new Diagnostic(
                new TextSpan(0, 0),
                "unresolved name",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    new SourceSpan(filePath, 2, 3, 2, 6),
                    ["hint"]));

            var location = DiagnosticSourceResolver.resolve_check_location(diagnostic, projectDir);

            Assert.Equal(filePath, location.source_file_path);
            Assert.Equal(Path.Combine("src", "main.v"), location.display_file_path);
            Assert.Equal(2, location.start_line);
            Assert.Equal(3, location.start_column);
            Assert.Equal(2, location.end_line);
            Assert.Equal(6, location.end_column);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void ResolveLintLocation_ShouldBuildPreciseLocationFromTextSpan()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            const string source = "foo\nbar\n";
            var diagnostic = new Diagnostic(
                new TextSpan(4, 7),
                "invalid token",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    default,
                    ["hint"]));

            var location = DiagnosticSourceResolver.resolve_lint_location(filePath, source, diagnostic, projectDir);

            Assert.Equal(filePath, location.source_file_path);
            Assert.Equal(Path.Combine("src", "main.v"), location.display_file_path);
            Assert.Equal(2, location.start_line);
            Assert.Equal(1, location.start_column);
            Assert.Equal(2, location.end_line);
            Assert.Equal(4, location.end_column);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void ResolveSourceLine_ShouldPreferInlineSourceAndNormalizeTabs()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "disk\tline\n");

            var location = new DiagnosticLocationViewModel(
                filePath,
                Path.Combine("src", "main.v"),
                1,
                1,
                1,
                5);

            var line = DiagnosticSourceResolver.resolve_source_line(location, "inline\tline\n");

            Assert.NotNull(line);
            Assert.Equal(1, line.Value.line_number);
            Assert.Equal("inline line", line.Value.text);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void ResolveSourceLines_ShouldReturnMultiLineContext()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "first\tline\nsecond line\nthird line\n");

            var location = new DiagnosticLocationViewModel(
                filePath,
                Path.Combine("src", "main.v"),
                1,
                2,
                2,
                7);

            var lines = DiagnosticSourceResolver.resolve_source_lines(location, null);

            Assert.Equal(2, lines.Length);
            Assert.Equal(1, lines[0].line_number);
            Assert.Equal("first line", lines[0].text);
            Assert.Equal(2, lines[1].line_number);
            Assert.Equal("second line", lines[1].text);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void ResolveSourceLines_ShouldIncludeContextLinesAroundPrimarySpan()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "first\nsecond\nthird\nfourth\n");

            var location = new DiagnosticLocationViewModel(
                filePath,
                Path.Combine("src", "main.v"),
                2,
                1,
                2,
                7);

            var lines = DiagnosticSourceResolver.resolve_source_lines(location, null, 1);

            Assert.Equal([1, 2, 3], lines.Select(line => line.line_number).ToArray());
            Assert.Equal("first", lines[0].text);
            Assert.Equal("second", lines[1].text);
            Assert.Equal("third", lines[2].text);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void ResolveSourceLines_ShouldMergeContextWindowsForMultipleLocations()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "l1\nl2\nl3\nl4\nl5\nl6\nl7\nl8\n");

            var lines = DiagnosticSourceResolver.resolve_source_lines(
                [
                    new DiagnosticLocationViewModel(
                        filePath,
                        Path.Combine("src", "main.v"),
                        2,
                        1,
                        2,
                        3),
                    new DiagnosticLocationViewModel(
                        filePath,
                        Path.Combine("src", "main.v"),
                        7,
                        1,
                        8,
                        3)
                ],
                null,
                1);

            Assert.Equal([1, 2, 3, 6, 7, 8], lines.Select(line => line.line_number).ToArray());
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    private static string create_temp_project_dir()
    {
        var projectDir = Path.Combine(
            Path.GetTempPath(),
            "sonic-diagnostic-source-resolver-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDir);
        return projectDir;
    }
}
