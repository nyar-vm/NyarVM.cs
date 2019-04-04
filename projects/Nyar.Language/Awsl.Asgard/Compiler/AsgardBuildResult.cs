namespace Nyar.Language.Awsl.Asgard.Compiler;

public sealed class AsgardBuildResult
{
    public bool success { get; set; }
    public string error { get; set; } = string.Empty;
    public string output_directory { get; set; } = string.Empty;
    public List<string> output_files { get; set; } = [];
}
