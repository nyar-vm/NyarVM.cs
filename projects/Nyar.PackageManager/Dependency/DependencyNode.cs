namespace Nyar.PackageManager.Dependency;

public class DependencyNode
{
    public string package_name { get; set; } = string.Empty;
    public string version { get; set; } = string.Empty;
    public string registry_name { get; set; } = "npm";
    public List<DependencyNode> dependencies { get; set; } = [];
    public string? target_condition { get; set; }
    public bool is_sdk { get; set; }
    public string? sdk_module_name { get; set; }
}