namespace Core.Compiler.FFI;

/// <summary>
///     调用约定枚举，定义外部函数接口支持的调用约定
/// </summary>
public enum CallingConvention
{
    /// <summary>
    ///     C 语言调用约定，调用方清理栈
    /// </summary>
    cdecl,

    /// <summary>
    ///     标准调用约定，被调用方清理栈
    /// </summary>
    stdcall,

    /// <summary>
    ///     快速调用约定，通过寄存器传递部分参数
    /// </summary>
    fastcall,

    /// <summary>
    ///     this 指针调用约定，用于 C++ 成员函数
    /// </summary>
    this_call
}