namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     外部方法引用，描述需要生的MemberRef 和对的TypeRef、AssemblyRef 的外部方法的
/// </summary>
public sealed record ClrExternalMethodRef
{
    /// <summary>
    ///     程序集名称（的<c>System.Console</c>）的
    /// </summary>
    public string assembly_name { get; init; } = string.Empty;

    /// <summary>
    ///     类型全名含命名空间（的<c>System.Console</c>）的
    /// </summary>
    public string type_full_name { get; init; } = string.Empty;

    /// <summary>
    ///     类型所在命名空间（的<c>System</c>）的
    /// </summary>
    public string type_namespace { get; init; } = string.Empty;

    /// <summary>
    ///     类型短名（如 <c>Console</c>）的
    /// </summary>
    public string type_name { get; init; } = string.Empty;

    /// <summary>
    ///     方法名（的<c>Write</c>）的
    /// </summary>
    public string method_name { get; init; } = string.Empty;

    /// <summary>
    ///     方法签名 Blob（如 <c>[0x00, 0x01, 0x01, 0x0E]</c> 表示 void(string)）的
    /// </summary>
    public byte[] method_signature { get; init; } = [];
}