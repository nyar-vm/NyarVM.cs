namespace Nyar.Types.Targets;

/// <summary>
///     `CanonicalTriple` 到目标配置的注册表。
///     内部委托 `CanonicalTarget.to_profile()` 从枚举派生，不再维护硬编码映射表。
/// </summary>
public sealed class CanonicalTargetRegistry
{
    /// <summary>
    ///     根据完整四段式 CanonicalTriple 解析目标配置。
    /// </summary>
    /// <param name="canonicalTriple">完整四段式 `arch-impl-spec-abi`，支持短名别名</param>
    /// <returns>对应的目标策略配置</returns>
    /// <exception cref="NotSupportedException">目标三元组不在注册表中</exception>
    public TargetProfile resolve(string canonicalTriple)
    {
        return resolve(canonicalTriple, null);
    }

    /// <summary>
    ///     根据完整四段式 CanonicalTriple 解析目标配置，允许覆盖编译目标模式。
    /// </summary>
    /// <param name="canonicalTriple">完整四段式 `arch-impl-spec-abi`，支持短名别名</param>
    /// <param name="targetMode">可选的目标模式覆盖，为 null 时使用默认 Prod</param>
    /// <returns>对应的目标策略配置</returns>
    /// <exception cref="NotSupportedException">目标三元组不在注册表中</exception>
    public TargetProfile resolve(string canonicalTriple, TargetMode? targetMode)
    {
        if (!CanonicalTarget.try_parse(canonicalTriple, out var ct))
            throw new NotSupportedException($"暂不支持的 CanonicalTriple：{canonicalTriple}");

        var profile = ct.to_profile(targetMode);

        if (canonicalTriple.StartsWith("gnosis-", StringComparison.OrdinalIgnoreCase))
            profile = profile with
            {
                backend_family = TargetBackendFamily.gnosis_vm,
                host_kind = TargetHostKind.gnosis_vm,
                artifact_policy = profile.artifact_policy with
                {
                    primary_extension = ".gnosis",
                    default_publish_format = "bundle",
                    supported_publish_formats = ["bundle"],
                    required_adaptors = ["std:gnosisvm"]
                }
            };

        return profile;
    }
}