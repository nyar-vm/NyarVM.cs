namespace Std.Data.Binary.Vrm.Data;

/// <summary>
///     VRM 格式常量的
/// </summary>
public static class VrmConstants
{
    /// <summary>
    ///     VRM 扩展名称的
    /// </summary>
    public const string extension_name = "VRM";

    /// <summary>
    ///     VRM 0.x 版本扩展名称的
    /// </summary>
    public const string vrm0_extension = "VRM";

    /// <summary>
    ///     VRM 1.0 版本扩展名称的
    /// </summary>
    public const string vrm1_extension = "VRMC_vrm";

    /// <summary>
    ///     VRM 1.0 材质扩展名称的
    /// </summary>
    public const string vrm1_material_extension = "VRMC_materials_mtoon";

    /// <summary>
    ///     VRM 1.0 约束扩展名称的
    /// </summary>
    public const string vrm1_constraint_extension = "VRMC_node_constraint";

    /// <summary>
    ///     VRM 1.0 弹簧骨骼扩展名称的
    /// </summary>
    public const string vrm1_spring_bone_extension = "VRMC_springBone";
}

/// <summary>
///     VRM 版本的
/// </summary>
public enum VrmVersion
{
    /// <summary>
    ///     未知版本的
    /// </summary>
    unknown,

    /// <summary>
    ///     VRM 0.x的
    /// </summary>
    vrm0,

    /// <summary>
    ///     VRM 1.0的
    /// </summary>
    vrm1
}