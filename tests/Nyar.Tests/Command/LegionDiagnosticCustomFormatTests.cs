using System.Text.Json;
using Legion.CLI.Commands;
using Std.Data.Text.Diagnostics;

namespace Nyar.Tests.Command;

public sealed class LegionDiagnosticCustomFormatTests : IDisposable
{
    private readonly string _root_dir;

    public LegionDiagnosticCustomFormatTests()
    {
        _root_dir = Path.Combine(Path.GetTempPath(), $"legion-diagnostic-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root_dir);
    }

    [Fact]
    public void TryResolveDiagnosticOptions_CommandLineJson_ShouldUseLegionCustomAdapter()
    {
        var projectDir = create_project(
            """
            { name: "app" }
            """);

        var succeeded = LegionHelper.try_resolve_diagnostic_options(
            projectDir,
            "json",
            null,
            null,
            null,
            out var options,
            out var error);

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Equal(DiagnosticFormat.Custom, options.format);
        Assert.Equal("json", options.custom_format);
        Assert.Same(LegionJsonDiagnosticAdapter.instance, options.custom_adapter);
    }

    [Fact]
    public void ResolveDiagnosticOptions_ProjectManifestJson_ShouldUseLegionCustomAdapter()
    {
        var projectDir = create_project(
            """
            {
                name: "app",
                diagnostics: {
                    format: "json"
                }
            }
            """);

        var options = LegionHelper.resolve_diagnostic_options(projectDir);

        Assert.Equal(DiagnosticFormat.Custom, options.format);
        Assert.Equal("json", options.custom_format);
        Assert.Same(LegionJsonDiagnosticAdapter.instance, options.custom_adapter);
    }

    [Fact]
    public void TryResolveDiagnosticOptions_CommandLinePretty_ShouldClearManifestCustomAdapter()
    {
        var projectDir = create_project(
            """
            {
                name: "app",
                diagnostics: {
                    format: "json"
                }
            }
            """);

        var succeeded = LegionHelper.try_resolve_diagnostic_options(
            projectDir,
            "pretty",
            null,
            null,
            null,
            out var options,
            out var error);

        Assert.True(succeeded);
        Assert.Null(error);
        Assert.Equal(DiagnosticFormat.Pretty, options.format);
        Assert.Null(options.custom_format);
        Assert.Null(options.custom_adapter);
    }

    [Fact]
    public void LegionJsonDiagnosticAdapter_ShouldSuppressHeadingAndHonorVisibilityOptions()
    {
        var projectDir = create_project(
            """
            { name: "app" }
            """);
        var options = new DiagnosticRenderOptions(
            DiagnosticFormat.Custom,
            DiagnosticSeverity.warning,
            show_code: false,
            show_hints: false,
            custom_format: "json",
            custom_adapter: LegionJsonDiagnosticAdapter.instance);
        var viewModel = new DiagnosticViewModel(
            true,
            DiagnosticSeverity.error,
            "error",
            "E0001",
            "unresolved name",
            new DiagnosticLocationViewModel(
                Path.Combine(projectDir, "src", "main.v"),
                "src/main.v",
                1,
                5,
                1,
                8),
            [],
            [],
            ["define `name` before use"]);

        var output = capture_output(() =>
        {
            LegionJsonDiagnosticAdapter.instance.write_context_heading(options, "check", projectDir, "nyar-x64");
            LegionJsonDiagnosticAdapter.instance.write_diagnostic(viewModel, options);
            LegionJsonDiagnosticAdapter.instance.write_summary(options, "check", 1, 0);
        });

        Assert.Equal(string.Empty, output.stderr);

        var lines = output.stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, lines.Length);

        using var diagnosticJson = JsonDocument.Parse(lines[0]);
        Assert.Equal("diagnostic", diagnosticJson.RootElement.GetProperty("type").GetString());
        Assert.Equal("unresolved name", diagnosticJson.RootElement.GetProperty("message").GetString());
        Assert.Equal("src/main.v", diagnosticJson.RootElement.GetProperty("file").GetString());
        Assert.Equal(1, diagnosticJson.RootElement.GetProperty("line").GetInt32());
        Assert.Equal(5, diagnosticJson.RootElement.GetProperty("column").GetInt32());
        Assert.Equal(JsonValueKind.Null, diagnosticJson.RootElement.GetProperty("code").ValueKind);
        Assert.Equal(0, diagnosticJson.RootElement.GetProperty("hints").GetArrayLength());

        using var summaryJson = JsonDocument.Parse(lines[1]);
        Assert.Equal("summary", summaryJson.RootElement.GetProperty("type").GetString());
        Assert.Equal("check", summaryJson.RootElement.GetProperty("tool").GetString());
        Assert.Equal(1, summaryJson.RootElement.GetProperty("errors").GetInt32());
        Assert.Equal(0, summaryJson.RootElement.GetProperty("warnings").GetInt32());
    }

    public void Dispose()
    {
        if (Directory.Exists(_root_dir))
        {
            Directory.Delete(_root_dir, true);
        }
    }

    private string create_project(string manifestContent)
    {
        var projectDir = Path.Combine(_root_dir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), manifestContent);
        return projectDir;
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
}
