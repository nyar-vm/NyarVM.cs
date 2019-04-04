using System.Collections.Immutable;
using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Shader;

/// <summary>
///     Shader 方言的 OA 接口定义。
///     该接口声明着色器结构、计算着色器、资源绑定、采样与向量矩阵操作。
/// </summary>
[Dialect("shader")]
public interface IShader<T>
{
    /// <summary>
    ///     Shader 声明。
    /// </summary>
    [Operator("shader_decl")]
    Term<T> ShaderDecl(string name, ImmutableArray<Term<T>> members);

    /// <summary>
    ///     Shader 阶段声明。
    /// </summary>
    [Operator("shader_stage_decl")]
    Term<T> ShaderStageDecl(string stage, string name, ImmutableArray<Term<T>> body);

    /// <summary>
    ///     计算着色器入口。
    /// </summary>
    [Operator("compute_kernel")]
    Term<T> ComputeKernel(Term<T> body, (int X, int Y, int Z) workGroupSize);

    /// <summary>
    ///     屏障同步。
    /// </summary>
    [Operator("barrier")]
    Term<T> Barrier(string memoryScope);

    /// <summary>
    ///     全局调用 ID。
    /// </summary>
    [Operator("global_invocation_id")]
    Term<T> GlobalInvocationId();

    /// <summary>
    ///     局部调用 ID。
    /// </summary>
    [Operator("local_invocation_id")]
    Term<T> LocalInvocationId();

    /// <summary>
    ///     工作组 ID。
    /// </summary>
    [Operator("work_group_id")]
    Term<T> WorkGroupId();

    /// <summary>
    ///     工作组数量。
    /// </summary>
    [Operator("num_work_groups")]
    Term<T> NumWorkGroups();

    /// <summary>
    ///     Uniform 读取。
    /// </summary>
    [Operator("uniform_load")]
    Term<T> UniformLoad(Term<T> buffer, Term<T> index);

    /// <summary>
    ///     Storage 读取。
    /// </summary>
    [Operator("storage_load")]
    Term<T> StorageLoad(Term<T> buffer, Term<T> index);

    /// <summary>
    ///     Storage 写入。
    /// </summary>
    [Operator("storage_store")]
    Term<T> StorageStore(Term<T> buffer, Term<T> index, Term<T> value);

    /// <summary>
    ///     Shared 读取。
    /// </summary>
    [Operator("shared_load")]
    Term<T> SharedLoad(Term<T> address);

    /// <summary>
    ///     Shared 写入。
    /// </summary>
    [Operator("shared_store")]
    Term<T> SharedStore(Term<T> address, Term<T> value);

    /// <summary>
    ///     向量构造。
    /// </summary>
    [Operator("vec")]
    Term<T> Vec(int size, IReadOnlyList<Term<T>> components);

    /// <summary>
    ///     向量加载。
    /// </summary>
    [Operator("vec_load")]
    Term<T> VecLoad(Term<T> array, Term<T> startIndex, int count);

    /// <summary>
    ///     数组加载。
    /// </summary>
    [Operator("array_load")]
    Term<T> ArrayLoad(Term<T> array, Term<T> index);

    /// <summary>
    ///     空操作。
    /// </summary>
    [Operator("nop")]
    Term<T> Nop();

    /// <summary>
    ///     向量分量提取。
    /// </summary>
    [Operator("vec_extract")]
    Term<T> VecExtract(Term<T> vector, string swizzle);

    /// <summary>
    ///     矩阵构造。
    /// </summary>
    [Operator("mat")]
    Term<T> Mat(int rows, int cols, IReadOnlyList<Term<T>> elements);

    /// <summary>
    ///     矩阵乘法。
    /// </summary>
    [Operator("mat_mul")]
    Term<T> MatMul(Term<T> left, Term<T> right);

    /// <summary>
    ///     纹理采样。
    /// </summary>
    [Operator("sample")]
    Term<T> Sample(Term<T> texture, Term<T> coordinates);

    /// <summary>
    ///     显式 LOD 采样。
    /// </summary>
    [Operator("sample_lod")]
    Term<T> SampleLod(Term<T> texture, Term<T> coordinates, Term<T> lod);

    /// <summary>
    ///     梯度采样。
    /// </summary>
    [Operator("sample_grad")]
    Term<T> SampleGrad(Term<T> texture, Term<T> coordinates, Term<T> ddx, Term<T> ddy);

    /// <summary>
    ///     深度比较采样。
    /// </summary>
    [Operator("sample_dref")]
    Term<T> SampleDref(Term<T> texture, Term<T> coordinates, Term<T> depthRef);

    /// <summary>
    ///     纹素读取。
    /// </summary>
    [Operator("texture_load")]
    Term<T> TextureLoad(Term<T> texture, Term<T> coordinates);

    /// <summary>
    ///     纹素写入。
    /// </summary>
    [Operator("texture_store")]
    Term<T> TextureStore(Term<T> texture, Term<T> coordinates, Term<T> value);

    /// <summary>
    ///     纹理尺寸查询。
    /// </summary>
    [Operator("texture_size")]
    Term<T> TextureSize(Term<T> texture, Term<T> lod);

    /// <summary>
    ///     纹理 LOD 查询。
    /// </summary>
    [Operator("texture_query_lod")]
    Term<T> TextureQueryLod(Term<T> texture, Term<T> coordinates);

    /// <summary>
    ///     纹理层级数查询。
    /// </summary>
    [Operator("texture_query_levels")]
    Term<T> TextureQueryLevels(Term<T> texture);

    /// <summary>
    ///     纹理采样数查询。
    /// </summary>
    [Operator("texture_query_samples")]
    Term<T> TextureQuerySamples(Term<T> texture);

    /// <summary>
    ///     Gather 操作。
    /// </summary>
    [Operator("image_gather")]
    Term<T> ImageGather(Term<T> texture, Term<T> coordinates, Term<T> component);

    /// <summary>
    ///     深度比较 Gather。
    /// </summary>
    [Operator("image_dref_gather")]
    Term<T> ImageDrefGather(Term<T> texture, Term<T> coordinates, Term<T> depthRef);

    /// <summary>
    ///     顶点输入。
    /// </summary>
    [Operator("vertex_input")]
    Term<T> VertexInput(int location, string semantic, string format);

    /// <summary>
    ///     顶点输出。
    /// </summary>
    [Operator("vertex_output")]
    Term<T> VertexOutput(int location, string name, Term<T> value);

    /// <summary>
    ///     片元输入。
    /// </summary>
    [Operator("fragment_input")]
    Term<T> FragmentInput(int location, string name);

    /// <summary>
    ///     片元输出。
    /// </summary>
    [Operator("fragment_output")]
    Term<T> FragmentOutput(int location, Term<T> value);

    /// <summary>
    ///     顶点绑定。
    /// </summary>
    [Operator("vertex_binding")]
    Term<T> VertexBinding(int binding, int stride, string inputRate);

    /// <summary>
    ///     内建变量。
    /// </summary>
    [Operator("builtin_variable")]
    Term<T> BuiltinVariable(string name);

    /// <summary>
    ///     几何输入拓扑。
    /// </summary>
    [Operator("geometry_input")]
    Term<T> GeometryInput(string topology);

    /// <summary>
    ///     几何输出拓扑。
    /// </summary>
    [Operator("geometry_output")]
    Term<T> GeometryOutput(string topology, int maxVertices);

    /// <summary>
    ///     Uniform Buffer 声明。
    /// </summary>
    [Operator("uniform_buffer_decl")]
    Term<T> UniformBufferDecl(int set, int binding, string name, string[] memberNames, string[] memberTypes);

    /// <summary>
    ///     Uniform Buffer 读取。
    /// </summary>
    [Operator("uniform_buffer_load")]
    Term<T> UniformBufferLoad(Term<T> buffer, string memberName, int memberIndex);

    /// <summary>
    ///     Push Constant 声明。
    /// </summary>
    [Operator("push_constant_decl")]
    Term<T> PushConstantDecl(string name, string[] memberNames, string[] memberTypes);

    /// <summary>
    ///     Push Constant 读取。
    /// </summary>
    [Operator("push_constant_load")]
    Term<T> PushConstantLoad(Term<T> buffer, string memberName, int memberIndex);

    /// <summary>
    ///     组合图像采样器声明。
    /// </summary>
    [Operator("combined_image_sampler")]
    Term<T> CombinedImageSampler(int set, int binding, string name);

    /// <summary>
    ///     采样器声明。
    /// </summary>
    [Operator("sampler_decl")]
    Term<T> SamplerDecl(int set, int binding, string name);

    /// <summary>
    ///     采样图像声明。
    /// </summary>
    [Operator("sampled_image_decl")]
    Term<T> SampledImageDecl(int set, int binding, string name, string dim);

    /// <summary>
    ///     存储图像声明。
    /// </summary>
    [Operator("storage_image_decl")]
    Term<T> StorageImageDecl(int set, int binding, string name, string dim, string format);

    /// <summary>
    ///     Storage Buffer 声明。
    /// </summary>
    [Operator("storage_buffer_decl")]
    Term<T> StorageBufferDecl(int set, int binding, string name, string[] memberNames, string[] memberTypes);

    /// <summary>
    ///     输入附件声明。
    /// </summary>
    [Operator("input_attachment_decl")]
    Term<T> InputAttachmentDecl(int set, int binding, string name, int inputAttachmentIndex);

    /// <summary>
    ///     原子加。
    /// </summary>
    [Operator("atomic_add")]
    Term<T> AtomicAdd(Term<T> pointer, Term<T> value);

    /// <summary>
    ///     原子交换。
    /// </summary>
    [Operator("atomic_exchange")]
    Term<T> AtomicExchange(Term<T> pointer, Term<T> value);

    /// <summary>
    ///     原子比较交换。
    /// </summary>
    [Operator("atomic_compare_exchange")]
    Term<T> AtomicCompareExchange(Term<T> pointer, Term<T> expected, Term<T> desired);

    /// <summary>
    ///     点积。
    /// </summary>
    [Operator("dot")]
    Term<T> Dot(Term<T> left, Term<T> right);

    /// <summary>
    ///     叉积。
    /// </summary>
    [Operator("cross")]
    Term<T> Cross(Term<T> left, Term<T> right);

    /// <summary>
    ///     向量长度。
    /// </summary>
    [Operator("length")]
    Term<T> Length(Term<T> vector);

    /// <summary>
    ///     归一化。
    /// </summary>
    [Operator("normalize")]
    Term<T> Normalize(Term<T> vector);

    /// <summary>
    ///     反射向量。
    /// </summary>
    [Operator("reflect")]
    Term<T> Reflect(Term<T> incident, Term<T> normal);

    /// <summary>
    ///     折射向量。
    /// </summary>
    [Operator("refract")]
    Term<T> Refract(Term<T> incident, Term<T> normal, Term<T> eta);
}