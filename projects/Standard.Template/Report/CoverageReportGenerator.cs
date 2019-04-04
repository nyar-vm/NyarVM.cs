using System.Xml.Linq;

namespace Std.Template.Report;

#region 数据模型

public class CoverageReportData
{
    public string Name { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public double LineRate { get; set; }
    public double BranchRate { get; set; }
    public int LinesCovered { get; set; }
    public int LinesValid { get; set; }
    public int BranchesCovered { get; set; }
    public int BranchesValid { get; set; }
    public double Complexity { get; set; }
    public List<CoveragePackage> Packages { get; set; } = [];
}

public class CoveragePackage
{
    public string Name { get; set; } = "";
    public double LineRate { get; set; }
    public double BranchRate { get; set; }
    public double Complexity { get; set; }
    public List<CoverageClass> Classes { get; set; } = [];
}

public class CoverageClass
{
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public double LineRate { get; set; }
    public double BranchRate { get; set; }
    public double Complexity { get; set; }
    public List<CoverageMethod> Methods { get; set; } = [];
    public List<CoverageLine> Lines { get; set; } = [];
}

public class CoverageMethod
{
    public string Name { get; set; } = "";
    public string Signature { get; set; } = "";
    public double LineRate { get; set; }
    public double BranchRate { get; set; }
    public double Complexity { get; set; }
}

public class CoverageLine
{
    public int Number { get; set; }
    public int Hits { get; set; }
    public bool IsBranch { get; set; }
    public bool IsCovered => Hits > 0;
}

public class CoverageReportResult
{
    public string Title { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public double LineRate { get; init; }
    public double BranchRate { get; init; }
    public List<string> GeneratedFiles { get; init; } = [];
}

#endregion

public sealed class CoverageReportGenerator
{
    private readonly DejaVuRenderer _renderer;
    private readonly string _sourceDir = "";
    private readonly TemplateManager _templateManager;

    public CoverageReportGenerator(string? sourceDir = null)
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

    public CoverageReportResult Generate(CoverageReportData coverage, string outputDir)
    {
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        var context = BuildCoverageContext(coverage);

        GenerateIndexPage(coverage, context, outputDir);
        GeneratePackagePages(coverage, context, outputDir);
        CopyAssets(outputDir);

        return new CoverageReportResult
        {
            Title = coverage.Name,
            OutputPath = outputDir,
            LineRate = coverage.LineRate,
            BranchRate = coverage.BranchRate,
            GeneratedFiles = [.. Directory.GetFiles(outputDir, "*.html", SearchOption.AllDirectories)]
        };
    }

    public CoverageReportData ParseCobertura(string coberturaPath)
    {
        var doc = XDocument.Load(coberturaPath);
        var root = doc.Root;

        var coverage = new CoverageReportData
        {
            Name = "代码覆盖率报告"
        };

        if (root is null) return coverage;

        coverage.LineRate = ParseDouble(root.Attribute("line-rate")?.Value);
        coverage.BranchRate = ParseDouble(root.Attribute("branch-rate")?.Value);
        coverage.LinesCovered = ParseInt(root.Attribute("lines-covered")?.Value);
        coverage.LinesValid = ParseInt(root.Attribute("lines-valid")?.Value);
        coverage.BranchesCovered = ParseInt(root.Attribute("branches-covered")?.Value);
        coverage.BranchesValid = ParseInt(root.Attribute("branches-valid")?.Value);
        coverage.Complexity = ParseDouble(root.Attribute("complexity")?.Value);

        var sources = root.Element("sources");
        var sourceDirs = new List<string>();
        if (sources is not null)
            foreach (var source in sources.elements("source"))
                if (!string.IsNullOrEmpty(source.Value))
                    sourceDirs.Add(source.Value);

        var packagesElement = root.Element("packages");
        if (packagesElement is null) return coverage;

        foreach (var pkgElement in packagesElement.elements("package"))
        {
            var package = new CoveragePackage
            {
                Name = pkgElement.Attribute("name")?.Value ?? "",
                LineRate = ParseDouble(pkgElement.Attribute("line-rate")?.Value),
                BranchRate = ParseDouble(pkgElement.Attribute("branch-rate")?.Value),
                Complexity = ParseDouble(pkgElement.Attribute("complexity")?.Value)
            };

            var classesElement = pkgElement.Element("classes");
            if (classesElement is not null)
                foreach (var clsElement in classesElement.elements("class"))
                {
                    var cls = new CoverageClass
                    {
                        Name = clsElement.Attribute("name")?.Value ?? "",
                        FileName = clsElement.Attribute("filename")?.Value ?? "",
                        LineRate = ParseDouble(clsElement.Attribute("line-rate")?.Value),
                        BranchRate = ParseDouble(clsElement.Attribute("branch-rate")?.Value),
                        Complexity = ParseDouble(clsElement.Attribute("complexity")?.Value)
                    };

                    var methodsElement = clsElement.Element("methods");
                    if (methodsElement is not null)
                        foreach (var methodElement in methodsElement.elements("method"))
                        {
                            var method = new CoverageMethod
                            {
                                Name = methodElement.Attribute("name")?.Value ?? "",
                                Signature = methodElement.Attribute("signature")?.Value ?? "",
                                LineRate = ParseDouble(methodElement.Attribute("line-rate")?.Value),
                                BranchRate = ParseDouble(methodElement.Attribute("branch-rate")?.Value),
                                Complexity = ParseDouble(methodElement.Attribute("complexity")?.Value)
                            };
                            cls.Methods.Add(method);
                        }

                    var linesElement = clsElement.Element("lines");
                    if (linesElement is not null)
                        foreach (var lineElement in linesElement.elements("line"))
                        {
                            var line = new CoverageLine
                            {
                                Number = ParseInt(lineElement.Attribute("number")?.Value),
                                Hits = ParseInt(lineElement.Attribute("hits")?.Value),
                                IsBranch = lineElement.Attribute("branch")?.Value == "true"
                            };
                            cls.Lines.Add(line);
                        }

                    package.Classes.Add(cls);
                }

            coverage.Packages.Add(package);
        }

        return coverage;
    }

    #region 上下文

    private static Dictionary<string, object> BuildCoverageContext(CoverageReportData coverage)
    {
        return new Dictionary<string, object>
        {
            ["report"] = new Dictionary<string, object>
            {
                ["name"] = coverage.Name,
                ["generatedAt"] = coverage.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["lineRate"] = FormatPercent(coverage.LineRate),
                ["branchRate"] = FormatPercent(coverage.BranchRate),
                ["lineRateRaw"] = coverage.LineRate,
                ["branchRateRaw"] = coverage.BranchRate,
                ["linesCovered"] = coverage.LinesCovered,
                ["linesValid"] = coverage.LinesValid,
                ["branchesCovered"] = coverage.BranchesCovered,
                ["branchesValid"] = coverage.BranchesValid,
                ["complexity"] = coverage.Complexity,
                ["lineRateClass"] = GetCoverageClass(coverage.LineRate),
                ["branchRateClass"] = GetCoverageClass(coverage.BranchRate)
            },
            ["packages"] = coverage.Packages.Select((p, i) => new Dictionary<string, object>
            {
                ["name"] = p.Name,
                ["index"] = i,
                ["lineRate"] = FormatPercent(p.LineRate),
                ["branchRate"] = FormatPercent(p.BranchRate),
                ["complexity"] = p.Complexity,
                ["classCount"] = p.Classes.Count,
                ["lineRateClass"] = GetCoverageClass(p.LineRate)
            }).ToList()
        };
    }

    #endregion

    #region 生成

    private void GenerateIndexPage(CoverageReportData coverage, Dictionary<string, object> context, string outputDir)
    {
        var html = RenderWithFallback("coverage-index", CoverageBuiltInTemplates.CoverageIndex, context);
        File.WriteAllText(Path.Combine(outputDir, "index.html"), html);
    }

    private void GeneratePackagePages(CoverageReportData coverage, Dictionary<string, object> context, string outputDir)
    {
        for (var i = 0; i < coverage.Packages.Count; i++)
        {
            var package = coverage.Packages[i];
            var packageContext = new Dictionary<string, object>(context)
            {
                ["package"] = new Dictionary<string, object>
                {
                    ["name"] = package.Name,
                    ["index"] = i,
                    ["lineRate"] = FormatPercent(package.LineRate),
                    ["branchRate"] = FormatPercent(package.BranchRate),
                    ["complexity"] = package.Complexity,
                    ["classes"] = package.Classes.Select(c => new Dictionary<string, object>
                    {
                        ["name"] = c.Name,
                        ["fileName"] = c.FileName,
                        ["lineRate"] = FormatPercent(c.LineRate),
                        ["branchRate"] = FormatPercent(c.BranchRate),
                        ["complexity"] = c.Complexity,
                        ["coveredLines"] = c.Lines.Count(l => l.IsCovered),
                        ["totalLines"] = c.Lines.Count,
                        ["lineRateClass"] = GetCoverageClass(c.LineRate),
                        ["methods"] = c.Methods.Select(m => new Dictionary<string, object>
                        {
                            ["name"] = m.Name,
                            ["lineRate"] = FormatPercent(m.LineRate),
                            ["branchRate"] = FormatPercent(m.BranchRate),
                            ["complexity"] = m.Complexity
                        }).ToList()
                    }).ToList()
                }
            };

            var html = RenderWithFallback($"package-{i}", CoverageBuiltInTemplates.CoveragePackage, packageContext);
            var safeName = string.Join("_", package.Name.Split(Path.GetInvalidFileNameChars()));
            File.WriteAllText(Path.Combine(outputDir, $"package-{i}.html"), html);
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

    private static string FormatPercent(double rate)
    {
        return $"{rate * 100:F1}%";
    }

    private static string GetCoverageClass(double rate)
    {
        if (rate >= 0.8) return "coverage-high";
        if (rate >= 0.6) return "coverage-medium";
        if (rate >= 0.4) return "coverage-low";
        return "coverage-critical";
    }

    private static double ParseDouble(string? value)
    {
        return double.TryParse(value, out var result) ? result : 0;
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

file static class CoverageBuiltInTemplates
{
    public const string CoverageIndex = """
                                        <!DOCTYPE html>
                                        <html lang="zh-CN">
                                        <head>
                                        <meta charset="UTF-8">
                                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                        <title><% report.name %> - 覆盖率报告</title>
                                        <style>
                                        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; line-height: 1.6; color: #333; }
                                        h1 { border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
                                        .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 15px; margin: 20px 0; }
                                        .card { padding: 15px; border-radius: 8px; text-align: center; }
                                        .card h3 { margin: 0; font-size: 2em; }
                                        .card p { margin: 5px 0 0; color: #666; }
                                        .coverage-high { background: #e8f5e9; color: #2e7d32; }
                                        .coverage-medium { background: #fff8e1; color: #f57f17; }
                                        .coverage-low { background: #fff3e0; color: #e65100; }
                                        .coverage-critical { background: #ffebee; color: #c62828; }
                                        .progress { height: 24px; background: #e0e0e0; border-radius: 12px; overflow: hidden; margin: 10px 0; position: relative; }
                                        .progress-fill { height: 100%; border-radius: 12px; transition: width 0.3s; }
                                        .progress-label { position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); font-size: 0.85em; font-weight: 600; }
                                        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                                        th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #e0e0e0; }
                                        th { background: #f5f5f5; font-weight: 600; }
                                        a { color: #1976d2; text-decoration: none; }
                                        a:hover { text-decoration: underline; }
                                        .meta { color: #666; font-size: 0.9em; }
                                        .rate-bar { display: inline-block; width: 80px; height: 8px; background: #e0e0e0; border-radius: 4px; overflow: hidden; vertical-align: middle; margin-right: 6px; }
                                        .rate-fill { height: 100%; border-radius: 4px; }
                                        .rate-fill-high { background: #4caf50; }
                                        .rate-fill-medium { background: #ff9800; }
                                        .rate-fill-low { background: #ff5722; }
                                        .rate-fill-critical { background: #f44336; }
                                        </style>
                                        </head>
                                        <body>
                                        <h1><% report.name %></h1>
                                        <p class="meta">生成时间：<% report.generatedAt %></p>

                                        <div class="summary">
                                        <div class="card <% report.lineRateClass %>">
                                        <h3><% report.lineRate %></h3><p>行覆盖率</p>
                                        <div class="progress"><div class="progress-fill" style="width: <% report.lineRateRaw * 100 %>%; background: #4caf50;"></div><span class="progress-label"><% report.linesCovered %>/<% report.linesValid %></span></div>
                                        </div>
                                        <div class="card <% report.branchRateClass %>">
                                        <h3><% report.branchRate %></h3><p>分支覆盖率</p>
                                        <div class="progress"><div class="progress-fill" style="width: <% report.branchRateRaw * 100 %>%; background: #2196f3;"></div><span class="progress-label"><% report.branchesCovered %>/<% report.branchesValid %></span></div>
                                        </div>
                                        <div class="card" style="background:#f3e5f5;color:#7b1fa2;">
                                        <h3><% report.complexity %></h3><p>复杂度</p>
                                        </div>
                                        </div>

                                        <h2>包覆盖率</h2>
                                        <table>
                                        <thead><tr><th>包</th><th>行覆盖率</th><th>分支覆盖率</th><th>复杂度</th><th>类数</th></tr></thead>
                                        <tbody>
                                        <% loop packages %>
                                        <tr>
                                        <td><a href="package-<% item.index %>.html"><% item.name %></a></td>
                                        <td><span class="rate-bar"><span class="rate-fill rate-fill-<% item.lineRateClass %>" style="width:<% item.lineRate %>"></span></span><% item.lineRate %></td>
                                        <td><% item.branchRate %></td>
                                        <td><% item.complexity %></td>
                                        <td><% item.classCount %></td>
                                        </tr>
                                        <% end loop %>
                                        </tbody>
                                        </table>
                                        </body>
                                        </html>
                                        """;

    public const string CoveragePackage = """
                                          <!DOCTYPE html>
                                          <html lang="zh-CN">
                                          <head>
                                          <meta charset="UTF-8">
                                          <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                          <title><% package.name %> - 覆盖率报告</title>
                                          <style>
                                          body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 1200px; margin: 0 auto; padding: 20px; line-height: 1.6; color: #333; }
                                          h1 { border-bottom: 2px solid #e0e0e0; padding-bottom: 10px; }
                                          a { color: #1976d2; text-decoration: none; }
                                          a:hover { text-decoration: underline; }
                                          table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                                          th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #e0e0e0; }
                                          th { background: #f5f5f5; font-weight: 600; }
                                          .coverage-high { color: #2e7d32; }
                                          .coverage-medium { color: #f57f17; }
                                          .coverage-low { color: #e65100; }
                                          .coverage-critical { color: #c62828; }
                                          .rate-bar { display: inline-block; width: 80px; height: 8px; background: #e0e0e0; border-radius: 4px; overflow: hidden; vertical-align: middle; margin-right: 6px; }
                                          .rate-fill { height: 100%; border-radius: 4px; }
                                          .rate-fill-high { background: #4caf50; }
                                          .rate-fill-medium { background: #ff9800; }
                                          .rate-fill-low { background: #ff5722; }
                                          .rate-fill-critical { background: #f44336; }
                                          details { margin: 5px 0; }
                                          summary { cursor: pointer; color: #1976d2; }
                                          .method-table { margin: 5px 0 15px 20px; width: calc(100% - 20px); }
                                          .method-table th, .method-table td { padding: 6px 10px; font-size: 0.9em; }
                                          </style>
                                          </head>
                                          <body>
                                          <h1><% package.name %></h1>
                                          <p><a href="index.html">← 返回总览</a> | 行覆盖率：<% package.lineRate %> | 分支覆盖率：<% package.branchRate %></p>

                                          <table>
                                          <thead><tr><th>类</th><th>文件</th><th>行覆盖率</th><th>分支覆盖率</th><th>行数</th></tr></thead>
                                          <tbody>
                                          <% loop package.classes %>
                                          <tr>
                                          <td><% item.name %></td>
                                          <td style="font-size:0.85em;color:#666;"><% item.fileName %></td>
                                          <td class="<% item.lineRateClass %>"><span class="rate-bar"><span class="rate-fill rate-fill-<% item.lineRateClass %>" style="width:<% item.lineRate %>"></span></span><% item.lineRate %></td>
                                          <td class="<% item.lineRateClass %>"><% item.branchRate %></td>
                                          <td><% item.coveredLines %>/<% item.totalLines %></td>
                                          </tr>
                                          <% if item.methods.size > 0 %>
                                          <tr><td colspan="5">
                                          <details><summary>方法详情 (<% item.methods.size %>)</summary>
                                          <table class="method-table">
                                          <thead><tr><th>方法</th><th>行覆盖率</th><th>分支覆盖率</th><th>复杂度</th></tr></thead>
                                          <tbody>
                                          <% loop item.methods %>
                                          <tr><td><% item.name %></td><td><% item.lineRate %></td><td><% item.branchRate %></td><td><% item.complexity %></td></tr>
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