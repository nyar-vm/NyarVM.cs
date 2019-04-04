namespace Nyar.Analyzer.ModuleSystem;

/// <summary>
///     函数抽象接口
/// </summary>
public interface IFunction
{
    /// <summary>
    ///     函数名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     参数数量
    /// </summary>
    int arity { get; }

    /// <summary>
    ///     局部变量数量（不含参数）
    /// </summary>
    int local_count { get; }

    /// <summary>
    ///     代码在模块字节码中的起始偏移量
    /// </summary>
    int code_offset { get; }

    /// <summary>
    ///     代码长度（字节数）
    /// </summary>
    int code_length { get; }

    /// <summary>
    ///     函数所属模块的引用
    /// </summary>
    IModule? module { get; set; }
}