namespace Nyar.VM.TextVM;

/// <summary>
/// 静态查询的编译期配置。
/// </summary>
public class StaticConfig
{
    /// <summary>
    /// 最大 DFA 状态数，超过此值放弃全量 JIT。
    /// </summary>
    public Int32 MaxDfaStates { get; set; } = 4096;

    /// <summary>
    /// 强制走纯字面量路径（将元字符视为普通字符）。
    /// </summary>
    public Boolean ForceLiteral { get; set; } = false;

    /// <summary>
    /// 反向引用超时毫秒数。
    /// </summary>
    public Int32 BacktrackTimeoutMs { get; set; } = 100;
}
