namespace Nyar.ObjectAlgebra;

/// <summary>
///     标记一个接口为 Nyar 方言定义。
///     Source Generator 将扫描此属性，自动生成对应的 OA 工厂接口和 Builder 实现。
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class DialectAttribute : Attribute
{
    /// <summary>
    ///     初始化 DialectAttribute 实例
    /// </summary>
    /// <param name="name">方言名称。</param>
    public DialectAttribute(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     方言名称（如 "core"、"standard"、"game"）
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     指示此方言是否可能有子方言继承。
    ///     当子方言位于不同项目时，Source Generator 无法自动检测继承关系，
    ///     需要显式标记此属性为 true 以确保生成的 Builder 类不被密封，
    ///     从而允许子方言的 Builder 继承此类。
    /// </summary>
    public bool may_have_children { get; set; }
}