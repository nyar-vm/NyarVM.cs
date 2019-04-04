using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;

namespace Sonic.Tests.Diagnostics;

public sealed class DiagnosticRendererTests
{
    [Fact]
    public void WriteCheckDiagnostics_ShortFormat_ShouldMatchExpectedOutput()
    {
        using var fixture = DiagnosticFixture.create(code: 1);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal("[E0001] unresolved name at src/main.v:1:5\n", output);
    }

    [Fact]
    public void WriteCheckDiagnostics_DetailFormat_ShouldMatchExpectedOutput()
    {
        using var fixture = DiagnosticFixture.create();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Detail,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:1:5, define `name` before use, compiler: test\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_PrettyFormat_ShouldMatchExpectedOutputWithoutChangingCurrentDirectory()
    {
        using var fixture = DiagnosticFixture.create();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            """
            [E0001]: unresolved name
              ┌─ src/main.v:1:5
                 │
               1 │ let name = foo;
                 │    ^ unresolved name
               = hint: define `name` before use
               = hint: compiler: test

            """,
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_PrettyFormat_ShouldRenderCrossFileRelatedInformationAsGroup()
    {
        using var fixture = DiagnosticFixture.create_with_related_information();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            """
            [E0001]: unresolved name
              ┌─ src/main.v:1:5
                 │
               1 │ let name = foo;
                 │    ^ unresolved name
               = hint: define `name` before use
               = hint: compiler: test
               = related:
                 - previous binding at src/other.v:1:5

            """,
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_ShortFormat_ShouldRenderCrossFileRelatedInformation()
    {
        using var fixture = DiagnosticFixture.create_with_related_information();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:1:5 (related: previous binding at src/other.v:1:5)\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_DetailFormat_ShouldRenderCrossFileRelatedInformation()
    {
        using var fixture = DiagnosticFixture.create_with_related_information();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Detail,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:1:5, define `name` before use, compiler: test, previous binding at src/other.v:1:5\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_ShortFormat_ShouldRenderSameFileSecondaryAnnotations()
    {
        using var fixture = DiagnosticFixture.create_with_same_file_secondary_annotation();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:2:5 (secondary: previous binding at src/main.v:1:5)\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_DetailFormat_ShouldRenderSameFileSecondaryAnnotations()
    {
        using var fixture = DiagnosticFixture.create_with_same_file_secondary_annotation();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Detail,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:2:5, define `name` before use, compiler: test, previous binding at src/main.v:1:5\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_ShortFormat_ShouldKeepSecondaryBeforeCrossFileRelated()
    {
        using var fixture = DiagnosticFixture.create_with_same_file_and_cross_file_related_information();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:2:5 (secondary: previous binding at src/main.v:1:5) (related: exported binding at src/other.v:1:5)\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_DetailFormat_ShouldKeepHintsThenSecondaryThenCrossFileRelated()
    {
        using var fixture = DiagnosticFixture.create_with_same_file_and_cross_file_related_information();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Detail,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:2:5, define `name` before use, compiler: test, previous binding at src/main.v:1:5, exported binding at src/other.v:1:5\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_ShortFormat_ShouldSortCrossFileRelatedInformationDeterministically()
    {
        using var fixture = DiagnosticFixture.create_with_unsorted_cross_file_related_information();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.warning);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal(
            "[E0001] unresolved name at src/main.v:2:5 (related: alpha binding at src/a.v:3:2; omega binding at src/z.v:1:5)\n",
            output);
    }

    [Fact]
    public void WriteCheckDiagnostics_DetailFormat_ShouldRenderNumericCodeWithCustomDigits()
    {
        using var fixture = DiagnosticFixture.create_input(
            DiagnosticSeverity.warning,
            1,
            "JWT expired",
            "reauthentication required",
            "access token timed out");
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Detail,
            DiagnosticSeverity.warning,
            digits: 3);

        var output = capture_output(() =>
        {
            _ = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);
        });

        Assert.Equal(
            "[W001] JWT expired, reauthentication required, access token timed out\n",
            output.stdout);
        Assert.Equal(string.Empty, output.stderr);
    }

    [Fact]
    public void WriteCheckDiagnostics_PrettyFormat_ShouldRenderNumericCodeWithCustomDigits()
    {
        using var fixture = DiagnosticFixture.create_input(
            DiagnosticSeverity.warning,
            1,
            "JWT expired",
            "reauthentication required",
            "access token timed out");
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Pretty,
            DiagnosticSeverity.warning,
            digits: 3);

        var output = capture_output(() =>
        {
            _ = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);
        });

        Assert.Equal(
            """
            [W001]: JWT expired
               = hint: reauthentication required
               = hint: access token timed out

            """,
            output.stdout);
        Assert.Equal(string.Empty, output.stderr);
    }

    [Fact]
    public void WriteCheckDiagnostics_FatalSeverity_ShouldCountAsError()
    {
        using var fixture = DiagnosticFixture.create(DiagnosticSeverity.fatal, 1);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.trace);

        var output = capture_error_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);

            Assert.Equal(1, counts.errors);
            Assert.Equal(0, counts.warnings);
        });

        Assert.Equal("[F0001] unresolved name at src/main.v:1:5\n", output);
    }

    [Fact]
    public void WriteCheckDiagnostics_MinimumSeverity_ShouldFilterHintDebugAndTrace()
    {
        using var warningFixture = DiagnosticFixture.create(DiagnosticSeverity.warning, 1);
        using var hintFixture = DiagnosticFixture.create(DiagnosticSeverity.hint, 1);
        using var debugFixture = DiagnosticFixture.create(DiagnosticSeverity.debug, 1);
        using var traceFixture = DiagnosticFixture.create(DiagnosticSeverity.trace, 1);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.warning);

        var output = capture_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [warningFixture.diagnostic, hintFixture.diagnostic, debugFixture.diagnostic, traceFixture.diagnostic],
                options,
                warningFixture.project_dir);

            Assert.Equal(0, counts.errors);
            Assert.Equal(1, counts.warnings);
        });

        Assert.Equal("[W0001] unresolved name at src/main.v:1:5\n", output.stdout);
        Assert.Equal(string.Empty, output.stderr);
    }

    [Fact]
    public void WriteCheckDiagnostics_MinimumSeverity_Debug_ShouldIncludeHintAndDebugButSkipTrace()
    {
        using var hintFixture = DiagnosticFixture.create(DiagnosticSeverity.hint, 1);
        using var debugFixture = DiagnosticFixture.create(DiagnosticSeverity.debug, 1);
        using var traceFixture = DiagnosticFixture.create(DiagnosticSeverity.trace, 1);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Short,
            DiagnosticSeverity.debug);

        var output = capture_output(() =>
        {
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [hintFixture.diagnostic, debugFixture.diagnostic, traceFixture.diagnostic],
                options,
                hintFixture.project_dir);

            Assert.Equal(0, counts.errors);
            Assert.Equal(2, counts.warnings);
        });

        Assert.Equal(
            "[H0001] unresolved name at src/main.v:1:5\n[D0001] unresolved name at src/main.v:1:5\n",
            output.stdout);
        Assert.Equal(string.Empty, output.stderr);
    }

    [Fact]
    public void CustomAdapter_ShouldTakeOverHeadingDiagnosticAndSummary()
    {
        using var fixture = DiagnosticFixture.create();
        var adapter = new TestDiagnosticRenderAdapter();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Custom,
            DiagnosticSeverity.warning,
            custom_format: "test",
            custom_adapter: adapter);

        var output = capture_output(() =>
        {
            DiagnosticRenderer.write_context_heading(options, "check", fixture.project_dir, "nyar-x64");
            var counts = DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir);
            DiagnosticRenderer.write_summary(options, "check", counts.errors, counts.warnings);
        });

        Assert.Equal(
            """
            heading:check:nyar-x64
            diagnostic:unresolved name:E0001
            summary:check:1:0
            """,
            output.stdout);
        Assert.Equal(string.Empty, output.stderr);
    }

    [Fact]
    public void CustomFormat_WithoutAdapter_ShouldThrowClearError()
    {
        using var fixture = DiagnosticFixture.create();
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Custom,
            DiagnosticSeverity.warning,
            custom_format: "json");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DiagnosticRenderer.write_check_diagnostics(
                [fixture.diagnostic],
                options,
                fixture.project_dir));

        Assert.Equal("自定义诊断格式 `json` 缺少适配器。", exception.Message);
    }

    private static string capture_error_output(Action action)
    {
        var output = capture_output(action);
        return output.stderr;
    }

    private static (string stdout, string stderr) capture_output(Action action)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        Console.SetOut(stdout);
        Console.SetError(stderr);

        try
        {
            action();
            return (
                normalize_line_endings(stdout.ToString()),
                normalize_line_endings(stderr.ToString()));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string normalize_line_endings(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private sealed class DiagnosticFixture : IDisposable
    {
        public required string project_dir { get; init; }

        public required Diagnostic diagnostic { get; init; }

        public static DiagnosticFixture create(
            DiagnosticSeverity severity = DiagnosticSeverity.error,
            int? code = 1,
            string message = "unresolved name",
            params string[] hints)
        {
            var projectDir = Path.Combine(
                Path.GetTempPath(),
                "sonic-diagnostic-renderer-tests",
                Guid.NewGuid().ToString("N"));
            var sourceDir = Path.Combine(projectDir, "src");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, "main.v");
            File.WriteAllText(filePath, "let name = foo;\n");

            return new DiagnosticFixture
            {
                project_dir = projectDir,
                diagnostic = new Diagnostic(
                    new TextSpan(4, 1),
                    message,
                    severity,
                    code,
                    new DiagnosticSource(
                        filePath,
                        SourceSpan.single_line(1, 5, 1, filePath),
                        hints.Length == 0
                            ? ["define `name` before use", "compiler: test"]
                            : hints))
            };
        }

        public static DiagnosticFixture create_input(
            DiagnosticSeverity severity,
            int? code,
            string message,
            params string[] hints)
        {
            var projectDir = Path.Combine(
                Path.GetTempPath(),
                "sonic-diagnostic-renderer-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(projectDir);

            return new DiagnosticFixture
            {
                project_dir = projectDir,
                diagnostic = new Diagnostic(
                    new TextSpan(0, 0),
                    message,
                    severity,
                    code,
                    new DiagnosticSource(
                        null,
                        default,
                        hints))
            };
        }

        public static DiagnosticFixture create_with_related_information()
        {
            var projectDir = Path.Combine(
                Path.GetTempPath(),
                "sonic-diagnostic-renderer-tests",
                Guid.NewGuid().ToString("N"));
            var sourceDir = Path.Combine(projectDir, "src");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, "main.v");
            var otherFilePath = Path.Combine(sourceDir, "other.v");
            File.WriteAllText(filePath, "let name = foo;\n");
            File.WriteAllText(otherFilePath, "let name = bar;\n");

            return new DiagnosticFixture
            {
                project_dir = projectDir,
                diagnostic = new Diagnostic(
                    new TextSpan(4, 1),
                    "unresolved name",
                    DiagnosticSeverity.error,
                    1,
                    new DiagnosticSource(
                        filePath,
                        SourceSpan.single_line(1, 5, 1, filePath),
                        ["define `name` before use", "compiler: test"],
                        [
                            new DiagnosticRelatedSpan(
                                SourceSpan.single_line(1, 5, 1, otherFilePath),
                                "previous binding",
                                otherFilePath)
                        ]))
            };
        }

        public static DiagnosticFixture create_with_same_file_secondary_annotation()
        {
            var projectDir = Path.Combine(
                Path.GetTempPath(),
                "sonic-diagnostic-renderer-tests",
                Guid.NewGuid().ToString("N"));
            var sourceDir = Path.Combine(projectDir, "src");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, "main.v");
            File.WriteAllText(filePath, "let name = bar;\nlet name = foo;\n");

            return new DiagnosticFixture
            {
                project_dir = projectDir,
                diagnostic = new Diagnostic(
                    new TextSpan(20, 1),
                    "unresolved name",
                    DiagnosticSeverity.error,
                    1,
                    new DiagnosticSource(
                        filePath,
                        new SourceSpan(filePath, 2, 5, 2, 8),
                        ["define `name` before use", "compiler: test"],
                        [
                            new DiagnosticRelatedSpan(
                                new SourceSpan(filePath, 1, 5, 1, 8),
                                "previous binding",
                                filePath)
                        ]))
            };
        }

        public static DiagnosticFixture create_with_same_file_and_cross_file_related_information()
        {
            var projectDir = Path.Combine(
                Path.GetTempPath(),
                "sonic-diagnostic-renderer-tests",
                Guid.NewGuid().ToString("N"));
            var sourceDir = Path.Combine(projectDir, "src");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, "main.v");
            var otherFilePath = Path.Combine(sourceDir, "other.v");
            File.WriteAllText(filePath, "let name = bar;\nlet name = foo;\n");
            File.WriteAllText(otherFilePath, "pub let name = baz;\n");

            return new DiagnosticFixture
            {
                project_dir = projectDir,
                diagnostic = new Diagnostic(
                    new TextSpan(20, 1),
                    "unresolved name",
                    DiagnosticSeverity.error,
                    1,
                    new DiagnosticSource(
                        filePath,
                        new SourceSpan(filePath, 2, 5, 2, 8),
                        ["define `name` before use", "compiler: test"],
                        [
                            new DiagnosticRelatedSpan(
                                new SourceSpan(filePath, 1, 5, 1, 8),
                                "previous binding",
                                filePath),
                            new DiagnosticRelatedSpan(
                                new SourceSpan(otherFilePath, 1, 5, 1, 8),
                                "exported binding",
                                otherFilePath)
                        ]))
            };
        }

        public static DiagnosticFixture create_with_unsorted_cross_file_related_information()
        {
            var projectDir = Path.Combine(
                Path.GetTempPath(),
                "sonic-diagnostic-renderer-tests",
                Guid.NewGuid().ToString("N"));
            var sourceDir = Path.Combine(projectDir, "src");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, "main.v");
            var aFilePath = Path.Combine(sourceDir, "a.v");
            var zFilePath = Path.Combine(sourceDir, "z.v");
            File.WriteAllText(filePath, "let name = bar;\nlet name = foo;\n");
            File.WriteAllText(aFilePath, "let alpha = value;\nline 2\nline 3\n");
            File.WriteAllText(zFilePath, "let omega = value;\n");

            return new DiagnosticFixture
            {
                project_dir = projectDir,
                diagnostic = new Diagnostic(
                    new TextSpan(20, 1),
                    "unresolved name",
                    DiagnosticSeverity.error,
                    1,
                    new DiagnosticSource(
                        filePath,
                        new SourceSpan(filePath, 2, 5, 2, 8),
                        ["define `name` before use", "compiler: test"],
                        [
                            new DiagnosticRelatedSpan(
                                new SourceSpan(zFilePath, 1, 5, 1, 8),
                                "omega binding",
                                zFilePath),
                            new DiagnosticRelatedSpan(
                                new SourceSpan(aFilePath, 3, 2, 3, 6),
                                "alpha binding",
                                aFilePath)
                        ]))
            };
        }

        public void Dispose()
        {
            if (Directory.Exists(project_dir))
            {
                Directory.Delete(project_dir, true);
            }
        }
    }

    private sealed class TestDiagnosticRenderAdapter : IDiagnosticRenderAdapter
    {
        public void write_context_heading(
            DiagnosticRenderOptions options,
            string verb,
            string projectDir,
            string canonicalTriple)
        {
            Console.WriteLine($"heading:{verb}:{canonicalTriple}");
        }

        public void write_diagnostic(
            DiagnosticViewModel viewModel,
            DiagnosticRenderOptions options)
        {
            Console.WriteLine($"diagnostic:{viewModel.message}:{viewModel.display_code}");
        }

        public void write_summary(
            DiagnosticRenderOptions options,
            string toolName,
            int errorCount,
            int warningCount)
        {
            Console.WriteLine($"summary:{toolName}:{errorCount}:{warningCount}");
        }
    }
}
