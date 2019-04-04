namespace Nyar.Types;

/// <summary>
///     模块原生函数，封装一个由 C# 实现的、可通过模块系统调用的函数
/// </summary>
public sealed class NyarNativeFunction
{
    /// <summary>
    ///     初始化 NyarNativeFunction
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <param name="module">所属模块名称。</param>
    /// <param name="func">函数实现委托。</param>
    public NyarNativeFunction(string name, string module, Func<Value[], Value> func)
    {
        this.name = name;
        this.module = module;
        this.func = func;
    }

    /// <summary>
    ///     函数名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     所属模块名称
    /// </summary>
    public string module { get; }

    /// <summary>
    ///     函数实现委托
    /// </summary>
    public Func<Value[], Value> func { get; }

    /// <summary>
    ///     获取完全限定名（模块名.函数名）
    /// </summary>
    public string full_name => $"{module}.{name}";

    /// <summary>
    ///     调用原生函数
    /// </summary>
    /// <param name="args">参数。</param>
    /// <returns>返回值。</returns>
    public Value invoke(Value[] args)
    {
        return func(args);
    }
}