namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     外部类型引用，描述需要生成 TypeRef 元数据令牌的类型。
///     用于 unbox.any、newarr、castclass 等需要类型令牌的 IL 指令。
/// </summary>
public sealed record ClrExternalTypeRef
{
    /// <summary>
    ///     程序集名称（如 <c>System.Runtime</c>）
    /// </summary>
    public string assembly_name { get; init; } = string.Empty;

    /// <summary>
    ///     类型全名含命名空间（如 <c>System.Int32</c>）
    /// </summary>
    public string type_full_name { get; init; } = string.Empty;

    /// <summary>
    ///     类型所在命名空间（如 <c>System</c>）
    /// </summary>
    public string type_namespace { get; init; } = string.Empty;

    /// <summary>
    ///     类型短名（如 <c>Int32</c>）
    /// </summary>
    public string type_name { get; init; } = string.Empty;
}
