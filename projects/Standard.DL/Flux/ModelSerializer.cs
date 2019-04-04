using System.Text;
using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     模型持久化器 —— 将完整模型保存到文件 / 从文件加载
///     二进制格式：[magic][version][numParams][param0_name_len][param0_name][param0_shape_len][param0_shape][param0_data]...
/// </summary>
public static class ModelPersistence
{
    private const uint MagicNumber = 0x47414C41;
    private const ushort CurrentVersion = 1;

    /// <summary>
    ///     将模型参数保存到文件
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="paramNames">参数名称列表（可选，用于调试）</param>
    public static void SaveToFile(ITrainableModel model, string filePath, string[]? paramNames = null)
    {
        var paramList = model.Parameters().ToList();
        if (paramNames == null)
        {
            paramNames = new string[paramList.Count];
            for (var i = 0; i < paramList.Count; i++) paramNames[i] = $"p{i}";
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write(MagicNumber);
        writer.Write(CurrentVersion);
        writer.Write(paramList.Count);

        for (var i = 0; i < paramList.Count; i++)
        {
            var param = paramList[i];
            var data = param.Value;
            var name = i < paramNames.Length ? paramNames[i] : $"p{i}";

            writer.Write(name.Length);
            writer.Write(Encoding.UTF8.GetBytes(name));

            writer.Write(data.Shape.Length);
            for (var d = 0; d < data.Shape.Length; d++) writer.Write(data.Shape[d]);

            var span = data.AsSpan();
            writer.Write(span.Length);
            for (var j = 0; j < span.Length; j++) writer.Write(span[j]);
        }
    }

    /// <summary>
    ///     从文件加载模型参数
    /// </summary>
    /// <param name="model">模型（参数将被覆盖）</param>
    /// <param name="filePath">文件路径</param>
    /// <returns>参数名称列表</returns>
    public static string[] LoadFromFile(ITrainableModel model, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadUInt32();
        if (magic != MagicNumber) throw new InvalidDataException($"无效的文件格式：magic 0x{magic:X8}，期望 0x{MagicNumber:X8}");

        var version = reader.ReadUInt16();
        if (version > CurrentVersion) throw new InvalidDataException($"不支持的版本 {version}，当前版本 {CurrentVersion}");

        var numParams = reader.ReadInt32();
        var paramList = model.Parameters().ToList();
        var names = new string[numParams];

        for (var i = 0; i < numParams; i++)
        {
            var nameLen = reader.ReadInt32();
            var nameBytes = reader.ReadBytes(nameLen);
            names[i] = Encoding.UTF8.GetString(nameBytes);

            var shapeLen = reader.ReadInt32();
            var shape = new int[shapeLen];
            for (var d = 0; d < shapeLen; d++) shape[d] = reader.ReadInt32();

            var dataLen = reader.ReadInt32();
            var data = new float[dataLen];
            for (var j = 0; j < dataLen; j++) data[j] = reader.ReadSingle();

            if (i < paramList.Count)
            {
                var param = paramList[i];
                var span = param.Value.AsWriteSpan();
                for (var j = 0; j < span.Length && j < data.Length; j++)
                    span[j] = data[j];
            }
        }

        return names;
    }

    /// <summary>
    ///     将模型参数保存到字节数组
    /// </summary>
    /// <param name="model">模型</param>
    /// <returns>二进制数据</returns>
    public static byte[] SaveToBytes(ITrainableModel model)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var paramList = model.Parameters().ToList();

        writer.Write(MagicNumber);
        writer.Write(CurrentVersion);
        writer.Write(paramList.Count);

        for (var i = 0; i < paramList.Count; i++)
        {
            var param = paramList[i];
            var data = param.Value;

            var name = $"p{i}";
            writer.Write(name.Length);
            writer.Write(Encoding.UTF8.GetBytes(name));

            writer.Write(data.Shape.Length);
            for (var d = 0; d < data.Shape.Length; d++) writer.Write(data.Shape[d]);

            var span = data.AsSpan();
            writer.Write(span.Length);
            for (var j = 0; j < span.Length; j++) writer.Write(span[j]);
        }

        writer.Flush();
        return ms.ToArray();
    }

    /// <summary>
    ///     从字节数组加载模型参数
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="bytes">二进制数据</param>
    public static void LoadFromBytes(ITrainableModel model, byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);

        var magic = reader.ReadUInt32();
        if (magic != MagicNumber) throw new InvalidDataException($"无效的格式：magic 0x{magic:X8}");

        var version = reader.ReadUInt16();
        if (version > CurrentVersion) throw new InvalidDataException($"不支持的版本 {version}");

        var numParams = reader.ReadInt32();
        var paramList = model.Parameters().ToList();

        for (var i = 0; i < numParams; i++)
        {
            var nameLen = reader.ReadInt32();
            reader.ReadBytes(nameLen);

            var shapeLen = reader.ReadInt32();
            for (var d = 0; d < shapeLen; d++) reader.ReadInt32();

            var dataLen = reader.ReadInt32();
            var data = new float[dataLen];
            for (var j = 0; j < dataLen; j++) data[j] = reader.ReadSingle();

            if (i < paramList.Count)
            {
                var span = paramList[i].Value.AsWriteSpan();
                for (var j = 0; j < span.Length && j < data.Length; j++)
                    span[j] = data[j];
            }
        }
    }

    /// <summary>
    ///     计算模型序列化后的字节大小
    /// </summary>
    /// <param name="model">模型</param>
    /// <returns>估计字节数</returns>
    public static long EstimateSize(ITrainableModel model)
    {
        long size = 4 + 2 + 4;
        foreach (var p in model.Parameters())
        {
            var dataLen = p.Value.Shape.Aggregate(1, (a, b) => a * b);
            size += 4 + 8 + 4 + p.Value.Shape.Length * 4 + 4 + dataLen * 4;
        }

        return size;
    }
}