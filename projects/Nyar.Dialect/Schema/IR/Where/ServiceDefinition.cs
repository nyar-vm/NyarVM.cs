using Nyar.Dialect.Schema.IR.Common;

namespace Nyar.Dialect.Schema.IR.Where;

public sealed class ServiceDefinition
{
    public ServiceDefinition(string name, IReadOnlyList<EndpointDefinition> endpoints,
        IReadOnlyList<AttributeDefinition>? attributes = null, string version = "1.0",
        IReadOnlyList<string>? deprecatedEndpoints = null, int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        this.endpoints = endpoints;
        this.attributes = attributes ?? [];
        this.version = version;
        deprecated_endpoints = deprecatedEndpoints ?? [];
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public IReadOnlyList<EndpointDefinition> endpoints { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public int source_line { get; }
    public int source_column { get; }

    /// <summary>
    ///     服务 Schema 版本号，由 @version 注解标记，默认为 1.0
    /// </summary>
    public string version { get; }

    /// <summary>
    ///     已废弃的端点名称集合，由 @deprecatedEndpoints 注解标记
    /// </summary>
    public IReadOnlyList<string> deprecated_endpoints { get; }
}