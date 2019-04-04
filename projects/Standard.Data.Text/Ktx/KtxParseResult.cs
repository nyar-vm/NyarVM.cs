namespace Std.Data.Text.Ktx;

/// <summary>
///     KTX 解析结果
/// </summary>
public sealed class KtxParseResult
{
    /// <summary>
    ///     纹理宽度
    /// </summary>
    public int width { get; init; }


    /// <summary>
    ///     纹理高度
    /// </summary>
    public int height { get; init; }


    /// <summary>
    ///     纹理深度
    /// </summary>
    public int depth { get; init; } = 1;


    /// <summary>
    ///     MipMap 层级数
    /// </summary>
    public int mip_levels { get; init; } = 1;


    /// <summary>
    ///     数组层数
    /// </summary>
    public int array_layers { get; init; } = 1;


    /// <summary>
    ///     纹理格式
    /// </summary>
    public TextureFormat format { get; init; }


    /// <summary>
    ///     纹理维度
    ///     。
    /// </summary>
    public TextureDimension dimension { get; init; }


    /// <summary>
    ///     原始纹理数据（第一层第一面）
    ///     。
    /// </summary>
    public byte[] raw_data { get; init; } = [];


    /// <summary>
    ///     所有 Mip 层级数据
    ///     。
    /// </summary>
    public List<byte[]> mip_data { get; init; } = [];


    /// <summary>
    ///     OpenGL 内部格式（KTX1）
    ///     。
    /// </summary>
    public uint gl_internal_format { get; init; }


    /// <summary>
    ///     Vulkan 格式（KTX2）
    ///     。
    /// </summary>
    public uint vk_format { get; init; }
}