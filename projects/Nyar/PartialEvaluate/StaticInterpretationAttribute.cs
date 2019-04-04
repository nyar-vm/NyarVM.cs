namespace Nyar.PartialEvaluate;

/// <summary>
///     指定静态解释实现类，该类的方法将用于静态路径下的部分求值
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StaticInterpretationAttribute : Attribute
{
    /// <summary>
    ///     指定静态解释实现类
    /// </summary>
    /// <param name="interpreterType">静态解释实现类的类型</param>
    public StaticInterpretationAttribute(Type interpreterType)
    {
        interpreter_type = interpreterType;
    }

    /// <summary>
    ///     静态解释实现类的类型
    /// </summary>
    public Type interpreter_type { get; }
}