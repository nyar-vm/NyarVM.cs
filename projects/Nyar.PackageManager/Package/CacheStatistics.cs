namespace Nyar.PackageManager.Package;

/// <summary>
///     缂撳瓨缁熻淇℃伅
/// </summary>
public class CacheStatistics
{
    /// <summary>
    ///     缂撳瓨涓殑鍖呮€绘暟
    /// </summary>
    public int total_packages { get; set; }

    /// <summary>
    ///     缂撳瓨鍗犵敤鐨勬€诲瓧鑺傛暟
    /// </summary>
    public long total_size_bytes { get; set; }

    /// <summary>
    ///     缂撳瓨鍛戒腑娆℃暟
    /// </summary>
    public long hits { get; set; }

    /// <summary>
    ///     缂撳瓨鏈懡涓鏁?    ///
    /// </summary>
    public long misses { get; set; }

    /// <summary>
    ///     缂撳瓨鍛戒腑鐜?    ///
    /// </summary>
    public double hit_rate { get; set; }

    /// <summary>
    ///     閫氳繃纭摼鎺?绗﹀彿閾炬帴鑺傜渷鐨勫瓧鑺傛暟
    /// </summary>
    public long bytes_saved_by_links { get; set; }

    /// <summary>
    ///     鏍煎紡鍖栨€诲ぇ灏?    ///
    /// </summary>
    public string total_size_formatted => format_bytes(total_size_bytes);

    /// <summary>
    ///     鏍煎紡鍖栬妭鐪佸瓧鑺傛暟
    /// </summary>
    public string bytes_saved_formatted => format_bytes(bytes_saved_by_links);

    private static string format_bytes(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
            >= 1_024 => $"{bytes / 1_024.0:F2} KB",
            _ => $"{bytes} B"
        };
    }
}