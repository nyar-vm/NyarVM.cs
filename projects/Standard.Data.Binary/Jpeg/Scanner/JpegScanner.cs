using Std.Data.Binary.Jpeg.Data;

namespace Std.Data.Binary.Jpeg.Scanner;

/// <summary>
///     JPEG 文件扫描器，提供的JPEG 图像文件的快速帧扫描的
/// </summary>
/// <remarks>
///     JPEG 是有损压缩的位图格式，使的DCT 变换的Huffman 编码的
///     扫描器定的SOI 标记和帧偏移，不做完整的像素数据解码，以实现快速探查的
/// </remarks>
public ref struct JpegScanner
{
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>
    ///     初始的<see cref="JpegScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 JPEG 字节数据的/param>
    public JpegScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
    }

    /// <summary>
    ///     扫描 JPEG 数据，查找所的SOI 标记位置的
    /// </summary>
    /// <returns>SOI 标记偏移列表的/returns>
    public List<int> scan()
    {
        var offsets = new List<int>();

        for (var i = 0; i < _data.Length - 1; i++)
            if (_data[i] == 0xFF && _data[i + 1] == JpegConstants.soi_marker)
                offsets.Add(i);

        return offsets;
    }

    /// <summary>
    ///     快速判断数据是否为 JPEG 格式的
    /// </summary>
    /// <returns>是否的JPEG 格式的/returns>
    public bool is_jpeg()
    {
        if (_data.Length < 3) return false;

        return _data[0] == JpegConstants.signature[0]
               && _data[1] == JpegConstants.signature[1]
               && _data[2] == JpegConstants.signature[2];
    }
}