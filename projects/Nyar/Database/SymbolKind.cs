namespace Nyar.Database;

/// <summary>
///     符号种类
/// </summary>
public enum SymbolKind
{
    /// <summary>
    ///     未知
    /// </summary>
    unknown,

    /// <summary>
    ///     命名空间
    /// </summary>
    @namespace,

    /// <summary>
    ///     模块
    /// </summary>
    module,

    /// <summary>
    ///     类
    /// </summary>
    @class,

    /// <summary>
    ///     接口
    /// </summary>
    @interface,

    /// <summary>
    ///     枚举
    /// </summary>
    @enum,

    /// <summary>
    ///     函数
    /// </summary>
    function,

    /// <summary>
    ///     方法
    /// </summary>
    method,

    /// <summary>
    ///     属性
    /// </summary>
    property,

    /// <summary>
    ///     字段
    /// </summary>
    field,

    /// <summary>
    ///     变量
    /// </summary>
    variable,

    /// <summary>
    ///     参数
    /// </summary>
    parameter,

    /// <summary>
    ///     类型参数
    /// </summary>
    type_parameter,

    /// <summary>
    ///     类型别名
    /// </summary>
    type_alias,

    /// <summary>
    ///     导入
    /// </summary>
    import,

    /// <summary>
    ///     导出
    /// </summary>
    export
}