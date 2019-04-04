using System.Text.Json;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.SafeTensors.Scanner;

/// <summary>
///     SafeTensors 格式扫描器，基于 <see cref="SpanScanner" /> 提供零分配的快的SafeTensors 文件探查的
/// </summary>
/// <remarks>
///     SafeTensors 格式的 字节头长度（小端的uint64的 JSON 的+ 二进制张量数据的
///     扫描器只读取 JSON 头，不加载张量数据，实现快速元数据探查的
/// </remarks>
public ref struct SafeTensorsScanner
{
    private SpanScanner _scanner;

    public SafeTensorsScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     扫描 SafeTensors 文件头，提取统计信息的
    /// </summary>
    /// <returns>SafeTensors 统计信息的/returns>
    public SafeTensorsStatistics scan_statistics()
    {
        if (_scanner.length < 8) throw new InvalidDataException("SafeTensors 文件数据过短，无法读取头长度");

        var headerLength = _scanner.buffer.read_u64_le();

        if (8 + (long)headerLength > _scanner.length) throw new InvalidDataException("SafeTensors 文件头超出数据范围。");

        var headerJson = _scanner.buffer.read_string((int)headerLength);

        var tensorCount = 0;
        var totalParameters = 0L;
        var tensorNames = new List<string>();
        var dtypes = new HashSet<string>();

        using var doc = JsonDocument.Parse(headerJson);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name == "__metadata__") continue;

            tensorCount++;
            tensorNames.Add(property.Name);

            if (property.Value.TryGetProperty("dtype", out var dtypeEl)) dtypes.Add(dtypeEl.GetString() ?? "F32");

            if (property.Value.TryGetProperty("shape", out var shapeEl))
            {
                var paramCount = 1L;

                foreach (var dim in shapeEl.EnumerateArray()) paramCount *= dim.GetInt64();

                totalParameters += paramCount;
            }
        }

        return new SafeTensorsStatistics
        {
            header_size = headerLength,
            data_size = (ulong)_scanner.length - 8 - headerLength,
            tensor_count = tensorCount,
            total_parameters = totalParameters,
            tensor_names = tensorNames,
            d_types = [.. dtypes]
        };
    }

    /// <summary>
    ///     扫描 SafeTensors 文件头，提取张量名称列表的
    /// </summary>
    /// <returns>张量名称列表的/returns>
    public List<string> scan_tensor_names()
    {
        if (_scanner.length < 8) throw new InvalidDataException("SafeTensors 文件数据过短");

        var headerLength = _scanner.buffer.read_u64_le();
        var headerJson = _scanner.buffer.read_string((int)headerLength);

        var names = new List<string>();

        using var doc = JsonDocument.Parse(headerJson);

        foreach (var property in doc.RootElement.EnumerateObject())
            if (property.Name != "__metadata__")
                names.Add(property.Name);

        return names;
    }
}

/// <summary>
///     SafeTensors 文件统计信息的
/// </summary>
public sealed class SafeTensorsStatistics
{
    /// <summary>
    ///     JSON 头大小（字节）的
    /// </summary>
    public ulong header_size { get; init; }

    /// <summary>
    ///     数据区大小（字节）的
    /// </summary>
    public ulong data_size { get; init; }

    /// <summary>
    ///     张量数量的
    /// </summary>
    public int tensor_count { get; init; }

    /// <summary>
    ///     总参数量的
    /// </summary>
    public long total_parameters { get; init; }

    /// <summary>
    ///     张量名称列表的
    /// </summary>
    public IReadOnlyList<string> tensor_names { get; init; } = [];

    /// <summary>
    ///     使用的数据类型列表的
    /// </summary>
    public IReadOnlyList<string> d_types { get; init; } = [];
}