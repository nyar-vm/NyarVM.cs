namespace Nyar.Types;

/// <summary>
///     函数参数定义
/// </summary>
public sealed class FunctionParameter
{
    /// <summary>
    ///     创建函数参数
    /// </summary>
    /// <param name="name">参数名称。</param>
    /// <param name="type">参数类型。</param>
    public FunctionParameter(string name, string type)
    {
        this.name = name;
        this.type = type;
    }

    /// <summary>
    ///     参数名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     参数类型
    /// </summary>
    public string type { get; }
}