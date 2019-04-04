using Oak.Diagnostics;
using Oak.Testing;
using Oak.Yaml;

namespace Oak.Yaml.Tests;

public abstract class YamlTestBase : TestBase
{
    protected (YamlValue? Value, DiagnosticSink Diagnostics) ParseWithTimeout(string source)
    {
        var diagnostics = new DiagnosticSink();
        var value = ExecuteWithTimeout(() =>
        {
            var parser = new YamlParser();
            var result = parser.Parse(source, diagnostics);
            return result.Value;
        }, "YAML 解析器");
        return (value, diagnostics);
    }
}
