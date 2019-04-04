namespace Nyar.VM.LegacyVM.Algebra;

/// <summary>
///     内置函数包装，用于将 C# 委托包装为可调用对象。
/// </summary>
public sealed class BuiltinFunction
{
    /// <summary>
    ///     函数名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     函数实现
    /// </summary>
    public Func<object[], object> Implementation { get; }

    /// <summary>
    ///     创建内置函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <param name="implementation">函数实现</param>
    public BuiltinFunction(string name, Func<object[], object> implementation)
    {
        Name = name;
        Implementation = implementation;
    }

    /// <summary>
    ///     调用内置函数
    /// </summary>
    /// <param name="args">参数列表</param>
    /// <returns>函数返回值</returns>
    public object invoke(object[] args)
    {
        return Implementation(args);
    }
}