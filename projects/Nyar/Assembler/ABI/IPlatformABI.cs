using Std.Data.Binary.Frame;

namespace Nyar.Assembler.ABI;

/// <summary>
///     平台 ABI 接口，定义调用约定和系统调用规范
/// </summary>
public interface IPlatformAbi
{
    /// <summary>
    ///     平台名称
    /// </summary>
    string name { get; }

    /// <summary>
    ///     发射系统调用指令
    /// </summary>
    void emit_syscall(ref ByteBufferWriter writer);

    /// <summary>
    ///     发射函数调用指令（通过地址的
    /// </summary>
    void emit_call_indirect(ref ByteBufferWriter writer);

    /// <summary>
    ///     发射退出程序指的
    /// </summary>
    void emit_exit(ref ByteBufferWriter writer, int exitCode);
}