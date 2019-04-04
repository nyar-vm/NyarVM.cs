namespace Nyar.Analyzer.Workspace;

public class Project
{
    private readonly List<string> _dependencies;
    private readonly Dictionary<string, string> _metadata;

    private readonly List<string> _source_files;

    public Project(string name, string rootPath)
    {
        this.name = name;
        root_path = rootPath;
        _source_files = [];
        _dependencies = [];
        _metadata = new Dictionary<string, string>();
    }

    public string name { get; }
    public string root_path { get; }
    public IReadOnlyList<string> source_files => _source_files;
    public IReadOnlyList<string> dependencies => _dependencies;

    public void add_source_file(string filePath)
    {
        if (!_source_files.Contains(filePath)) _source_files.Add(filePath);
    }

    public void remove_source_file(string filePath)
    {
        _source_files.Remove(filePath);
    }

    public void add_dependency(string projectPath)
    {
        if (!_dependencies.Contains(projectPath)) _dependencies.Add(projectPath);
    }

    public void remove_dependency(string projectPath)
    {
        _dependencies.Remove(projectPath);
    }

    public void set_metadata(string key, string value)
    {
        _metadata[key] = value;
    }

    public string? get_metadata(string key)
    {
        return _metadata.GetValueOrDefault(key);
    }

    public override string ToString()
    {
        return $"{name} ({_source_files.Count} files)";
    }
}