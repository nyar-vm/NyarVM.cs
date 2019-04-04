namespace Std.Data.Text.Valkyrie.AST.ECS;

/// <summary>
///     ECS 查询类型枚举
/// </summary>
public enum QueryKind
{
    /// <summary>
    ///     查询实体必须拥有所有指定组件（AND 逻辑）
    /// </summary>
    all,

    /// <summary>
    ///     查询实体拥有任意一个指定组件即可（OR 逻辑）
    /// </summary>
    any,

    /// <summary>
    ///     查询实体不能拥有任何指定组件（排除逻辑）
    /// </summary>
    none
}