using Std.DL.Flux;

namespace Std.DL.Engram;

/// <summary>
///     模型参数序列化器 —— 将模型可训练参数序列化为字节数组，
///     支持保存/加载/校验
/// </summary>
public static class ModelSerializer
{
    private const int Magic = 0x4D4F444C;
    private const int Version = 1;

    /// <summary>
    ///     将模型参数序列化为字节数组
    /// </summary>
    /// <param name="parameters">模型参数列表</param>
    /// <returns>序列化后的字节数组</returns>
    public static byte[] Serialize(IEnumerable<IParameter> parameters)
    {
        var paramList = parameters.ToList();

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(paramList.Count);

        foreach (var p in paramList)
        {
            var data = p.Value.AsSpan();
            writer.Write(p.Value.Shape.Length);
            foreach (var dim in p.Value.Shape) writer.Write(dim);
            writer.Write(data.Length);
            for (var i = 0; i < data.Length; i++) writer.Write(data[i]);
        }

        return ms.ToArray();
    }

    /// <summary>
    ///     从字节数组反序列化参数张量
    /// </summary>
    /// <param name="data">序列化的字节数据</param>
    /// <returns>张量列表</returns>
    public static IReadOnlyList<ArrayND> Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var magic = reader.ReadInt32();
        if (magic != Magic) throw new InvalidOperationException($"无效的模型数据：magic 不匹配 0x{magic:X8}");

        var version = reader.ReadInt32();
        if (version != Version) throw new InvalidOperationException($"不支持的模型格式版本：{version}");

        var count = reader.ReadInt32();
        var tensors = new List<ArrayND>(count);

        for (var p = 0; p < count; p++)
        {
            var dimCount = reader.ReadInt32();
            var shape = new int[dimCount];
            for (var d = 0; d < dimCount; d++) shape[d] = reader.ReadInt32();

            var dataLen = reader.ReadInt32();
            var values = new float[dataLen];
            for (var i = 0; i < dataLen; i++) values[i] = reader.ReadSingle();

            tensors.Add(ArrayND.FromArray(values, shape));
        }

        return tensors;
    }

    /// <summary>
    ///     将反序列化的张量加载到参数中
    /// </summary>
    /// <param name="parameters">目标参数列表</param>
    /// <param name="tensors">加载的张量列表</param>
    public static void LoadInto(IReadOnlyList<IParameter> parameters, IReadOnlyList<ArrayND> tensors)
    {
        if (parameters.Count != tensors.Count)
            throw new InvalidOperationException(
                $"参数数量不匹配：期望 {parameters.Count}，实际 {tensors.Count}");

        for (var i = 0; i < parameters.Count; i++)
        {
            var srcSpan = tensors[i].AsSpan();
            var dstSpan = parameters[i].Value.AsWriteSpan();

            if (srcSpan.Length != dstSpan.Length)
                throw new InvalidOperationException(
                    $"参数 {i} 大小不匹配：期望 {dstSpan.Length}，实际 {srcSpan.Length}");

            for (var j = 0; j < srcSpan.Length; j++) dstSpan[j] = srcSpan[j];
        }
    }

    /// <summary>
    ///     验证序列化/反序列化往返正确性
    /// </summary>
    /// <param name="parameters">原参数列表</param>
    /// <param name="deserialized">反序列化的张量</param>
    /// <returns>最大逐元素误差</returns>
    public static float VerifyRoundTrip(IReadOnlyList<IParameter> parameters, IReadOnlyList<ArrayND> deserialized)
    {
        var maxError = 0.0f;

        for (var i = 0; i < parameters.Count; i++)
        {
            var srcSpan = parameters[i].Value.AsSpan();
            var dstSpan = deserialized[i].AsSpan();

            for (var j = 0; j < srcSpan.Length; j++) maxError = MathF.Max(maxError, MathF.Abs(srcSpan[j] - dstSpan[j]));
        }

        return maxError;
    }
}