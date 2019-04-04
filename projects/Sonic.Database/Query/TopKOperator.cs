using Olympus.Athena.Core;

namespace Olympus.Athena.Query;

#region TopKOperator TopK 算子

/// <summary>
///     TopK 算子，使用最小堆维护 OrderBy + Limit 的 Top-K 结果
/// </summary>
public sealed class TopKOperator : VectorizedOperator
{
    #region 构造函数

    /// <summary>
    ///     创建 TopK 算子
    /// </summary>
    /// <param name="k">返回的行数</param>
    /// <param name="sortColumnIndex">排序列的索引</param>
    /// <param name="ascending">是否升序</param>
    public TopKOperator(int k, int sortColumnIndex, bool ascending = true)
    {
        _k = k;
        _sortColumnIndex = sortColumnIndex;
        _ascending = ascending;
        _inputRows = [];
        Results = [];
    }

    #endregion

    #region 属性

    /// <summary>
    ///     TopK 结果行集
    /// </summary>
    public List<DataValue[]> Results { get; }

    #endregion

    #region 执行

    /// <summary>
    ///     使用最小堆维护 TopK 结果
    /// </summary>
    public override void Execute()
    {
        CollectInputRows();

        if (_inputRows.Count == 0)
        {
            foreach (var child in Children) child.Execute();

            return;
        }

        var comparer = new RowComparer(_sortColumnIndex, _ascending);
        var heap = new PriorityQueue<DataValue[], DataValue[]>(comparer);

        foreach (var row in _inputRows)
        {
            if (_sortColumnIndex >= row.Length) continue;

            if (heap.Count < _k)
            {
                var cloned = (DataValue[])row.Clone();
                heap.Enqueue(cloned, cloned);
            }
            else
            {
                var peekPriority = heap.Peek();
                var cmp = comparer.Compare(row, peekPriority);

                if (_ascending ? cmp < 0 : cmp > 0)
                {
                    heap.Dequeue();
                    var cloned = (DataValue[])row.Clone();
                    heap.Enqueue(cloned, cloned);
                }
            }
        }

        Results.Capacity = heap.Count;
        while (heap.Count > 0) Results.Add(heap.Dequeue());

        if (_ascending) Results.Reverse();

        foreach (var child in Children) child.Execute();
    }

    #endregion

    #region 输入收集

    private void CollectInputRows()
    {
        foreach (var child in Children)
            switch (child)
            {
                case ProjectOperator projectOp:
                    _inputRows.AddRange(projectOp.Results);
                    break;
                case HashJoinOperator joinOp:
                    _inputRows.AddRange(joinOp.Results);
                    break;
                case AggregateOperator aggOp:
                {
                    foreach (var (key, result) in aggOp.Results) _inputRows.Add([key, result]);

                    break;
                }
            }
    }

    #endregion

    #region RowComparer

    private sealed class RowComparer : IComparer<DataValue[]>
    {
        private readonly bool _ascending;
        private readonly int _sortColumnIndex;

        public RowComparer(int sortColumnIndex, bool ascending)
        {
            _sortColumnIndex = sortColumnIndex;
            _ascending = ascending;
        }

        public int Compare(DataValue[]? x, DataValue[]? y)
        {
            if (x is null && y is null) return 0;

            if (x is null) return -1;

            if (y is null) return 1;

            if (_sortColumnIndex >= x.Length || _sortColumnIndex >= y.Length) return 0;

            var result = CompareDataValues(x[_sortColumnIndex], y[_sortColumnIndex]);
            return _ascending ? result : -result;
        }

        private static int CompareDataValues(DataValue a, DataValue b)
        {
            if (a.TryGetInt64(out var aInt) && b.TryGetInt64(out var bInt)) return aInt.CompareTo(bInt);

            if (a.TryGetFloat64(out var aFloat) && b.TryGetFloat64(out var bFloat)) return aFloat.CompareTo(bFloat);

            if (a.TryGetString(out var aStr) && b.TryGetString(out var bStr))
                return string.Compare(aStr, bStr, StringComparison.Ordinal);

            return 0;
        }
    }

    #endregion

    #region 字段

    private readonly int _k;
    private readonly int _sortColumnIndex;
    private readonly bool _ascending;
    private readonly List<DataValue[]> _inputRows;

    #endregion
}

#endregion