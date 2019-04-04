namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 格式常量，包含魔数、版本号等标量常量的
/// </summary>
/// <remarks>
///     所有常量值均来自 Khronos SPIR-V 规范，Acorn 独占二进制编解码职责的
///     枚举类常量（Capability、ExecutionModel 等）已迁移为独立的enum 类型的
/// </remarks>
public static class SpirvConstants
{
    /// <summary>
    ///     SPIR-V 魔数的x07230203）的
    /// </summary>
    public const uint magic_number = 0x07230203;

    /// <summary>
    ///     SPIR-V 1.0 版本号的
    /// </summary>
    public const uint version10 = 0x00010000;

    /// <summary>
    ///     SPIR-V 1.1 版本号的
    /// </summary>
    public const uint version11 = 0x00010100;

    /// <summary>
    ///     SPIR-V 1.2 版本号的
    /// </summary>
    public const uint version12 = 0x00010200;

    /// <summary>
    ///     SPIR-V 1.3 版本号的
    /// </summary>
    public const uint version13 = 0x00010300;

    /// <summary>
    ///     SPIR-V 1.4 版本号的
    /// </summary>
    public const uint version14 = 0x00010400;

    /// <summary>
    ///     SPIR-V 1.5 版本号的
    /// </summary>
    public const uint version15 = 0x00010500;

    /// <summary>
    ///     Gnosis 生成器魔数的
    /// </summary>
    public const uint generator_magic_number = 0x00470000;

    /// <summary>
    ///     默认 Schema 值的
    /// </summary>
    public const uint schema = 0;
}