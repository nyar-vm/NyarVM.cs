namespace Std.Data.Binary.Jvm;

/// <summary>
///     JVM 类型引用的抽象基类，表示一个 JVM 类型的引用。
/// </summary>
public abstract class JvmType
{
    /// <summary>
    ///     返回该类型的 JVM 类型名字符串表示（如 <c>int</c>、<c>java/lang/String</c>、<c>int[]</c>）。
    /// </summary>
    /// <returns>JVM 类型名。</returns>
    public abstract string to_type_name();
}
