using System.Xml.Linq;

namespace Std.Template.Report;

#region 数据模型

public class TestRunReport
{
    public string Name { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public List<TestSuiteReport> Suites { get; set; } = [];
    public List<TestCaseReport> Cases { get; set; } = [];
}

public class TestSuiteReport
{
    public string Name { get; set; } = "";
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public TimeSpan Duration { get; set; }
    public List<TestCaseReport> Cases { get; set; } = [];
}

public class TestCaseReport
{
    public string Name { get; set; } = "";
    public string FullyQualifiedName { get; set; } = "";
    public string SuiteName { get; set; } = "";
    public TestOutcome Outcome { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public string StandardOutput { get; set; } = "";
    public string StandardError { get; set; } = "";
}

public enum TestOutcome
{
    Passed,
    Failed,
    Skipped,
    NotFound,
    Inconclusive
}

public class TestReportResult
{
    public string Title { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public int TotalTests { get; init; }
    public int PassedTests { get; init; }
    public int FailedTests { get; init; }
    public List<string> GeneratedFiles { get; init; } = [];
}

#endregion

public sealed class TestReportGenerator
{
    private readonly DejaVuRenderer _renderer;
    private readonly string _sourceDir = "";
    private readonly TemplateManager _templateManager;

    public TestReportGenerator(string? sourceDir = null)
    {
        if (!string.IsNullOrEmpty(sourceDir))
        {
            _sourceDir = Path.GetFullPath(sourceDir);
            var loader = new FileSystemTemplateLoader(_sourceDir);
            _templateManager = new TemplateManager(loader);
        }
        else
        {
            _sourceDir = "";
            var loader = new MemoryTemplateLoader();
            _templateManager = new TemplateManager(loader);
        }

        _renderer = new DejaVuRenderer(DejaVuLanguage.dora, _templateManager);
    }

    public TestReportResult Generate(TestRunReport testRun, string outputDir)
    {
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var context = BuildTestContext(testRun);

        GenerateIndexPage(testRun, context, outputDir);
        GenerateSuitePages(testRun, context, outputDir);
        GenerateFailedPage(testRun, context, outputDir);
        CopyAssets(outputDir);

        return new TestReportResult
        {
            Title = testRun.Name,
            OutputPath = outputDir,
            TotalTests = testRun.Total,
            PassedTests = testRun.Passed,
            FailedTests = testRun.Failed,
            GeneratedFiles = [.. Directory.GetFiles(outputDir, "*.html", SearchOption.AllDirectories)]
        };
    }

    public TestRunReport ParseTrx(string trxPath)
    {
        var doc = XDocument.Load(trxPath);
        var ns = XNamespace.Get("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

        var testRun = new TestRunReport();
        var root = doc.Root;

        if (root is null) return testRun;

        testRun.Name = root.Attribute("name")?.Value ?? "Test Run";

        var times = root.Element(ns + "Times");
        if (times is not null)
        {
            var startStr = times.Attribute("start")?.Value;
            var endStr = times.Attribute("finish")?.Value;

            if (DateTime.TryParse(startStr, out var start)) testRun.StartTime = start;
            if (DateTime.TryParse(endStr, out var end)) testRun.EndTime = end;
            testRun.Duration = testRun.EndTime - testRun.StartTime;
        }

        var counters = root.Element(ns + "ResultSummary")?.Element(ns + "Counters");
        if (counters is not null)
        {
            testRun.Total = ParseInt(counters.Attribute("total")?.Value);
            testRun.Passed = ParseInt(counters.Attribute("passed")?.Value);
            testRun.Failed = ParseInt(counters.Attribute("failed")?.Value);
            testRun.Skipped = ParseInt(counters.Attribute("skipped")?.Value);
        }

        var testDefinitions = root.Element(ns + "TestDefinitions");
        var classNameMap = new Dictionary<string, string>();

        if (testDefinitions is not null)
            foreach (var unitTest in testDefinitions.elements(ns + "UnitTest"))
            {
                var id = unitTest.Attribute("id")?.Value ?? "";
                var testMethod = unitTest.Element(ns + "TestMethod");
                if (testMethod is not null)
                {
                    var className = testMethod.Attribute("className")?.Value ?? "";
                    classNameMap[id] = className;
                }
            }

        var results = root.Element(ns + "Results");
        if (results is not null)
            foreach (var result in results.elements(ns + "UnitTestResult"))
            {
                var testCase = new TestCaseReport
                {
                    Name = result.Attribute("testName")?.Value ?? "",
                    Outcome = ParseOutcome(result.Attribute("outcome")?.Value),
                    Duration = ParseDuration(result.Attribute("duration")?.Value)
                };

                var testId = result.Attribute("testId")?.Value ?? "";
                if (classNameMap.TryGetValue(testId, out var className))
                {
                    testCase.SuiteName = className;
                    testCase.FullyQualifiedName = $"{className}.{testCase.Name}";
                }

                var output = result.Element(ns + "Output");
                if (output is not null)
                {
                    var errorInfo = output.Element(ns + "ErrorInfo");
                    if (errorInfo is not null)
                    {
                        testCase.ErrorMessage = errorInfo.Element(ns + "Message")?.Value ?? "";
                        testCase.StackTrace = errorInfo.Element(ns + "StackTrace")?.Value ?? "";
                    }

                    var stdOut = output.Element(ns + "StdOut");
                    if (stdOut is not null) testCase.StandardOutput = stdOut.Value;

                    var stdErr = output.Element(ns + "StdErr");
                    if (stdErr is not null) testCase.StandardError = stdErr.Value;
                }

                testRun.Cases.Add(testCase);
            }

        var suiteGroups = testRun.Cases.GroupBy(c => c.SuiteName);
        foreach (var group in suiteGroups)
        {
            var suite = new TestSuiteReport
            {
                Name = string.IsNullOrEmpty(group.Key) ? "默认" : group.Key,
                Cases = [.. group],
                Total = group.Count(),
                Passed = group.Count(c => c.Outcome == TestOutcome.Passed),
                Failed = group.Count(c => c.Outcome == TestOutcome.Failed),
                Skipped = group.Count(c => c.Outcome == TestOutcome.Skipped),
                Duration = TimeSpan.FromTicks(group.Sum(c => c.Duration.Ticks))
            };
            testRun.Suites.Add(suite);
        }

        if (testRun.Total == 0 && testRun.Cases.Count > 0)
        {
            testRun.Total = testRun.Cases.Count;
            testRun.Passed = testRun.Cases.Count(c => c.Outcome == TestOutcome.Passed);
            testRun.Failed = testRun.Cases.Count(c => c.Outcome == TestOutcome.Failed);
            testRun.Skipped = testRun.Cases.Count(c => c.Outcome == TestOutcome.Skipped);
        }

        return testRun;
    }

    #region 上下文

    private static Dictionary<string, object> BuildTestContext(TestRunReport testRun)
    {
        var total = Math.Max(testRun.Total, 1);
        return new Dictionary<string, object>
        {
            ["report"] = new Dictionary<string, object>
            {
                ["name"] = testRun.Name,
                ["startTime"] = testRun.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["endTime"] = testRun.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["duration"] = FormatDuration(testRun.Duration),
                ["total"] = testRun.Total,
                ["passed"] = testRun.Passed,
                ["failed"] = testRun.Failed,
                ["skipped"] = testRun.Skipped,
                ["passRate"] = Math.Round((double)testRun.Passed / total * 100, 1),
                ["passedPercent"] = Math.Round((double)testRun.Passed / total * 100, 1),
                ["failedPercent"] = Math.Round((double)testRun.Failed / total * 100, 1),
                ["skippedPercent"] = Math.Round((double)testRun.Skipped / total * 100, 1),
                ["hasFailed"] = testRun.Failed > 0
            },
            ["suites"] = testRun.Suites.Select((s, i) => new Dictionary<string, object>
            {
                ["name"] = s.Name,
                ["index"] = i,
                ["total"] = s.Total,
                ["passed"] = s.Passed,
                ["failed"] = s.Failed,
                ["skipped"] = s.Skipped,
                ["duration"] = FormatDuration(s.Duration)
            }).ToList()
        };
    }

    #endregion

    #region 生成

    private void GenerateIndexPage(TestRunReport testRun, Dictionary<string, object> context, string outputDir)
    {
        var html = RenderWithFallback("test-index", BuiltInTemplates.TestIndex, context);
        File.WriteAllText(Path.Combine(outputDir, "index.html"), html);
    }

    private void GenerateSuitePages(TestRunReport testRun, Dictionary<string, object> context, string outputDir)
    {
        for (var i = 0; i < testRun.Suites.Count; i++)
        {
            var suite = testRun.Suites[i];
            var suiteContext = new Dictionary<string, object>(context)
            {
                ["suite"] = new Dictionary<string, object>
                {
                    ["name"] = suite.Name,
                    ["index"] = i,
                    ["total"] = suite.Total,
                    ["passed"] = suite.Passed,
                    ["failed"] = suite.Failed,
                    ["skipped"] = suite.Skipped,
                    ["duration"] = FormatDuration(suite.Duration),
                    ["cases"] = suite.Cases.Select(c => new Dictionary<string, object>
                    {
                        ["name"] = c.Name,
                        ["outcome"] = c.Outcome.ToString().ToLower(),
                        ["duration"] = FormatDuration(c.Duration),
                        ["errorMessage"] = c.ErrorMessage,
                        ["stackTrace"] = c.StackTrace,
                        ["hasError"] = c.Outcome == TestOutcome.Failed
                    }).ToList()
                }
            };

            var html = RenderWithFallback($"suite-{i}", BuiltInTemplates.TestSuite, suiteContext);
            File.WriteAllText(Path.Combine(outputDir, $"suite-{i}.html"), html);
        }
    }

    private void GenerateFailedPage(TestRunReport testRun, Dictionary<string, object> context, string outputDir)
    {
        var failedCases = testRun.Cases.Where(c => c.Outcome == TestOutcome.Failed).ToList();
        if (failedCases.Count == 0) return;

        var failedContext = new Dictionary<string, object>(context)
        {
            ["failedCases"] = failedCases.Select(c => new Dictionary<string, object>
            {
                ["name"] = c.Name,
                ["suiteName"] = c.SuiteName,
                ["errorMessage"] = c.ErrorMessage,
                ["stackTrace"] = c.StackTrace,
                ["duration"] = FormatDuration(c.Duration)
            }).ToList()
        };

        var html = RenderWithFallback("failed", BuiltInTemplates.TestFailed, failedContext);
        File.WriteAllText(Path.Combine(outputDir, "failed.html"), html);
    }

    #endregion

    #region 工具方法

    private string RenderWithFallback(string templateName, string fallbackTemplate, Dictionary<string, object> context)
    {
        var templatePath = Path.Combine(_sourceDir, "layouts", $"{templateName}.dora");
        if (File.Exists(templatePath))
        {
            var templateContent = File.ReadAllText(templatePath);
            return _renderer.Render(templateContent, context);
        }

        return _renderer.Render(fallbackTemplate, context);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1) return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1) return $"{duration.TotalSeconds:F2}s";
        return $"{duration.TotalMinutes:F1}min";
    }

    private static TestOutcome ParseOutcome(string? outcome)
    {
        return outcome?.ToLower() switch
        {
            "passed" => TestOutcome.Passed,
            "failed" => TestOutcome.Failed,
            "skipped" or "notexecuted" => TestOutcome.Skipped,
            "notfound" => TestOutcome.NotFound,
            "inconclusive" => TestOutcome.Inconclusive,
            _ => TestOutcome.Inconclusive
        };
    }

    private static TimeSpan ParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration)) return TimeSpan.Zero;

        if (TimeSpan.TryParse(duration, out var ts)) return ts;

        if (duration.EndsWith("ms") && double.TryParse(duration[..^2], out var ms))
            return TimeSpan.FromMilliseconds(ms);

        return TimeSpan.Zero;
    }

    private static int ParseInt(string? value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }

    private void CopyAssets(string outputDir)
    {
        var assetsDir = Path.Combine(_sourceDir, "assets");
        if (!Directory.Exists(assetsDir)) return;

        foreach (var file in Directory.GetFiles(assetsDir, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(assetsDir, file);
            var outputPath = Path.Combine(outputDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(file, outputPath, true);
        }
    }

    #endregion
}

file static class BuiltInTemplates
{
    public const string TestIndex = """
                                    <!DOCTYPE html>
                                    <html lang="zh-CN">
                                    <head>
                                    <meta charset="UTF-8">
                                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                    <title><% report.name %> - 测试报告</title>
                                    <style>
                                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; line-height: 1.6; color: #333; }
                                    h1 { border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
                                    .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 15px; margin: 20px 0; }
                                    .card { padding: 15px; border-radius: 8px; text-align: center; }
                                    .card h3 { margin: 0; font-size: 2em; }
                                    .card p { margin: 5px 0 0; color: #666; }
                                    .card.total { background: #e3f2fd; color: #1565c0; }
                                    .card.passed { background: #e8f5e9; color: #2e7d32; }
                                    .card.failed { background: #ffebee; color: #c62828; }
                                    .card.skipped { background: #fff8e1; color: #f57f17; }
                                    .progress { height: 20px; background: #e0e0e0; border-radius: 10px; overflow: hidden; margin: 20px 0; }
                                    .progress-bar { height: 100%; display: flex; }
                                    .progress-passed { background: #4caf50; }
                                    .progress-failed { background: #f44336; }
                                    .progress-skipped { background: #ff9800; }
                                    table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                                    th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #e0e0e0; }
                                    th { background: #f5f5f5; font-weight: 600; }
                                    .badge { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 0.85em; font-weight: 500; }
                                    .badge-passed { background: #e8f5e9; color: #2e7d32; }
                                    .badge-failed { background: #ffebee; color: #c62828; }
                                    .badge-skipped { background: #fff8e1; color: #f57f17; }
                                    a { color: #1976d2; text-decoration: none; }
                                    a:hover { text-decoration: underline; }
                                    .meta { color: #666; font-size: 0.9em; }
                                    </style>
                                    </head>
                                    <body>
                                    <h1><% report.name %> - 测试报告</h1>
                                    <p class="meta">运行时间：<% report.startTime %> | 持续：<% report.duration %> | 通过率：<% report.passRate %>%</p>
                                    <div class="summary">
                                    <div class="card total"><h3><% report.total %></h3><p>总计</p></div>
                                    <div class="card passed"><h3><% report.passed %></h3><p>通过</p></div>
                                    <div class="card failed"><h3><% report.failed %></h3><p>失败</p></div>
                                    <div class="card skipped"><h3><% report.skipped %></h3><p>跳过</p></div>
                                    </div>
                                    <div class="progress">
                                    <div class="progress-bar">
                                    <div class="progress-passed" style="width: <% report.passedPercent %>%"></div>
                                    <div class="progress-failed" style="width: <% report.failedPercent %>%"></div>
                                    <div class="progress-skipped" style="width: <% report.skippedPercent %>%"></div>
                                    </div>
                                    </div>
                                    <h2>测试套件</h2>
                                    <table>
                                    <thead><tr><th>套件</th><th>总计</th><th>通过</th><th>失败</th><th>跳过</th><th>耗时</th></tr></thead>
                                    <tbody>
                                    <% loop suites %>
                                    <tr>
                                    <td><a href="suite-<% item.index %>.html"><% item.name %></a></td>
                                    <td><% item.total %></td>
                                    <td><% item.passed %></td>
                                    <td><% item.failed %></td>
                                    <td><% item.skipped %></td>
                                    <td><% item.duration %></td>
                                    </tr>
                                    <% end loop %>
                                    </tbody>
                                    </table>
                                    <% if report.hasFailed %>
                                    <p><a href="failed.html">查看所有失败测试</a></p>
                                    <% end if %>
                                    </body>
                                    </html>
                                    """;

    public const string TestSuite = """
                                    <!DOCTYPE html>
                                    <html lang="zh-CN">
                                    <head>
                                    <meta charset="UTF-8">
                                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                    <title><% suite.name %> - 测试报告</title>
                                    <style>
                                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; line-height: 1.6; color: #333; }
                                    h1 { border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
                                    a { color: #1976d2; text-decoration: none; }
                                    a:hover { text-decoration: underline; }
                                    table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                                    th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #e0e0e0; }
                                    th { background: #f5f5f5; font-weight: 600; }
                                    .badge { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 0.85em; font-weight: 500; }
                                    .badge-passed { background: #e8f5e9; color: #2e7d32; }
                                    .badge-failed { background: #ffebee; color: #c62828; }
                                    .badge-skipped { background: #fff8e1; color: #f57f17; }
                                    .error-detail { background: #fff5f5; border-left: 3px solid #f44336; padding: 10px 15px; margin: 5px 0 15px; font-family: monospace; font-size: 0.9em; white-space: pre-wrap; }
                                    </style>
                                    </head>
                                    <body>
                                    <h1><% suite.name %></h1>
                                    <p><a href="index.html">← 返回总览</a></p>
                                    <table>
                                    <thead><tr><th>测试</th><th>结果</th><th>耗时</th></tr></thead>
                                    <tbody>
                                    <% loop suite.cases %>
                                    <tr>
                                    <td><% item.name %></td>
                                    <td><span class="badge badge-<% item.outcome %>"><% item.outcome %></span></td>
                                    <td><% item.duration %></td>
                                    </tr>
                                    <% if item.hasError %>
                                    <tr><td colspan="3"><div class="error-detail"><% item.errorMessage %><% if item.stackTrace %>
                                    <% item.stackTrace %><% end if %></div></td></tr>
                                    <% end if %>
                                    <% end loop %>
                                    </tbody>
                                    </table>
                                    </body>
                                    </html>
                                    """;

    public const string TestFailed = """
                                     <!DOCTYPE html>
                                     <html lang="zh-CN">
                                     <head>
                                     <meta charset="UTF-8">
                                     <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                     <title>失败测试 - 测试报告</title>
                                     <style>
                                     body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; line-height: 1.6; color: #333; }
                                     h1 { border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; color: #c62828; }
                                     a { color: #1976d2; text-decoration: none; }
                                     a:hover { text-decoration: underline; }
                                     .failure-card { background: #fff5f5; border: 1px solid #ffcdd2; border-radius: 8px; padding: 15px; margin: 15px 0; }
                                     .failure-card h3 { color: #c62828; margin: 0 0 10px; }
                                     .failure-card .suite { color: #666; font-size: 0.9em; }
                                     .error-message { background: #ffebee; padding: 10px; border-radius: 4px; font-family: monospace; font-size: 0.9em; white-space: pre-wrap; margin: 10px 0; }
                                     .stack-trace { background: #f5f5f5; padding: 10px; border-radius: 4px; font-family: monospace; font-size: 0.85em; white-space: pre-wrap; margin: 10px 0; color: #555; }
                                     </style>
                                     </head>
                                     <body>
                                     <h1>失败测试</h1>
                                     <p><a href="index.html">← 返回总览</a></p>
                                     <% loop failedCases %>
                                     <div class="failure-card">
                                     <h3><% item.name %></h3>
                                     <p class="suite">套件：<% item.suiteName %> | 耗时：<% item.duration %></p>
                                     <div class="error-message"><% item.errorMessage %></div>
                                     <% if item.stackTrace %>
                                     <details><summary>堆栈跟踪</summary><div class="stack-trace"><% item.stackTrace %></div></details>
                                     <% end if %>
                                     </div>
                                     <% end loop %>
                                     </body>
                                     </html>
                                     """;
}