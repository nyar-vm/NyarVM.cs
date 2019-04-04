namespace Hermes.Migration;

public sealed class SchemaDiff
{
    public List<ClassDefinition> AddedClasses { get; } = [];
    public List<ClassDefinition> RemovedClasses { get; } = [];
    public List<ClassDiff> ModifiedClasses { get; } = [];

    public List<EnumDefinition> AddedEnums { get; } = [];
    public List<EnumDefinition> RemovedEnums { get; } = [];
    public List<EnumDiff> ModifiedEnums { get; } = [];

    public List<FlagsDefinition> AddedFlags { get; } = [];
    public List<FlagsDefinition> RemovedFlags { get; } = [];

    public List<UnionDefinition> AddedUnions { get; } = [];
    public List<UnionDefinition> RemovedUnions { get; } = [];
    public List<UnionDiff> ModifiedUnions { get; } = [];

    public List<StorageDefinition> AddedStorages { get; } = [];
    public List<StorageDefinition> RemovedStorages { get; } = [];
    public List<StorageDiff> ModifiedStorages { get; } = [];

    public List<ServiceDefinition> AddedServices { get; } = [];
    public List<ServiceDefinition> RemovedServices { get; } = [];

    public bool HasChanges =>
        AddedClasses.Count > 0 || RemovedClasses.Count > 0 || ModifiedClasses.Count > 0
        || AddedEnums.Count > 0 || RemovedEnums.Count > 0 || ModifiedEnums.Count > 0
        || AddedFlags.Count > 0 || RemovedFlags.Count > 0
        || AddedUnions.Count > 0 || RemovedUnions.Count > 0 || ModifiedUnions.Count > 0
        || AddedStorages.Count > 0 || RemovedStorages.Count > 0 || ModifiedStorages.Count > 0
        || AddedServices.Count > 0 || RemovedServices.Count > 0;
}

public sealed class ClassDiff
{
    public ClassDiff(string name, ClassDefinition source, ClassDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public ClassDefinition Source { get; }
    public ClassDefinition Target { get; }
    public List<FieldDefinition> AddedFields { get; } = [];
    public List<FieldDefinition> RemovedFields { get; } = [];
    public List<FieldDiff> ModifiedFields { get; } = [];

    public bool HasChanges => AddedFields.Count > 0 || RemovedFields.Count > 0 || ModifiedFields.Count > 0;
}

public sealed class FieldDiff
{
    public FieldDiff(string name, FieldDefinition source, FieldDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public FieldDefinition Source { get; }
    public FieldDefinition Target { get; }
}

public sealed class EnumDiff
{
    public EnumDiff(string name, EnumDefinition source, EnumDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public EnumDefinition Source { get; }
    public EnumDefinition Target { get; }
    public List<EnumMember> AddedMembers { get; } = [];
    public List<EnumMember> RemovedMembers { get; } = [];
    public List<EnumMemberDiff> ModifiedMembers { get; } = [];

    public bool HasChanges => AddedMembers.Count > 0 || RemovedMembers.Count > 0 || ModifiedMembers.Count > 0;
}

public sealed class EnumMemberDiff
{
    public EnumMemberDiff(string name, EnumMember source, EnumMember target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public EnumMember Source { get; }
    public EnumMember Target { get; }
}

public sealed class UnionDiff
{
    public UnionDiff(string name, UnionDefinition source, UnionDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public UnionDefinition Source { get; }
    public UnionDefinition Target { get; }
    public List<UnionVariant> AddedVariants { get; } = [];
    public List<UnionVariant> RemovedVariants { get; } = [];
    public List<UnionVariantDiff> ModifiedVariants { get; } = [];

    public bool HasChanges => AddedVariants.Count > 0 || RemovedVariants.Count > 0 || ModifiedVariants.Count > 0;
}

public sealed class UnionVariantDiff
{
    public UnionVariantDiff(string name, UnionVariant source, UnionVariant target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public UnionVariant Source { get; }
    public UnionVariant Target { get; }
}

public sealed class StorageDiff
{
    public StorageDiff(string name, StorageDefinition source, StorageDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public StorageDefinition Source { get; }
    public StorageDefinition Target { get; }
    public List<ModelDefinition> AddedModels { get; } = [];
    public List<ModelDefinition> RemovedModels { get; } = [];
    public List<ModelDiff> ModifiedModels { get; } = [];
    public List<StreamDefinition> AddedStreams { get; } = [];
    public List<StreamDefinition> RemovedStreams { get; } = [];
    public List<CacheDefinition> AddedCaches { get; } = [];
    public List<CacheDefinition> RemovedCaches { get; } = [];

    public bool HasChanges =>
        AddedModels.Count > 0 || RemovedModels.Count > 0 || ModifiedModels.Count > 0
        || AddedStreams.Count > 0 || RemovedStreams.Count > 0
        || AddedCaches.Count > 0 || RemovedCaches.Count > 0;
}

public sealed class ModelDiff
{
    public ModelDiff(string name, ModelDefinition source, ModelDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public ModelDefinition Source { get; }
    public ModelDefinition Target { get; }
    public List<FieldDefinition> AddedFields { get; } = [];
    public List<FieldDefinition> RemovedFields { get; } = [];
    public List<FieldDiff> ModifiedFields { get; } = [];

    public bool HasChanges => AddedFields.Count > 0 || RemovedFields.Count > 0 || ModifiedFields.Count > 0;
}

public sealed class ServiceDiff
{
    public ServiceDiff(string name, ServiceDefinition source, ServiceDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public ServiceDefinition Source { get; }
    public ServiceDefinition Target { get; }
    public List<EndpointDefinition> AddedEndpoints { get; } = [];
    public List<EndpointDefinition> RemovedEndpoints { get; } = [];
    public List<EndpointDiff> ModifiedEndpoints { get; } = [];

    public bool HasChanges => AddedEndpoints.Count > 0 || RemovedEndpoints.Count > 0 || ModifiedEndpoints.Count > 0;
}

public sealed class EndpointDiff
{
    public EndpointDiff(string name, EndpointDefinition source, EndpointDefinition target)
    {
        Name = name;
        Source = source;
        Target = target;
    }

    public string Name { get; }
    public EndpointDefinition Source { get; }
    public EndpointDefinition Target { get; }
}