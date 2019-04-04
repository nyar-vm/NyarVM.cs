namespace Nyar.Types.Targets;

/// <summary>
///     `CanonicalTriple` 到运行时策略配置之间的中间层，承载目标平台的策略配置。
///     层次关系：`CanonicalTriple → TargetProfile → EntryPolicy / ArtifactPolicy`
/// </summary>
public sealed record TargetProfile
{
    /// <summary>
    ///     标准三元组
    /// </summary>
    public string canonical_triple { get; init; } = string.Empty;

    /// <summary>
    ///     编译目标模式（Dev = HMR 优先 per-file / Prod = 优化合并）
    /// </summary>
    public TargetMode target_mode { get; init; } = TargetMode.prod;

    /// <summary>
    ///     后端家族
    /// </summary>
    public TargetBackendFamily backend_family { get; init; }

    /// <summary>
    ///     宿主类型
    /// </summary>
    public TargetHostKind host_kind { get; init; }

    /// <summary>
    ///     宿主风味标识。
    ///     这是开放字符串，而不是枚举，用来承接小游戏宿主、编辑器扩展宿主这类
    ///     “属于某个宿主家族，但 API 面与打包约束并不标准”的平台。
    /// </summary>
    public string host_flavor { get; init; } = "default";

    /// <summary>
    ///     目标 ABI。
    ///     直接保留真实 ABI，避免再引入一层只用于分组的近义枚举。
    /// </summary>
    public TargetAbi abi { get; init; }

    /// <summary>
    ///     编译与运行阶段需要的宿主能力标签。
    ///     例如 dom / canvas / node-fs / miniapp-file / game-lifecycle / extension-api。
    /// </summary>
    public string[] capability_tags { get; init; } = [];

    /// <summary>
    ///     入口策略
    /// </summary>
    public EntryPolicy entry_policy { get; init; } = new();

    /// <summary>
    ///     产物策略
    /// </summary>
    public ArtifactPolicy artifact_policy { get; init; } = new();
}