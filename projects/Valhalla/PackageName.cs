using System.Text.RegularExpressions;

namespace Valhalla;

/// <summary>
///     瓦尓哈拉包名，支持标准化算法
/// </summary>
public readonly struct PackageName : IEquatable<PackageName>
{
    private static readonly Regex _canonical_pattern = new(
        @"^[a-z0-9]+(\.[a-z0-9]+)*$",
        RegexOptions.Compiled);

    /// <summary>
    ///     规范形式的包名
    /// </summary>
    public string canonical { get; }

    /// <summary>
    ///     根组织前缀（用第一个 . 分割），若无组织返回完整包名
    /// </summary>
    public string root => canonical.IndexOf('.') is int idx and >= 0
        ? canonical[..idx]
        : canonical;

    /// <summary>
    ///     将任意形式的名字标准化
    /// </summary>
    /// <param name="raw">用户输入的原始名字</param>
    public PackageName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException("包名不能为空", nameof(raw));

        var lower = raw.ToLowerInvariant();
        var replaced = Regex.Replace(lower, @"[_\-\s]+", ".");
        var collapsed = Regex.Replace(replaced, @"\.{2,}", ".");
        var trimmed = collapsed.Trim('.');

        if (string.IsNullOrEmpty(trimmed)) throw new ArgumentException($"包名 '{raw}' 标准化后为空", nameof(raw));

        if (!_canonical_pattern.IsMatch(trimmed))
            throw new ArgumentException(
                $"包名 '{raw}' 标准化为 '{trimmed}' 后包含非法字符", nameof(raw));

        canonical = trimmed;
    }

    /// <summary>
    ///     判断此包名是否属于指定根组织
    /// </summary>
    public bool belongs_to_org(string orgCanonical)
    {
        return canonical == orgCanonical
               || canonical.StartsWith(orgCanonical + ".", StringComparison.Ordinal);
    }

    /// <summary>
    ///     获取指定深度的前缀部分
    /// </summary>
    /// <param name="depth">深度（1=仅 root，2=root.first，以此类推）</param>
    public string get_prefix(int depth)
    {
        var parts = canonical.Split('.');

        if (depth >= parts.Length) return canonical;

        return string.Join(".", parts, 0, depth);
    }

    public bool Equals(PackageName other)
    {
        return canonical == other.canonical;
    }

    public override bool Equals(object? obj)
    {
        return obj is PackageName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return canonical.GetHashCode();
    }

    public override string ToString()
    {
        return canonical;
    }

    public static bool operator ==(PackageName left, PackageName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PackageName left, PackageName right)
    {
        return !left.Equals(right);
    }
}