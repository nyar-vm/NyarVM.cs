namespace Nyar.PackageManager.Version;

public class VersionRange
{
    private VersionRange(string raw, SemanticVersion? minVersion, bool minInclusive, SemanticVersion? maxVersion,
        bool maxInclusive)
    {
        this.raw = raw;
        min_version = minVersion;
        min_inclusive = minInclusive;
        max_version = maxVersion;
        max_inclusive = maxInclusive;
    }

    public string raw { get; }
    public SemanticVersion? min_version { get; }
    public bool min_inclusive { get; }
    public SemanticVersion? max_version { get; }
    public bool max_inclusive { get; }

    public static VersionRange parse(string range)
    {
        range = range.Trim();

        if (range is "*" or "latest") return new VersionRange(range, null, true, null, true);

        // 精确版本：1.2.3
        if (!range.StartsWith("^") && !range.StartsWith("~") && !range.StartsWith(">") && !range.StartsWith("<") &&
            !range.StartsWith("="))
        {
            var version = SemanticVersion.parse(range);
            return new VersionRange(range, version, true, version, true);
        }

        // 兼容版本：^1.2.3
        if (range.StartsWith("^"))
        {
            var version = SemanticVersion.parse(range[1..]);
            var maxVersion = new SemanticVersion(version.major + 1, 0, 0);
            return new VersionRange(range, version, true, maxVersion, false);
        }

        // 近似版本：~1.2.3
        if (range.StartsWith("~"))
        {
            var version = SemanticVersion.parse(range[1..]);
            var maxVersion = new SemanticVersion(version.major, version.minor + 1, 0);
            return new VersionRange(range, version, true, maxVersion, false);
        }

        // 大于等于：>=1.2.3
        if (range.StartsWith(">="))
        {
            var version = SemanticVersion.parse(range[2..]);
            return new VersionRange(range, version, true, null, true);
        }

        // 大于：>1.2.3
        if (range.StartsWith(">"))
        {
            var version = SemanticVersion.parse(range[1..]);
            return new VersionRange(range, version, false, null, true);
        }

        // 小于等于：<=1.2.3
        if (range.StartsWith("<="))
        {
            var version = SemanticVersion.parse(range[2..]);
            return new VersionRange(range, null, true, version, true);
        }

        // 小于：<1.2.3
        if (range.StartsWith("<"))
        {
            var version = SemanticVersion.parse(range[1..]);
            return new VersionRange(range, null, true, version, false);
        }

        throw new FormatException($"无法解析版本范围: {range}");
    }

    public bool satisfies(SemanticVersion version)
    {
        if (min_version is not null)
        {
            var cmp = version.CompareTo(min_version);
            if (min_inclusive && cmp < 0) return false;

            if (!min_inclusive && cmp <= 0) return false;
        }

        if (max_version is not null)
        {
            var cmp = version.CompareTo(max_version);
            if (max_inclusive && cmp > 0) return false;

            if (!max_inclusive && cmp >= 0) return false;
        }

        return true;
    }

    public override string ToString()
    {
        return raw;
    }
}