namespace Oak.Wit.AST;

/// <summary>
///     WIT 文档节点，表示一个完整的 .wit 文件。
/// </summary>
public class WitDocument
{
    /// <summary>
    ///     文档中的顶层定义列表。
    /// </summary>
    public List<WitDefinition> Definitions { get; init; } = [];
}

/// <summary>
///     WIT 顶层定义抽象基类。
/// </summary>
public abstract class WitDefinition
{
}

/// <summary>
///     WIT 类型定义，如 record、variant、enum、flags 等。
/// </summary>
public class WitTypeDef : WitDefinition
{
    /// <summary>
    ///     类型名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     WIT 接口定义，包含一组类型和函数。
/// </summary>
public class WitInterface : WitDefinition
{
    /// <summary>
    ///     接口名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     WIT 世界定义，组合多个导入和导出接口。
/// </summary>
public class WitWorld : WitDefinition
{
    /// <summary>
    ///     世界名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;
}