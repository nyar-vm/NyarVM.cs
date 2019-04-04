using System.Reflection;
using Hermes.YYDB.Query;

namespace Hermes.ORM;

internal static class EntityMapper
{
    public static IReadOnlyList<FieldAssignment> ToAssignments<T>(T entity) where T : class
    {
        var assignments = new List<FieldAssignment>();
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanRead) continue;

            var value = prop.GetValue(entity);
            if (value != null) assignments.Add(new FieldAssignment(prop.Name, value));
        }

        return assignments;
    }
}