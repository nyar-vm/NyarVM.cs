namespace Std.DataStorage;

/// <summary>
///     文件存储后端接口，提供基于文件路径的读写操作�?///
/// </summary>
public interface IFileStorage
{
    /// <summary>
    ///     将值写入指定路径的文件�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     值类型�?/typeparam>
    ///     <param name="path">
    ///         文件路径�?/param>
    ///         <param name="value">要写入的值�?/param>
    void write<T>(string path, in T value);

    /// <summary>
    ///     从指定路径的文件读取值�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     值类型�?/typeparam>
    ///     <param name="path">
    ///         文件路径�?/param>
    ///         <returns>读取到的值�?/returns>
    T read<T>(string path);
}