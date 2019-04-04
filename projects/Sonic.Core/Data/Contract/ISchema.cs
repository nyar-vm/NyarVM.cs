namespace Core.Data.Contract;

/// <summary>
///     架构接口，定义获取类型元数据的契约。
/// </summary>
/// <typeparam name="T">目标类型。</typeparam>
public interface ISchema<T>
{
    /// <summary>
    ///     获取目标类型的元数据。
    /// </summary>
    /// <returns>类型元数据。</returns>
    TypeMeta get_meta();
}