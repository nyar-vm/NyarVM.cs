namespace Nyar.IR.Rewrite;

/// <summary>
///     指定实现静态解释的类型。
///     应用于 <see cref="StaticAwareAttribute" /> 方法，指向提供静态求值实现的解释器类。
///     解释器类必须实现对应代数接口的静态求值方法。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class StaticInterpretationAttribute : Attribute
{
    /// <summary>
    ///     创建静态解释属性
    /// </summary>
    /// <param name="interpreterType">解释器类型，必须实现相关的 I*Algebra 接口</param>
    public StaticInterpretationAttribute(Type interpreterType)
    {
        interpreter_type = interpreterType;
    }

    /// <summary>
    ///     解释器类型，必须实现相关的 I*Algebra 接口
    /// </summary>
    public Type interpreter_type { get; }

    /// <summary>
    ///     解释器版本/优先级，多个解释器可共存
    /// </summary>
    public int priority { get; set; }
}