using Olympus.Athena.Core;
using Olympus.Athena.Storage;

namespace Olympus.Athena.Query;

#region ProjectOperator 投影算子

/// <summary>
///     投影算子，从列式数据中提取指定列并转换为行式结果
/// </summary>
public sealed class ProjectOperator : VectorizedOperator
{
    #region 构造函数

    /// <summary>
    ///     创建投影算子
    /// </summary>
    /// <param name="columnIndexMap">列名到列数据索引的映射</param>
    /// <param name="projectedColumns">需要投影的列名列表</param>
    /// <param name="columnData">列数据字典（索引 → 列数据对象），用于实际读取数据</param>
    /// <param name="rowCount">行数</param>
    public ProjectOperator(IReadOnlyDictionary<string, int> columnIndexMap, IReadOnlyList<string> projectedColumns,
        Dictionary<int, object> columnData, int rowCount)
    {
        _columnIndexMap = columnIndexMap;
        _projectedColumns = projectedColumns;
        _columnData = columnData;
        _rowCount = rowCount;
        Results = [];
    }

    #endregion

    #region 属性

    /// <summary>
    ///     投影后的行集，每行为 <see cref="DataValue" /> 数组
    /// </summary>
    public List<DataValue[]> Results { get; }

    #endregion

    #region 执行

    /// <summary>
    ///     从列式数据中提取投影列并转换为行式结果
    /// </summary>
    public override void Execute()
    {
        var projectedIndices = new List<int>();
        foreach (var colName in _projectedColumns)
            if (_columnIndexMap.TryGetValue(colName, out var idx))
                projectedIndices.Add(idx);

        if (projectedIndices.Count == 0)
        {
            foreach (var child in Children) child.Execute();

            return;
        }

        var columnReaders = new List<Func<int, DataValue>>();
        foreach (var colIdx in projectedIndices)
            if (_columnData.TryGetValue(colIdx, out var chunkObj))
                columnReaders.Add(CreateReader(chunkObj));
            else
                columnReaders.Add(_ => default);

        for (var row = 0; row < _rowCount; row++)
        {
            var rowValues = new DataValue[projectedIndices.Count];
            for (var col = 0; col < projectedIndices.Count; col++) rowValues[col] = columnReaders[col](row);

            Results.Add(rowValues);
        }

        foreach (var child in Children) child.Execute();
    }

    #endregion

    #region 内部方法

    private static Func<int, DataValue> CreateReader(object chunkObj)
    {
        var type = chunkObj.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ColumnChunk<>))
        {
            var elementType = type.GetGenericArguments()[0];

            if (elementType == typeof(long))
            {
                var values = ((ColumnChunk<long>)chunkObj).GetValues();
                return row => values[row];
            }

            if (elementType == typeof(double))
            {
                var values = ((ColumnChunk<double>)chunkObj).GetValues();
                return row => values[row];
            }

            if (elementType == typeof(bool))
            {
                var values = ((ColumnChunk<bool>)chunkObj).GetValues();
                return row => values[row];
            }

            if (elementType == typeof(Guid))
            {
                var values = ((ColumnChunk<Guid>)chunkObj).GetValues();
                return row => values[row];
            }
        }

        return _ => default;
    }

    #endregion

    #region 字段

    private readonly IReadOnlyDictionary<string, int> _columnIndexMap;
    private readonly IReadOnlyList<string> _projectedColumns;
    private readonly Dictionary<int, object> _columnData;
    private readonly int _rowCount;

    #endregion
}

#endregion