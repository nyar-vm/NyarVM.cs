namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V Capability 枚举的
/// </summary>
public enum SpirvCapability : uint
{
    matrix = 0,
    shader = 1,
    geometry = 2,
    tessellation = 3,
    addresses = 4,
    linkage = 5,
    kernel = 6,
    vector16 = 7,
    float16_buffer = 8,
    float16 = 9,
    float64 = 10,
    int64 = 11,
    int64_atomics = 12,
    image_basic = 13,
    image_read_write = 14,
    image_mipmap = 15,
    pipes = 17,
    groups = 18,
    device_enqueue = 19,
    literal_sampler = 20,
    atomic_storage = 21,
    int16 = 22,
    tessellation_point_size = 23,
    geometry_point_size = 24,
    image_gather_extended = 25,
    storage_image_multisample = 27,
    uniform_buffer_array_dynamic_indexing = 28,
    sampled_image_array_dynamic_indexing = 29,
    storage_buffer_array_dynamic_indexing = 30,
    storage_image_array_dynamic_indexing = 31,
    clip_distance = 32,
    cull_distance = 33,
    image_cube_array = 34,
    sample_rate_shading = 35,
    image_rect = 36,
    sampled_rect = 37,
    generic_pointer = 38,
    int8 = 39,
    input_attachment = 40,
    sparse_residency = 41,
    min_lod = 42,
    sampled1_d = 43,
    image1_d = 44,
    sampled_cube_array = 45,
    sampled_buffer = 46,
    image_buffer = 47,
    image_ms_array = 48,
    storage_image_extended_formats = 49,
    image_query = 50,
    derivative_control = 51,
    interpolation_function = 52,
    transform_feedback = 53,
    geometry_streams = 54,
    storage_image_read_without_format = 55,
    storage_image_write_without_format = 56,
    multi_viewport = 57,
    subgroup_dispatch = 58,
    named_barrier = 59,
    mesh_shading_nv = 60,

    /// <summary>
    ///     GLSL 样式的按组件插值的
    /// </summary>
    fragment_barycentric_khr = 4484,

    /// <summary>
    ///     物理存储缓冲的64 位地址的
    /// </summary>
    physical_storage_buffer_addresses = 5347,

    /// <summary>
    ///     协同矩阵运算的
    /// </summary>
    cooperative_matrix_khr = 5366,

    /// <summary>
    ///     Mesh Shading 扩展的
    /// </summary>
    mesh_shading_ext = 5368,
    subgroup_ballot_khr = 4423,
    draw_parameters = 4427,
    subgroup_vote_khr = 4431,
    storage_buffer16_bit_access = 4433,
    storage_push_constant16 = 4435,
    storage_input_output16 = 4436,
    device_group = 4437,
    multi_view = 4439,
    variable_pointers_storage_buffer = 4441,
    variable_pointers = 4442,
    fragment_density_ext = 4444,
    shader_non_uniform_ext = 4446,
    runtime_descriptor_array_ext = 4447,
    ray_tracing_khr = 4479,
    ray_query_khr = 4472,
    vulkan_memory_model = 4434,

    /// <summary>
    ///     着色器时钟（用于性能测量）的
    /// </summary>
    shader_clock_khr = 5068,

    /// <summary>
    ///     片段着色器交错执行的
    /// </summary>
    fragment_shader_interlock_ext = 5363,

    /// <summary>
    ///     按片段着色率的
    /// </summary>
    fragment_shading_rate_khr = 5408
}