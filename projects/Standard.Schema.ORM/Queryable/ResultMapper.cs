using System.Reflection;

namespace Hermes.ORM;

internal static class ResultMapper
{
    public static List<T> Map<T>(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows) where T : class
    {
        var result = new List<T>(rows.Count);
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var row in rows)
        {
            var instance = Activator.CreateInstance(type) as T;
            if (instance == null) continue;

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;

                var key = prop.Name;
                if (!row.TryGetValue(key, out var value) || value == null) continue;

                try
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var converted = Convert.ChangeType(value, targetType);
                    prop.SetValue(instance, converted);
                }
                catch
                {
                    // 跳过类型不匹配的字段
                }
            }

            result.Add(instance);
        }

        return result;
    }
}