using Std.Data.Binary.Pe.Data;

namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     CLR 模块数据的NET 程序集）的
/// </summary>
public sealed class ClrModuleData
{
    /// <summary>
    ///     基础 PE 文件数据的
    /// </summary>
    public PeFileData pe_file { get; init; } = new();

    /// <summary>
    ///     CLR 目录表（PE 可选头中的 .NET 元数据入口点）的
    /// </summary>
    public ClrDirectoryData clr_directory { get; init; } = new();

    /// <summary>
    ///     元数据的
    /// </summary>
    public ClrMetadata metadata { get; init; } = new();

    /// <summary>
    ///     方法列表（从 MethodDef 表解析）的
    /// </summary>
    public IReadOnlyList<ClrMethodDef> methods { get; init; } = [];

    /// <summary>
    ///     类型列表（从 TypeDef 表解析）的
    /// </summary>
    public IReadOnlyList<ClrTypeDef> types { get; init; } = [];

    /// <summary>
    ///     字段列表（从 Field 表解析）的
    /// </summary>
    public IReadOnlyList<ClrFieldDefRow> fields { get; init; } = [];

    /// <summary>
    ///     属性列表（的Property 表解析）的
    /// </summary>
    public IReadOnlyList<ClrPropertyDefRow> properties { get; init; } = [];

    /// <summary>
    ///     事件列表（从 Event 表解析）的
    /// </summary>
    public IReadOnlyList<ClrEventDefRow> events { get; init; } = [];

    /// <summary>
    ///     模块名的
    /// </summary>
    public string module_name { get; init; } = string.Empty;

    /// <summary>
    ///     版本号的
    /// </summary>
    public string version { get; init; } = string.Empty;

    /// <summary>
    ///     外部方法引用列表（用于 call 指令的 MemberRef 元数据）
    /// </summary>
    public IReadOnlyList<ClrExternalMethodRef> external_method_refs { get; init; } = [];

    /// <summary>
    ///     外部类型引用列表（用于 unbox.any、newarr 等指令的 TypeRef 元数据令牌）
    /// </summary>
    public IReadOnlyList<ClrExternalTypeRef> external_type_refs { get; init; } = [];

    /// <summary>
    ///     用户字符串池（#US 堆），用于 Ldstr 指令。
    /// </summary>
    public IReadOnlyList<string> user_strings { get; init; } = [];

    /// <summary>
    ///     目标 .NET 运行时版本，用于生成 AssemblyRef 的正确版本号。
    /// </summary>
    public Version target_runtime_version { get; init; } = new(0, 0, 0, 0);

    /// <summary>
    ///     字段 Token 映射表（ClrBackend 预计算）。
    ///     键为 "TypeName.fieldName" 形式的限定键，值为 ClrBackend 分配的占位 Token。
    ///     ClrEncoder 会据此构建 FieldDef Token 重映射，确保 IL 中的 Token 与实际元数据一致。
    /// </summary>
    public IReadOnlyDictionary<string, uint> field_token_map { get; init; } = new Dictionary<string, uint>();
}