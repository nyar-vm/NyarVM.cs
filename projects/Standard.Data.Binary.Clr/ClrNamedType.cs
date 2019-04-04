namespace Std.Data.Binary.Clr;

/// <summary>
///     CLR 命名类型引用，表示通过名称和命名空间引用的类型
/// </summary>
public sealed class ClrNamedType : ClrType
{
    /// <summary>
    ///     创建命名类型引用
    /// </summary>
    /// <param name="name">类型名称。</param>
    /// <param name="namespace">类型命名空间。</param>
    public ClrNamedType(string name, string @namespace)
    {
        this.name = name;
        this.@namespace = @namespace;
    }

    /// <summary>
    ///     类型名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     类型命名空间
    /// </summary>
    public string @namespace { get; }

    /// <summary>
    ///     类型全名（命名空间.类型名）。
    /// </summary>
    public string full_name => string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

    /// <summary>
    ///     从 CLR 类型全名创建命名类型引用。
    /// </summary>
    /// <param name="fullName">CLR 类型全名（如 <c>System.String</c>）。</param>
    /// <returns>对应的命名类型引用。</returns>
    public static ClrNamedType from_full_name(string fullName)
    {
        var dotIndex = fullName.LastIndexOf('.');
        if (dotIndex >= 0)
        {
            return new ClrNamedType(fullName[(dotIndex + 1)..], fullName[..dotIndex]);
        }

        return new ClrNamedType(fullName, string.Empty);
    }
}