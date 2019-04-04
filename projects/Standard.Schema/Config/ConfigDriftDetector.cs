namespace Hermes.Config;

/// <summary>
///     Config Drift 检测器 — 检测线上实际值是否偏离了 schema 默认值
/// </summary>
public sealed class ConfigDriftDetector
{
    /// <summary>
    ///     检测 config class 的 drift
    /// </summary>
    /// <param name="trait">配置类特征</param>
    /// <param name="resolvedValues">从 provider 链解析后的字段值</param>
    /// <param name="overrideInfo">每个字段的覆盖来源信息（字段名 → source名）</param>
    /// <returns>Drift 条目列表</returns>
    public IReadOnlyList<DriftEntry> Detect(
        ConfigClassTrait trait,
        IReadOnlyDictionary<string, string?> resolvedValues,
        IReadOnlyDictionary<string, string>? overrideInfo = null)
    {
        var entries = new List<DriftEntry>();

        foreach (var field in trait.fields)
        {
            if (field.IsSuperSecret)
            {
                entries.Add(new DriftEntry(
                    $"{trait.Namespace}.{field.FieldName}",
                    null,
                    null,
                    false,
                    string.Empty,
                    true));
                continue;
            }

            var defaultValue = field.DefaultValue?.ToString();
            var hasValue = resolvedValues.TryGetValue(field.FieldName, out var actualValue);
            var hasDrifted = hasValue && !string.Equals(defaultValue, actualValue, StringComparison.Ordinal);

            string? overrideSource = null;
            overrideInfo?.TryGetValue(field.FieldName, out overrideSource);

            entries.Add(new DriftEntry(
                $"{trait.Namespace}.{field.FieldName}",
                field.DefaultValue,
                actualValue,
                hasDrifted,
                overrideSource ?? string.Empty,
                false));
        }

        return entries;
    }

    /// <summary>
    ///     单个字段的 Drift 信息
    /// </summary>
    public sealed class DriftEntry
    {
        public DriftEntry(
            string path,
            object? defaultValue,
            string? actualValue,
            bool hasDrifted,
            string overrideSource,
            bool isSkipped)
        {
            Path = path;
            DefaultValue = defaultValue;
            ActualValue = actualValue;
            HasDrifted = hasDrifted;
            OverrideSource = overrideSource;
            IsSkipped = isSkipped;
        }

        /// <summary>
        ///     字段路径（"class.field" 格式）
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Schema 中声明的默认值
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        ///     当前线上实际值
        /// </summary>
        public string? ActualValue { get; }

        /// <summary>
        ///     是否偏移
        /// </summary>
        public bool HasDrifted { get; }

        /// <summary>
        ///     覆盖来源（如 "consul:kv"、"env"、"json"），空字符串表示无覆盖
        /// </summary>
        public string OverrideSource { get; }

        /// <summary>
        ///     是否为超级机密字段（跳过检测）
        /// </summary>
        public bool IsSkipped { get; }
    }
}