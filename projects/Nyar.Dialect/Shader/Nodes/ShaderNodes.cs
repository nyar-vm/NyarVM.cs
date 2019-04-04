using System.Collections.Immutable;
using Nyar.IR.Intent;

namespace Nyar.Dialect.Shader.Nodes;

#region Shader 声明

[AlgebraNode]
public sealed partial record ShaderDecl(string name, ImmutableArray<Id> members) : AlgebraNode;

[AlgebraNode]
public sealed partial record ShaderStageDecl(string stage, string name, ImmutableArray<Id> body) : AlgebraNode;

#endregion

/// <summary>
///     Shader 方言内置函数标识
/// </summary>
public enum ShaderBuiltin
{
    /// <summary>计算着色器入口。</summary>
    compute_kernel_def = 0xC001,

    /// <summary>屏障同步。</summary>
    barrier_def = 0xC002,

    /// <summary>全局调用 ID。</summary>
    global_invocation_id_def = 0xC003,

    /// <summary>局部调用 ID。</summary>
    local_invocation_id_def = 0xC004,

    /// <summary>工作组 ID。</summary>
    work_group_id_def = 0xC005,

    /// <summary>工作组数量。</summary>
    num_work_groups_def = 0xC006,

    /// <summary>向量构造。</summary>
    vec_def = 0xC101,

    /// <summary>向量加载。</summary>
    vec_load_def = 0xC102,

    /// <summary>向量分量提取。</summary>
    vec_extract_def = 0xC103,

    /// <summary>数组加载。</summary>
    array_load_def = 0xC104,

    /// <summary>空操作。</summary>
    nop_def = 0xC105,

    /// <summary>矩阵构造。</summary>
    mat_def = 0xC106,

    /// <summary>Shader 声明。</summary>
    shader_decl_def = 0xC201,

    /// <summary>Shader 阶段声明。</summary>
    shader_stage_decl_def = 0xC202
}

#region 计算着色器

[AlgebraNode]
public sealed partial record Barrier(string memory_scope) : AlgebraNode
{
    /// <summary>
    ///     内存范围（用于生成代码兼容）
    /// </summary>
    public string memoryScope => memory_scope;
}

[AlgebraNode]
public sealed partial record GlobalInvocationId : AlgebraNode;

[AlgebraNode]
public sealed partial record LocalInvocationId : AlgebraNode;

[AlgebraNode]
public sealed partial record WorkGroupId : AlgebraNode;

[AlgebraNode]
public sealed partial record NumWorkGroups : AlgebraNode;

#endregion

#region 存储与缓冲

[AlgebraNode]
public sealed partial record StorageLoad(Id buffer, Id index) : AlgebraNode;

[AlgebraNode]
public sealed partial record StorageStore(Id buffer, Id index, Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record SharedLoad(Id address) : AlgebraNode;

[AlgebraNode]
public sealed partial record SharedStore(Id address, Id value) : AlgebraNode;

#endregion

#region 向量与矩阵

[AlgebraNode]
public sealed partial record Vec(int size, IReadOnlyList<Id> components) : AlgebraNode;

[AlgebraNode]
public sealed partial record VecLoad(Id array, Id start_index, int count) : AlgebraNode
{
    /// <summary>
    ///     起始索引（用于生成代码兼容）
    /// </summary>
    public Id startIndex => start_index;
}

[AlgebraNode]
public sealed partial record ArrayLoad(Id array, Id index) : AlgebraNode;

[AlgebraNode]
public sealed partial record Nop : AlgebraNode;

[AlgebraNode]
public sealed partial record VecExtract(Id vector, string swizzle) : AlgebraNode;

[AlgebraNode]
public sealed partial record Mat(int rows, int cols, IReadOnlyList<Id> elements) : AlgebraNode;

[AlgebraNode]
public sealed partial record MatMul(Id left, Id right) : AlgebraNode;

#endregion

#region 图像采样

/// <summary>
///     隐式 LOD 纹理采样（片段着色器中使用硬件自动计算 LOD）
/// </summary>
[AlgebraNode]
public sealed partial record Sample(Id texture, Id coordinates) : AlgebraNode;

/// <summary>
///     显式 LOD 纹理采样（计算着色器中必须显式指定 LOD 级别）
/// </summary>
[AlgebraNode]
public sealed partial record SampleLod(Id texture, Id coordinates, Id lod) : AlgebraNode;

/// <summary>
///     梯度纹理采样（使用显式导数计算 LOD，用于各向异性过滤等场景）
/// </summary>
[AlgebraNode]
public sealed partial record SampleGrad(Id texture, Id coordinates, Id ddx, Id ddy) : AlgebraNode;

/// <summary>
///     深度比较纹理采样（阴影贴图，将深度值与采样结果比较后返回 0 或 1）
/// </summary>
[AlgebraNode]
public sealed partial record SampleDref(Id texture, Id coordinates, Id depth_ref) : AlgebraNode
{
    /// <summary>
    ///     深度参考值（用于生成代码兼容）
    /// </summary>
    public Id depthRef => depth_ref;
}

/// <summary>
///     纹理纹素读取（无采样器，直接按整数坐标读取纹素）
/// </summary>
[AlgebraNode]
public sealed partial record TextureLoad(Id texture, Id coordinates) : AlgebraNode;

/// <summary>
///     纹理纹素写入（Storage Image 写入）
/// </summary>
[AlgebraNode]
public sealed partial record TextureStore(Id texture, Id coordinates, Id value) : AlgebraNode;

/// <summary>
///     纹理尺寸查询（返回指定 LOD 级别的纹理宽高）
/// </summary>
[AlgebraNode]
public sealed partial record TextureSize(Id texture, Id lod) : AlgebraNode;

/// <summary>
///     纹理 LOD 查询（返回给定坐标下硬件计算的 LOD 值）
/// </summary>
[AlgebraNode]
public sealed partial record TextureQueryLod(Id texture, Id coordinates) : AlgebraNode;

/// <summary>
///     纹理 Mip 层级数查询
/// </summary>
[AlgebraNode]
public sealed partial record TextureQueryLevels(Id texture) : AlgebraNode;

/// <summary>
///     纹理采样数查询（多重采样纹理的采样数）
/// </summary>
[AlgebraNode]
public sealed partial record TextureQuerySamples(Id texture) : AlgebraNode;

/// <summary>
///     纹理 Gather 操作（采集指定分量的四个相邻纹素）
/// </summary>
[AlgebraNode]
public sealed partial record ImageGather(Id texture, Id coordinates, Id component) : AlgebraNode;

/// <summary>
///     深度比较 Gather 操作（采集四个相邻纹素并与深度参考值比较）
/// </summary>
[AlgebraNode]
public sealed partial record ImageDrefGather(Id texture, Id coordinates, Id depth_ref) : AlgebraNode
{
    /// <summary>
    ///     深度参考值（用于生成代码兼容）
    /// </summary>
    public Id depthRef => depth_ref;
}

#endregion

#region 顶点管线接口

/// <summary>
///     顶点输入属性声明，指定位置、语义名称和数据格式
/// </summary>
[AlgebraNode]
public sealed partial record VertexInput(int location, string semantic, string format) : AlgebraNode;

/// <summary>
///     顶点着色器输出（传递至片元着色器的插值变量）
/// </summary>
[AlgebraNode]
public sealed partial record VertexOutput(int location, string name, Id value) : AlgebraNode;

/// <summary>
///     片元着色器输入（从顶点输出插值而来）
/// </summary>
[AlgebraNode]
public sealed partial record FragmentInput(int location, string name) : AlgebraNode;

/// <summary>
///     片元着色器输出（颜色附件）
/// </summary>
[AlgebraNode]
public sealed partial record FragmentOutput(int location, Id value) : AlgebraNode;

/// <summary>
///     顶点缓冲区绑定描述，定义绑定索引、步长和输入速率
/// </summary>
[AlgebraNode]
public sealed partial record VertexBinding(int binding, int stride, string input_rate) : AlgebraNode
{
    /// <summary>
    ///     输入速率（用于生成代码兼容）
    /// </summary>
    public string inputRate => input_rate;
}

/// <summary>
///     SPIR-V 内建变量引用（如 Position、VertexIndex、InstanceIndex、FragCoord 等）
/// </summary>
[AlgebraNode]
public sealed partial record BuiltinVariable(string name) : AlgebraNode;

/// <summary>
///     几何着色器图元拓扑声明
/// </summary>
[AlgebraNode]
public sealed partial record GeometryInput(string topology) : AlgebraNode;

/// <summary>
///     几何着色器输出图元拓扑声明
/// </summary>
[AlgebraNode]
public sealed partial record GeometryOutput(string topology, int max_vertices) : AlgebraNode
{
    /// <summary>
    ///     最大顶点数（用于生成代码兼容）
    /// </summary>
    public int maxVertices => max_vertices;
}

#endregion

#region Uniform 参数绑定

/// <summary>
///     Uniform Buffer 声明（对应 Vulkan descriptor set + binding）
/// </summary>
[AlgebraNode]
public sealed partial record UniformBufferDecl(
    int set,
    int binding,
    string name,
    string[] member_names,
    string[] member_types) : AlgebraNode
{
    /// <summary>
    ///     成员名称列表（用于生成代码兼容）
    /// </summary>
    public string[] memberNames => member_names;

    /// <summary>
    ///     成员类型列表（用于生成代码兼容）
    /// </summary>
    public string[] memberTypes => member_types;
}

/// <summary>
///     从 Uniform Buffer 中读取成员变量
/// </summary>
[AlgebraNode]
public sealed partial record UniformBufferLoad(Id buffer, string member_name, int member_index) : AlgebraNode
{
    /// <summary>
    ///     成员名称（用于生成代码兼容）
    /// </summary>
    public string memberName => member_name;

    /// <summary>
    ///     成员索引（用于生成代码兼容）
    /// </summary>
    public int memberIndex => member_index;
}

/// <summary>
///     Push Constant 声明（少量高频更新数据）
/// </summary>
[AlgebraNode]
public sealed partial record PushConstantDecl(string name, string[] member_names, string[] member_types) : AlgebraNode
{
    /// <summary>
    ///     成员名称列表（用于生成代码兼容）
    /// </summary>
    public string[] memberNames => member_names;

    /// <summary>
    ///     成员类型列表（用于生成代码兼容）
    /// </summary>
    public string[] memberTypes => member_types;
}

/// <summary>
///     读取 Push Constant 成员
/// </summary>
[AlgebraNode]
public sealed partial record PushConstantLoad(Id buffer, string member_name, int member_index) : AlgebraNode
{
    /// <summary>
    ///     成员名称（用于生成代码兼容）
    /// </summary>
    public string memberName => member_name;

    /// <summary>
    ///     成员索引（用于生成代码兼容）
    /// </summary>
    public int memberIndex => member_index;
}

/// <summary>
///     Sampler + Texture 组合绑定（对应 DescriptorSet layout）
/// </summary>
[AlgebraNode]
public sealed partial record CombinedImageSampler(int set, int binding, string name) : AlgebraNode;

/// <summary>
///     独立采样器声明（Vulkan 风格分离式采样器，与 SampledImageDecl 配合使用）
/// </summary>
[AlgebraNode]
public sealed partial record SamplerDecl(int set, int binding, string name) : AlgebraNode;

/// <summary>
///     只读采样图像声明（Vulkan 风格分离式纹理，与 SamplerDecl 配合使用）
/// </summary>
[AlgebraNode]
public sealed partial record SampledImageDecl(int set, int binding, string name, string dim) : AlgebraNode;

/// <summary>
///     存储图像声明（可写纹理，用于 Compute Shader 的 imageStore 等操作）
/// </summary>
[AlgebraNode]
public sealed partial record StorageImageDecl(int set, int binding, string name, string dim, string format) : AlgebraNode;

/// <summary>
///     Storage Buffer 声明（SSBO，可读写缓冲区，对应 Vulkan descriptor set + binding）
/// </summary>
[AlgebraNode]
public sealed partial record StorageBufferDecl(
    int set,
    int binding,
    string name,
    string[] member_names,
    string[] member_types) : AlgebraNode
{
    /// <summary>
    ///     成员名称列表（用于生成代码兼容）
    /// </summary>
    public string[] memberNames => member_names;

    /// <summary>
    ///     成员类型列表（用于生成代码兼容）
    /// </summary>
    public string[] memberTypes => member_types;
}

/// <summary>
///     子通道输入附件声明（延迟渲染等场景中从上一子通道读取颜色/深度）
/// </summary>
[AlgebraNode]
public sealed partial record InputAttachmentDecl(int set, int binding, string name, int input_attachment_index) : AlgebraNode
{
    /// <summary>
    ///     子通道输入附件索引（用于生成代码兼容）
    /// </summary>
    public int inputAttachmentIndex => input_attachment_index;
}

#endregion

#region 原子操作

[AlgebraNode]
public sealed partial record AtomicAdd(Id pointer, Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record AtomicExchange(Id pointer, Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record AtomicCompareExchange(Id pointer, Id expected, Id desired) : AlgebraNode;

#endregion

#region 特殊函数

[AlgebraNode]
public sealed partial record Dot(Id left, Id right) : AlgebraNode;

[AlgebraNode]
public sealed partial record Cross(Id left, Id right) : AlgebraNode;

[AlgebraNode]
public sealed partial record Length(Id vector) : AlgebraNode;

[AlgebraNode]
public sealed partial record Normalize(Id vector) : AlgebraNode;

[AlgebraNode]
public sealed partial record Reflect(Id incident, Id normal) : AlgebraNode;

[AlgebraNode]
public sealed partial record Refract(Id incident, Id normal, Id eta) : AlgebraNode;

#endregion