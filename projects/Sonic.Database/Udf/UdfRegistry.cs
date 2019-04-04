namespace Olympus.Athena.Udf;

#region UdfRegistry 注册表

/// <summary>
///     UDF 注册表，管理标量 UDF 和聚合 UDF 的注册、查找与注销
/// </summary>
public sealed class UdfRegistry
{
    private readonly Dictionary<string, IAggregateUdf> _aggregateUdfs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IScalarUdf> _scalarUdfs = new(StringComparer.OrdinalIgnoreCase);

    #region 注销

    /// <summary>
    ///     注销指定名称的 UDF（同时检查标量和聚合注册表）
    /// </summary>
    /// <param name="name">UDF 名称</param>
    /// <returns>成功注销返回 <c>true</c>，未找到返回 <c>false</c></returns>
    public bool Unregister(string name)
    {
        if (_scalarUdfs.Remove(name)) return true;

        return _aggregateUdfs.Remove(name);
    }

    #endregion

    #region 属性

    /// <summary>
    ///     获取已注册的标量 UDF 名称列表
    /// </summary>
    public IReadOnlyCollection<string> ScalarNames => _scalarUdfs.Keys;

    /// <summary>
    ///     获取已注册的聚合 UDF 名称列表
    /// </summary>
    public IReadOnlyCollection<string> AggregateNames => _aggregateUdfs.Keys;

    #endregion

    #region 注册

    /// <summary>
    ///     注册一个标量 UDF
    /// </summary>
    /// <param name="udf">标量 UDF 实例</param>
    public void Register(IScalarUdf udf)
    {
        _scalarUdfs[udf.Name] = udf;
    }

    /// <summary>
    ///     注册一个聚合 UDF
    /// </summary>
    /// <param name="udf">聚合 UDF 实例</param>
    public void Register(IAggregateUdf udf)
    {
        _aggregateUdfs[udf.Name] = udf;
    }

    #endregion

    #region 查找

    /// <summary>
    ///     根据名称查找标量 UDF
    /// </summary>
    /// <param name="name">UDF 名称</param>
    /// <returns>找到的 UDF 实例，未找到时返回 <c>null</c></returns>
    public IScalarUdf? GetScalar(string name)
    {
        _scalarUdfs.TryGetValue(name, out var udf);
        return udf;
    }

    /// <summary>
    ///     根据名称查找聚合 UDF
    /// </summary>
    /// <param name="name">UDF 名称</param>
    /// <returns>找到的 UDF 实例，未找到时返回 <c>null</c></returns>
    public IAggregateUdf? GetAggregate(string name)
    {
        _aggregateUdfs.TryGetValue(name, out var udf);
        return udf;
    }

    #endregion
}

#endregion