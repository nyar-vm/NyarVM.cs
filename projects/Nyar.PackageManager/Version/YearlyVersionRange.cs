namespace Nyar.PackageManager.Version;

public class YearlyVersionRange
{
    private readonly bool _is_inclusive_or_higher;
    private readonly int? _major;
    private readonly int? _minor;
    private readonly int? _patch;

    private readonly int? _yearly;

    private YearlyVersionRange(string raw, int? yearly, int? major, int? minor, int? patch,
        bool isInclusiveOrHigher = false)
    {
        this.raw = raw;
        _yearly = yearly;
        _major = major;
        _minor = minor;
        _patch = patch;
        _is_inclusive_or_higher = isInclusiveOrHigher;
    }

    public string raw { get; }

    public static YearlyVersionRange parse(string range)
    {
        range = range.Trim();

        if (range == "*") return new YearlyVersionRange(range, null, null, null, null);

        // 检测 + 后缀，表示包括该版本及更高
        var isInclusiveOrHigher = range.EndsWith("+");
        if (isInclusiveOrHigher) range = range[..^1];

        var parts = range.Split('.');
        if (parts.Length is < 1 or > 4)
            throw new FormatException(
                $"无效的 YearlyVersion 范围格式: {range}，期望格式: yearly 或 yearly.major 或 yearly.major.minor 或 yearly.major.minor.patch");

        var yearly = parse_part(parts[0]);

        // 如果 yearly 是 *，后面都不看
        if (!yearly.HasValue) return new YearlyVersionRange(range, null, null, null, null);

        var major = parts.Length > 1 ? parse_part(parts[1]) : null;

        // 如果 major 是 *，后面都不看
        if (parts.Length > 1 && !major.HasValue) return new YearlyVersionRange(range, yearly, null, null, null);

        var minor = parts.Length > 2 ? parse_part(parts[2]) : null;

        // 如果 minor 是 *，后面都不看
        if (parts.Length > 2 && !minor.HasValue) return new YearlyVersionRange(range, yearly, major, null, null);

        var patch = parts.Length > 3 ? parse_part(parts[3]) : null;

        return new YearlyVersionRange(range, yearly, major, minor, patch, isInclusiveOrHigher);
    }

    public bool satisfies(YearlyVersion version)
    {
        // 如果是 + 模式，表示最低版本要求（包括该版本及更高）
        if (_is_inclusive_or_higher) return is_version_at_least(version);

        if (_yearly.HasValue && version.yearly != _yearly.Value) return false;

        if (_major.HasValue && version.major != _major.Value) return false;

        if (_minor.HasValue && version.minor != _minor.Value) return false;

        if (_patch.HasValue && version.patch != _patch.Value) return false;

        return true;
    }

    private bool is_version_at_least(YearlyVersion version)
    {
        if (_yearly.HasValue && version.yearly < _yearly.Value) return false;

        if (_yearly.HasValue && version.yearly > _yearly.Value) return true;

        if (_major.HasValue && version.major < _major.Value) return false;

        if (_major.HasValue && version.major > _major.Value) return true;

        if (_minor.HasValue && version.minor < _minor.Value) return false;

        if (_minor.HasValue && version.minor > _minor.Value) return true;

        if (_patch.HasValue && version.patch < _patch.Value) return false;

        if (_patch.HasValue && version.patch > _patch.Value) return true;

        return true;
    }

    public override string ToString()
    {
        return raw;
    }

    private static int? parse_part(string part)
    {
        if (part == "*") return null;

        if (int.TryParse(part, out var value)) return value;

        throw new FormatException($"无效的范围部分: {part}，期望数字或 *");
    }
}