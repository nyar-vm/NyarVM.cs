namespace Core.Compiler;

/// <summary>
///     Pass 管线接口，支持将多个 Pass 串联执行
/// </summary>
public interface IPassPipeline
{
    /// <summary>
    ///     向管线中添加一个 Pass
    /// </summary>
    /// <param name="pass">要添加的 Pass</param>
    /// <returns>添加后的管线实例，支持链式调用</returns>
    IPassPipeline add(IPass pass);

    /// <summary>
    ///     按顺序执行管线中的所有 Pass
    /// </summary>
    /// <param name="input">输入的中间表示</param>
    /// <returns>经过所有 Pass 变换后的中间表示</returns>
    IIntermediateRepresentation execute(IIntermediateRepresentation input);
}