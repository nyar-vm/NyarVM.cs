using System.Text;

namespace Std.DL.Data;

/// <summary>
///     快速包格式 .gfpk 的读写器
/// </summary>
public static class FastPack
{
    private const uint Magic = 0x31304647;

    /// <summary>
    ///     从 .glpx 的 record source 构建 .gfpk 单文件
    /// </summary>
    public static void Write(string outputPath, DatasetSchema schema,
        int recordCount, Func<int, byte[]> recordSource)
    {
        using var fs = new FileStream(outputPath, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        var fields = schema.AllFields.ToArray();
        var recordBytes = (ulong)fields.Sum(f => f.ByteSize);

        bw.Write(Magic);
        bw.Write((ushort)1);
        bw.Write((ushort)0);
        bw.Write((ulong)recordCount);
        bw.Write(recordBytes);
        bw.Write((byte)fields.Length);
        bw.Write((byte)0);
        bw.Write(0UL);
        bw.Write(0UL);
        bw.Write(0UL);
        bw.Write(new byte[32]);

        var fieldTableOff = (ulong)fs.Position;
        foreach (var f in fields)
        {
            var nameBytes = Encoding.UTF8.GetBytes(f.Name);
            bw.Write((byte)nameBytes.Length);
            bw.Write(nameBytes);
            bw.Write(f.TypeCode);
            bw.Write((byte)f.Shape.Length);
            foreach (var dim in f.Shape) bw.Write((ulong)dim);
        }

        var indexOff = (ulong)fs.Position;
        var dataOff = indexOff + (ulong)recordCount * 12UL;

        fs.Seek((long)dataOff, SeekOrigin.Begin);
        var records = new (ulong Offset, uint Size)[recordCount];
        for (var i = 0; i < recordCount; i++)
        {
            var record = recordSource(i);
            records[i] = ((ulong)fs.Position, (uint)record.Length);
            bw.Write(record);
        }

        fs.Seek((long)indexOff, SeekOrigin.Begin);
        foreach (var r in records)
        {
            bw.Write(r.Offset);
            bw.Write(r.Size);
        }

        fs.Seek(30, SeekOrigin.Begin);
        bw.Write(fieldTableOff);
        bw.Write(indexOff);
        bw.Write(dataOff);
    }

    /// <summary>
    ///     .gfpk 头部
    /// </summary>
    public sealed class Header
    {
        /// <summary>记录总数</summary>
        public ulong RecordCount { get; init; }

        /// <summary>每记录总字节数</summary>
        public ulong RecordBytes { get; init; }

        /// <summary>字段数量</summary>
        public byte FieldCount { get; init; }

        /// <summary>压缩级别：0=无压缩，1=zstd</summary>
        public byte Compression { get; init; }

        /// <summary>字段表偏移</summary>
        public ulong FieldTableOff { get; init; }

        /// <summary>记录索引偏移</summary>
        public ulong IndexOff { get; init; }

        /// <summary>数据区偏移</summary>
        public ulong DataOff { get; init; }

        /// <summary>字段定义列表</summary>
        public FieldDef[] Fields { get; init; } = [];
    }
}