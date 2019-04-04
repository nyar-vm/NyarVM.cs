namespace Nyar.Database.Index;

/// <summary>
///     引用种类
/// </summary>
public enum ReferenceKind
{
    /// <summary>
    ///     读取引用
    /// </summary>
    read,

    /// <summary>
    ///     写入引用
    /// </summary>
    write,

    /// <summary>
    ///     调用引用
    /// </summary>
    call,

    /// <summary>
    ///     类型引用
    /// </summary>
    type_reference,

    /// <summary>
    ///     导入引用
    /// </summary>
    import,

    /// <summary>
    ///     继承引用
    /// </summary>
    inherit,

    /// <summary>
    ///     实现引用
    /// </summary>
    implement
}