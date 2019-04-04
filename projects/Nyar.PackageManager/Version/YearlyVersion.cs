using System.Text.RegularExpressions;

namespace Nyar.PackageManager.Version;

public class YearlyVersion : IComparable<YearlyVersion>, IEquatable<YearlyVersion>
{
    public YearlyVersion(int yearly, int major, int minor, int patch, string? buildInfo = null)
    {
        if (yearly < 0 || major < 0 || minor < 0 || patch < 0) throw new ArgumentException("版本号各部分不能为负数");

        this.yearly = yearly;
        this.major = major;
        this.minor = minor;
        this.patch = patch;
        build_info = buildInfo;
    }

    public int yearly { get; }
    public int major { get; }
    public int minor { get; }
    public int patch { get; }
    public string? build_info { get; }

    public bool is_research => yearly == 0;
    public bool is_beta => yearly > 0 && major == 0;
    public bool is_stable => yearly > 0 && major > 0;

    public int CompareTo(YearlyVersion? other)
    {
        if (other is null) return 1;

        if (yearly != other.yearly) return yearly.CompareTo(other.yearly);

        if (major != other.major) return major.CompareTo(other.major);

        if (minor != other.minor) return minor.CompareTo(other.minor);

        if (patch != other.patch) return patch.CompareTo(other.patch);

        // BuildInfo 参与比较，按字符串排序
        return string.Compare(build_info, other.build_info, StringComparison.Ordinal);
    }

    public bool Equals(YearlyVersion? other)
    {
        if (other is null) return false;

        return yearly == other.yearly &&
               major == other.major &&
               minor == other.minor &&
               patch == other.patch &&
               build_info == other.build_info;
    }

    public static YearlyVersion parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("版本字符串不能为空");

        // 格式: yearly.major.minor.patch-build_info
        var match = Regex.Match(version.Trim(), @"^(\d+)\.(\d+)\.(\d+)\.(\d+)(?:-(.+))?$");
        if (!match.Success)
            throw new FormatException(
                $"无效的 YearlyVersion 格式: {version}，期望格式: yearly.major.minor.patch 或 yearly.major.minor.patch-build_info");

        var yearly = int.Parse(match.Groups[1].Value);
        var major = int.Parse(match.Groups[2].Value);
        var minor = int.Parse(match.Groups[3].Value);
        var patch = int.Parse(match.Groups[4].Value);
        var buildInfo = match.Groups[5].Success ? match.Groups[5].Value : null;

        return new YearlyVersion(yearly, major, minor, patch, buildInfo);
    }

    public static bool try_parse(string version, out YearlyVersion? result)
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

    public bool satisfies(YearlyVersionRange range)
    {
        return range.satisfies(this);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as YearlyVersion);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(yearly, major, minor, patch, build_info);
    }

    public override string ToString()
    {
        var version = $"{yearly}.{major}.{minor}.{patch}";
        if (build_info is not null) version += $"-{build_info}";

        return version;
    }

    public static bool operator ==(YearlyVersion? left, YearlyVersion? right)
    {
        if (left is null) return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(YearlyVersion? left, YearlyVersion? right)
    {
        return !(left == right);
    }

    public static bool operator <(YearlyVersion left, YearlyVersion right)
    {
        if (left is null) return right is not null;

        return left.CompareTo(right) < 0;
    }

    public static bool operator >(YearlyVersion left, YearlyVersion right)
    {
        if (left is null) return false;

        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(YearlyVersion left, YearlyVersion right)
    {
        if (left is null) return true;

        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(YearlyVersion left, YearlyVersion right)
    {
        if (left is null) return right is null;

        return left.CompareTo(right) >= 0;
    }
}