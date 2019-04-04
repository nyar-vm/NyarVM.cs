namespace Hermes.Migration;

public sealed class MigrationPlanGenerator
{
    private int _versionCounter;

    public MigrationPlan Generate(SchemaDiff diff, string? description = null)
    {
        var plan = new MigrationPlan
        {
            Version = (++_versionCounter).ToString("D3"),
            Description = description ?? "schema migration",
            Created = DateTime.UtcNow
        };

        GenerateAddedClasses(plan, diff.AddedClasses);
        GenerateRemovedClasses(plan, diff.RemovedClasses);
        GenerateModifiedClasses(plan, diff.ModifiedClasses);

        GenerateAddedStorages(plan, diff.AddedStorages);
        GenerateRemovedStorages(plan, diff.RemovedStorages);
        GenerateModifiedStorages(plan, diff.ModifiedStorages);

        return plan;
    }

    #region Class 迁移

    private static void GenerateAddedClasses(MigrationPlan plan,
        IReadOnlyList<Nyar.Dialect.Schema.IR.What.ClassDefinition> addedClasses)
    {
        foreach (var addedClass in addedClasses)
        {
            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.CreateTable,
                Table = addedClass.Name
            });

            foreach (var field in addedClass.fields)
            {
                plan.Changes.Add(new MigrationOperation
                {
                    Type = MigrationOperationType.AddColumn,
                    Table = addedClass.Name,
                    Column = field.Name,
                    DataType = field.FieldType.TypeName,
                    Nullable = field.IsOptional
                });

                plan.Rollback.Add(new MigrationOperation
                {
                    Type = MigrationOperationType.DropColumn,
                    Table = addedClass.Name,
                    Column = field.Name
                });
            }

            plan.Rollback.Add(new MigrationOperation
            {
                Type = MigrationOperationType.DropTable,
                Table = addedClass.Name
            });
        }
    }

    private static void GenerateRemovedClasses(MigrationPlan plan,
        IReadOnlyList<Nyar.Dialect.Schema.IR.What.ClassDefinition> removedClasses)
    {
        foreach (var removedClass in removedClasses)
            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.DropTable,
                Table = removedClass.Name
            });
    }

    private static void GenerateModifiedClasses(MigrationPlan plan, IReadOnlyList<ClassDiff> modifiedClasses)
    {
        foreach (var modifiedClass in modifiedClasses)
        {
            GenerateAddedFields(plan, modifiedClass.Name, modifiedClass.AddedFields);
            GenerateRemovedFields(plan, modifiedClass.Name, modifiedClass.RemovedFields);
            GenerateModifiedFields(plan, modifiedClass.Name, modifiedClass.ModifiedFields);
        }
    }

    private static void GenerateAddedFields(MigrationPlan plan, string table,
        IReadOnlyList<Nyar.Dialect.Schema.IR.What.FieldDefinition> addedFields)
    {
        foreach (var field in addedFields)
        {
            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.AddColumn,
                Table = table,
                Column = field.Name,
                DataType = field.FieldType.TypeName,
                Nullable = field.IsOptional,
                DefaultValue = field.DefaultValue
            });

            plan.Rollback.Add(new MigrationOperation
            {
                Type = MigrationOperationType.DropColumn,
                Table = table,
                Column = field.Name
            });
        }
    }

    private static void GenerateRemovedFields(MigrationPlan plan, string table,
        IReadOnlyList<Nyar.Dialect.Schema.IR.What.FieldDefinition> removedFields)
    {
        foreach (var field in removedFields)
            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.DropColumn,
                Table = table,
                Column = field.Name
            });
    }

    private static void GenerateModifiedFields(MigrationPlan plan, string table,
        IReadOnlyList<FieldDiff> modifiedFields)
    {
        foreach (var field in modifiedFields)
            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.AlterColumn,
                Table = table,
                Column = field.Name,
                DataType = field.Target.FieldType.TypeName,
                Nullable = field.Target.IsOptional
            });
    }

    #endregion

    #region Storage 迁移

    private static void GenerateAddedStorages(MigrationPlan plan,
        IReadOnlyList<Nyar.Dialect.Schema.IR.How.StorageDefinition> addedStorages)
    {
        foreach (var storage in addedStorages)
        foreach (var model in storage.Models)
        {
            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.CreateTable,
                Table = model.Name
            });

            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.AddColumn,
                Table = model.Name,
                Column = "__key__",
                DataType = model.KeyType.TypeName,
                Nullable = false
            });

            foreach (var field in model.fields)
                plan.Changes.Add(new MigrationOperation
                {
                    Type = MigrationOperationType.AddColumn,
                    Table = model.Name,
                    Column = field.Name,
                    DataType = field.FieldType.TypeName,
                    Nullable = field.IsOptional
                });

            plan.Rollback.Add(new MigrationOperation
            {
                Type = MigrationOperationType.DropTable,
                Table = model.Name
            });
        }
    }

    private static void GenerateRemovedStorages(MigrationPlan plan,
        IReadOnlyList<Nyar.Dialect.Schema.IR.How.StorageDefinition> removedStorages)
    {
        foreach (var storage in removedStorages)
        foreach (var model in storage.Models)
            plan.Changes.Add(new MigrationOperation
            {
                Type = MigrationOperationType.DropTable,
                Table = model.Name
            });
    }

    private static void GenerateModifiedStorages(MigrationPlan plan, IReadOnlyList<StorageDiff> modifiedStorages)
    {
        foreach (var storageDiff in modifiedStorages)
        {
            foreach (var model in storageDiff.AddedModels)
            {
                plan.Changes.Add(new MigrationOperation
                {
                    Type = MigrationOperationType.CreateTable,
                    Table = model.Name
                });

                foreach (var field in model.fields)
                    plan.Changes.Add(new MigrationOperation
                    {
                        Type = MigrationOperationType.AddColumn,
                        Table = model.Name,
                        Column = field.Name,
                        DataType = field.FieldType.TypeName,
                        Nullable = field.IsOptional
                    });

                plan.Rollback.Add(new MigrationOperation
                {
                    Type = MigrationOperationType.DropTable,
                    Table = model.Name
                });
            }

            foreach (var model in storageDiff.RemovedModels)
                plan.Changes.Add(new MigrationOperation
                {
                    Type = MigrationOperationType.DropTable,
                    Table = model.Name
                });

            foreach (var modelDiff in storageDiff.ModifiedModels)
            {
                GenerateAddedFields(plan, modelDiff.Name, modelDiff.AddedFields);
                GenerateRemovedFields(plan, modelDiff.Name, modelDiff.RemovedFields);
                GenerateModifiedFields(plan, modelDiff.Name, modelDiff.ModifiedFields);
            }
        }
    }

    #endregion
}