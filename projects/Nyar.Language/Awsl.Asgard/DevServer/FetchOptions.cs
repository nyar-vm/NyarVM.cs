namespace Valkyrie.Asgard.DevServer;

public sealed class FetchOptions
{
    public string? method { get; set; }
    public Dictionary<string, string>? headers { get; set; }
    public string? body { get; set; }
    public string? cache { get; set; }
    public int next_revalidate { get; set; }
    public List<string>? next_tags { get; set; }
}
