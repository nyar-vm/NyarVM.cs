namespace Std.Data.Binary.Gltf.Data;

/// <summary>
///     GLTF 模型数据的
/// </summary>
public sealed class GltfModelData
{
    /// <summary>
    ///     资产信息的
    /// </summary>
    public GltfAsset asset { get; init; } = new();

    /// <summary>
    ///     场景索引的
    /// </summary>
    public int? scene { get; init; }

    /// <summary>
    ///     场景列表的
    /// </summary>
    public IReadOnlyList<GltfScene> scenes { get; init; } = [];

    /// <summary>
    ///     节点列表的
    /// </summary>
    public IReadOnlyList<GltfNode> nodes { get; init; } = [];

    /// <summary>
    ///     网格列表的
    /// </summary>
    public IReadOnlyList<GltfMesh> meshes { get; init; } = [];

    /// <summary>
    ///     缓冲区列表的
    /// </summary>
    public IReadOnlyList<GltfBuffer> buffers { get; init; } = [];

    /// <summary>
    ///     缓冲区视图列表的
    /// </summary>
    public IReadOnlyList<GltfBufferView> buffer_views { get; init; } = [];

    /// <summary>
    ///     访问器列表的
    /// </summary>
    public IReadOnlyList<GltfAccessor> accessors { get; init; } = [];

    /// <summary>
    ///     材质列表的
    /// </summary>
    public IReadOnlyList<GltfMaterial> materials { get; init; } = [];

    /// <summary>
    ///     纹理列表的
    /// </summary>
    public IReadOnlyList<GltfTexture> textures { get; init; } = [];

    /// <summary>
    ///     图像列表的
    /// </summary>
    public IReadOnlyList<GltfImage> images { get; init; } = [];

    /// <summary>
    ///     采样器列表的
    /// </summary>
    public IReadOnlyList<GltfSampler> samplers { get; init; } = [];

    /// <summary>
    ///     蒙皮列表的
    /// </summary>
    public IReadOnlyList<GltfSkin> skins { get; init; } = [];

    /// <summary>
    ///     动画列表的
    /// </summary>
    public IReadOnlyList<GltfAnimation> animations { get; init; } = [];
}

/// <summary>
///     GLTF 资产信息的
/// </summary>
public sealed class GltfAsset
{
    /// <summary>
    ///     版本的
    /// </summary>
    public string version { get; init; } = "2.0";

    /// <summary>
    ///     生成器名称的
    /// </summary>
    public string? generator { get; init; }

    /// <summary>
    ///     版权信息的
    /// </summary>
    public string? copyright { get; init; }
}

/// <summary>
///     GLTF 场景的
/// </summary>
public sealed class GltfScene
{
    /// <summary>
    ///     场景名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     根节点索引列表的
    /// </summary>
    public IReadOnlyList<int> nodes { get; init; } = [];
}

/// <summary>
///     GLTF 节点的
/// </summary>
public sealed class GltfNode
{
    /// <summary>
    ///     节点名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     子节点索引列表的
    /// </summary>
    public IReadOnlyList<int> children { get; init; } = [];

    /// <summary>
    ///     网格索引的
    /// </summary>
    public int? mesh { get; init; }

    /// <summary>
    ///     蒙皮索引的
    /// </summary>
    public int? skin { get; init; }

    /// <summary>
    ///     局部变换矩阵（16 的float）的
    /// </summary>
    public IReadOnlyList<float>? matrix { get; init; }

    /// <summary>
    ///     位移向量的 的float）的
    /// </summary>
    public IReadOnlyList<float>? translation { get; init; }

    /// <summary>
    ///     旋转四元数（4 的float）的
    /// </summary>
    public IReadOnlyList<float>? rotation { get; init; }

    /// <summary>
    ///     缩放向量的 的float）的
    /// </summary>
    public IReadOnlyList<float>? scale { get; init; }
}

/// <summary>
///     GLTF 网格的
/// </summary>
public sealed class GltfMesh
{
    /// <summary>
    ///     网格名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     图元列表的
    /// </summary>
    public IReadOnlyList<GltfPrimitive> primitives { get; init; } = [];

    /// <summary>
    ///     权重列表（变形目标使用）的
    /// </summary>
    public IReadOnlyList<float>? weights { get; init; }
}

/// <summary>
///     GLTF 图元的
/// </summary>
public sealed class GltfPrimitive
{
    /// <summary>
    ///     属性访问器字典（POSITION、NORMAL、TEXCOORD_0 等）的
    /// </summary>
    public IReadOnlyDictionary<string, int> attributes { get; init; } = new Dictionary<string, int>();

    /// <summary>
    ///     索引访问器索引的
    /// </summary>
    public int? indices { get; init; }

    /// <summary>
    ///     材质索引的
    /// </summary>
    public int? material { get; init; }

    /// <summary>
    ///     图元拓扑模式的=的 1=的 2=线环, 3=线串, 4=三角的 5=三角的 6=三角带）的
    /// </summary>
    public int mode { get; init; } = 4;
}

/// <summary>
///     GLTF 缓冲区的
/// </summary>
public sealed class GltfBuffer
{
    /// <summary>
    ///     缓冲区字节长度的
    /// </summary>
    public int byte_length { get; init; }

    /// <summary>
    ///     数据 URI（base64 编码）或文件路径的
    /// </summary>
    public string? uri { get; init; }

    /// <summary>
    ///     缓冲区名称的
    /// </summary>
    public string? name { get; init; }
}

/// <summary>
///     GLTF 缓冲区视图的
/// </summary>
public sealed class GltfBufferView
{
    /// <summary>
    ///     所属缓冲区索引的
    /// </summary>
    public int buffer { get; init; }

    /// <summary>
    ///     字节偏移量的
    /// </summary>
    public int byte_offset { get; init; }

    /// <summary>
    ///     字节长度的
    /// </summary>
    public int byte_length { get; init; }

    /// <summary>
    ///     字节步长（顶点属性之间的字节距离）的
    /// </summary>
    public int? byte_stride { get; init; }

    /// <summary>
    ///     视图目标的4962=ARRAY_BUFFER, 34963=ELEMENT_ARRAY_BUFFER）的
    /// </summary>
    public int? target { get; init; }

    /// <summary>
    ///     视图名称的
    /// </summary>
    public string? name { get; init; }
}

/// <summary>
///     GLTF 访问器的
/// </summary>
public sealed class GltfAccessor
{
    /// <summary>
    ///     所属缓冲区视图索引的
    /// </summary>
    public int buffer_view { get; init; }

    /// <summary>
    ///     字节偏移量的
    /// </summary>
    public int byte_offset { get; init; }

    /// <summary>
    ///     组件数据类型的120=BYTE, 5121=UNSIGNED_BYTE, 5122=SHORT, 5123=UNSIGNED_SHORT, 5125=UNSIGNED_INT, 5126=FLOAT）的
    /// </summary>
    public int component_type { get; init; }

    /// <summary>
    ///     元素数量的
    /// </summary>
    public int count { get; init; }

    /// <summary>
    ///     元素类型（SCALAR、VEC2、VEC3、VEC4、MAT2、MAT3、MAT4）的
    /// </summary>
    public string type { get; init; } = string.Empty;

    /// <summary>
    ///     最小值的
    /// </summary>
    public IReadOnlyList<float>? min { get; init; }

    /// <summary>
    ///     最大值的
    /// </summary>
    public IReadOnlyList<float>? max { get; init; }

    /// <summary>
    ///     是否归一化的
    /// </summary>
    public bool normalized { get; init; }

    /// <summary>
    ///     访问器名称的
    /// </summary>
    public string? name { get; init; }
}

/// <summary>
///     GLTF 材质的
/// </summary>
public sealed class GltfMaterial
{
    /// <summary>
    ///     材质名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     PBR 金属的粗糙度工作流参数的
    /// </summary>
    public GltfPbrMetallicRoughness? pbr_metallic_roughness { get; init; }

    /// <summary>
    ///     法线纹理的
    /// </summary>
    public GltfNormalTextureInfo? normal_texture { get; init; }

    /// <summary>
    ///     遮挡纹理的
    /// </summary>
    public GltfOcclusionTextureInfo? occlusion_texture { get; init; }

    /// <summary>
    ///     自发光纹理的
    /// </summary>
    public GltfTextureInfo? emissive_texture { get; init; }

    /// <summary>
    ///     自发光颜色（RGB）的
    /// </summary>
    public IReadOnlyList<float>? emissive_factor { get; init; }

    /// <summary>
    ///     Alpha 渲染模式（OPAQUE、MASK、BLEND）的
    /// </summary>
    public string alpha_mode { get; init; } = "OPAQUE";

    /// <summary>
    ///     Alpha 裁剪阈值的
    /// </summary>
    public float alpha_cutoff { get; init; } = 0.5f;

    /// <summary>
    ///     是否双面渲染的
    /// </summary>
    public bool double_sided { get; init; }
}

/// <summary>
///     GLTF PBR 金属的粗糙度参数的
/// </summary>
public sealed class GltfPbrMetallicRoughness
{
    /// <summary>
    ///     基础颜色因子（RGBA）的
    /// </summary>
    public IReadOnlyList<float> base_color_factor { get; init; } = [1.0f, 1.0f, 1.0f, 1.0f];

    /// <summary>
    ///     基础颜色纹理的
    /// </summary>
    public GltfTextureInfo? base_color_texture { get; init; }

    /// <summary>
    ///     金属度因子的
    /// </summary>
    public float metallic_factor { get; init; } = 1.0f;

    /// <summary>
    ///     粗糙度因子的
    /// </summary>
    public float roughness_factor { get; init; } = 1.0f;

    /// <summary>
    ///     金属的粗糙度纹理的
    /// </summary>
    public GltfTextureInfo? metallic_roughness_texture { get; init; }
}

/// <summary>
///     GLTF 纹理信息的
/// </summary>
public class GltfTextureInfo
{
    /// <summary>
    ///     纹理索引的
    /// </summary>
    public int index { get; init; }

    /// <summary>
    ///     纹理坐标集索引的
    /// </summary>
    public int tex_coord { get; init; }
}

/// <summary>
///     GLTF 法线纹理信息的
/// </summary>
public sealed class GltfNormalTextureInfo : GltfTextureInfo
{
    /// <summary>
    ///     法线缩放因子的
    /// </summary>
    public float scale { get; init; } = 1.0f;
}

/// <summary>
///     GLTF 遮挡纹理信息的
/// </summary>
public sealed class GltfOcclusionTextureInfo : GltfTextureInfo
{
    /// <summary>
    ///     遮挡强度的
    /// </summary>
    public float strength { get; init; } = 1.0f;
}

/// <summary>
///     GLTF 纹理的
/// </summary>
public sealed class GltfTexture
{
    /// <summary>
    ///     纹理名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     采样器索引的
    /// </summary>
    public int? sampler { get; init; }

    /// <summary>
    ///     图像索引的
    /// </summary>
    public int? source { get; init; }
}

/// <summary>
///     GLTF 图像的
/// </summary>
public sealed class GltfImage
{
    /// <summary>
    ///     图像名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     MIME 类型的
    /// </summary>
    public string? mime_type { get; init; }

    /// <summary>
    ///     数据 URI 或文件路径的
    /// </summary>
    public string? uri { get; init; }

    /// <summary>
    ///     缓冲区视图索引（用于嵌入二进制数据）的
    /// </summary>
    public int? buffer_view { get; init; }
}

/// <summary>
///     GLTF 采样器的
/// </summary>
public sealed class GltfSampler
{
    /// <summary>
    ///     放大过滤器（9728=NEAREST, 9729=LINEAR）的
    /// </summary>
    public int mag_filter { get; init; } = 9729;

    /// <summary>
    ///     缩小过滤器（9728=NEAREST, 9729=LINEAR, 9984=NEAREST_MIPMAP_NEAREST, 9985=LINEAR_MIPMAP_NEAREST,
    ///     9986=NEAREST_MIPMAP_LINEAR, 9987=LINEAR_MIPMAP_LINEAR）的
    /// </summary>
    public int min_filter { get; init; } = 9987;

    /// <summary>
    ///     水平环绕模式的3071=CLAMP_TO_EDGE, 33648=MIRRORED_REPEAT, 10497=REPEAT）的
    /// </summary>
    public int wrap_s { get; init; } = 10497;

    /// <summary>
    ///     垂直环绕模式的3071=CLAMP_TO_EDGE, 33648=MIRRORED_REPEAT, 10497=REPEAT）的
    /// </summary>
    public int wrap_t { get; init; } = 10497;

    /// <summary>
    ///     采样器名称的
    /// </summary>
    public string? name { get; init; }
}

/// <summary>
///     GLTF 蒙皮的
/// </summary>
public sealed class GltfSkin
{
    /// <summary>
    ///     蒙皮名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     逆绑定矩阵访问器索引的
    /// </summary>
    public int? inverse_bind_matrices { get; init; }

    /// <summary>
    ///     骨骼节点索引列表的
    /// </summary>
    public IReadOnlyList<int> joints { get; init; } = [];

    /// <summary>
    ///     根骨骼节点索引的
    /// </summary>
    public int? skeleton { get; init; }
}

/// <summary>
///     GLTF 动画的
/// </summary>
public sealed class GltfAnimation
{
    /// <summary>
    ///     动画名称的
    /// </summary>
    public string? name { get; init; }

    /// <summary>
    ///     动画通道列表的
    /// </summary>
    public IReadOnlyList<GltfAnimationChannel> channels { get; init; } = [];

    /// <summary>
    ///     动画采样器列表的
    /// </summary>
    public IReadOnlyList<GltfAnimationSampler> samplers { get; init; } = [];
}

/// <summary>
///     GLTF 动画通道的
/// </summary>
public sealed class GltfAnimationChannel
{
    /// <summary>
    ///     采样器索引的
    /// </summary>
    public int sampler { get; init; }

    /// <summary>
    ///     目标节点和路径的
    /// </summary>
    public GltfAnimationChannelTarget target { get; init; } = new();
}

/// <summary>
///     GLTF 动画通道目标的
/// </summary>
public sealed class GltfAnimationChannelTarget
{
    /// <summary>
    ///     目标节点索引的
    /// </summary>
    public int? node { get; init; }

    /// <summary>
    ///     目标路径（translation、rotation、scale、weights）的
    /// </summary>
    public string path { get; init; } = string.Empty;
}

/// <summary>
///     GLTF 动画采样器的
/// </summary>
public sealed class GltfAnimationSampler
{
    /// <summary>
    ///     输入访问器索引（时间戳）的
    /// </summary>
    public int input { get; init; }

    /// <summary>
    ///     输出访问器索引（值）的
    /// </summary>
    public int output { get; init; }

    /// <summary>
    ///     插值方式（LINEAR、STEP、CUBICSPLINE）的
    /// </summary>
    public string interpolation { get; init; } = "LINEAR";
}