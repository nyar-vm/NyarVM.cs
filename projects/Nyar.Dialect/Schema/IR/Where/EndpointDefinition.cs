using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.Where;

public abstract class EndpointDefinition
{
    protected EndpointDefinition(string name, IReadOnlyList<ParameterDefinition> parameters, SchemaType? returnType,
        IReadOnlyList<AttributeDefinition>? attributes = null, StreamingMode streamingMode = StreamingMode.unary,
        bool isDeprecated = false, string? deprecatedMessage = null, string? replaceWith = null, int sourceLine = 0,
        int sourceColumn = 0)
    {
        this.name = name;
        this.parameters = parameters;
        return_type = returnType;
        this.attributes = attributes ?? [];
        streaming_mode = streamingMode;
        is_deprecated = isDeprecated;
        deprecated_message = deprecatedMessage;
        replace_with = replaceWith;
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public IReadOnlyList<ParameterDefinition> parameters { get; }
    public SchemaType? return_type { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public int source_line { get; }
    public int source_column { get; }

    /// <summary>
    ///     端点的流式通信模式，默认为一元调用
    /// </summary>
    public StreamingMode streaming_mode { get; }

    /// <summary>
    ///     端点是否已废弃，由 @deprecated 注解标记
    /// </summary>
    public bool is_deprecated { get; }

    /// <summary>
    ///     废弃提示信息，来自 @deprecated(message="...") 注解参数
    /// </summary>
    public string? deprecated_message { get; }

    /// <summary>
    ///     废弃后推荐的替代端点名称，来自 @deprecated(replaceWith="...") 注解参数
    /// </summary>
    public string? replace_with { get; }
}