namespace Valkyrie.Asgard.DevServer;

public sealed class CacheStats
{
    public int entries { get; set; }
    public long hits { get; set; }
    public long misses { get; set; }
    public long evictions { get; set; }
}
