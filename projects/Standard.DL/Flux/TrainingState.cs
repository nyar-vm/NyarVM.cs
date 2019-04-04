namespace Std.DL.Flux;

/// <summary>
///     训练状态管理 —— 保存/恢复模型参数和优化器状态
///     支持断点续训、模型检查点
/// </summary>
public static class TrainingState
{
    /// <summary>
    ///     保存模型参数到字典
    /// </summary>
    /// <param name="parameters">可训练参数</param>
    /// <returns>参数名 → 数据的字典</returns>
    public static Dictionary<string, float[]> SaveParameters(IEnumerable<IParameter> parameters)
    {
        var state = new Dictionary<string, float[]>();
        var idx = 0;
        foreach (var p in parameters)
        {
            var span = p.Value.AsSpan();
            var data = new float[span.Length];
            span.CopyTo(data);
            state[$"param_{idx}"] = data;
            idx++;
        }

        return state;
    }

    /// <summary>
    ///     从字典恢复模型参数
    /// </summary>
    /// <param name="parameters">可训练参数</param>
    /// <param name="state">参数名 → 数据的字典</param>
    public static void LoadParameters(IEnumerable<IParameter> parameters, Dictionary<string, float[]> state)
    {
        var idx = 0;
        foreach (var p in parameters)
        {
            var key = $"param_{idx}";
            if (!state.TryGetValue(key, out var data)) continue;

            var span = p.Value.AsWriteSpan();
            var len = System.Math.Min(span.Length, data.Length);
            for (var i = 0; i < len; i++) span[i] = data[i];
            idx++;
        }
    }

    /// <summary>
    ///     计算参数校验和（用于验证保存/加载正确性）
    /// </summary>
    /// <param name="parameters">可训练参数</param>
    /// <returns>校验和</returns>
    public static float ParameterChecksum(IEnumerable<IParameter> parameters)
    {
        var sum = 0.0f;
        foreach (var p in parameters)
        {
            var span = p.Value.AsSpan();
            for (var i = 0; i < span.Length; i++) sum += span[i];
        }

        return sum;
    }

    /// <summary>
    ///     保存训练检查点（参数 + 元数据）
    /// </summary>
    /// <param name="parameters">可训练参数</param>
    /// <param name="epoch">当前 epoch</param>
    /// <param name="loss">当前损失</param>
    /// <returns>检查点字典</returns>
    public static Dictionary<string, object> SaveCheckpoint(IEnumerable<IParameter> parameters, int epoch, float loss)
    {
        var checkpoint = new Dictionary<string, object>
        {
            ["epoch"] = epoch,
            ["loss"] = loss,
            ["params"] = SaveParameters(parameters)
        };
        return checkpoint;
    }

    /// <summary>
    ///     从检查点恢复训练
    /// </summary>
    /// <param name="parameters">可训练参数</param>
    /// <param name="checkpoint">检查点字典</param>
    /// <returns>恢复的 (epoch, loss)</returns>
    public static (int Epoch, float Loss) LoadCheckpoint(IEnumerable<IParameter> parameters,
        Dictionary<string, object> checkpoint)
    {
        LoadParameters(parameters, (Dictionary<string, float[]>)checkpoint["params"]);
        var epoch = (int)checkpoint["epoch"];
        var loss = (float)checkpoint["loss"];
        return (epoch, loss);
    }
}