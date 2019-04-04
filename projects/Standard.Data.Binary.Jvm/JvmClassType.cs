namespace Std.Data.Binary.Jvm;

/// <summary>
///     JVM 类类型引用，表示通过类名引用的引用类型。
/// </summary>
public sealed class JvmClassType : JvmType
{
    /// <summary>
    ///     创建类类型引用。
    /// </summary>
    /// <param name="className">JVM 内部类名（如 <c>java/lang/String</c>）。</param>
    public JvmClassType(string className)
    {
        class_name = className;
    }

    /// <summary>
    ///     JVM 内部类名。
    /// </summary>
    public string class_name { get; }

    /// <summary>
    ///     返回该类型的 JVM 类型名字符串表示。
    /// </summary>
    /// <returns>JVM 类型名。</returns>
    public override string to_type_name()
    {
        return class_name;
    }
}
