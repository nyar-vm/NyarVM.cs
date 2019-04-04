namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V SourceLanguage 枚举的
/// </summary>
public enum SpirvSourceLanguage : uint
{
    unknown = 0,
    essl = 1,
    glsl = 2,
    open_cl_c = 3,
    open_cl_cpp = 4,
    hlsl = 5
}