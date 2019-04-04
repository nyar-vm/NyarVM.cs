namespace Nyar.ObjectAlgebra;

/// <summary>
///     方言定义接口，描述一个方言的静态元数据。
///     每个方言对应一个 IDialectDefinition 实例。
/// </summary>
public interface IDialectDefinition
{
    /// <summary>
    ///     方言名称（全局唯一）
    /// </summary>
    string name { get; }

    /// <summary>
    ///     方言 GUID（从名称确定性计算）
    /// </summary>
    Guid id { get; }

    /// <summary>
    ///     本方言注册的所有操作符描述符
    /// </summary>
    IReadOnlyList<IOperatorDescriptor> operators { get; }
}