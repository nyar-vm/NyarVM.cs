namespace Std.Data.Binary.Jvm;

/// <summary>
///     JVM 数组类型引用，表示元素类型的数组。
/// </summary>
public sealed class JvmArrayType : JvmType
{
    /// <summary>
    ///     创建数组类型引用。
    /// </summary>
    /// <param name="elementType">元素类型。</param>
    public JvmArrayType(JvmType elementType)
    {
        element_type = elementType;
    }

    /// <summary>
    ///     数组元素类型。
    /// </summary>
    public JvmType element_type { get; }

    /// <summary>
    ///     返回该类型的 JVM 类型名字符串表示。
    /// </summary>
    /// <returns>JVM 类型名。</returns>
    public override string to_type_name()
    {
        return $"{element_type.to_type_name()}[]";
    }
}
