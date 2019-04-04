namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     指定 PE 文件的目标机器架构的
/// </summary>
public enum MachineType : ushort
{
    /// <summary>
    ///     未知目标机器的
    /// </summary>
    unknown = 0x0000,

    /// <summary>
    ///     Intel 386 或兼容处理器的
    /// </summary>
    i386 = 0x014c,

    /// <summary>
    ///     Intel Itanium 处理器家族的
    /// </summary>
    ia64 = 0x0200,

    /// <summary>
    ///     AMD64（x64）处理器的
    /// </summary>
    amd64 = 0x8664,

    /// <summary>
    ///     ARM 处理器的
    /// </summary>
    arm = 0x01c0,

    /// <summary>
    ///     ARM Thumb 处理器的
    /// </summary>
    arm_thumb = 0x01c2,

    /// <summary>
    ///     ARM64（AArch64）处理器的
    /// </summary>
    arm64 = 0xaa64,

    /// <summary>
    ///     Mitsubishi M32R 处理器的
    /// </summary>
    m32_r = 0x9041,

    /// <summary>
    ///     MIPS16 处理器的
    /// </summary>
    mips16 = 0x0266,

    /// <summary>
    ///     MIPS FPU 处理器的
    /// </summary>
    mipsfpu = 0x0366,

    /// <summary>
    ///     MIPS FPU16 处理器的
    /// </summary>
    mipsfpu16 = 0x0466,

    /// <summary>
    ///     Power PC 小端序的
    /// </summary>
    power_pc = 0x01f0,

    /// <summary>
    ///     Power PC 浮点支持的
    /// </summary>
    power_pcfp = 0x01f1,

    /// <summary>
    ///     Hitachi SH3 处理器的
    /// </summary>
    sh3 = 0x01a2,

    /// <summary>
    ///     Hitachi SH3 DSP 处理器的
    /// </summary>
    sh3_dsp = 0x01a3,

    /// <summary>
    ///     Hitachi SH4 处理器的
    /// </summary>
    sh4 = 0x01a6,

    /// <summary>
    ///     Hitachi SH5 处理器的
    /// </summary>
    sh5 = 0x01a8,

    /// <summary>
    ///     ARM Thumb-2 小端序的
    /// </summary>
    armnt = 0x01c4
}