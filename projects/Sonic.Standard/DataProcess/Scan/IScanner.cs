namespace Std.DataProcess.Scan;

/// <summary>
///     扫描器：从格式化载荷中快速提取指定路径的字段值�?/// 允许在不完全反序列化整个对象的情况下，直接从格式化字节中提取某个字段值�?///
/// </summary>
/// <typeparam name="TResult">扫描结果的类型�?/typeparam>
public interface IScanner<TResult>
{
    /// <summary>
    ///     从格式化载荷中扫描指定路径的字段值�?    ///
    /// </summary>
    /// <param name="formattedPayload">
    ///     格式化的载荷字节�?/param>
    ///     <param name="fieldPath">
    ///         字段路径（如 JSONPath、XPath 等），语法由具体实现定义�?/param>
    ///         <returns>扫描到的字段值�?/returns>
    TResult scan(ReadOnlySpan<byte> formattedPayload, string fieldPath);
}