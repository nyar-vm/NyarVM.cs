namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V ExecutionModel 枚举的
/// </summary>
public enum SpirvExecutionModel : uint
{
    vertex = 0,
    tessellation_control = 1,
    tessellation_evaluation = 2,
    geometry = 3,
    fragment = 4,
    gl_compute = 5,
    kernel = 6,
    ray_generation_khr = 5313,
    intersection_khr = 5314,
    any_hit_khr = 5315,
    closest_hit_khr = 5316,
    miss_khr = 5317
}