namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     方法定义（解析后的高级视图）的
/// </summary>
public sealed class ClrMethodDef
{
    /// <summary>
    ///     方法名的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     方法签名（不的Blob 压缩长度前缀），例如 `00 00 08` 表示 `int32 ()`的
    /// </summary>
    public byte[] signature { get; init; } = [];

    /// <summary>
    ///     访问标志的
    /// </summary>
    public ClrMethodAttributes flags { get; init; }

    /// <summary>
    ///     方法 RVA的
    /// </summary>
    public uint rva { get; init; }

    /// <summary>
    ///     代码大小的
    /// </summary>
    public uint code_size { get; init; }

    /// <summary>
    ///     局部变量签名令牌的
    /// </summary>
    public uint local_var_sig_tok { get; init; }

    /// <summary>
    ///     局部变量类型列表。
    ///     由编码阶段生成 `StandAloneSig`，避免后端手写元数据令牌。
    /// </summary>
    public IReadOnlyList<string> local_variable_types { get; init; } = [];

    /// <summary>
    ///     局部变量名称列表。
    ///     仅用于 `MSIL` 文本调试输出，不参与最终二进制编码。
    /// </summary>
    public IReadOnlyList<string> local_variable_names { get; init; } = [];

    /// <summary>
    ///     最大栈深度的
    /// </summary>
    public ushort max_stack { get; init; }

    /// <summary>
    ///     是否自动初始化局部变量为零值。
    ///     可验证方法中有局部变量时必须设置为 <c>true</c>。
    /// </summary>
    public bool init_locals { get; init; }

    /// <summary>
    ///     MSIL 指令列表的
    /// </summary>
    public IReadOnlyList<ClrInstruction> instructions { get; init; } = [];

    /// <summary>
    ///     异常处理表的
    /// </summary>
    public IReadOnlyList<ClrExceptionHandler> exception_handlers { get; init; } = [];
}
