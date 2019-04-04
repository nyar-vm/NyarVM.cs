namespace Std.Config.Node;

/// <summary>
///     配置节点树的构建辅助器，从扁平键值对构建层级化的 ConfigNode 树�?/// 支持数字索引路径段自动创建数组节点�?///
/// </summary>
internal sealed class ConfigNodeBuilder
{
    private readonly Dictionary<string, ConfigNode> _fields = new();

    /// <summary>
    ///     添加一个标量值到指定路径�?    ///
    /// </summary>
    /// <param name="path">
    ///     点分隔的路径，如 "database.host" �?"servers.0"�?/param>
    ///     <param name="value">标量值�?/param>
    public void add(string path, string value)
    {
        var parts = path.Split('.');
        add_recursive(_fields, parts, 0, value);
    }

    /// <summary>
    ///     构建最终的 ObjectConfigNode�?    ///
    /// </summary>
    /// <returns>包含所有已添加字段�?ObjectConfigNode�?/returns>
    public ObjectConfigNode build()
    {
        return new ObjectConfigNode(new Dictionary<string, ConfigNode>(_fields));
    }

    private static void add_recursive(Dictionary<string, ConfigNode> fields, string[] parts, int index, string value)
    {
        if (index == parts.Length - 1)
        {
            fields[parts[index]] = new ScalarConfigNode(value);
            return;
        }

        var key = parts[index];
        var nextKey = parts[index + 1];

        if (int.TryParse(nextKey, out _))
        {
            if (!fields.TryGetValue(key, out var existing) || existing is not ArrayConfigNode arr)
            {
                arr = new ArrayConfigNode([]);
                fields[key] = arr;
            }

            var items = arr.enumerate_array().ToList();
            add_recursive_array(items, parts, index + 1, value);
            fields[key] = new ArrayConfigNode(items);
        }
        else
        {
            if (!fields.TryGetValue(key, out var existing) || existing is not ObjectConfigNode obj)
            {
                obj = new ObjectConfigNode(new Dictionary<string, ConfigNode>());
                fields[key] = obj;
            }

            var innerFields = new Dictionary<string, ConfigNode>();
            foreach (var kvp in obj.enumerate_fields()) innerFields[kvp.Key] = kvp.Value;

            add_recursive(innerFields, parts, index + 1, value);

            fields[key] = new ObjectConfigNode(innerFields);
        }
    }

    private static void add_recursive_array(List<ConfigNode> items, string[] parts, int index, string value)
    {
        if (!int.TryParse(parts[index], out var arrayIndex)) return;

        while (items.Count <= arrayIndex) items.Add(NullConfigNode.instance);

        if (index == parts.Length - 1)
        {
            items[arrayIndex] = new ScalarConfigNode(value);
            return;
        }

        var nextKey = parts[index + 1];

        if (int.TryParse(nextKey, out _))
        {
            var existing = items[arrayIndex];
            var subItems = existing is ArrayConfigNode subArr
                ? subArr.enumerate_array().ToList()
                : [];

            add_recursive_array(subItems, parts, index + 1, value);
            items[arrayIndex] = new ArrayConfigNode(subItems);
        }
        else
        {
            var existing = items[arrayIndex];
            var innerFields = new Dictionary<string, ConfigNode>();

            if (existing is ObjectConfigNode obj)
                foreach (var kvp in obj.enumerate_fields())
                    innerFields[kvp.Key] = kvp.Value;

            add_recursive(innerFields, parts, index + 1, value);
            items[arrayIndex] = new ObjectConfigNode(innerFields);
        }
    }
}