namespace Nyar.Assembler.ISA;

/// <summary>
///     x86-64 通用寄存器枚举，包含 64 位和 32 位变的
/// </summary>
public enum X64Reg
{
    /// <summary>
    ///     累加器寄存器的4 位）
    /// </summary>
    rax,

    /// <summary>
    ///     计数寄存器（64 位）
    /// </summary>
    rcx,

    /// <summary>
    ///     数据寄存器（64 位）
    /// </summary>
    rdx,

    /// <summary>
    ///     基址寄存器（64 位）
    /// </summary>
    rbx,

    /// <summary>
    ///     栈指针寄存器的4 位）
    /// </summary>
    rsp,

    /// <summary>
    ///     帧指针寄存器的4 位）
    /// </summary>
    rbp,

    /// <summary>
    ///     源索引寄存器的4 位）
    /// </summary>
    rsi,

    /// <summary>
    ///     目的索引寄存器（64 位）
    /// </summary>
    rdi,

    /// <summary>
    ///     通用寄存的R8的4 位）
    /// </summary>
    r8,

    /// <summary>
    ///     通用寄存的R9的4 位）
    /// </summary>
    r9,

    /// <summary>
    ///     通用寄存的R10的4 位）
    /// </summary>
    r10,

    /// <summary>
    ///     通用寄存的R11的4 位）
    /// </summary>
    r11,

    /// <summary>
    ///     通用寄存的R12的4 位）
    /// </summary>
    r12,

    /// <summary>
    ///     通用寄存的R13的4 位）
    /// </summary>
    r13,

    /// <summary>
    ///     通用寄存的R14的4 位）
    /// </summary>
    r14,

    /// <summary>
    ///     通用寄存的R15的4 位）
    /// </summary>
    r15,

    /// <summary>
    ///     累加器寄存器的2 位）
    /// </summary>
    eax,

    /// <summary>
    ///     计数寄存器（32 位）
    /// </summary>
    ecx,

    /// <summary>
    ///     数据寄存器（32 位）
    /// </summary>
    edx,

    /// <summary>
    ///     基址寄存器（32 位）
    /// </summary>
    ebx,

    /// <summary>
    ///     栈指针寄存器的2 位）
    /// </summary>
    esp,

    /// <summary>
    ///     帧指针寄存器的2 位）
    /// </summary>
    ebp,

    /// <summary>
    ///     源索引寄存器的2 位）
    /// </summary>
    esi,

    /// <summary>
    ///     目的索引寄存器（32 位）
    /// </summary>
    edi,

    /// <summary>
    ///     通用寄存的R8的2 位）
    /// </summary>
    r8_d,

    /// <summary>
    ///     通用寄存的R9的2 位）
    /// </summary>
    r9_d,

    /// <summary>
    ///     通用寄存的R10的2 位）
    /// </summary>
    r10_d,

    /// <summary>
    ///     通用寄存的R11的2 位）
    /// </summary>
    r11_d,

    /// <summary>
    ///     通用寄存的R12的2 位）
    /// </summary>
    r12_d,

    /// <summary>
    ///     通用寄存的R13的2 位）
    /// </summary>
    r13_d,

    /// <summary>
    ///     通用寄存的R14的2 位）
    /// </summary>
    r14_d,

    /// <summary>
    ///     通用寄存的R15的2 位）
    /// </summary>
    r15_d
}