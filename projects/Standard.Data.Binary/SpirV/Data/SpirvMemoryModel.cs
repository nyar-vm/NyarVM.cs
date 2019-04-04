namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V MemoryModel 枚举的
/// </summary>
public enum SpirvMemoryModel : uint
{
    simple = 0,
    glsl450 = 1,
    open_cl = 2,
    vulkan = 3
}