namespace Valkyrie.Asgard.DevServer;

public sealed class FetchResponse
{
    public bool ok { get; set; }
    public int status { get; set; }
    public string status_text { get; set; } = "";
    public Dictionary<string, string> headers { get; set; } = new();
    public string body { get; set; } = "";
    public bool cached { get; set; }
    public long timestamp { get; set; }
}
