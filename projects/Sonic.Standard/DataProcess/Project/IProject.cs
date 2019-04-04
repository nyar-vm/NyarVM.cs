using Std.DataProcess.Write;

namespace Std.DataProcess.Project;

/// <summary>
///     单向投射器：将对象投射到原始字节序列�?/// 方法动词 <c>Project</c> 完全属于类型编解码层，不污染任何其他层次�?///
/// </summary>
/// <typeparam name="T">要投射的类型�?/typeparam>
public interface IProject<in T>
{
    /// <summary>
    ///     将指定类型的值投射到字节输出器�?    ///
    /// </summary>
    /// <param name="value">
    ///     要投射的值�?/param>
    ///     <param name="writer">目标字节输出器�?/param>
    void project(T value, IBufferWriter<byte> writer);
}