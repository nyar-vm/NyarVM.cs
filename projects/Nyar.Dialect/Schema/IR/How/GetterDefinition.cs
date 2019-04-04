using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.How;

public sealed class GetterDefinition
{
    public GetterDefinition(string name, SchemaType? returnType, string body, string? docComment = null,
        int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        return_type = returnType;
        this.body = body;
        doc_comment = docComment;
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public SchemaType? return_type { get; }
    public string body { get; }
    public string? doc_comment { get; }
    public int source_line { get; }
    public int source_column { get; }
}