using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace Std.Command.Hosting;

/// <summary>
///     命令行配置绑定，支持�?JSON 配置文件、环境变量和命令行参数读取配�?///
/// </summary>
public sealed class CommandConfiguration
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     获取所有配置键
    /// </summary>
    public IEnumerable<string> keys => _values.Keys;

    /// <summary>
    ///     �?JSON 配置文件加载配置
    /// </summary>
    /// <param name="filePath">JSON 文件路径，如 "appsettings.json"</param>
    /// <param name="optional">文件不存在时是否静默跳过（默�?true�?/param>
    public CommandConfiguration add_json_file(string filePath, bool optional = true)
    {
        if (!File.Exists(filePath))
        {
            if (!optional) throw new FileNotFoundException($"配置文件未找�? {filePath}", filePath);

            return this;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            flatten_json_element(doc.RootElement, "");
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }

        return this;
    }

    /// <summary>
    ///     从环境变量加载配置（前缀过滤�?    ///
    /// </summary>
    /// <param name="prefix">环境变量前缀，如 "IRIS_"</param>
    public CommandConfiguration add_environment_variables(string prefix = "IRIS_")
    {
        foreach (var env in Environment.GetEnvironmentVariables())
            if (env is DictionaryEntry { Key: string key } entry
                && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && entry.Value is string value)
            {
                var configKey = key[prefix.Length..].Replace("__", ":");
                _values[configKey] = value;
            }

        return this;
    }

    /// <summary>
    ///     从命令行参数加载配置�?-key=value �?--key value 格式�?    ///
    /// </summary>
    /// <param name="args">命令行参�?/param>
    public CommandConfiguration add_command_line(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--"))
            {
                var eqIndex = arg.IndexOf('=');
                string key;
                string? value;

                if (eqIndex >= 0)
                {
                    key = arg[2..eqIndex];
                    value = arg[(eqIndex + 1)..];
                }
                else
                {
                    key = arg[2..];

                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        value = args[++i];
                    else
                        value = "true";
                }

                _values[key] = value;
            }
        }

        return this;
    }

    /// <summary>
    ///     设置配置�?    ///
    /// </summary>
    /// <param name="key">
    ///     配置�?/param>
    ///     <param name="value">配置�?/param>
    public CommandConfiguration set(string key, string? value)
    {
        _values[key] = value;
        return this;
    }

    /// <summary>
    ///     获取配置�?    ///
    /// </summary>
    /// <param name="key">
    ///     配置�?/param>
    ///     <returns>配置值，不存在时返回 null</returns>
    public string? get(string key)
    {
        _values.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    ///     获取并转换配置�?    ///
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="key">
    ///     配置�?/param>
    ///     <param name="defaultValue">
    ///         默认�?/param>
    ///         <returns>配置值或默认�?/returns>
    public T get<T>(string key, T defaultValue = default!) where T : notnull
    {
        var strValue = get(key);
        if (strValue == null) return defaultValue;

        try
        {
            return (T)Convert.ChangeType(strValue, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    ///     是否包含指定�?    ///
    /// </summary>
    /// <param name="key">
    ///     配置�?/param>
    ///     <returns>是否存在</returns>
    public bool contains_key(string key)
    {
        return _values.ContainsKey(key);
    }

    /// <summary>
    ///     将配置值绑定到对象属�?    ///
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="prefix">键前缀（如 "Database" 对应 "Database:Host" 等）</param>
    /// <param name="instance">要绑定的实例，为 null 时创建新实例</param>
    /// <returns>绑定后的实例</returns>
    public T bind<T>(string? prefix = null, T? instance = null) where T : class, new()
    {
        var obj = instance ?? new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;

            var fullKey = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";
            var value = get(fullKey);

            if (value != null)
                try
                {
                    var converted = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(obj, converted);
                }
                catch
                {
                }
        }

        return obj;
    }

    private void flatten_json_element(JsonElement element, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    flatten_json_element(property.Value, key);
                }

                break;
            case JsonValueKind.Array:
                for (var i = 0; i < element.GetArrayLength(); i++) flatten_json_element(element[i], $"{prefix}:{i}");

                break;
            case JsonValueKind.String:
                _values[prefix] = element.GetString();
                break;
            case JsonValueKind.Number:
                _values[prefix] = element.GetRawText();
                break;
            case JsonValueKind.True:
                _values[prefix] = "true";
                break;
            case JsonValueKind.False:
                _values[prefix] = "false";
                break;
            case JsonValueKind.Null:
                _values[prefix] = null;
                break;
        }
    }
}