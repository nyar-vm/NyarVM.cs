using System.Reflection;
using System.Text.Json;
using Std.Text;

namespace Std.Net.Routing;

/// <summary>
///     请求体绑定器，将 HTTP 请求体反序列化为指定类型的对象。
///     Sonic.Standard 使用 <c>byte[]</c> 作为请求体，而非 <c>Stream</c>。
/// </summary>
public static class RequestBodyBinder
{
    private static readonly JsonSerializerOptions _json_options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    ///     从请求体中读取 JSON 并反序列化为目标类型。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="body">包含 JSON 的请求体字节</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="JsonException">JSON 格式无效或请求体为空</exception>
    public static T bind_json<T>(byte[] body) where T : class
    {
        if (body.Length == 0) throw new JsonException("请求体为空");

        var json = SonicEncoding.decode_utf8(body);

        if (string.IsNullOrWhiteSpace(json)) throw new JsonException("请求体为空");

        var result = JsonSerializer.Deserialize<T>(json, _json_options);

        if (result is null) throw new JsonException($"无法将请求体反序列化为 {typeof(T).Name}");

        return result;
    }

    /// <summary>
    ///     从请求体中读取表单数据并映射到目标类型。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="body">包含表单数据的请求体字节</param>
    /// <returns>绑定后的对象</returns>
    public static T bind_form<T>(byte[] body) where T : class, new()
    {
        var formData = SonicEncoding.decode_utf8(body);
        var result = new T();
        var type = typeof(T);

        foreach (var pair in parse_form_data(formData))
        {
            var prop = type.GetProperty(pair.Key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is not null && prop.CanWrite)
            {
                var value = Convert.ChangeType(pair.Value, prop.PropertyType);
                prop.SetValue(result, value);
            }
        }

        return result;
    }

    /// <summary>
    ///     解析 URL 编码的表单数据为键值对。
    /// </summary>
    /// <param name="formData">表单数据字符串</param>
    /// <returns>键值对字典</returns>
    public static Dictionary<string, string> parse_form_data(string formData)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(formData)) return result;

        var pairs = formData.Split('&');

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);

            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }

        return result;
    }
}