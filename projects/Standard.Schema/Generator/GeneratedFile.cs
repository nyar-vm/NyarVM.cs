namespace Hermes.Generator;

public enum FileChangeKind
{
    Unchanged,
    Added,
    Modified,
    Deleted
}

public sealed class GeneratedFile
{
    public string Path { get; set; } = "";
    public string Content { get; set; } = "";
    public string Generator { get; set; } = "";
    public FileChangeKind ChangeKind { get; set; } = FileChangeKind.Unchanged;
    public string TypeName { get; set; } = "";
}