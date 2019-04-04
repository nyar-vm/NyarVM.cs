using System.Text;

namespace Std.DL.Data;

/// <summary>
///     Galatea 打包格式 .galx 的读写器
///     每个 split 一个 .galx 文件（如 train.galx, test.galx）
///     支持 zstd 块级压缩、mmap 随机访问 O(1)、追加写入
///     压缩策略：按目标未压缩字节数（默认 1MB）自适应 chunk，整块压缩
/// </summary>
public static class GalxPack
{
    /// <summary>
    ///     魔数 "GA01"
    /// </summary>
    public const uint Magic = 0x31304147;

    /// <summary>
    ///     头部固定大小（字节）
    /// </summary>
    public const int HeaderSize = 64;

    /// <summary>
    ///     默认 chunk 目标大小（未压缩字节数，1 MiB）
    ///     zstd 在此大小下压缩率和速度平衡最优
    /// </summary>
    public const int DefaultChunkBytes = 1 * 1024 * 1024;

    /// <summary>
    ///     Chunk 磁盘对齐粒度（4 KiB，匹配 OS 页大小和 SSD 块大小）
    ///     每个压缩 chunk 的起始偏移对齐到此值，确保每次读盘都是整页读取
    /// </summary>
    public const int ChunkAlignment = 4096;

    /// <summary>
    ///     从头创建 .galx 文件（zstd 压缩）
    ///     文件布局：Header → FieldTable → Data(chunks) → Index
    ///     每个 chunk 包含最多 DefaultChunkSize 条记录，整块压缩
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="schema">数据集 Schema</param>
    /// <param name="recordCount">记录数量</param>
    /// <param name="recordSource">回调：recordIndex → 原始字节</param>
    /// <param name="compression">压缩级别：0=无压缩，1=zstd</param>
    public static void Write(string outputPath, DatasetSchema schema,
        int recordCount, Func<int, byte[]> recordSource, byte compression = 1)
    {
        var fields = schema.AllFields.ToArray();
        var recordBytes = (ulong)fields.Sum(f => f.ByteSize);

        using var fs = new FileStream(outputPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        WriteHeaderPlaceholder(bw);

        var fieldTableOff = (ulong)fs.Position;
        WriteFieldTable(bw, fields);

        if (compression == 1) AlignToBoundary(bw);

        var dataOff = (ulong)fs.Position;
        var indices = new RecordIndex[recordCount];

        if (compression == 1)
            WriteCompressedChunks(bw, recordSource, recordCount, dataOff, indices);
        else
            for (var i = 0; i < recordCount; i++)
            {
                var record = recordSource(i);
                indices[i] = new RecordIndex
                {
                    Offset = (ulong)fs.Position - dataOff,
                    CompressedSize = (uint)record.Length,
                    RawSize = (uint)record.Length
                };
                bw.Write(record);
            }

        var indexOff = (ulong)fs.Position;
        WriteIndex(bw, indices);

        fs.Seek(0, SeekOrigin.Begin);
        WriteHeader(bw, new Header
        {
            RecordCount = (ulong)recordCount,
            RecordBytes = recordBytes,
            FieldCount = (byte)fields.Length,
            Compression = compression,
            FieldTableOff = fieldTableOff,
            DataOff = dataOff,
            IndexOff = indexOff,
            Fields = fields
        });
    }

    /// <summary>
    ///     向已有 .galx 文件追加记录
    ///     读取现有头部和索引，追加数据后重建索引
    /// </summary>
    /// <param name="galxPath">已有 .galx 文件路径</param>
    /// <param name="newRecords">新记录的原始字节数组</param>
    public static void Append(string galxPath, byte[][] newRecords)
    {
        Header header;
        RecordIndex[] existingIndices;

        using (var readFs = new FileStream(galxPath, FileMode.Open, FileAccess.Read))
        using (var br = new BinaryReader(readFs))
        {
            header = ReadHeader(br);
            existingIndices = ReadIndex(br, header);
        }

        using var fs = new FileStream(galxPath, FileMode.Open, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        fs.Seek((long)header.IndexOff, SeekOrigin.Begin);

        var allIndices = new RecordIndex[existingIndices.Length + newRecords.Length];
        existingIndices.CopyTo(allIndices, 0);

        if (header.Compression == 1)
        {
            AlignToBoundary(bw);
            var newIndices = new RecordIndex[newRecords.Length];
            WriteCompressedChunks(bw, i => newRecords[i], newRecords.Length, header.DataOff, newIndices);
            Array.Copy(newIndices, 0, allIndices, existingIndices.Length, newRecords.Length);
        }
        else
        {
            for (var i = 0; i < newRecords.Length; i++)
            {
                var record = newRecords[i];
                allIndices[existingIndices.Length + i] = new RecordIndex
                {
                    Offset = (ulong)fs.Position - header.DataOff,
                    CompressedSize = (uint)record.Length,
                    RawSize = (uint)record.Length
                };
                bw.Write(record);
            }
        }

        var newIndexOff = (ulong)fs.Position;
        WriteIndex(bw, allIndices);

        fs.Seek(0, SeekOrigin.Begin);
        WriteHeader(bw, new Header
        {
            RecordCount = (ulong)allIndices.Length,
            RecordBytes = header.RecordBytes,
            FieldCount = header.FieldCount,
            Compression = header.Compression,
            FieldTableOff = header.FieldTableOff,
            DataOff = header.DataOff,
            IndexOff = newIndexOff,
            Fields = header.fields
        });

        fs.SetLength(fs.Position);
    }

    /// <summary>
    ///     读取 .galx 文件头部
    /// </summary>
    /// <param name="galxPath">.galx 文件路径</param>
    /// <returns>头部信息</returns>
    public static Header ReadHeader(string galxPath)
    {
        using var fs = new FileStream(galxPath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        return ReadHeader(br);
    }

    /// <summary>
    ///     读取 .galx 文件的全部记录索引
    /// </summary>
    /// <param name="galxPath">.galx 文件路径</param>
    /// <returns>记录索引数组</returns>
    public static RecordIndex[] ReadIndex(string galxPath)
    {
        using var fs = new FileStream(galxPath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        var header = ReadHeader(br);
        return ReadIndex(br, header);
    }

    /// <summary>
    ///     读取 .galx 文件中指定索引的记录（自动解压，适合训练随机采样）
    ///     按 chunk 分组，每个 chunk 只解压一次，结果按输入顺序返回
    /// </summary>
    /// <param name="galxPath">.galx 文件路径</param>
    /// <param name="indices">要读取的记录索引数组（可重复、可乱序）</param>
    /// <returns>记录原始字节数组，与 indices 一一对应</returns>
    public static byte[][] ReadRecords(string galxPath, int[] indices)
    {
        using var fs = new FileStream(galxPath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        var header = ReadHeader(br);
        var allIndices = ReadIndex(br, header);

        var result = new byte[indices.Length][];

        if (header.Compression == 1)
        {
            var chunkCache = new Dictionary<ulong, byte[]>();
            var groups = GroupByChunk(indices, allIndices);

            foreach (var (chunkOffset, group) in groups)
            {
                if (!chunkCache.TryGetValue(chunkOffset, out var decompressed))
                {
                    var absOffset = (long)(header.DataOff + chunkOffset);
                    fs.Seek(absOffset, SeekOrigin.Begin);
                    var compressedSize = allIndices[group[0].RecordIndex].CompressedSize;
                    var compressed = br.ReadBytes((int)compressedSize);
                    decompressed = ZstdCodec.Decompress(compressed);
                    chunkCache[chunkOffset] = decompressed;
                }

                var chunkStartIdx = FindChunkStart(group[0].RecordIndex, allIndices);

                var runningOffset = 0;
                for (var r = chunkStartIdx; r <= group[^1].RecordIndex; r++)
                {
                    if (r == group[0].RecordIndex) group[0].ByteOffset = runningOffset;

                    for (var g = 1; g < group.Count; g++)
                        if (r == group[g].RecordIndex)
                            group[g].ByteOffset = runningOffset;

                    runningOffset += (int)allIndices[r].RawSize;
                }

                foreach (var entry in group)
                {
                    result[entry.OriginalIndex] = new byte[allIndices[entry.RecordIndex].RawSize];
                    Array.Copy(decompressed, entry.ByteOffset, result[entry.OriginalIndex],
                        0, (int)allIndices[entry.RecordIndex].RawSize);
                }
            }
        }
        else
        {
            for (var i = 0; i < indices.Length; i++)
            {
                var ri = indices[i];
                var absOffset = (long)(header.DataOff + allIndices[ri].Offset);
                fs.Seek(absOffset, SeekOrigin.Begin);
                result[i] = br.ReadBytes((int)allIndices[ri].CompressedSize);
            }
        }

        return result;
    }

    /// <summary>
    ///     按 chunk 分组请求的索引
    /// </summary>
    private static Dictionary<ulong, List<IndexGroupEntry>> GroupByChunk(
        int[] indices, RecordIndex[] allIndices)
    {
        var groups = new Dictionary<ulong, List<IndexGroupEntry>>();
        for (var i = 0; i < indices.Length; i++)
        {
            var ri = indices[i];
            if (ri < 0 || ri >= allIndices.Length)
                throw new ArgumentOutOfRangeException(nameof(indices), $"记录索引超出范围：{ri}");

            var chunkOffset = allIndices[ri].Offset;
            if (!groups.TryGetValue(chunkOffset, out var list))
            {
                list = [];
                groups[chunkOffset] = list;
            }

            list.Add(new IndexGroupEntry { OriginalIndex = i, RecordIndex = ri });
        }

        foreach (var group in groups.Values) group.Sort((a, b) => a.RecordIndex.CompareTo(b.RecordIndex));

        return groups;
    }

    /// <summary>
    ///     找到包含 recordIdx 的 chunk 的起始索引
    /// </summary>
    private static int FindChunkStart(int recordIdx, RecordIndex[] allIndices)
    {
        var targetOffset = allIndices[recordIdx].Offset;
        var startIdx = recordIdx;
        while (startIdx > 0 && allIndices[startIdx - 1].Offset == targetOffset) startIdx--;

        return startIdx;
    }

    /// <summary>
    ///     读取 .galx 文件中指定范围的记录（自动解压）
    ///     压缩模式下按 chunk 解压后按偏移切分，避免重复解压
    /// </summary>
    /// <param name="galxPath">.galx 文件路径</param>
    /// <param name="startIndex">起始记录索引</param>
    /// <param name="count">记录数量</param>
    /// <returns>记录原始字节数组</returns>
    public static byte[][] ReadRecords(string galxPath, int startIndex, int count)
    {
        using var fs = new FileStream(galxPath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        var header = ReadHeader(br);
        var indices = ReadIndex(br, header);

        var result = new byte[count][];

        if (header.Compression == 1)
        {
            var chunkCache = new Dictionary<ulong, byte[]>();

            for (var i = 0; i < count; i++)
            {
                var idx = startIndex + i;
                if (idx >= (int)header.RecordCount)
                    throw new ArgumentOutOfRangeException(nameof(startIndex), $"记录索引超出范围：{idx}");

                var index = indices[idx];
                if (!chunkCache.TryGetValue(index.Offset, out var decompressed))
                {
                    var absOffset = (long)(header.DataOff + index.Offset);
                    fs.Seek(absOffset, SeekOrigin.Begin);
                    var compressed = br.ReadBytes((int)index.CompressedSize);
                    decompressed = ZstdCodec.Decompress(compressed);
                    chunkCache[index.Offset] = decompressed;
                }

                var recordOffset = 0;
                for (var j = idx; j > 0 && indices[j - 1].Offset == index.Offset; j--)
                    recordOffset += (int)indices[j - 1].RawSize;

                result[i] = new byte[index.RawSize];
                Array.Copy(decompressed, recordOffset, result[i], 0, (int)index.RawSize);
            }
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                var idx = startIndex + i;
                if (idx >= (int)header.RecordCount)
                    throw new ArgumentOutOfRangeException(nameof(startIndex), $"记录索引超出范围：{idx}");

                var absOffset = (long)(header.DataOff + indices[idx].Offset);
                fs.Seek(absOffset, SeekOrigin.Begin);
                result[i] = br.ReadBytes((int)indices[idx].CompressedSize);
            }
        }

        return result;
    }

    /// <summary>
    ///     读取 .galx 文件的全部记录（自动解压）
    /// </summary>
    /// <param name="galxPath">.galx 文件路径</param>
    /// <returns>记录原始字节数组</returns>
    public static byte[][] ReadAllRecords(string galxPath)
    {
        var header = ReadHeader(galxPath);
        return ReadRecords(galxPath, 0, (int)header.RecordCount);
    }

    /// <summary>
    ///     按自适应 chunk 压缩写入记录
    ///     每个 chunk 累计到 DefaultChunkBytes 为止，整块压缩后写入
    ///     索引记录每条记录的压缩块偏移、压缩大小和原始大小
    /// </summary>
    private static void WriteCompressedChunks(BinaryWriter bw, Func<int, byte[]> recordSource,
        int recordCount, ulong dataOff, RecordIndex[] indices)
    {
        var chunkStart = 0;
        while (chunkStart < recordCount)
        {
            var raw = new List<byte>();
            var rawSizes = new List<int>();
            var i = chunkStart;

            while (i < recordCount && raw.Count < DefaultChunkBytes)
            {
                var record = recordSource(i);
                rawSizes.Add(record.Length);
                raw.AddRange(record);
                i++;
            }

            if (i == chunkStart && i < recordCount)
            {
                var record = recordSource(i);
                rawSizes.Add(record.Length);
                raw.AddRange(record);
                i++;
            }

            var compressed = ZstdCodec.Compress(raw.ToArray());
            var chunkOffset = (ulong)bw.BaseStream.Position - dataOff;

            bw.Write(compressed);
            AlignToBoundary(bw);

            for (var r = 0; r < rawSizes.Count; r++)
                indices[chunkStart + r] = new RecordIndex
                {
                    Offset = chunkOffset,
                    CompressedSize = (uint)compressed.Length,
                    RawSize = (uint)rawSizes[r]
                };

            chunkStart = i;
        }
    }

    /// <summary>
    ///     将当前流位置对齐到下一个 ChunkAlignment 边界（填充 0）
    /// </summary>
    private static void AlignToBoundary(BinaryWriter bw)
    {
        var pos = (ulong)bw.BaseStream.Position;
        var aligned = (pos + ChunkAlignment - 1) / ChunkAlignment * ChunkAlignment;
        var padding = (int)(aligned - pos);
        if (padding > 0) bw.Write(new byte[padding]);
    }

    /// <summary>
    ///     验证 .galx 文件中所有 chunk 的磁盘对齐状态
    ///     返回 (对齐的 chunk 数, 总 chunk 数)，以及不对齐的 chunk 详情
    /// </summary>
    /// <param name="galxPath">.galx 文件路径</param>
    /// <returns>对齐验证结果</returns>
    public static AlignmentReport VerifyAlignment(string galxPath)
    {
        var header = ReadHeader(galxPath);
        var indices = ReadIndex(galxPath);
        if (indices.Length == 0) return new AlignmentReport { Aligned = 0, Total = 0, MisalignedOffsets = [] };

        var chunkOffsets = new HashSet<ulong>();
        foreach (var idx in indices) chunkOffsets.Add(idx.Offset);

        var dataStart = header.DataOff;
        var misaligned = new List<(ulong ChunkOffset, ulong AbsoluteOffset, ulong Remainder)>();

        foreach (var chunkOff in chunkOffsets)
        {
            var absOff = dataStart + chunkOff;
            var remainder = absOff % ChunkAlignment;
            if (remainder != 0) misaligned.Add((chunkOff, absOff, remainder));
        }

        return new AlignmentReport
        {
            Aligned = chunkOffsets.Count - misaligned.Count,
            Total = chunkOffsets.Count,
            MisalignedOffsets = [.. misaligned]
        };
    }

    private static void WriteHeaderPlaceholder(BinaryWriter bw)
    {
        bw.Write(Magic);
        bw.Write((ushort)1);
        bw.Write((ushort)0);
        bw.Write(0UL);
        bw.Write(0UL);
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write(0UL);
        bw.Write(0UL);
        bw.Write(0UL);
        bw.Write(new byte[32]);
    }

    private static void WriteHeader(BinaryWriter bw, Header header)
    {
        bw.Write(Magic);
        bw.Write((ushort)1);
        bw.Write((ushort)0);
        bw.Write(header.RecordCount);
        bw.Write(header.RecordBytes);
        bw.Write(header.FieldCount);
        bw.Write(header.Compression);
        bw.Write(header.FieldTableOff);
        bw.Write(header.DataOff);
        bw.Write(header.IndexOff);
        bw.Write(new byte[32]);
    }

    private static Header ReadHeader(BinaryReader br)
    {
        var magic = br.ReadUInt32();
        if (magic != Magic) throw new InvalidDataException($"无效的 .galx 魔数：0x{magic:X8}，期望 0x{Magic:X8}");

        var version = br.ReadUInt16();
        var flags = br.ReadUInt16();
        var recordCount = br.ReadUInt64();
        var recordBytes = br.ReadUInt64();
        var fieldCount = br.ReadByte();
        var compression = br.ReadByte();
        var fieldTableOff = br.ReadUInt64();
        var dataOff = br.ReadUInt64();
        var indexOff = br.ReadUInt64();
        br.ReadBytes(32);

        var fields = new FieldDef[fieldCount];
        var savedPos = br.BaseStream.Position;
        br.BaseStream.Seek((long)fieldTableOff, SeekOrigin.Begin);

        for (var i = 0; i < fieldCount; i++)
        {
            var nameLen = br.ReadByte();
            var nameBytes = br.ReadBytes(nameLen);
            var name = Encoding.UTF8.GetString(nameBytes);
            var typeCode = br.ReadByte();
            var ndim = br.ReadByte();
            var shape = new int[ndim];
            for (var d = 0; d < ndim; d++) shape[d] = (int)br.ReadUInt64();

            fields[i] = new FieldDef { Name = name, TypeCode = typeCode, Shape = shape };
        }

        br.BaseStream.Seek(savedPos, SeekOrigin.Begin);

        return new Header
        {
            RecordCount = recordCount,
            RecordBytes = recordBytes,
            FieldCount = fieldCount,
            Compression = compression,
            FieldTableOff = fieldTableOff,
            DataOff = dataOff,
            IndexOff = indexOff,
            Fields = fields
        };
    }

    private static void WriteFieldTable(BinaryWriter bw, FieldDef[] fields)
    {
        foreach (var f in fields)
        {
            var nameBytes = Encoding.UTF8.GetBytes(f.Name);
            bw.Write((byte)nameBytes.Length);
            bw.Write(nameBytes);
            bw.Write(f.TypeCode);
            bw.Write((byte)f.Shape.Length);
            foreach (var dim in f.Shape) bw.Write((ulong)dim);
        }
    }

    private static void WriteIndex(BinaryWriter bw, RecordIndex[] indices)
    {
        foreach (var idx in indices)
        {
            bw.Write(idx.Offset);
            bw.Write(idx.CompressedSize);
            bw.Write(idx.RawSize);
        }
    }

    private static RecordIndex[] ReadIndex(BinaryReader br, Header header)
    {
        br.BaseStream.Seek((long)header.IndexOff, SeekOrigin.Begin);

        var indices = new RecordIndex[header.RecordCount];
        for (var i = 0; i < (int)header.RecordCount; i++)
            indices[i] = new RecordIndex
            {
                Offset = br.ReadUInt64(),
                CompressedSize = br.ReadUInt32(),
                RawSize = br.ReadUInt32()
            };

        return indices;
    }

    /// <summary>
    ///     .galx 文件头部
    /// </summary>
    public sealed class Header
    {
        /// <summary>记录总数</summary>
        public ulong RecordCount { get; init; }

        /// <summary>每记录原始字节数（固定长度时 > 0，变长时 = 0）</summary>
        public ulong RecordBytes { get; init; }

        /// <summary>字段数量</summary>
        public byte FieldCount { get; init; }

        /// <summary>压缩级别：0=无压缩，1=zstd</summary>
        public byte Compression { get; init; }

        /// <summary>字段表偏移</summary>
        public ulong FieldTableOff { get; init; }

        /// <summary>数据区偏移</summary>
        public ulong DataOff { get; init; }

        /// <summary>索引区偏移（文件末尾，追加友好）</summary>
        public ulong IndexOff { get; init; }

        /// <summary>字段定义列表</summary>
        public FieldDef[] Fields { get; init; } = [];
    }

    /// <summary>
    ///     记录索引条目
    /// </summary>
    public sealed class RecordIndex
    {
        /// <summary>该记录在数据区的字节偏移（压缩后）</summary>
        public ulong Offset { get; init; }

        /// <summary>压缩后字节数（0 表示与下一条记录之间的距离）</summary>
        public uint CompressedSize { get; init; }

        /// <summary>原始未压缩字节数</summary>
        public uint RawSize { get; init; }
    }

    /// <summary>
    ///     索引分组条目
    /// </summary>
    private sealed class IndexGroupEntry
    {
        /// <summary>在原始请求数组中的位置</summary>
        public int OriginalIndex { get; init; }

        /// <summary>记录索引</summary>
        public int RecordIndex { get; init; }

        /// <summary>在解压块内的字节偏移（计算时填入）</summary>
        public int ByteOffset { get; set; }
    }

    /// <summary>
    ///     磁盘对齐验证报告
    /// </summary>
    public sealed class AlignmentReport
    {
        /// <summary>已对齐的 chunk 数量</summary>
        public int Aligned { get; init; }

        /// <summary>总 chunk 数量</summary>
        public int Total { get; init; }

        /// <summary>未对齐的 chunk 偏移列表</summary>
        public (ulong ChunkOffset, ulong AbsoluteOffset, ulong Remainder)[] MisalignedOffsets { get; init; } = [];

        /// <summary>是否全部对齐</summary>
        public bool IsPerfect => Aligned == Total;
    }
}