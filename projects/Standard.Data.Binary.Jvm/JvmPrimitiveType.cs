namespace Std.Data.Binary.Jvm;

/// <summary>
///     JVM 原始类型，表示内置的基本类型。
/// </summary>
public sealed class JvmPrimitiveType : JvmType
{
    /// <summary>
    ///     创建原始类型引用。
    /// </summary>
    /// <param name="kind">原始类型种类。</param>
    private JvmPrimitiveType(JvmPrimitiveKind kind)
    {
        this.kind = kind;
    }

    /// <summary>
    ///     原始类型种类。
    /// </summary>
    public JvmPrimitiveKind kind { get; }

    /// <summary>
    ///     Void 类型。
    /// </summary>
    public static JvmPrimitiveType @void { get; } = new(JvmPrimitiveKind.@void);

    /// <summary>
    ///     Boolean 类型。
    /// </summary>
    public static JvmPrimitiveType @boolean { get; } = new(JvmPrimitiveKind.@boolean);

    /// <summary>
    ///     Byte 类型。
    /// </summary>
    public static JvmPrimitiveType @byte { get; } = new(JvmPrimitiveKind.@byte);

    /// <summary>
    ///     Short 类型。
    /// </summary>
    public static JvmPrimitiveType @short { get; } = new(JvmPrimitiveKind.@short);

    /// <summary>
    ///     Int 类型。
    /// </summary>
    public static JvmPrimitiveType @int { get; } = new(JvmPrimitiveKind.@int);

    /// <summary>
    ///     Long 类型。
    /// </summary>
    public static JvmPrimitiveType @long { get; } = new(JvmPrimitiveKind.@long);

    /// <summary>
    ///     Float 类型。
    /// </summary>
    public static JvmPrimitiveType @float { get; } = new(JvmPrimitiveKind.@float);

    /// <summary>
    ///     Double 类型。
    /// </summary>
    public static JvmPrimitiveType @double { get; } = new(JvmPrimitiveKind.@double);

    /// <summary>
    ///     Char 类型。
    /// </summary>
    public static JvmPrimitiveType @char { get; } = new(JvmPrimitiveKind.@char);

    /// <summary>
    ///     返回该类型的 JVM 类型名字符串表示。
    /// </summary>
    /// <returns>JVM 类型名。</returns>
    public override string to_type_name()
    {
        return kind switch
        {
            JvmPrimitiveKind.@void => "void",
            JvmPrimitiveKind.@boolean => "boolean",
            JvmPrimitiveKind.@byte => "byte",
            JvmPrimitiveKind.@short => "short",
            JvmPrimitiveKind.@int => "int",
            JvmPrimitiveKind.@long => "long",
            JvmPrimitiveKind.@float => "float",
            JvmPrimitiveKind.@double => "double",
            JvmPrimitiveKind.@char => "char",
            _ => throw new InvalidOperationException($"未知的 JVM 原始类型种类：{kind}")
        };
    }
}
