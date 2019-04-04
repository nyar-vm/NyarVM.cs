namespace Std.Data.Binary.Basis.Data;

/// <summary>
///     Basis/KTX2 纹理文件数据的
/// </summary>
public sealed class BasisFileData
{
    /// <summary>
    ///     纹理宽度的
    /// </summary>
    public int width { get; init; }

    /// <summary>
    ///     纹理高度的
    /// </summary>
    public int height { get; init; }

    /// <summary>
    ///     Mipmap 级别数的
    /// </summary>
    public int mip_levels { get; init; }

    /// <summary>
    ///     纹理格式的
    /// </summary>
    public BasisTextureFormat format { get; init; }

    /// <summary>
    ///     是否的sRGB的
    /// </summary>
    public bool is_srgb { get; init; }

    /// <summary>
    ///     图像数量的
    /// </summary>
    public int image_count { get; init; }
}