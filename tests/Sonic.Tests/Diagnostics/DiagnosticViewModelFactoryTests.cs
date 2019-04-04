using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;

namespace Sonic.Tests.Diagnostics;

public sealed class DiagnosticViewModelFactoryTests
{
    [Fact]
    public void CreateCheckViewModel_ShouldNormalizeCodeAndHints()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "let name = foo;\n");

            var diagnostic = new Diagnostic(
                new TextSpan(4, 7),
                " unresolved name ",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    SourceSpan.single_line(1, 5, 3, filePath),
                    ["define `name` before use", "compiler: test"]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.True(viewModel.is_error);
            Assert.Equal(DiagnosticSeverity.error, viewModel.severity);
            Assert.Equal("E0001", viewModel.display_code);
            Assert.Equal("unresolved name", viewModel.message);
            Assert.Equal(["define `name` before use", "compiler: test"], viewModel.hints);
            Assert.Equal(Path.Combine("src", "main.v"), viewModel.location.display_file_path);
            Assert.NotNull(viewModel.source_line);
            Assert.Equal([1], viewModel.source_lines.Select(line => line.line_number).ToArray());
            Assert.Single(viewModel.annotations);
            Assert.Equal(DiagnosticAnnotationKind.Primary, viewModel.annotations[0].kind);
            Assert.NotNull(viewModel.primary_annotation);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateLintViewModel_ShouldAppendLintHint()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "lint.v");
            const string source = "foo\nbar\n";
            var diagnostic = new Diagnostic(
                new TextSpan(4, 7),
                "invalid token",
                DiagnosticSeverity.warning,
                1,
                new DiagnosticSource(
                    filePath,
                    default,
                    ["keep source stable"]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Detail,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_lint_view_model(
                filePath,
                source,
                diagnostic,
                options,
                projectDir);

            Assert.Equal("W0001", viewModel.display_code);
            Assert.Equal(
                ["keep source stable", "tool: lint"],
                viewModel.hints);
            Assert.Equal(2, viewModel.location.start_line);
            Assert.Equal(1, viewModel.location.start_column);
            Assert.Equal([1, 2], viewModel.source_lines.Select(line => line.line_number).ToArray());
            Assert.Single(viewModel.annotations);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateCheckViewModel_ShouldCreateSecondaryAnnotationsFromRelatedSpans()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "line 1\nlet name = foo;\nline 3\nline 4\nline 5\nlet name = bar;\nline 7\n");

            var diagnostic = new Diagnostic(
                new TextSpan(0, 0),
                "unresolved name",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    SourceSpan.single_line(2, 5, 4, filePath),
                    ["define `name` before use"],
                    [
                        new DiagnosticRelatedSpan(
                            SourceSpan.single_line(6, 5, 4, filePath),
                            "previous binding")
                    ]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.Equal([1, 2, 3, 5, 6, 7], viewModel.source_lines.Select(line => line.line_number).ToArray());
            Assert.Equal(2, viewModel.annotations.Count);
            Assert.Equal(DiagnosticAnnotationKind.Primary, viewModel.annotations[0].kind);
            Assert.Equal(DiagnosticAnnotationKind.Secondary, viewModel.annotations[1].kind);
            Assert.Equal("previous binding", viewModel.annotations[1].label);
            Assert.Equal(6, viewModel.annotations[1].start_line);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateCheckViewModel_ShouldNormalizeZeroWidthRelatedSpan()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "line 1\nlet name = foo;\n");

            var diagnostic = new Diagnostic(
                new TextSpan(0, 0),
                "unresolved name",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    SourceSpan.single_line(2, 5, 0, filePath),
                    null,
                    [
                        new DiagnosticRelatedSpan(
                            new SourceSpan(filePath, 2, 1, 2, 1),
                            " binding start ")
                    ]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.Equal(2, viewModel.annotations.Count);
            Assert.Equal("binding start", viewModel.annotations[1].label);
            Assert.Equal(1, viewModel.annotations[1].start_column);
            Assert.Equal(2, viewModel.annotations[1].end_column);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateCheckViewModel_ShouldUseTighterContextForMultiLinePrimarySpan()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "line 1\nline 2\nline 3\nline 4\nline 5\nline 6\n");

            var diagnostic = new Diagnostic(
                new TextSpan(0, 0),
                "unresolved name",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    new SourceSpan(filePath, 2, 1, 4, 5),
                    null));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.Equal([2, 3, 4], viewModel.source_lines.Select(line => line.line_number).ToArray());
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateCheckViewModel_ShouldExposeCrossFileRelatedInformationAsMetadata()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            var otherFilePath = Path.Combine(projectDir, "src", "other.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "let name = foo;\n");
            File.WriteAllText(otherFilePath, "let name = bar;\n");

            var diagnostic = new Diagnostic(
                new TextSpan(0, 0),
                "unresolved name",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    SourceSpan.single_line(1, 5, 4, filePath),
                    null,
                    [
                        new DiagnosticRelatedSpan(
                            SourceSpan.single_line(1, 5, 4, otherFilePath),
                            "previous binding",
                            otherFilePath)
                    ]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.Single(viewModel.annotations);
            Assert.NotNull(viewModel.related_information);
            Assert.Single(viewModel.related_information!);
            Assert.Equal("previous binding", viewModel.related_information![0].label);
            Assert.Equal(Path.Combine("src", "other.v"), viewModel.related_information![0].location.display_file_path);
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateCheckViewModel_ShouldDeduplicateAndSortSameFileSecondaryAnnotations()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "line 1\nlet name = foo;\nline 3\nline 4\nlet name = bar;\nline 6\n");

            var diagnostic = new Diagnostic(
                new TextSpan(0, 0),
                "unresolved name",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    SourceSpan.single_line(2, 5, 4, filePath),
                    null,
                    [
                        new DiagnosticRelatedSpan(
                            SourceSpan.single_line(5, 5, 4, filePath),
                            "later binding"),
                        new DiagnosticRelatedSpan(
                            SourceSpan.single_line(5, 5, 4, filePath),
                            "later binding"),
                        new DiagnosticRelatedSpan(
                            SourceSpan.single_line(1, 1, 4, filePath),
                            "earlier binding")
                    ]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.Equal(3, viewModel.annotations.Count);
            Assert.Equal(
                ["earlier binding", "later binding"],
                viewModel.annotations
                    .Where(annotation => annotation.kind == DiagnosticAnnotationKind.Secondary)
                    .Select(annotation => annotation.label)
                    .ToArray());
            Assert.Equal([1, 2, 3, 4, 5, 6], viewModel.source_lines.Select(line => line.line_number).ToArray());
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateCheckViewModel_ShouldDeduplicateAndSortCrossFileRelatedInformation()
    {
        var projectDir = create_temp_project_dir();

        try
        {
            var filePath = Path.Combine(projectDir, "src", "main.v");
            var aFilePath = Path.Combine(projectDir, "src", "a.v");
            var zFilePath = Path.Combine(projectDir, "src", "z.v");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "let name = foo;\n");
            File.WriteAllText(aFilePath, "line 1\nline 2\nline 3\n");
            File.WriteAllText(zFilePath, "line 1\n");

            var diagnostic = new Diagnostic(
                new TextSpan(0, 0),
                "unresolved name",
                DiagnosticSeverity.error,
                1,
                new DiagnosticSource(
                    filePath,
                    SourceSpan.single_line(1, 5, 4, filePath),
                    null,
                    [
                        new DiagnosticRelatedSpan(
                            new SourceSpan(zFilePath, 1, 5, 1, 8),
                            "omega binding",
                            zFilePath),
                        new DiagnosticRelatedSpan(
                            new SourceSpan(aFilePath, 3, 2, 3, 6),
                            "alpha binding",
                            aFilePath),
                        new DiagnosticRelatedSpan(
                            new SourceSpan(aFilePath, 3, 2, 3, 6),
                            "alpha binding",
                            aFilePath)
                    ]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.Single(viewModel.annotations);
            Assert.NotNull(viewModel.related_information);
            Assert.Equal(
                ["alpha binding", "omega binding"],
                viewModel.related_information!.Select(item => item.label).ToArray());
        }
        finally
        {
            Directory.Delete(projectDir, true);
        }
    }

    [Fact]
    public void CreateCheckViewModel_ShouldSkipSameFileRelatedSpanThatMatchesPrimaryLocation()
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
                    SourceSpan.single_line(1, 5, 4, filePath),
                    null,
                    [
                        new DiagnosticRelatedSpan(
                            SourceSpan.single_line(1, 5, 4, filePath),
                            "same place")
                    ]));
            var options = new DiagnosticRenderOptions(
                DiagnosticFormat.Pretty,
                DiagnosticSeverity.warning);

            var viewModel = DiagnosticViewModelFactory.create_check_view_model(
                diagnostic,
                options,
                projectDir);

            Assert.Single(viewModel.annotations);
            Assert.Null(viewModel.related_information);
            Assert.Equal([1], viewModel.source_lines.Select(line => line.line_number).ToArray());
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
            "sonic-diagnostic-view-model-factory-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDir);
        return projectDir;
    }
}
