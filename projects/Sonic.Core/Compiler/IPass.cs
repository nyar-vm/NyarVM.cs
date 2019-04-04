namespace Core.Compiler;

/// <summary>
///     编译 Pass 接口，表示对中间表示的一次变换操作
/// </summary>
public interface IPass
{
    /// <summary>
    ///     对输入的中间表示执行变换并返回结果
    /// </summary>
    /// <param name="input">输入的中间表示</param>
    /// <returns>变换后的中间表示</returns>
    IIntermediateRepresentation run(IIntermediateRepresentation input);
}