namespace Nyar.PackageManager.Scripts;

public class ScriptResult
{
    public bool success { get; set; }
    public int exit_code { get; set; }
    public string output { get; set; } = string.Empty;
    public string error { get; set; } = string.Empty;
    public TimeSpan duration { get; set; }
}