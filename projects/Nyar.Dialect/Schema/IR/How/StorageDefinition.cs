using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.How;

public sealed class StorageDefinition
{
    public StorageDefinition(string name, SchemaType entityType, IReadOnlyList<ModelDefinition> models,
        IReadOnlyList<StreamDefinition> streams, IReadOnlyList<CacheDefinition> caches,
        IReadOnlyList<AttributeDefinition>? attributes = null, StorageKind kind = StorageKind.database,
        int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        entity_type = entityType;
        this.kind = kind;
        this.models = models;
        this.streams = streams;
        this.caches = caches;
        this.attributes = attributes ?? [];
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public SchemaType entity_type { get; }
    public StorageKind kind { get; }
    public IReadOnlyList<ModelDefinition> models { get; }
    public IReadOnlyList<StreamDefinition> streams { get; }
    public IReadOnlyList<CacheDefinition> caches { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public int source_line { get; }
    public int source_column { get; }

    public static StorageKind infer_kind_from_attributes(IReadOnlyList<AttributeDefinition> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.name is "cache" or "Cache") return StorageKind.cache;

            if (attr.name is "storage" or "Storage") return StorageKind.storage;

            if (attr.name is "stream" or "Stream" or "message_queue" or "MessageQueue") return StorageKind.stream;

            if (attr.name is "database" or "Database") return StorageKind.database;
        }

        return StorageKind.database;
    }
}