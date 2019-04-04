using System.Text;
using System.Text.Json;

namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 消息序列化器，提供文本和二进制消息与对象之间的转换。
/// </summary>
public static class WebSocketMessageSerializer
{
    private static readonly JsonSerializerOptions _default_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     将对象序列化为 JSON 文本，通过 <see cref="IWebSocketContext.send_text" /> 发送。
    /// </summary>
    /// <typeparam name="T">消息类型。</typeparam>
    /// <param name="context">WebSocket 连接上下文。</param>
    /// <param name="message">消息对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task send_json<T>(this IWebSocketContext context, T message,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message, _default_options);
        await context.send_text(json, cancellationToken);
    }

    /// <summary>
    ///     将收到的 JSON 文本反序列化为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="json">JSON 文本。</param>
    /// <returns>反序列化的对象。</returns>
    public static T? deserialize_json<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _default_options);
    }

    /// <summary>
    ///     将二进制数据与消息类型名组合编码。
    /// </summary>
    /// <param name="typeName">消息类型名。</param>
    /// <param name="data">二进制数据。</param>
    /// <returns>组合后的二进制帧。</returns>
    public static byte[] encode_typed_binary(string typeName, byte[] data)
    {
        var typeNameBytes = Encoding.UTF8.GetBytes(typeName);
        var typeNameLength = BitConverter.GetBytes(typeNameBytes.Length);

        if (BitConverter.IsLittleEndian) Array.Reverse(typeNameLength);

        var result = new byte[4 + typeNameBytes.Length + data.Length];
        Buffer.BlockCopy(typeNameLength, 0, result, 0, 4);
        Buffer.BlockCopy(typeNameBytes, 0, result, 4, typeNameBytes.Length);
        Buffer.BlockCopy(data, 0, result, 4 + typeNameBytes.Length, data.Length);
        return result;
    }

    /// <summary>
    ///     将组合编码的二进制数据解码为类型名和数据。
    /// </summary>
    /// <param name="encoded">编码后的数据。</param>
    /// <returns>类型名和数据元组。</returns>
    public static (string TypeName, byte[] Data) decode_typed_binary(byte[] encoded)
    {
        var lengthBytes = new byte[4];
        Buffer.BlockCopy(encoded, 0, lengthBytes, 0, 4);

        if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);

        var typeNameLength = BitConverter.ToInt32(lengthBytes, 0);
        var typeName = Encoding.UTF8.GetString(encoded, 4, typeNameLength);
        var data = new byte[encoded.Length - 4 - typeNameLength];
        Buffer.BlockCopy(encoded, 4 + typeNameLength, data, 0, data.Length);

        return (typeName, data);
    }
}