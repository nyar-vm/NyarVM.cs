namespace Std.Data.Binary.Dxil.Data;

/// <summary>
///     DXIL/DXContainer 格式常量的
/// </summary>
/// <remarks>
///     所有常量值均来自 Microsoft DXIL 规范的DXContainer 格式规范的
///     Acorn 独占二进制编解码职责的
/// </remarks>
public static class DxilConstants
{
    /// <summary>
    ///     DXContainer 魔数的DXBC" 小端的= 0x44434247 的实际字节序为 44 58 42 43）的
    /// </summary>
    /// <remarks>
    ///     注意：DXBC 的实际字节序的D X B C的x44 0x58 0x42 0x43），
    ///     小端序读取后的0x43425844。此处使用小端序读取后的 uint 值的
    /// </remarks>
    public const uint container_magic_number = 0x43425844u;

    /// <summary>
    ///     DXContainer 格式主版本号的
    /// </summary>
    public const ushort container_version_major = 1;

    /// <summary>
    ///     DXContainer 格式次版本号的
    /// </summary>
    public const ushort container_version_minor = 0;

    /// <summary>
    ///     DXIL 1.0 主版本号的
    /// </summary>
    public const byte dxil_version10_major = 1;

    /// <summary>
    ///     DXIL 1.0 次版本号的
    /// </summary>
    public const byte dxil_version10_minor = 0;

    /// <summary>
    ///     DXIL 1.1 主版本号的
    /// </summary>
    public const byte dxil_version11_major = 1;

    /// <summary>
    ///     DXIL 1.1 次版本号的
    /// </summary>
    public const byte dxil_version11_minor = 1;

    /// <summary>
    ///     DXIL 1.2 主版本号的
    /// </summary>
    public const byte dxil_version12_major = 1;

    /// <summary>
    ///     DXIL 1.2 次版本号的
    /// </summary>
    public const byte dxil_version12_minor = 2;

    /// <summary>
    ///     DXIL Program Header 大小的4 字节）的
    /// </summary>
    public const int program_header_size = 24;

    /// <summary>
    ///     Gnosis 生成器标识的
    /// </summary>
    public const uint generator_magic_number = 0x00470000;
}

/// <summary>
///     DXContainer Part FourCC 标识枚举的
/// </summary>
public enum DxilPartFourCc : uint
{
    /// <summary>
    ///     着色器特征标志的
    /// </summary>
    feature_info = 0x30494653u,

    /// <summary>
    ///     着色器哈希的
    /// </summary>
    hash = 0x48534148u,

    /// <summary>
    ///     DXIL 着色器程序的
    /// </summary>
    dxil = 0x4C495844u,

    /// <summary>
    ///     调试信息 DXIL的
    /// </summary>
    debug_info_dxil = 0x42444C49u,

    /// <summary>
    ///     管线状态验证的
    /// </summary>
    pipeline_state_validation = 0x30565350u,

    /// <summary>
    ///     运行时数据的
    /// </summary>
    runtime_data = 0x54414452u,

    /// <summary>
    ///     着色器统计信息的
    /// </summary>
    statistics = 0x54415453u,

    /// <summary>
    ///     着色器调试名称的
    /// </summary>
    debug_name = 0x4E4D4424u,

    /// <summary>
    ///     根签名的
    /// </summary>
    root_signature = 0x54534F52u,

    /// <summary>
    ///     DXIL 1.x 着色器程序的
    /// </summary>
    dxil1 = 0x314C4958u
}

/// <summary>
///     DXIL 着色器模型类型枚举的
/// </summary>
public enum DxilShaderModelKind : byte
{
    /// <summary>
    ///     顶点着色器的
    /// </summary>
    vertex = 0,

    /// <summary>
    ///     像素着色器的
    /// </summary>
    pixel = 1,

    /// <summary>
    ///     几何着色器的
    /// </summary>
    geometry = 2,

    /// <summary>
    ///     外壳着色器的
    /// </summary>
    hull = 3,

    /// <summary>
    ///     域着色器的
    /// </summary>
    domain = 4,

    /// <summary>
    ///     计算着色器的
    /// </summary>
    compute = 5,

    /// <summary>
    ///     库的
    /// </summary>
    library = 6,

    /// <summary>
    ///     光线生成着色器的
    /// </summary>
    ray_generation = 7,

    /// <summary>
    ///     相交着色器的
    /// </summary>
    intersection = 8,

    /// <summary>
    ///     任意命中着色器的
    /// </summary>
    any_hit = 9,

    /// <summary>
    ///     最近命中着色器的
    /// </summary>
    closest_hit = 10,

    /// <summary>
    ///     未命中着色器的
    /// </summary>
    miss = 11,

    /// <summary>
    ///     可调用着色器的
    /// </summary>
    callable = 12,

    /// <summary>
    ///     网格着色器的
    /// </summary>
    mesh = 13,

    /// <summary>
    ///     放大着色器的
    /// </summary>
    amplification = 14
}

/// <summary>
///     DXIL 操作码枚举（CoreOps）的
/// </summary>
/// <remarks>
///     操作码值来的Microsoft DXIL 规范，用的dx.op.* 外部函数调用的第一个参数的
/// </remarks>
public enum DxilOpCode
{
    temp_reg_load = 0,
    temp_reg_store = 1,
    min_prec_x_reg_load = 2,
    min_prec_x_reg_store = 3,
    load_input = 4,
    store_output = 5,
    f_abs = 6,
    saturate = 7,
    is_na_n = 8,
    is_inf = 9,
    is_finite = 10,
    is_normal = 11,
    cos = 12,
    sin = 13,
    tan = 14,
    acos = 15,
    asin = 16,
    atan = 17,
    hcos = 18,
    hsin = 19,
    htan = 20,
    exp = 21,
    frc = 22,
    log = 23,
    sqrt = 24,
    rsqrt = 25,
    round_ne = 26,
    round_ni = 27,
    round_pi = 28,
    round_z = 29,
    bfrev = 30,
    countbits = 31,
    firstbit_lo = 32,
    firstbit_hi = 33,
    firstbit_s_hi = 34,
    f_max = 35,
    f_min = 36,
    i_max = 37,
    i_min = 38,
    u_max = 39,
    u_min = 40,
    i_mul = 41,
    u_mul = 42,
    u_div = 43,
    u_addc = 44,
    u_subb = 45,
    f_mad = 46,
    fma = 47,
    i_mad = 48,
    u_mad = 49,
    msad = 50,
    ibfe = 51,
    ubfe = 52,
    bfi = 53,
    dot2 = 54,
    dot3 = 55,
    dot4 = 56,
    create_handle = 57,
    c_buffer_load = 58,
    c_buffer_load_legacy = 59,
    sample = 60,
    sample_bias = 61,
    sample_level = 62,
    sample_grad = 63,
    sample_cmp = 64,
    sample_cmp_level_zero = 65,
    texture_load = 66,
    texture_store = 67,
    buffer_load = 68,
    buffer_store = 69,
    buffer_update_counter = 70,
    check_access_fully_mapped = 71,
    get_dimensions = 72,
    texture_gather = 73,
    texture_gather_cmp = 74,
    texture2_dms_get_sample_position = 75,
    render_target_get_sample_position = 76,
    render_target_get_sample_count = 77,
    atomic_bin_op = 78,
    atomic_compare_exchange = 79,
    barrier = 80,
    calculate_lod = 81,
    discard = 82,
    deriv_coarse_x = 83,
    deriv_coarse_y = 84,
    deriv_fine_x = 85,
    deriv_fine_y = 86,
    eval_snapped = 87,
    eval_sample_index = 88,
    eval_centroid = 89,
    sample_index = 90,
    coverage = 91,
    inner_coverage = 92,
    thread_id = 93,
    group_id = 94,
    thread_id_in_group = 95,
    flattened_thread_id_in_group = 96,
    emit_stream = 97,
    cut_stream = 98,
    emit_then_cut_stream = 99,
    gs_instance_id = 100,
    make_double = 101,
    split_double = 102,
    load_output_control_point = 103,
    load_patch_constant = 104,
    domain_location = 105,
    store_patch_constant = 106,
    output_control_point_id = 107,
    primitive_id = 108,
    cycle_counter_legacy = 109,
    wave_is_first_lane = 110,
    wave_get_lane_index = 111,
    wave_get_lane_count = 112,
    wave_any_true = 113,
    wave_all_true = 114,
    wave_active_all_equal = 115,
    wave_active_ballot = 116,
    wave_read_lane_at = 117,
    wave_read_lane_first = 118,
    wave_active_op = 119,
    wave_active_bit = 120,
    wave_prefix_op = 121,
    quad_read_lane_at = 122,
    quad_op = 123,
    bitcast_i16_to_f16 = 124,
    bitcast_f16_to_i16 = 125,
    bitcast_i32_to_f32 = 126,
    bitcast_f32_to_i32 = 127,
    bitcast_i64_to_f64 = 128,
    bitcast_f64_to_i64 = 129,
    legacy_f32_to_f16 = 130,
    legacy_f16_to_f32 = 131,
    legacy_double_to_float = 132,
    legacy_double_to_s_int32 = 133,
    legacy_double_to_u_int32 = 134,
    wave_all_bit_count = 135,
    wave_prefix_bit_count = 136,
    attribute_at_vertex = 137,
    view_id = 138,
    raw_buffer_load = 139,
    raw_buffer_store = 140,
    instance_id = 141,
    instance_index = 142,
    hit_kind = 143,
    ray_flags = 144,
    dispatch_rays_index = 145,
    dispatch_rays_dimensions = 146,
    world_ray_origin = 147,
    world_ray_direction = 148,
    object_ray_origin = 149,
    object_ray_direction = 150,
    object_to_world = 151,
    world_to_object = 152,
    ray_t_min = 153,
    ray_t_current = 154,
    ignore_hit = 155,
    accept_hit_and_end_search = 156,
    trace_ray = 157,
    report_hit = 158,
    call_shader = 159,
    create_handle_for_lib = 160,
    primitive_index = 161,
    dot2_add_half = 162,
    dot4_add_i8_packed = 163,
    dot4_add_u8_packed = 164,
    wave_match = 165,
    wave_multi_prefix_op = 166,
    wave_multi_prefix_bit_count = 167,
    set_mesh_output_counts = 168,
    emit_indices = 169,
    get_mesh_payload = 170,
    store_vertex_output = 171,
    store_primitive_output = 172,
    dispatch_mesh = 173,
    write_sampler_feedback = 174,
    write_sampler_feedback_bias = 175,
    write_sampler_feedback_level = 176,
    write_sampler_feedback_grad = 177,
    allocate_ray_query = 178,
    ray_query_trace_ray_inline = 179,
    ray_query_proceed = 180,
    ray_query_abort = 181,
    ray_query_commit_non_opaque_triangle_hit = 182,
    ray_query_commit_procedural_primitive_hit = 183,
    ray_query_committed_status = 184,
    ray_query_candidate_type = 185,
    barrier_by_memory_type = 244,
    barrier_by_memory_handle = 245,
    annotate_handle = 216,
    create_handle_from_binding = 217,
    create_handle_from_heap = 218,
    is_helper_lane = 221,
    sample_cmp_level = 224,
    raw_buffer_vector_load = 303,
    raw_buffer_vector_store = 304,
    f_dot = 311
}

/// <summary>
///     DXIL 组件类型枚举的
/// </summary>
public enum DxilComponentType : byte
{
    invalid = 0,
    i1 = 1,
    i16 = 2,
    u16 = 3,
    i32 = 4,
    u32 = 5,
    i64 = 6,
    u64 = 7,
    f16 = 8,
    f32 = 9,
    f64 = 10,
    s_norm_f16 = 11,
    u_norm_f16 = 12,
    s_norm_f32 = 13,
    u_norm_f32 = 14,
    s_norm_f64 = 15,
    u_norm_f64 = 16
}

/// <summary>
///     DXIL 资源类型枚举的
/// </summary>
public enum DxilResourceKind : byte
{
    invalid = 0,
    texture1_d = 1,
    texture2_d = 2,
    texture2_dms = 3,
    texture3_d = 4,
    texture_cube = 5,
    texture1_d_array = 6,
    texture2_d_array = 7,
    texture2_dms_array = 8,
    texture_cube_array = 9,
    typed_buffer = 10,
    raw_buffer = 11,
    structured_buffer = 12,
    c_buffer = 13,
    sampler = 14,
    t_buffer = 15,
    rt_acceleration_structure = 16,
    feedback_texture2_d = 17,
    feedback_texture2_d_array = 18
}

/// <summary>
///     DXIL 资源类枚举的
/// </summary>
public enum DxilResourceClass : byte
{
    srv = 0,
    uav = 1,
    cbv = 2,
    sampler = 3
}

/// <summary>
///     DXIL 插值模式枚举的
/// </summary>
public enum DxilInterpolationMode : byte
{
    undefined = 0,
    constant = 1,
    linear = 2,
    linear_centroid = 3,
    linear_noperspective = 4,
    linear_noperspective_centroid = 5,
    linear_sample = 6,
    linear_noperspective_sample = 7,
    no_interpolation = 9
}

/// <summary>
///     DXIL 地址空间枚举的
/// </summary>
public enum DxilAddressSpace : uint
{
    @default = 0,
    device_memory = 1,
    c_buffer = 2,
    group_shared = 3,
    generic = 4,
    node_record = 6
}