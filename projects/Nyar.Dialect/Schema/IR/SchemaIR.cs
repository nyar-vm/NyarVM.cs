using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.How;
using Nyar.Dialect.Schema.IR.Micro;
using Nyar.Dialect.Schema.IR.What;
using Nyar.Dialect.Schema.IR.Where;

namespace Nyar.Dialect.Schema.IR;

public sealed class SchemaIr
{
    public SchemaIr(
        string @namespace,
        IReadOnlyList<UsingEntry>? usings = null,
        IReadOnlyList<ClassDefinition>? classes = null,
        IReadOnlyList<StructureDefinition>? structures = null,
        IReadOnlyList<EnumDefinition>? enums = null,
        IReadOnlyList<FlagsDefinition>? flags = null,
        IReadOnlyList<UnionDefinition>? unions = null,
        IReadOnlyList<StorageDefinition>? storages = null,
        IReadOnlyList<ServiceDefinition>? services = null,
        IReadOnlyList<MicroDefinition>? micros = null,
        string? sourceFile = null,
        bool namespaceIsPrimary = false)
    {
        this.@namespace = @namespace;
        namespace_is_primary = namespaceIsPrimary;
        this.usings = usings ?? [];
        this.classes = classes ?? [];
        this.structures = structures ?? [];
        this.enums = enums ?? [];
        this.flags = flags ?? [];
        this.unions = unions ?? [];
        this.storages = storages ?? [];
        this.services = services ?? [];
        this.micros = micros ?? [];
        source_file = sourceFile;
    }

    public string @namespace { get; }
    public bool namespace_is_primary { get; }
    public IReadOnlyList<UsingEntry> usings { get; }
    public IReadOnlyList<ClassDefinition> classes { get; }
    public IReadOnlyList<StructureDefinition> structures { get; }
    public IReadOnlyList<EnumDefinition> enums { get; }
    public IReadOnlyList<FlagsDefinition> flags { get; }
    public IReadOnlyList<UnionDefinition> unions { get; }
    public IReadOnlyList<StorageDefinition> storages { get; }
    public IReadOnlyList<ServiceDefinition> services { get; }
    public IReadOnlyList<MicroDefinition> micros { get; }
    public string? source_file { get; }
}