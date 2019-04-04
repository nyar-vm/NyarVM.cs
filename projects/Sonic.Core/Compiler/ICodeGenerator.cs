namespace Core.Compiler;

/// <summary>
///     代码生成器接口，将中间表示转换为目标平台代码
/// </summary>
/// <typeparam name="TTarget">目标平台的输出类型</typeparam>
public interface ICodeGenerator<TTarget>
{
    /// <summary>
    ///     根据中间表示生成目标平台代码
    /// </summary>
    /// <param name="ir">输入的中间表示</param>
    /// <returns>生成的目标平台代码</returns>
    TTarget generate(IIntermediateRepresentation ir);
}