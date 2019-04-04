namespace Nyar.Language.Valkyrie.Compiler.Hir.Attributes;

/// <summary>
///     `[data]` 派生操作种类。
///     这里只表达“数据类型 <-> 结构化容器”的编译请求，
///     不绑定具体容器实现，也不绑定文本格式。
/// </summary>
public enum HirDataDeriveOperationKind
{
    /// <summary>
    ///     将 `[data]` 类型投影到结构化容器。
    /// </summary>
    serialize,

    /// <summary>
    ///     从结构化容器恢复 `[data]` 类型。
    /// </summary>
    deserialize
}