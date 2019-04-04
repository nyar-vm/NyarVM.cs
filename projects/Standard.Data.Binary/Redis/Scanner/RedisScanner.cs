using Std.Data.Binary.Frame;
using Std.Data.Binary.Redis.Data;

namespace Std.Data.Binary.Redis.Scanner;

/// <summary>
///     Redis RESP 协议扫描器，基于 <see cref="SpanScanner" /> 提供的Redis RESP 协议消息的快速扫描的
/// </summary>
/// <remarks>
///     Redis 使用 RESP（REdis Serialization Protocol）文本协议的
///     消息以类型前缀的的的的的）开头，的CRLF 结尾的
///     扫描器逐行解析消息结构，提取消息类型和内容摘要的
/// </remarks>
public ref struct RedisScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="RedisScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 Redis 字节数据的/param>
    public RedisScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 Redis RESP 消息，提取统计信息的
    /// </summary>
    public RedisScanStatistics scan_statistics()
    {
        var stats = new RedisScanStatistics();
        var typeCounts = new Dictionary<char, int>();

        while (!_scanner.is_end)
        {
            var lineEnd = find_line_end();

            if (lineEnd < 0) break;

            var lineLength = lineEnd - _scanner.position;
            var prefix = (char)_scanner.buffer.read_u8();

            typeCounts.TryAdd(prefix, 0);

            typeCounts[prefix]++;

            switch (prefix)
            {
                case RedisConstants.simple_string_prefix:
                    stats.simple_string_count++;
                    break;
                case RedisConstants.error_prefix:
                    stats.error_count++;
                    break;
                case RedisConstants.integer_prefix:
                    stats.integer_count++;
                    break;
                case RedisConstants.bulk_string_prefix:
                    stats.bulk_string_count++;
                    break;
                case RedisConstants.array_prefix:
                    stats.array_count++;
                    break;
            }

            _scanner.position = lineEnd + 2;
        }

        stats.type_counts = typeCounts;
        return stats;
    }

    /// <summary>
    ///     扫描 Redis RESP 消息，提取所有简单字符串和错误消息内容的
    /// </summary>
    public List<(char Type, string Content)> scan_messages()
    {
        var messages = new List<(char Type, string Content)>();

        while (!_scanner.is_end)
        {
            var lineEnd = find_line_end();

            if (lineEnd < 0) break;

            var prefix = (char)_scanner.buffer.read_u8();
            var contentLength = lineEnd - _scanner.position;
            var content = contentLength > 0 ? _scanner.buffer.read_string(contentLength) : string.Empty;

            messages.Add((prefix, content));
            _scanner.position = lineEnd + 2;
        }

        return messages;
    }

    private int find_line_end()
    {
        var pos = _scanner.position;

        while (pos + 1 < _scanner.length)
        {
            if (_scanner.buffer.read_u8_at(pos) == '\r' && _scanner.buffer.read_u8_at(pos + 1) == '\n') return pos;

            pos++;
        }

        return -1;
    }
}

/// <summary>
///     Redis 扫描统计信息的
/// </summary>
public sealed class RedisScanStatistics
{
    public int simple_string_count { get; set; }
    public int error_count { get; set; }
    public int integer_count { get; set; }
    public int bulk_string_count { get; set; }
    public int array_count { get; set; }
    public Dictionary<char, int> type_counts { get; set; } = new();
}