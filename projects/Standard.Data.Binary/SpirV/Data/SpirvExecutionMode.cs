namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V ExecutionMode 枚举的
/// </summary>
public enum SpirvExecutionMode : uint
{
    invocations = 0,
    spacing_equal = 1,
    spacing_fractional_even = 2,
    spacing_fractional_odd = 3,
    vertex_order_cw = 4,
    vertex_order_ccw = 5,
    pixel_center_integer = 6,
    origin_upper_left = 7,
    origin_lower_left = 8,
    early_fragment_tests = 9,
    point_mode = 10,
    xfb = 11,
    depth_replacing = 12,
    depth_greater = 14,
    depth_less = 15,
    depth_unchanged = 16,
    local_size = 17,
    local_size_hint = 18,
    input_points = 19,
    input_lines = 20,
    input_lines_adjacency = 21,
    triangles = 22,
    input_triangles_adjacency = 23,
    quads = 24,
    isolines = 25,
    output_vertices = 26,
    output_points = 27,
    output_line_strip = 28,
    output_triangle_strip = 29,

    /// <summary>
    ///     VecTypeHint 整数类型提示的
    /// </summary>
    vec_type_hint = 30,

    /// <summary>
    ///     连续线输出的
    /// </summary>
    contraction_off = 31,

    /// <summary>
    ///     后段深度覆盖的
    /// </summary>
    post_depth_coverage = 4446,
    denorm_preserve = 4459,
    denorm_flush_to_zero = 4460,
    signed_zero_inf_nan_preserve = 4461,
    rounding_mode_rte = 4462,
    rounding_mode_rtz = 4463,
    stencil_ref_replacing_ext = 5101,
    output_lines_ext = 5195,
    output_primitives_ext = 5196,
    local_size_id = 5197,
    local_size_hint_id = 5198,
    subgroup_uniform_control_flow_khr = 5021,
    subgroup_size = 5027,
    subgroups_per_workgroup = 5028,
    subgroups_per_workgroup_id = 5029
}