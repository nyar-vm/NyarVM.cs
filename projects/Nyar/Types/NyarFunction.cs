using Nyar.Analyzer.ModuleSystem;

namespace Nyar.Types;

/// <summary>
///     Nyar 函数定义，封装函数元数据和字节码
/// </summary>
public sealed class NyarFunction : IFunction
{
    /// <summary>
    ///     初始化 NyarFunction
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <param name="arity">参数数量。</param>
    /// <param name="localCount">局部变量数量。</param>
    /// <param name="codeOffset">代码偏移。</param>
    /// <param name="codeLength">代码长度。</param>
    public NyarFunction(string name, int arity, int localCount, int codeOffset, int codeLength)
    {
        this.name = name;
        this.arity = arity;
        local_count = localCount;
        code_offset = codeOffset;
        code_length = codeLength;
    }

    /// <summary>
    ///     函数所属模块的引用
    /// </summary>
    public NyarModule? module { get; set; }

    /// <summary>
    ///     函数名称
    /// </summary>
    public string name { get; init; }

    /// <summary>
    ///     参数数量
    /// </summary>
    public int arity { get; init; }

    /// <summary>
    ///     局部变量数量（不含参数）
    /// </summary>
    public int local_count { get; init; }

    /// <summary>
    ///     代码在模块字节码中的起始偏移量
    /// </summary>
    public int code_offset { get; init; }

    /// <summary>
    ///     代码长度（字节数）
    /// </summary>
    public int code_length { get; init; }

    /// <summary>
    ///     接口层模块引用
    /// </summary>
    IModule? IFunction.module
    {
        get => module;
        set => module = value as NyarModule;
    }
}