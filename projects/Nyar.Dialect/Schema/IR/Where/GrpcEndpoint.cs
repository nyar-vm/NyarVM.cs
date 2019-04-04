using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.Where;

public sealed class GrpcEndpoint : EndpointDefinition
{
    public GrpcEndpoint(string name, IReadOnlyList<ParameterDefinition> parameters, SchemaType? returnType,
        string serviceName, string methodName, IReadOnlyList<AttributeDefinition>? attributes = null,
        StreamingMode streamingMode = StreamingMode.unary, bool isDeprecated = false, string? deprecatedMessage = null,
        string? replaceWith = null, int sourceLine = 0, int sourceColumn = 0)
        : base(name, parameters, returnType, attributes, streamingMode, isDeprecated, deprecatedMessage, replaceWith,
            sourceLine, sourceColumn)
    {
        service_name = serviceName;
        method_name = methodName;
    }

    public string service_name { get; }
    public string method_name { get; }
}