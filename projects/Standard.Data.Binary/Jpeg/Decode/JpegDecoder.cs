using Std.Data.Binary.Jpeg.Data;

namespace Std.Data.Binary.Jpeg.Decode;

/// <summary>
///     JPEG Baseline DCT 解码器——纯 C# 实现，无第三方依赖的
/// </summary>
/// <remarks>
///     实现的JPEG Baseline (SOF0) 解码，支的YCbCr 4:4:4/4:2:0/4:2:2 采样的
///     Huffman 解码、IDCT、颜色空间转换的
///     不支的Progressive (SOF2)、算术编码、分层编码的
/// </remarks>
public ref struct JpegDecoder
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    private int _width;
    private int _height;
    private int _precision;
    private JpegComponent[] _components;
    private int _max_h;
    private int _max_v;
    private readonly byte[][] _quant_tables;
    private readonly JpegHuffmanTable[] _dc_tables;
    private readonly JpegHuffmanTable[] _ac_tables;
    private readonly int[] _dc_predictors;

    /// <summary>
    ///     初始的<see cref="JpegDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">JPEG 二进制数据的/param>
    public JpegDecoder(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
        _components = [];
        _max_h = 1;
        _max_v = 1;
        _quant_tables = new byte[4][];
        _dc_tables = new JpegHuffmanTable[4];
        _ac_tables = new JpegHuffmanTable[4];
        _dc_predictors = new int[4];
    }

    /// <summary>
    ///     解码 JPEG 图像为像素数据的
    /// </summary>
    /// <returns>JPEG 图像数据的/returns>
    public JpegImageData decode()
    {
        parse_markers();

        var mcuWidth = (_width + _max_h * 8 - 1) / (_max_h * 8);
        var mcuHeight = (_height + _max_v * 8 - 1) / (_max_v * 8);

        var componentCount = _components.Length;
        var channelData = new float[componentCount][];

        for (var c = 0; c < componentCount; c++)
        {
            var comp = _components[c];
            var fullW = mcuWidth * comp.h * 8;
            var fullH = mcuHeight * comp.v * 8;
            channelData[c] = new float[fullW * fullH];
        }

        var bitReader = new JpegBitReader(_data, _position);

        for (var mcuY = 0; mcuY < mcuHeight; mcuY++)
        for (var mcuX = 0; mcuX < mcuWidth; mcuX++)
        for (var c = 0; c < componentCount; c++)
        {
            var comp = _components[c];
            var qt = _quant_tables[comp.quant_table_id];
            var dcTable = _dc_tables[comp.dc_table_id];
            var acTable = _ac_tables[comp.ac_table_id];

            for (var v = 0; v < comp.v; v++)
            for (var h = 0; h < comp.h; h++)
            {
                var block = new float[64];
                decode_block(ref bitReader, dcTable, acTable, qt, ref _dc_predictors[c], block);
                idct(block);

                var blockX = mcuX * comp.h + h;
                var blockY = mcuY * comp.v + v;
                var baseX = blockX * 8;
                var baseY = blockY * 8;
                var stride = mcuWidth * comp.h * 8;

                for (var y = 0; y < 8; y++)
                for (var x = 0; x < 8; x++)
                {
                    var val = block[y * 8 + x] + 128f;
                    channelData[c][(baseY + y) * stride + baseX + x] = System.Math.Clamp(val, 0, 255);
                }
            }
        }

        var pixelData = new byte[_width * _height * 4];
        var colorSpace = componentCount == 1 ? JpegColorSpace.grayscale : JpegColorSpace.y_cb_cr;

        if (componentCount == 1)
            for (var y = 0; y < _height; y++)
            for (var x = 0; x < _width; x++)
            {
                var srcIdx = y * mcuWidth * _max_h * 8 + x;
                var gray = (byte)channelData[0][srcIdx];
                var dstIdx = (y * _width + x) * 4;
                pixelData[dstIdx] = gray;
                pixelData[dstIdx + 1] = gray;
                pixelData[dstIdx + 2] = gray;
                pixelData[dstIdx + 3] = 255;
            }
        else
            for (var y = 0; y < _height; y++)
            for (var x = 0; x < _width; x++)
            {
                var yIdx = y * mcuWidth * _max_h * 8 + x;
                var cbX = x * _components[1].h / _max_h;
                var cbY = y * _components[1].v / _max_v;
                var cbStride = mcuWidth * _components[1].h * 8;
                var cbIdx = cbY * cbStride + cbX;
                var crX = x * _components[2].h / _max_h;
                var crY = y * _components[2].v / _max_v;
                var crStride = mcuWidth * _components[2].h * 8;
                var crIdx = crY * crStride + crX;

                var yVal = channelData[0][yIdx];
                var cbVal = channelData[1][cbIdx] - 128f;
                var crVal = channelData[2][crIdx] - 128f;

                var r = yVal + 1.402f * crVal;
                var g = yVal - 0.344136f * cbVal - 0.714136f * crVal;
                var b = yVal + 1.772f * cbVal;

                var dstIdx = (y * _width + x) * 4;
                pixelData[dstIdx] = (byte)System.Math.Clamp(r, 0, 255);
                pixelData[dstIdx + 1] = (byte)System.Math.Clamp(g, 0, 255);
                pixelData[dstIdx + 2] = (byte)System.Math.Clamp(b, 0, 255);
                pixelData[dstIdx + 3] = 255;
            }

        var componentInfos = new JpegComponentInfo[componentCount];

        for (var i = 0; i < componentCount; i++)
        {
            var comp = _components[i];
            componentInfos[i] = new JpegComponentInfo
            {
                id = comp.id,
                h = comp.h,
                v = comp.v,
                quant_table_id = comp.quant_table_id,
                dc_table_id = comp.dc_table_id,
                ac_table_id = comp.ac_table_id
            };
        }

        return new JpegImageData
        {
            width = _width,
            height = _height,
            precision = _precision,
            color_space = colorSpace,
            components = componentInfos,
            pixel_data = pixelData,
            is_progressive = false
        };
    }

    #region 标记解析

    private void parse_markers()
    {
        while (_position < _data.Length - 1)
        {
            if (_data[_position++] != 0xFF) continue;

            var marker = _data[_position++];

            switch (marker)
            {
                case 0xD8: break;
                case 0xD9: return;
                case 0x00: break;
                case >= 0xD0 and <= 0xD7:
                    _dc_predictors[(marker - 0xD0) & 3] = 0;
                    break;
                case 0xDB: parse_dqt(); break;
                case 0xC0: parse_sof0(); break;
                case 0xC4: parse_dht(); break;
                case 0xDA:
                    parse_sos();
                    return;
                default:
                    if (marker is not (0xD0 or 0xD1 or 0xD2 or 0xD3 or 0xD4 or 0xD5 or 0xD6 or 0xD7))
                        if (_position + 1 < _data.Length)
                        {
                            var len = (_data[_position] << 8) | _data[_position + 1];
                            _position += len;
                        }

                    break;
            }
        }
    }

    private void parse_sof0()
    {
        var len = read_u16();
        _precision = _data[_position++];
        _height = read_u16();
        _width = read_u16();
        var numComponents = _data[_position++];

        _components = new JpegComponent[numComponents];
        _max_h = 0;
        _max_v = 0;

        for (var i = 0; i < numComponents; i++)
        {
            var id = _data[_position++];
            var sampling = _data[_position++];
            var qtId = _data[_position++];
            var h = (sampling >> 4) & 0x0F;
            var v = sampling & 0x0F;

            _components[i] = new JpegComponent(id, h, v, qtId);
            if (h > _max_h) _max_h = h;

            if (v > _max_v) _max_v = v;
        }
    }

    private void parse_dqt()
    {
        var len = read_u16();
        var end = _position + len - 2;

        while (_position < end)
        {
            var info = _data[_position++];
            var tableId = info & 0x0F;
            var precision = (info >> 4) & 0x0F;

            _quant_tables[tableId] = new byte[64];
            if (precision == 0)
                for (var i = 0; i < 64; i++)
                    _quant_tables[tableId][i] = _data[_position++];
            else
                for (var i = 0; i < 64; i++)
                {
                    _position += 2;
                    _quant_tables[tableId][i] = _data[_position - 2];
                }
        }
    }

    private void parse_dht()
    {
        var len = read_u16();
        var end = _position + len - 2;

        while (_position < end)
        {
            var info = _data[_position++];
            var tableClass = (info >> 4) & 0x0F;
            var tableId = info & 0x0F;

            var counts = new int[16];
            var totalSymbols = 0;
            for (var i = 0; i < 16; i++)
            {
                counts[i] = _data[_position++];
                totalSymbols += counts[i];
            }

            var symbols = new byte[totalSymbols];
            for (var i = 0; i < totalSymbols; i++) symbols[i] = _data[_position++];

            var table = build_huffman_table(counts, symbols);

            if (tableClass == 0)
                _dc_tables[tableId] = table;
            else
                _ac_tables[tableId] = table;
        }
    }

    private void parse_sos()
    {
        var len = read_u16();
        var numComponents = _data[_position++];

        for (var i = 0; i < numComponents; i++)
        {
            var componentId = _data[_position++];
            var tableInfo = _data[_position++];

            for (var c = 0; c < _components.Length; c++)
                if (_components[c].id == componentId)
                {
                    _components[c].dc_table_id = (tableInfo >> 4) & 0x0F;
                    _components[c].ac_table_id = tableInfo & 0x0F;
                    break;
                }
        }

        _position += 3;
    }

    #endregion

    #region 块解的

    private static void decode_block(ref JpegBitReader reader, JpegHuffmanTable dcTable,
        JpegHuffmanTable acTable, byte[] quantTable, ref int dcPredictor, float[] block)
    {
        Array.Clear(block);

        var dcCategory = decode_huffman_symbol(ref reader, dcTable);
        var dcDiff = read_extend(ref reader, dcCategory);
        dcPredictor += dcDiff;
        block[0] = dcPredictor * quantTable[0];

        var k = 1;
        while (k < 64)
        {
            var acSymbol = decode_huffman_symbol(ref reader, acTable);
            if (acSymbol == 0) break;

            var run = acSymbol >> 4;
            var category = acSymbol & 0x0F;
            k += run;

            if (k >= 64) break;

            if (category > 0)
            {
                var value = read_extend(ref reader, category);
                var zigIdx = k;
                if (zigIdx < 64) block[_zig_zag_order[zigIdx]] = value * quantTable[_zig_zag_order[zigIdx]];
            }

            k++;
        }
    }

    private static int decode_huffman_symbol(ref JpegBitReader reader, JpegHuffmanTable table)
    {
        var node = table.root;
        while (node != null)
        {
            if (node.symbol >= 0) return node.symbol;

            var bit = reader.read_bit();
            node = bit == 0 ? node.zero : node.one;
        }

        return 0;
    }

    private static int read_extend(ref JpegBitReader reader, int category)
    {
        if (category == 0) return 0;

        var value = reader.read_bits(category);
        var threshold = 1 << (category - 1);
        if (value < threshold) value += (-1 << category) + 1;

        return value;
    }

    private static void idct(float[] block)
    {
        var temp = new float[64];

        for (var i = 0; i < 8; i++)
        {
            var x0 = block[i * 8];
            var x1 = block[i * 8 + 4];
            var x2 = block[i * 8 + 2];
            var x3 = block[i * 8 + 6];
            var x4 = block[i * 8 + 1];
            var x5 = block[i * 8 + 5];
            var x6 = block[i * 8 + 3];
            var x7 = block[i * 8 + 7];

            var x07 = x0 + x7;
            var x16 = x1 + x6;
            var x25 = x2 + x5;
            var x34 = x3 + x4;
            var x07M = x0 - x7;
            var x16M = x1 - x6;
            var x25M = x2 - x5;
            var x34M = x3 - x4;

            var even0 = x07 + x34;
            var even1 = x16 + x25;
            var even2 = x07 - x34;
            var even3 = x16 - x25;

            temp[i * 8] = even0 + even1;
            temp[i * 8 + 4] = even0 - even1;
            temp[i * 8 + 2] = even2 * 1.4142135f + even3 * 0.7071068f;
            temp[i * 8 + 6] = even2 * 1.4142135f - even3 * 0.7071068f;

            temp[i * 8 + 1] = x07M * 0.7071068f + x16M * 0.9238795f + x25M * 0.3826834f + x34M * 0.7071068f;
            temp[i * 8 + 3] = x07M * 0.7071068f + x16M * 0.3826834f - x25M * 0.9238795f - x34M * 0.7071068f;
            temp[i * 8 + 5] = x07M * 0.7071068f - x16M * 0.3826834f - x25M * 0.9238795f + x34M * 0.7071068f;
            temp[i * 8 + 7] = x07M * 0.7071068f - x16M * 0.9238795f + x25M * 0.3826834f - x34M * 0.7071068f;
        }

        for (var i = 0; i < 8; i++)
        {
            var x0 = temp[i];
            var x1 = temp[i + 32];
            var x2 = temp[i + 16];
            var x3 = temp[i + 48];
            var x4 = temp[i + 8];
            var x5 = temp[i + 40];
            var x6 = temp[i + 24];
            var x7 = temp[i + 56];

            var x07 = x0 + x7;
            var x16 = x1 + x6;
            var x25 = x2 + x5;
            var x34 = x3 + x4;
            var x07M = x0 - x7;
            var x16M = x1 - x6;
            var x25M = x2 - x5;
            var x34M = x3 - x4;

            var even0 = x07 + x34;
            var even1 = x16 + x25;

            block[i] = (even0 + even1) / 8f;
            block[i + 32] = (even0 - even1) / 8f;
            block[i + 16] = ((x07 - x34) * 1.4142135f + (x16 - x25) * 0.7071068f) / 8f;
            block[i + 48] = ((x07 - x34) * 1.4142135f - (x16 - x25) * 0.7071068f) / 8f;

            block[i + 8] = (x07M * 0.7071068f + x16M * 0.9238795f + x25M * 0.3826834f + x34M * 0.7071068f) / 8f;
            block[i + 24] = (x07M * 0.7071068f + x16M * 0.3826834f - x25M * 0.9238795f - x34M * 0.7071068f) / 8f;
            block[i + 40] = (x07M * 0.7071068f - x16M * 0.3826834f - x25M * 0.9238795f + x34M * 0.7071068f) / 8f;
            block[i + 56] = (x07M * 0.7071068f - x16M * 0.9238795f + x25M * 0.3826834f - x34M * 0.7071068f) / 8f;
        }
    }

    #endregion

    #region Huffman 表构的

    private static JpegHuffmanTable build_huffman_table(int[] counts, byte[] symbols)
    {
        var root = new JpegHuffmanNode();
        var symbolIdx = 0;
        var code = 0;

        for (var len = 1; len <= 16; len++)
        {
            for (var i = 0; i < counts[len - 1]; i++)
            {
                if (symbolIdx >= symbols.Length) break;

                var node = root;
                for (var bit = len - 1; bit >= 0; bit--)
                {
                    var b = (code >> bit) & 1;
                    if (b == 0)
                    {
                        node.zero ??= new JpegHuffmanNode();
                        node = node.zero;
                    }
                    else
                    {
                        node.one ??= new JpegHuffmanNode();
                        node = node.one;
                    }
                }

                node.symbol = symbols[symbolIdx];
                symbolIdx++;
                code++;
            }

            code <<= 1;
        }

        return new JpegHuffmanTable(root);
    }

    #endregion

    #region 辅助方法

    private ushort read_u16()
    {
        var value = (ushort)((_data[_position] << 8) | _data[_position + 1]);
        _position += 2;
        return value;
    }

    private static readonly int[] _zig_zag_order =
    [
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    ];

    #endregion
}

#region 内部类型

internal sealed class JpegHuffmanTable
{
    /// <summary>
    ///     初始的<see cref="JpegHuffmanTable" /> 类的新实例的
    /// </summary>
    /// <param name="root">Huffman 树根节点的/param>
    public JpegHuffmanTable(JpegHuffmanNode root)
    {
        this.root = root;
    }

    /// <summary>
    ///     Huffman 树根节点的
    /// </summary>
    public JpegHuffmanNode root { get; }
}

internal sealed class JpegHuffmanNode
{
    /// <summary>
    ///     1 分支子节点的
    /// </summary>
    public JpegHuffmanNode? one;

    /// <summary>
    ///     符号值，-1 表示非叶节点的
    /// </summary>
    public int symbol = -1;

    /// <summary>
    ///     0 分支子节点的
    /// </summary>
    public JpegHuffmanNode? zero;
}

internal sealed class JpegComponent
{
    /// <summary>
    ///     交流 Huffman 表标识符的
    /// </summary>
    public int ac_table_id;

    /// <summary>
    ///     直流 Huffman 表标识符的
    /// </summary>
    public int dc_table_id;

    /// <summary>
    ///     水平采样因子的
    /// </summary>
    public int h;

    /// <summary>
    ///     分量标识符的
    /// </summary>
    public int id;

    /// <summary>
    ///     量化表标识符的
    /// </summary>
    public int quant_table_id;

    /// <summary>
    ///     垂直采样因子的
    /// </summary>
    public int v;

    /// <summary>
    ///     初始的<see cref="JpegComponent" /> 类的新实例的
    /// </summary>
    /// <param name="id">
    ///     分量标识符的/param>
    ///     <param name="h">
    ///         水平采样因子的/param>
    ///         <param name="v">
    ///             垂直采样因子的/param>
    ///             <param name="qtId">量化表标识符的/param>
    public JpegComponent(int id, int h, int v, int qtId)
    {
        this.id = id;
        this.h = h;
        this.v = v;
        quant_table_id = qtId;
    }
}

internal ref struct JpegBitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _byte_pos;
    private int _bit_pos;

    /// <summary>
    ///     初始的<see cref="JpegBitReader" /> 结构的新实例的
    /// </summary>
    /// <param name="data">
    ///     JPEG 扫描数据的/param>
    ///     <param name="startPos">起始字节位置的/param>
    public JpegBitReader(ReadOnlySpan<byte> data, int startPos)
    {
        _data = data;
        _byte_pos = startPos;
        _bit_pos = 0;
    }

    /// <summary>
    ///     读取单个比特的
    /// </summary>
    /// <returns>比特值（0 的1）的/returns>
    public int read_bit()
    {
        if (_byte_pos >= _data.Length) return 0;

        var bit = (_data[_byte_pos] >> (7 - _bit_pos)) & 1;
        _bit_pos++;

        if (_bit_pos >= 8)
        {
            _bit_pos = 0;
            _byte_pos++;

            if (_byte_pos < _data.Length && _data[_byte_pos] == 0xFF)
                if (_byte_pos + 1 < _data.Length && _data[_byte_pos + 1] == 0x00)
                    _byte_pos++;
        }

        return bit;
    }

    /// <summary>
    ///     读取指定数量的比特的
    /// </summary>
    /// <param name="count">
    ///     要读取的比特数的/param>
    ///     <returns>读取的整数值的/returns>
    public int read_bits(int count)
    {
        var value = 0;
        for (var i = 0; i < count; i++) value = (value << 1) | read_bit();

        return value;
    }
}

#endregion