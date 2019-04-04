using System.Text.RegularExpressions;

namespace Nyar.PackageManager.Version;

public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    public SemanticVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)
    {
        if (major < 0 || minor < 0 || patch < 0) throw new ArgumentException("版本号各部分不能为负数");

        this.major = major;
        this.minor = minor;
        this.patch = patch;
        pre_release = preRelease;
        build_metadata = buildMetadata;
    }

    public int major { get; }
    public int minor { get; }
    public int patch { get; }
    public string? pre_release { get; }
    public string? build_metadata { get; }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;

        if (major != other.major) return major.CompareTo(other.major);

        if (minor != other.minor) return minor.CompareTo(other.minor);

        if (patch != other.patch) return patch.CompareTo(other.patch);

        if (pre_release is null && other.pre_release is null) return 0;

        if (pre_release is not null && other.pre_release is null) return -1;

        if (pre_release is null && other.pre_release is not null) return 1;

        return compare_pre_release(pre_release ?? string.Empty, other.pre_release ?? string.Empty);
    }

    public bool Equals(SemanticVersion? other)
    {
        if (other is null) return false;

        return major == other.major &&
               minor == other.minor &&
               patch == other.patch &&
               pre_release == other.pre_release;
    }

    public static SemanticVersion parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("版本字符串不能为空");

        version = version.TrimStart('v', 'V');

        var match = Regex.Match(version, @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9.]+))?(?:\+([a-zA-Z0-9.]+))?$");
        if (!match.Success) throw new FormatException($"无效的语义化版本格式: {version}");

        var major = int.Parse(match.Groups[1].Value);
        var minor = int.Parse(match.Groups[2].Value);
        var patch = int.Parse(match.Groups[3].Value);
        var preRelease = match.Groups[4].Success ? match.Groups[4].Value : null;
        var buildMetadata = match.Groups[5].Success ? match.Groups[5].Value : null;

        return new SemanticVersion(major, minor, patch, preRelease, buildMetadata);
    }

    public static bool try_parse(string version, out SemanticVersion? result)
    {
        try
        {
            result = parse(version);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public bool satisfies(VersionRange range)
    {
        return range.satisfies(this);
    }

    /// <summary>
    ///     按 SemVer 2.0 规范第 11 条比较预发布标识符：
    ///     逐段比较，纯数字段按数值比较（1-11 > 1-2），纯字母或混合段按字符串比较
    /// </summary>
    private static int compare_pre_release(string a, string b)
    {
        var aParts = a.Split('.');
        var bParts = b.Split('.');
        var minLen = Math.Min(aParts.Length, bParts.Length);

        for (var i = 0; i < minLen; i++)
        {
            var aIsNum = int.TryParse(aParts[i], out var aNum);
            var bIsNum = int.TryParse(bParts[i], out var bNum);

            if (aIsNum && bIsNum)
            {
                if (aNum != bNum) return aNum.CompareTo(bNum);
            }
            else
            {
                var cmp = string.Compare(aParts[i], bParts[i], StringComparison.Ordinal);
                if (cmp != 0) return cmp;
            }
        }

        return aParts.Length.CompareTo(bParts.Length);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SemanticVersion);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(major, minor, patch, pre_release);
    }

    public override string ToString()
    {
        var version = $"{major}.{minor}.{patch}";
        if (pre_release is not null) version += $"-{pre_release}";

        if (build_metadata is not null) version += $"+{build_metadata}";

        return version;
    }

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right)
    {
        if (left is null) return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right)
    {
        return !(left == right);
    }

    public static bool operator <(SemanticVersion left, SemanticVersion right)
    {
        if (left is null) return right is not null;

        return left.CompareTo(right) < 0;
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right)
    {
        if (left is null) return false;

        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(SemanticVersion left, SemanticVersion right)
    {
        if (left is null) return true;

        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(SemanticVersion left, SemanticVersion right)
    {
        if (left is null) return right is null;

        return left.CompareTo(right) >= 0;
    }
}