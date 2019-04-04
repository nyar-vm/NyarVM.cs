using Std.Data.Text.Syntax;

namespace Nyar.Analyzer.Workspace;

public class VirtualFile
{
    public VirtualFile(string filePath, string languageId, ISource source)
    {
        file_path = filePath;
        language_id = languageId;
        this.source = source;
        version = 0;
        last_modified = DateTime.UtcNow;
    }

    public string file_path { get; }
    public string language_id { get; }
    public ISource source { get; private set; }
    public int version { get; private set; }
    public DateTime last_modified { get; private set; }

    public void update_source(ISource newSource)
    {
        source = newSource;
        version++;
        last_modified = DateTime.UtcNow;
    }

    public void apply_edit(Edit edit)
    {
        var oldText = source.ToString();
        var newText = oldText.Remove(edit.old_span.start, edit.old_span.length)
            .Insert(edit.old_span.start, edit.new_text);
        source = new StringSource(newText);
        version++;
        last_modified = DateTime.UtcNow;
    }

    public override string ToString()
    {
        return $"{file_path} (v{version})";
    }
}