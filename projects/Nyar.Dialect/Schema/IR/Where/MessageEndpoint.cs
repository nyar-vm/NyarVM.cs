using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.Where;

public sealed class MessageEndpoint : EndpointDefinition
{
    public MessageEndpoint(string name, IReadOnlyList<ParameterDefinition> parameters, SchemaType? returnType,
        string topic, SchemaType? payloadType = null, IReadOnlyList<AttributeDefinition>? attributes = null,
        StreamingMode streamingMode = StreamingMode.unary, bool isDeprecated = false, string? deprecatedMessage = null,
        string? replaceWith = null, int sourceLine = 0, int sourceColumn = 0)
        : base(name, parameters, returnType, attributes, streamingMode, isDeprecated, deprecatedMessage, replaceWith,
            sourceLine, sourceColumn)
    {
        this.topic = topic;
        payload_type = payloadType;
    }

    public string topic { get; }
    public SchemaType? payload_type { get; }
}