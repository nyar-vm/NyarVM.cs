namespace Std.Template.Report;

#region 数据模型

public class BenchmarkReportData
{
    public string Title { get; set; } = "性能基准报告";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string HostEnvironment { get; set; } = "";
    public List<BenchmarkGroup> Groups { get; set; } = [];
    public List<BenchmarkCase> AllBenchmarks { get; set; } = [];
}

public class BenchmarkGroup
{
    public string Name { get; set; } = "";
    public List<BenchmarkCase> Benchmarks { get; set; } = [];
}

public class BenchmarkCase
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Type { get; set; } = "";
    public string Method { get; set; } = "";
    public string Parameters { get; set; } = "";
    public BenchmarkStatistics Statistics { get; set; } = new();
    public BenchmarkMemory Memory { get; set; } = new();
    public List<BenchmarkMetric> Metrics { get; set; } = [];
}

public class BenchmarkStatistics
{
    public double Mean { get; set; }
    public double StdDev { get; set; }
    public double StdErr { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Median { get; set; }
    public double Q1 { get; set; }
    public double Q3 { get; set; }
    public double P95 { get; set; }
    public int N { get; set; }
    public string Unit { get; set; } = "ns";
}

public class BenchmarkMemory
{
    public long Gen0Collections { get; set; }
    public long Gen1Collections { get; set; }
    public long Gen2Collections { get; set; }
    public long TotalOperations { get; set; }
    public long BytesAllocatedPerOperation { get; set; }
}

public class BenchmarkMetric
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public string Unit { get; set; } = "";
}

public class BenchmarkReportResult
{
    public string Title { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public int BenchmarkCount { get; init; }
    public List<string> GeneratedFiles { get; init; } = [];
}

#endregion

public sealed class BenchmarkReportGenerator
{
    private readonly DejaVuRenderer _renderer;
    private readonly string _sourceDir = "";
    private readonly TemplateManager _templateManager;

    public BenchmarkReportGenerator(string? sourceDir = null)
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

    public BenchmarkReportResult Generate(BenchmarkReportData benchmark, string outputDir)
    {
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var context = BuildBenchmarkContext(benchmark);

        GenerateIndexPage(benchmark, context, outputDir);
        GenerateGroupPages(benchmark, context, outputDir);
        CopyAssets(outputDir);

        return new BenchmarkReportResult
        {
            Title = benchmark.Title,
            OutputPath = outputDir,
            BenchmarkCount = benchmark.AllBenchmarks.Count,
            GeneratedFiles = [.. Directory.GetFiles(outputDir, "*.html", SearchOption.AllDirectories)]
        };
    }

    public BenchmarkReportData ParseBenchmarkDotNet(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var parser = new JsonParser();
        var parseResult = parser.Parse(json);

        var report = new BenchmarkReportData
        {
            Title = "性能基准报告",
            GeneratedAt = DateTime.Now
        };

        if (!parseResult.Success || parseResult.Value is not JsonObject root) return report;

        if (root.TryGetValue("HostEnvironmentInfo", out var hostEnv) && hostEnv is JsonString hostEnvStr)
            report.HostEnvironment = hostEnvStr.Value;

        if (!root.TryGetValue("Benchmarks", out var benchmarksValue) ||
            benchmarksValue is not JsonArray benchmarks) return report;

        foreach (var bmValue in benchmarks.Items)
        {
            if (bmValue is not JsonObject bm) continue;

            var benchmarkCase = new BenchmarkCase
            {
                FullName = DataConvert.GetJsonString(bm, "FullName"),
                Type = DataConvert.GetJsonString(bm, "Type"),
                Method = DataConvert.GetJsonString(bm, "Method")
            };

            benchmarkCase.Name = $"{benchmarkCase.type}.{benchmarkCase.Method}";

            if (bm.TryGetValue("Parameters", out var paramsVal) && paramsVal is JsonString paramsStr)
            {
                benchmarkCase.Parameters = paramsStr.Value;
                benchmarkCase.Name += $"({benchmarkCase.Parameters})";
            }

            if (bm.TryGetValue("Statistics", out var statsVal) && statsVal is JsonObject stats)
                benchmarkCase.Statistics = new BenchmarkStatistics
                {
                    Mean = DataConvert.GetJsonDouble(stats, "Mean"),
                    StdDev = DataConvert.GetJsonDouble(stats, "StandardDeviation"),
                    StdErr = DataConvert.GetJsonDouble(stats, "StandardError"),
                    Min = DataConvert.GetJsonDouble(stats, "Min"),
                    Max = DataConvert.GetJsonDouble(stats, "Max"),
                    Median = DataConvert.GetJsonDouble(stats, "Median"),
                    Q1 = DataConvert.GetJsonDouble(stats, "Q1"),
                    Q3 = DataConvert.GetJsonDouble(stats, "Q3"),
                    P95 = DataConvert.GetJsonDouble(stats, "P95"),
                    N = DataConvert.GetJsonInt(stats, "N"),
                    Unit = "ns"
                };

            if (bm.TryGetValue("Memory", out var memoryVal) && memoryVal is JsonObject memory)
                benchmarkCase.Memory = new BenchmarkMemory
                {
                    Gen0Collections = DataConvert.GetJsonLong(memory, "Gen0Collections"),
                    Gen1Collections = DataConvert.GetJsonLong(memory, "Gen1Collections"),
                    Gen2Collections = DataConvert.GetJsonLong(memory, "Gen2Collections"),
                    TotalOperations = DataConvert.GetJsonLong(memory, "TotalOperations"),
                    BytesAllocatedPerOperation = DataConvert.GetJsonLong(memory, "BytesAllocatedPerOp")
                };

            if (bm.TryGetValue("Metrics", out var metricsVal) && metricsVal is JsonArray metrics)
                foreach (var metricValue in metrics.Items)
                {
                    if (metricValue is not JsonObject metric) continue;

                    var name = "";
                    if (metric.TryGetValue("Descriptor", out var descVal) && descVal is JsonObject desc)
                        name = DataConvert.GetJsonString(desc, "DisplayName");

                    var value = metric.TryGetValue("Value", out var v) && v is JsonNumber vn ? vn.Value : 0;
                    var unit = "";
                    if (metric.TryGetValue("Descriptor", out var d2Val) && d2Val is JsonObject d2)
                        unit = DataConvert.GetJsonString(d2, "Unit");

                    benchmarkCase.Metrics.Add(new BenchmarkMetric
                    {
                        Name = name,
                        Value = value,
                        Unit = unit
                    });
                }

            report.AllBenchmarks.Add(benchmarkCase);
        }

        var typeGroups = report.AllBenchmarks.GroupBy(b => b.type);
        foreach (var group in typeGroups)
            report.Groups.Add(new BenchmarkGroup
            {
                Name = group.Key,
                Benchmarks = [.. group]
            });

        return report;
    }

    #region 上下文

    private static Dictionary<string, object> BuildBenchmarkContext(BenchmarkReportData benchmark)
    {
        return new Dictionary<string, object>
        {
            ["report"] = new Dictionary<string, object>
            {
                ["title"] = benchmark.Title,
                ["generatedAt"] = benchmark.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["hostEnvironment"] = benchmark.HostEnvironment,
                ["totalBenchmarks"] = benchmark.AllBenchmarks.Count,
                ["groupCount"] = benchmark.Groups.Count
            },
            ["groups"] = benchmark.Groups.Select((g, i) => new Dictionary<string, object>
            {
                ["name"] = g.Name,
                ["index"] = i,
                ["benchmarkCount"] = g.Benchmarks.Count,
                ["fastest"] = g.Benchmarks.Count > 0
                    ? FormatTime(g.Benchmarks.Min(b => b.Statistics.Mean))
                    : "-",
                ["slowest"] = g.Benchmarks.Count > 0
                    ? FormatTime(g.Benchmarks.Max(b => b.Statistics.Mean))
                    : "-"
            }).ToList()
        };
    }

    #endregion

    #region 生成

    private void GenerateIndexPage(BenchmarkReportData benchmark, Dictionary<string, object> context, string outputDir)
    {
        var html = RenderWithFallback("benchmark-index", BenchmarkBuiltInTemplates.BenchmarkIndex, context);
        File.WriteAllText(Path.Combine(outputDir, "index.html"), html);
    }

    private void GenerateGroupPages(BenchmarkReportData benchmark, Dictionary<string, object> context, string outputDir)
    {
        for (var i = 0; i < benchmark.Groups.Count; i++)
        {
            var group = benchmark.Groups[i];
            var groupContext = new Dictionary<string, object>(context)
            {
                ["group"] = new Dictionary<string, object>
                {
                    ["name"] = group.Name,
                    ["index"] = i,
                    ["benchmarks"] = group.Benchmarks.Select(b => new Dictionary<string, object>
                    {
                        ["name"] = b.Name,
                        ["method"] = b.Method,
                        ["parameters"] = b.Parameters,
                        ["mean"] = FormatTime(b.Statistics.Mean),
                        ["stdDev"] = FormatTime(b.Statistics.StdDev),
                        ["median"] = FormatTime(b.Statistics.Median),
                        ["min"] = FormatTime(b.Statistics.Min),
                        ["max"] = FormatTime(b.Statistics.Max),
                        ["p95"] = FormatTime(b.Statistics.P95),
                        ["n"] = b.Statistics.N,
                        ["gen0"] = b.Memory.Gen0Collections,
                        ["gen1"] = b.Memory.Gen1Collections,
                        ["gen2"] = b.Memory.Gen2Collections,
                        ["allocated"] = b.Memory.BytesAllocatedPerOperation > 0
                            ? FormatBytes(b.Memory.BytesAllocatedPerOperation)
                            : "-",
                        ["allocatedBytes"] = b.Memory.BytesAllocatedPerOperation,
                        ["hasMemory"] = b.Memory.BytesAllocatedPerOperation > 0 || b.Memory.Gen0Collections > 0,
                        ["metrics"] = b.Metrics.Select(m => new Dictionary<string, object>
                        {
                            ["name"] = m.Name,
                            ["value"] = m.Value.ToString("F3"),
                            ["unit"] = m.Unit
                        }).ToList(),
                        ["hasMetrics"] = b.Metrics.Count > 0
                    }).ToList()
                }
            };

            var html = RenderWithFallback($"group-{i}", BenchmarkBuiltInTemplates.BenchmarkGroup, groupContext);
            File.WriteAllText(Path.Combine(outputDir, $"group-{i}.html"), html);
        }
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

    private static string FormatTime(double nanoseconds)
    {
        if (nanoseconds == 0) return "0ns";
        if (nanoseconds < 1) return $"{nanoseconds * 1000:F2}ps";
        if (nanoseconds < 1000) return $"{nanoseconds:F2}ns";
        if (nanoseconds < 1_000_000) return $"{nanoseconds / 1000:F2}μs";
        if (nanoseconds < 1_000_000_000) return $"{nanoseconds / 1_000_000:F2}ms";
        return $"{nanoseconds / 1_000_000_000:F2}s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1}KB";
        return $"{bytes / (1024.0 * 1024.0):F1}MB";
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

file static class BenchmarkBuiltInTemplates
{
    public const string BenchmarkIndex = """
                                         <!DOCTYPE html>
                                         <html lang="zh-CN">
                                         <head>
                                         <meta charset="UTF-8">
                                         <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                         <title><% report.title %></title>
                                         <style>
                                         body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; line-height: 1.6; color: #333; }
                                         h1 { border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
                                         .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 15px; margin: 20px 0; }
                                         .card { padding: 15px; border-radius: 8px; text-align: center; background: #e3f2fd; color: #1565c0; }
                                         .card h3 { margin: 0; font-size: 2em; }
                                         .card p { margin: 5px 0 0; color: #666; }
                                         table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                                         th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #e0e0e0; }
                                         th { background: #f5f5f5; font-weight: 600; }
                                         a { color: #1976d2; text-decoration: none; }
                                         a:hover { text-decoration: underline; }
                                         .meta { color: #666; font-size: 0.9em; }
                                         </style>
                                         </head>
                                         <body>
                                         <h1><% report.title %></h1>
                                         <p class="meta">生成时间：<% report.generatedAt %> | 基准数：<% report.totalBenchmarks %> | 分组数：<% report.groupCount %></p>

                                         <div class="summary">
                                         <div class="card"><h3><% report.totalBenchmarks %></h3><p>基准测试</p></div>
                                         <div class="card"><h3><% report.groupCount %></h3><p>分组</p></div>
                                         </div>

                                         <h2>基准分组</h2>
                                         <table>
                                         <thead><tr><th>类型</th><th>基准数</th><th>最快</th><th>最慢</th></tr></thead>
                                         <tbody>
                                         <% loop groups %>
                                         <tr>
                                         <td><a href="group-<% item.index %>.html"><% item.name %></a></td>
                                         <td><% item.benchmarkCount %></td>
                                         <td><% item.fastest %></td>
                                         <td><% item.slowest %></td>
                                         </tr>
                                         <% end loop %>
                                         </tbody>
                                         </table>
                                         </body>
                                         </html>
                                         """;

    public const string BenchmarkGroup = """
                                         <!DOCTYPE html>
                                         <html lang="zh-CN">
                                         <head>
                                         <meta charset="UTF-8">
                                         <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                         <title><% group.name %> - 性能基准报告</title>
                                         <style>
                                         body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; line-height: 1.6; color: #333; }
                                         h1 { border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
                                         a { color: #1976d2; text-decoration: none; }
                                         a:hover { text-decoration: underline; }
                                         table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                                         th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #e0e0e0; }
                                         th { background: #f5f5f5; font-weight: 600; }
                                         .time-fast { color: #2e7d32; }
                                         .time-medium { color: #f57f17; }
                                         .time-slow { color: #c62828; }
                                         .bar { display: inline-block; height: 8px; background: #4caf50; border-radius: 4px; vertical-align: middle; }
                                         .memory { color: #7b1fa2; font-size: 0.9em; }
                                         details { margin: 5px 0; }
                                         summary { cursor: pointer; color: #1976d2; }
                                         .metric-table { margin: 5px 0 15px 20px; width: calc(100% - 20px); }
                                         .metric-table th, .metric-table td { padding: 6px 10px; font-size: 0.9em; }
                                         </style>
                                         </head>
                                         <body>
                                         <h1><% group.name %></h1>
                                         <p><a href="index.html">← 返回总览</a></p>

                                         <table>
                                         <thead><tr><th>方法</th><th>均值</th><th>标准差</th><th>中位数</th><th>P95</th><th>内存</th><th>GC</th></tr></thead>
                                         <tbody>
                                         <% loop group.benchmarks %>
                                         <tr>
                                         <td><% item.method %><% if item.parameters %><br><small style="color:#666"><% item.parameters %></small><% end if %></td>
                                         <td><% item.mean %></td>
                                         <td><% item.stdDev %></td>
                                         <td><% item.median %></td>
                                         <td><% item.p95 %></td>
                                         <td class="memory"><% item.allocated %></td>
                                         <td><% if item.hasMemory %>Gen0:<% item.gen0 %> Gen1:<% item.gen1 %> Gen2:<% item.gen2 %><% end if %></td>
                                         </tr>
                                         <% if item.hasMetrics %>
                                         <tr><td colspan="7">
                                         <details><summary>详细指标 (<% item.metrics.size %>)</summary>
                                         <table class="metric-table">
                                         <thead><tr><th>指标</th><th>值</th><th>单位</th></tr></thead>
                                         <tbody>
                                         <% loop item.metrics %>
                                         <tr><td><% item.name %></td><td><% item.value %></td><td><% item.unit %></td></tr>
                                         <% end loop %>
                                         </tbody>
                                         </table>
                                         </details>
                                         </td></tr>
                                         <% end if %>
                                         <% end loop %>
                                         </tbody>
                                         </table>
                                         </body>
                                         </html>
                                         """;
}