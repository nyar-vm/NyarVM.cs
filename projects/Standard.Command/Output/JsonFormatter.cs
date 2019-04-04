using System.Text.Encodings.Web;
using System.Text.Json;

namespace Std.Command.Output;

/// <summary>
///     JSON 格式化器，使用 System.Text.Json 序列化对象
/// </summary>
/// <typeparam name="T">输出数据类型</typeparam>
public sealed class JsonFormatter<T> : IOutputFormatter<T>
{
    private static readonly JsonSerializerOptions _default_options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly JsonSerializerOptions _options;

    /// <summary>
    ///     创建使用默认选项的 JSON 格式化器
    /// </summary>
    public JsonFormatter()
    {
        _options = _default_options;
    }

    /// <summary>
    ///     创建使用自定义选项的 JSON 格式化器
    /// </summary>
    /// <param name="options">JSON 序列化选项</param>
    public JsonFormatter(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public bool supports_format(OutputFormat format)
    {
        return format == OutputFormat.json;
    }

    /// <inheritdoc />
    public string format(T data)
    {
        if (data is null) return "null";

        return JsonSerializer.Serialize(data, _options);
    }
}