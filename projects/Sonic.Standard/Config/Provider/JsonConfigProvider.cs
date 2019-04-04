using System.Text.Json;
using Std.Config.Node;

namespace Std.Config.Provider;

/// <summary>
///     基于 JSON 文件的配置源，读�?JSON 文件并转换为 ConfigNode 树�?///
/// </summary>
public sealed class JsonConfigProvider : IConfigProvider
{
    /// <summary>
    ///     使用指定的文件路径初始化 <see cref="JsonConfigProvider" /> 的新实例�?    ///
    /// </summary>
    /// <param name="path">JSON 文件路径�?/param>
    public JsonConfigProvider(string path)
    {
        file_path = path;
    }

    /// <summary>
    ///     获取 JSON 文件的路径�?    ///
    /// </summary>
    public string file_path { get; }

    /// <summary>
    ///     加载并返回配置树根节点�?    ///
    /// </summary>
    /// <returns>
    ///     配置树的根节点�?/returns>
    ///     <exception cref="FileNotFoundException">当指定的文件不存在时抛出�?/exception>
    public ConfigNode load()
    {
        if (!File.Exists(file_path)) throw new FileNotFoundException($"配置文件未找到：{file_path}", file_path);

        var json = File.ReadAllText(file_path);
        using var document = JsonDocument.Parse(json);
        return convert_element(document.RootElement);
    }

    #region JSON 转换

    private static ConfigNode convert_element(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => convert_object(element),
            JsonValueKind.Array => convert_array(element),
            JsonValueKind.String => new ScalarConfigNode(element.GetString() ?? string.Empty),
            JsonValueKind.Number => new ScalarConfigNode(element.GetRawText()),
            JsonValueKind.True => new ScalarConfigNode("true"),
            JsonValueKind.False => new ScalarConfigNode("false"),
            JsonValueKind.Null => NullConfigNode.instance,
            _ => NullConfigNode.instance
        };
    }

    private static ObjectConfigNode convert_object(JsonElement element)
    {
        var fields = new Dictionary<string, ConfigNode>();
        foreach (var property in element.EnumerateObject()) fields[property.Name] = convert_element(property.Value);
        return new ObjectConfigNode(fields);
    }

    private static ArrayConfigNode convert_array(JsonElement element)
    {
        var items = new List<ConfigNode>();
        foreach (var item in element.EnumerateArray()) items.Add(convert_element(item));
        return new ArrayConfigNode(items);
    }

    #endregion
}