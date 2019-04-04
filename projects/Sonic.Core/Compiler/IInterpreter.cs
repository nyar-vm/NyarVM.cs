namespace Core.Compiler;

/// <summary>
///     解释器接口，直接执行中间表示
/// </summary>
public interface IInterpreter
{
    /// <summary>
    ///     执行中间表示并返回执行结果
    /// </summary>
    /// <param name="ir">待执行的中间表示</param>
    /// <returns>执行结果，若无返回值则为 null</returns>
    object? execute(IIntermediateRepresentation ir);
}