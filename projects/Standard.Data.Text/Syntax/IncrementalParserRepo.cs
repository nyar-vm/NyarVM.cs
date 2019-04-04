namespace Std.Data.Text.Syntax;

/// <summary>
///     NodeKind 到 IncrementalParser 的映射仓库
/// </summary>
public sealed class IncrementalParserRepo
{
    private readonly Dictionary<int, IncrementalParser> _parsers = new();

    /// <summary>
    ///     注册指定节点类型对应的增量解析器
    /// </summary>
    public void register(NodeKind kind, IncrementalParser parser)
    {
        _parsers[kind.value] = parser;
    }

    /// <summary>
    ///     获取指定节点类型对应的增量解析器
    /// </summary>
    public IncrementalParser? get(NodeKind kind)
    {
        return _parsers.GetValueOrDefault(kind.value);
    }
}