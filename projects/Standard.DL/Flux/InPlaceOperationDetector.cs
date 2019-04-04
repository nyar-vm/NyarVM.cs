namespace Std.DL.Flux;

/// <summary>
///     原地操作检测器：在反向传播前验证张量数据一致性
///     通过版本号检测前向计算后被原地修改的张量
/// </summary>
public sealed class InPlaceOperationDetector
{
    private readonly Dictionary<ArrayND, int> _snapshots = new();

    /// <summary>
    ///     快照指定张量的当前版本号
    ///     通常在前向计算完成后、反向传播前调用
    /// </summary>
    /// <param name="tensors">需要监控的张量列表</param>
    public void Snapshot(IEnumerable<ArrayND> tensors)
    {
        _snapshots.Clear();
        foreach (var tensor in tensors) _snapshots[tensor] = tensor.Version;
    }

    /// <summary>
    ///     快照单个张量的当前版本号
    /// </summary>
    public void Snapshot(ArrayND tensor)
    {
        _snapshots[tensor] = tensor.Version;
    }

    /// <summary>
    ///     检测自快照以来是否有张量被原地修改
    /// </summary>
    /// <returns>被原地修改的张量列表（空列表表示无修改）</returns>
    public List<ArrayND> DetectInPlaceModifications()
    {
        var modified = new List<ArrayND>();
        foreach (var (tensor, version) in _snapshots)
            if (tensor.Version != version)
                modified.Add(tensor);

        return modified;
    }

    /// <summary>
    ///     验证所有被监控的张量未被原地修改
    ///     如果检测到原地修改，抛出异常
    /// </summary>
    /// <exception cref="InvalidOperationException">检测到原地修改时抛出</exception>
    public void Validate()
    {
        var modified = DetectInPlaceModifications();
        if (modified.Count > 0)
        {
            var shapes = string.Join(", ", modified.Select(t => $"[{string.Join(",", t.Shape)}]"));
            throw new InvalidOperationException(
                $"检测到 {modified.Count} 个张量在前向计算后被原地修改（版本号变化），" +
                $"反向传播结果可能不正确。被修改张量形状：{shapes}。");
        }
    }

    /// <summary>
    ///     清空所有快照
    /// </summary>
    public void Clear()
    {
        _snapshots.Clear();
    }
}