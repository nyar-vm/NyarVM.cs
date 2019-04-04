using System.Text;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Onnx.Scanner;

/// <summary>
///     ONNX 格式扫描器，基于 <see cref="SpanScanner" /> 提供零分配的快的ONNX 模型数据探查的
/// </summary>
public ref struct OnnxScanner
{
    private SpanScanner _scanner;

    public OnnxScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 ONNX 模型统计信息的
    /// </summary>
    /// <returns>ONNX 模型统计信息的/returns>
    public OnnxStatistics scan_statistics()
    {
        var stats = new OnnxStatistics();
        var pos = 0;

        while (pos < _scanner.length)
        {
            var tagResult = read_tag_at(pos, out var tagBytes);
            pos += tagBytes;

            if (tagResult == 0) break;

            var fieldNumber = (int)(tagResult >> 3);
            var wireType = (int)(tagResult & 0x7);

            switch (fieldNumber)
            {
                case 1:
                    stats.ir_version = (long)read_varint_at(pos, out var irBytes);
                    pos += irBytes;
                    break;
                case 2:
                    var (producer, pBytes) = read_string_at(pos);
                    stats.producer_name = producer;
                    pos += pBytes;
                    break;
                case 7:
                    var graphStats = scan_graph_at(pos);
                    stats.node_count = graphStats.NodeCount;
                    stats.input_count = graphStats.InputCount;
                    stats.output_count = graphStats.OutputCount;
                    stats.initializer_count = graphStats.InitializerCount;
                    var graphLen = (int)read_varint_at(pos, out var glBytes);
                    pos += glBytes + graphLen;
                    break;
                case 8:
                    var opsetLen = (int)read_varint_at(pos, out var olBytes);
                    stats.opset_count++;
                    pos += olBytes + opsetLen;
                    break;
                default:
                    pos = skip_field_at(pos, wireType);
                    break;
            }
        }

        return stats;
    }

    private (int NodeCount, int InputCount, int OutputCount, int InitializerCount) scan_graph_at(int startPos)
    {
        var nodeCount = 0;
        var inputCount = 0;
        var outputCount = 0;
        var initializerCount = 0;

        var graphLen = (int)read_varint_at(startPos, out var lenBytes);
        var pos = startPos + lenBytes;
        var endPos = pos + graphLen;

        while (pos < endPos && pos < _scanner.length)
        {
            var tagResult = read_tag_at(pos, out var tagBytes);
            pos += tagBytes;

            if (tagResult == 0) break;

            var fieldNumber = (int)(tagResult >> 3);

            switch (fieldNumber)
            {
                case 1:
                    nodeCount++;
                    goto default;
                case 3:
                    inputCount++;
                    goto default;
                case 4:
                    outputCount++;
                    goto default;
                case 10:
                    initializerCount++;
                    goto default;
                default:
                    var wireType = (int)(tagResult & 0x7);
                    pos = skip_field_at(pos, wireType);
                    break;
            }
        }

        return (nodeCount, inputCount, outputCount, initializerCount);
    }

    private ulong read_tag_at(int pos, out int bytesRead)
    {
        bytesRead = 0;
        ulong value = 0;
        var shift = 0;

        while (pos + bytesRead < _scanner.length)
        {
            var b = _scanner.buffer.read_u8_at(pos + bytesRead);
            bytesRead++;
            value |= (ulong)(b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0) break;
        }

        return value;
    }

    private ulong read_varint_at(int pos, out int bytesRead)
    {
        bytesRead = 0;
        ulong value = 0;
        var shift = 0;

        while (pos + bytesRead < _scanner.length)
        {
            var b = _scanner.buffer.read_u8_at(pos + bytesRead);
            bytesRead++;
            value |= (ulong)(b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0) break;
        }

        return value;
    }

    private (string Value, int BytesRead) read_string_at(int pos)
    {
        var length = (int)read_varint_at(pos, out var lenBytes);
        var totalBytes = lenBytes + length;

        if (pos + totalBytes > _scanner.length) return (string.Empty, totalBytes);

        var strBytes = _scanner.data.Slice(pos + lenBytes, length);
        return (Encoding.UTF8.GetString(strBytes), totalBytes);
    }

    private int skip_field_at(int pos, int wireType)
    {
        switch (wireType)
        {
            case 0:
            {
                read_varint_at(pos, out var vb);
                return pos + vb;
            }
            case 1:
                return pos + 8;
            case 2:
            {
                var length = (int)read_varint_at(pos, out var lb);
                return pos + lb + length;
            }
            case 5:
                return pos + 4;
            default:
                return pos;
        }
    }
}

/// <summary>
///     ONNX 模型统计信息的
/// </summary>
public sealed class OnnxStatistics
{
    public long ir_version { get; set; }
    public string producer_name { get; set; } = string.Empty;
    public int node_count { get; set; }
    public int input_count { get; set; }
    public int output_count { get; set; }
    public int initializer_count { get; set; }
    public int opset_count { get; set; }

    public string ir_version_string => ir_version switch
    {
        1 => "1.0",
        2 => "1.1",
        3 => "1.2",
        4 => "1.3",
        5 => "1.4",
        6 => "1.5",
        7 => "1.6",
        8 => "1.7",
        9 => "1.8",
        10 => "1.9",
        _ => $"{ir_version}"
    };
}