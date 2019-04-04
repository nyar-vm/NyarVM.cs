namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V StorageClass 枚举的
/// </summary>
public enum SpirvStorageClass : uint
{
    uniform_constant = 0,
    input = 1,
    uniform = 2,
    output = 3,
    workgroup = 4,
    cross_workgroup = 5,
    @private = 6,
    function = 7,
    generic = 8,
    push_constant = 9,
    atomic_counter = 10,
    image = 11,
    storage_buffer = 12,
    ray_payload_khr = 33,
    hit_attribute_khr = 34,
    incoming_ray_payload_khr = 35,
    shader_record_buffer_khr = 36
}