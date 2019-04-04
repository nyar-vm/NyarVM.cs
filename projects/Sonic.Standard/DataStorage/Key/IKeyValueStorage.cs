namespace Std.DataStorage.Key;

/// <summary>
///     键值存储后端接口，提供基于键值对的存取操作�?///
/// </summary>
public interface IKeyValueStorage
{
    /// <summary>
    ///     写入键值对�?    ///
    /// </summary>
    /// <typeparam name="K">
    ///     键类型�?/typeparam>
    ///     <typeparam name="V">
    ///         值类型�?/typeparam>
    ///         <param name="key">
    ///             键�?/param>
    ///             <param name="value">值�?/param>
    void put<K, V>(in K key, in V value)
        where K : notnull;

    /// <summary>
    ///     根据键读取值�?    ///
    /// </summary>
    /// <typeparam name="K">
    ///     键类型�?/typeparam>
    ///     <typeparam name="V">
    ///         值类型�?/typeparam>
    ///         <param name="key">
    ///             键�?/param>
    ///             <returns>找到的值，未找到时�?<c>null</c>�?/returns>
    V? get<K, V>(in K key)
        where K : notnull;

    /// <summary>
    ///     根据键删除键值对�?    ///
    /// </summary>
    /// <typeparam name="K">
    ///     键类型�?/typeparam>
    ///     <param name="key">键�?/param>
    void delete<K>(in K key)
        where K : notnull;
}