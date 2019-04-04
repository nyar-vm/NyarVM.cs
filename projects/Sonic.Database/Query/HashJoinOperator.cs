using Nyar.Dialect.Query;
using Olympus.Athena.Core;

namespace Olympus.Athena.Query;

#region HashJoinOperator 哈希连接算子

/// <summary>
///     哈希连接算子，使用哈希表实现等值连接
/// </summary>
public sealed class HashJoinOperator : VectorizedOperator
{
    #region 构造函数

    /// <summary>
    ///     创建哈希连接算子
    /// </summary>
    /// <param name="leftColumnIndex">左表连接列的索引</param>
    /// <param name="rightColumnIndex">右表连接列的索引</param>
    /// <param name="joinType">连接类型</param>
    public HashJoinOperator(int leftColumnIndex, int rightColumnIndex, JoinKind joinType)
    {
        _leftColumnIndex = leftColumnIndex;
        _rightColumnIndex = rightColumnIndex;
        _joinType = joinType;
        _leftRows = [];
        _rightRows = [];
        Results = [];
    }

    #endregion

    #region 属性

    /// <summary>
    ///     连接后的结果行集
    /// </summary>
    public List<DataValue[]> Results { get; }

    #endregion

    #region 执行

    /// <summary>
    ///     构建哈希表并执行哈希连接
    /// </summary>
    public override void Execute()
    {
        CollectInputRows();

        var hashTable = BuildHashTable();

        foreach (var leftRow in _leftRows)
        {
            if (_leftColumnIndex >= leftRow.Length) continue;

            var key = leftRow[_leftColumnIndex];

            if (hashTable.TryGetValue(key, out var matchingRows))
            {
                foreach (var rightRow in matchingRows)
                {
                    var joined = new DataValue[leftRow.Length + rightRow.Length];
                    Array.Copy(leftRow, 0, joined, 0, leftRow.Length);
                    Array.Copy(rightRow, 0, joined, leftRow.Length, rightRow.Length);
                    Results.Add(joined);
                }
            }
            else if (_joinType == JoinKind.left)
            {
                var joined = new DataValue[leftRow.Length + 1];
                Array.Copy(leftRow, 0, joined, 0, leftRow.Length);
                joined[leftRow.Length] = default;
                Results.Add(joined);
            }
        }

        if (_joinType == JoinKind.right)
            foreach (var rightRow in _rightRows)
            {
                if (_rightColumnIndex >= rightRow.Length) continue;

                var key = rightRow[_rightColumnIndex];
                var found = false;

                foreach (var leftRow in _leftRows)
                    if (_leftColumnIndex < leftRow.Length && leftRow[_leftColumnIndex].Equals(key))
                    {
                        found = true;
                        break;
                    }

                if (!found)
                {
                    var joined = new DataValue[_rightRows[0].Length + 1];
                    Array.Copy(rightRow, 0, joined, 1, rightRow.Length);
                    joined[0] = default;
                    Results.Add(joined);
                }
            }

        foreach (var child in Children) child.Execute();
    }

    #endregion

    #region 字段

    private readonly int _leftColumnIndex;
    private readonly int _rightColumnIndex;
    private readonly JoinKind _joinType;
    private readonly List<DataValue[]> _leftRows;
    private readonly List<DataValue[]> _rightRows;

    #endregion

    #region 内部方法

    private Dictionary<DataValue, List<DataValue[]>> BuildHashTable()
    {
        var table = new Dictionary<DataValue, List<DataValue[]>>();

        foreach (var rightRow in _rightRows)
        {
            if (_rightColumnIndex >= rightRow.Length) continue;

            var key = rightRow[_rightColumnIndex];

            if (!table.TryGetValue(key, out var list))
            {
                list = [];
                table[key] = list;
            }

            list.Add(rightRow);
        }

        return table;
    }

    private void CollectInputRows()
    {
        foreach (var child in Children)
            switch (child)
            {
                case ProjectOperator projectOp:
                {
                    if (_leftRows.Count == 0)
                        _leftRows.AddRange(projectOp.Results);
                    else
                        _rightRows.AddRange(projectOp.Results);

                    break;
                }
            }
    }

    #endregion
}

#endregion