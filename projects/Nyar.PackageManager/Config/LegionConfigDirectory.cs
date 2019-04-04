namespace Nyar.PackageManager.Config;

public class LegionConfigDirectory
{
    public LegionConfigDirectory(string projectDirectory)
    {
        config_directory = Path.Combine(projectDirectory, ".config", "legion");
        legion_config_path = Path.Combine(config_directory, "legion.von");
        valkyrie_config_path = Path.Combine(config_directory, "valkyrie.von");

        ensure_exists();
    }

    public string config_directory { get; }

    public string legion_config_path { get; }

    public string valkyrie_config_path { get; }

    public void ensure_exists()
    {
        if (!Directory.Exists(config_directory)) Directory.CreateDirectory(config_directory);
    }

    public bool has_legion_config()
    {
        return File.Exists(legion_config_path);
    }

    public bool has_valkyrie_config()
    {
        return File.Exists(valkyrie_config_path);
    }

    public string? read_legion_config()
    {
        return has_legion_config() ? File.ReadAllText(legion_config_path) : null;
    }

    public string? read_valkyrie_config()
    {
        return has_valkyrie_config() ? File.ReadAllText(valkyrie_config_path) : null;
    }

    public void write_legion_config(string content)
    {
        ensure_exists();
        File.WriteAllText(legion_config_path, content);
    }

    public void write_valkyrie_config(string content)
    {
        ensure_exists();
        File.WriteAllText(valkyrie_config_path, content);
    }
}