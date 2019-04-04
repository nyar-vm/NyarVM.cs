namespace Hermes.Migration;

public sealed class SchemaDiffer
{
    public SchemaDiff Diff(SchemaIR source, SchemaIR target)
    {
        var diff = new SchemaDiff();

        DiffClasses(diff, source.Classes, target.Classes);
        DiffEnums(diff, source.Enums, target.Enums);
        DiffFlags(diff, source.Flags, target.Flags);
        DiffUnions(diff, source.Unions, target.Unions);
        DiffStorages(diff, source.Storages, target.Storages);
        DiffServices(diff, source.Services, target.Services);

        return diff;
    }

    #region Flags 差异

    private void DiffFlags(SchemaDiff diff, IReadOnlyList<FlagsDefinition> source,
        IReadOnlyList<FlagsDefinition> target)
    {
        var sourceMap = source.ToDictionary(f => f.Name);
        var targetMap = target.ToDictionary(f => f.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedFlags.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedFlags.Add(sourceMap[name]);
    }

    #endregion

    #region Service 差异

    private void DiffServices(SchemaDiff diff, IReadOnlyList<ServiceDefinition> source,
        IReadOnlyList<ServiceDefinition> target)
    {
        var sourceMap = source.ToDictionary(s => s.Name);
        var targetMap = target.ToDictionary(s => s.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedServices.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedServices.Add(sourceMap[name]);
    }

    #endregion

    #region Class 差异

    private void DiffClasses(SchemaDiff diff, IReadOnlyList<ClassDefinition> source,
        IReadOnlyList<ClassDefinition> target)
    {
        var sourceMap = source.ToDictionary(c => c.Name);
        var targetMap = target.ToDictionary(c => c.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedClasses.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedClasses.Add(sourceMap[name]);

        foreach (var name in sourceMap.Keys.Intersect(targetMap.Keys))
        {
            var classDiff = DiffClassFields(sourceMap[name], targetMap[name]);
            if (classDiff.HasChanges) diff.ModifiedClasses.Add(classDiff);
        }
    }

    private static ClassDiff DiffClassFields(ClassDefinition source, ClassDefinition target)
    {
        var diff = new ClassDiff(source.Name, source, target);
        var sourceFields = source.fields.ToDictionary(f => f.Name);
        var targetFields = target.fields.ToDictionary(f => f.Name);

        foreach (var name in targetFields.Keys.Except(sourceFields.Keys)) diff.AddedFields.Add(targetFields[name]);

        foreach (var name in sourceFields.Keys.Except(targetFields.Keys)) diff.RemovedFields.Add(sourceFields[name]);

        foreach (var name in sourceFields.Keys.Intersect(targetFields.Keys))
            if (sourceFields[name].FieldType.TypeName != targetFields[name].FieldType.TypeName)
                diff.ModifiedFields.Add(new FieldDiff(name, sourceFields[name], targetFields[name]));

        return diff;
    }

    #endregion

    #region Enum 差异

    private void DiffEnums(SchemaDiff diff, IReadOnlyList<EnumDefinition> source, IReadOnlyList<EnumDefinition> target)
    {
        var sourceMap = source.ToDictionary(e => e.Name);
        var targetMap = target.ToDictionary(e => e.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedEnums.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedEnums.Add(sourceMap[name]);

        foreach (var name in sourceMap.Keys.Intersect(targetMap.Keys))
        {
            var enumDiff = DiffEnumMembers(sourceMap[name], targetMap[name]);
            if (enumDiff.HasChanges) diff.ModifiedEnums.Add(enumDiff);
        }
    }

    private static EnumDiff DiffEnumMembers(EnumDefinition source, EnumDefinition target)
    {
        var diff = new EnumDiff(source.Name, source, target);
        var sourceMembers = source.Members.ToDictionary(m => m.Name);
        var targetMembers = target.Members.ToDictionary(m => m.Name);

        foreach (var name in targetMembers.Keys.Except(sourceMembers.Keys)) diff.AddedMembers.Add(targetMembers[name]);

        foreach (var name in sourceMembers.Keys.Except(targetMembers.Keys))
            diff.RemovedMembers.Add(sourceMembers[name]);

        foreach (var name in sourceMembers.Keys.Intersect(targetMembers.Keys))
            if (sourceMembers[name].Value != targetMembers[name].Value)
                diff.ModifiedMembers.Add(new EnumMemberDiff(name, sourceMembers[name], targetMembers[name]));

        return diff;
    }

    #endregion

    #region Union 差异

    private void DiffUnions(SchemaDiff diff, IReadOnlyList<UnionDefinition> source,
        IReadOnlyList<UnionDefinition> target)
    {
        var sourceMap = source.ToDictionary(u => u.Name);
        var targetMap = target.ToDictionary(u => u.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedUnions.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedUnions.Add(sourceMap[name]);

        foreach (var name in sourceMap.Keys.Intersect(targetMap.Keys))
        {
            var unionDiff = DiffUnionVariants(sourceMap[name], targetMap[name]);
            if (unionDiff.HasChanges) diff.ModifiedUnions.Add(unionDiff);
        }
    }

    private static UnionDiff DiffUnionVariants(UnionDefinition source, UnionDefinition target)
    {
        var diff = new UnionDiff(source.Name, source, target);
        var sourceVariants = source.Variants.ToDictionary(v => v.Name);
        var targetVariants = target.Variants.ToDictionary(v => v.Name);

        foreach (var name in targetVariants.Keys.Except(sourceVariants.Keys))
            diff.AddedVariants.Add(targetVariants[name]);

        foreach (var name in sourceVariants.Keys.Except(targetVariants.Keys))
            diff.RemovedVariants.Add(sourceVariants[name]);

        foreach (var name in sourceVariants.Keys.Intersect(targetVariants.Keys))
        {
            var sourcePayload = sourceVariants[name].Payload?.TypeName;
            var targetPayload = targetVariants[name].Payload?.TypeName;
            if (sourcePayload != targetPayload)
                diff.ModifiedVariants.Add(new UnionVariantDiff(name, sourceVariants[name], targetVariants[name]));
        }

        return diff;
    }

    #endregion

    #region Storage 差异

    private void DiffStorages(SchemaDiff diff, IReadOnlyList<StorageDefinition> source,
        IReadOnlyList<StorageDefinition> target)
    {
        var sourceMap = source.ToDictionary(s => s.Name);
        var targetMap = target.ToDictionary(s => s.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedStorages.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedStorages.Add(sourceMap[name]);

        foreach (var name in sourceMap.Keys.Intersect(targetMap.Keys))
        {
            var storageDiff = DiffStorageInternals(sourceMap[name], targetMap[name]);
            if (storageDiff.HasChanges) diff.ModifiedStorages.Add(storageDiff);
        }
    }

    private static StorageDiff DiffStorageInternals(StorageDefinition source, StorageDefinition target)
    {
        var diff = new StorageDiff(source.Name, source, target);

        DiffStorageModels(diff, source.Models, target.Models);
        DiffStorageStreams(diff, source.Streams, target.Streams);
        DiffStorageCaches(diff, source.Caches, target.Caches);

        return diff;
    }

    private static void DiffStorageModels(StorageDiff diff, IReadOnlyList<ModelDefinition> source,
        IReadOnlyList<ModelDefinition> target)
    {
        var sourceMap = source.ToDictionary(m => m.Name);
        var targetMap = target.ToDictionary(m => m.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedModels.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedModels.Add(sourceMap[name]);

        foreach (var name in sourceMap.Keys.Intersect(targetMap.Keys))
        {
            var modelDiff = DiffModelFields(sourceMap[name], targetMap[name]);
            if (modelDiff.HasChanges) diff.ModifiedModels.Add(modelDiff);
        }
    }

    private static ModelDiff DiffModelFields(ModelDefinition source, ModelDefinition target)
    {
        var diff = new ModelDiff(source.Name, source, target);
        var sourceFields = source.fields.ToDictionary(f => f.Name);
        var targetFields = target.fields.ToDictionary(f => f.Name);

        foreach (var name in targetFields.Keys.Except(sourceFields.Keys)) diff.AddedFields.Add(targetFields[name]);

        foreach (var name in sourceFields.Keys.Except(targetFields.Keys)) diff.RemovedFields.Add(sourceFields[name]);

        foreach (var name in sourceFields.Keys.Intersect(targetFields.Keys))
            if (sourceFields[name].FieldType.TypeName != targetFields[name].FieldType.TypeName)
                diff.ModifiedFields.Add(new FieldDiff(name, sourceFields[name], targetFields[name]));

        return diff;
    }

    private static void DiffStorageStreams(StorageDiff diff, IReadOnlyList<StreamDefinition> source,
        IReadOnlyList<StreamDefinition> target)
    {
        var sourceMap = source.ToDictionary(s => s.Name);
        var targetMap = target.ToDictionary(s => s.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedStreams.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedStreams.Add(sourceMap[name]);
    }

    private static void DiffStorageCaches(StorageDiff diff, IReadOnlyList<CacheDefinition> source,
        IReadOnlyList<CacheDefinition> target)
    {
        var sourceMap = source.ToDictionary(c => c.Name);
        var targetMap = target.ToDictionary(c => c.Name);

        foreach (var name in targetMap.Keys.Except(sourceMap.Keys)) diff.AddedCaches.Add(targetMap[name]);

        foreach (var name in sourceMap.Keys.Except(targetMap.Keys)) diff.RemovedCaches.Add(sourceMap[name]);
    }

    #endregion
}