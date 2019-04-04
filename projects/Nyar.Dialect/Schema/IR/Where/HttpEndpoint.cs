using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.Where;

public sealed class HttpEndpoint : EndpointDefinition
{
    public HttpEndpoint(string name, IReadOnlyList<ParameterDefinition> parameters, SchemaType? returnType,
        string httpMethod, string? path = null, bool isJson = true,
        IReadOnlyList<AttributeDefinition>? attributes = null, StreamingMode streamingMode = StreamingMode.unary,
        bool isDeprecated = false, string? deprecatedMessage = null, string? replaceWith = null, int sourceLine = 0,
        int sourceColumn = 0)
        : base(name, parameters, returnType, attributes, streamingMode, isDeprecated, deprecatedMessage, replaceWith,
            sourceLine, sourceColumn)
    {
        http_method = httpMethod;
        this.path = path;
        is_json = isJson;
    }

    public string http_method { get; }
    public string? path { get; }
    public bool is_json { get; }
}