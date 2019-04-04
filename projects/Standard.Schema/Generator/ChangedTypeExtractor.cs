using Hermes.Migration;

namespace Hermes.Generator;

public sealed class ChangedTypeExtractor
{
    public (ISet<string> Changed, ISet<string> Removed) Extract(SchemaDiff diff)
    {
        var changed = new HashSet<string>();
        var removed = new HashSet<string>();

        ExtractClasses(diff, changed, removed);
        ExtractEnums(diff, changed, removed);
        ExtractFlags(diff, changed, removed);
        ExtractUnions(diff, changed, removed);
        ExtractStorages(diff, changed, removed);
        ExtractServices(diff, changed, removed);

        return (changed, removed);
    }

    private static void ExtractClasses(SchemaDiff diff, ISet<string> changed, ISet<string> removed)
    {
        foreach (var cls in diff.AddedClasses) changed.Add(cls.Name);

        foreach (var cls in diff.RemovedClasses) removed.Add(cls.Name);

        foreach (var cls in diff.ModifiedClasses) changed.Add(cls.Name);
    }

    private static void ExtractEnums(SchemaDiff diff, ISet<string> changed, ISet<string> removed)
    {
        foreach (var en in diff.AddedEnums) changed.Add(en.Name);

        foreach (var en in diff.RemovedEnums) removed.Add(en.Name);

        foreach (var en in diff.ModifiedEnums) changed.Add(en.Name);
    }

    private static void ExtractFlags(SchemaDiff diff, ISet<string> changed, ISet<string> removed)
    {
        foreach (var f in diff.AddedFlags) changed.Add(f.Name);

        foreach (var f in diff.RemovedFlags) removed.Add(f.Name);
    }

    private static void ExtractUnions(SchemaDiff diff, ISet<string> changed, ISet<string> removed)
    {
        foreach (var u in diff.AddedUnions) changed.Add(u.Name);

        foreach (var u in diff.RemovedUnions) removed.Add(u.Name);

        foreach (var u in diff.ModifiedUnions) changed.Add(u.Name);
    }

    private static void ExtractStorages(SchemaDiff diff, ISet<string> changed, ISet<string> removed)
    {
        foreach (var s in diff.AddedStorages) changed.Add(s.Name);

        foreach (var s in diff.RemovedStorages) removed.Add(s.Name);

        foreach (var s in diff.ModifiedStorages) changed.Add(s.Name);
    }

    private static void ExtractServices(SchemaDiff diff, ISet<string> changed, ISet<string> removed)
    {
        foreach (var s in diff.AddedServices) changed.Add(s.Name);

        foreach (var s in diff.RemovedServices) removed.Add(s.Name);
    }
}