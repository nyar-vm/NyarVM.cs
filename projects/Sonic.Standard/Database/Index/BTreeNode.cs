using Std.Database.Core;

namespace Std.Database.Index;

/// <summary>
///     B+ 树节点
/// </summary>
internal sealed class BTreeNode
{
    /// <summary>
    ///     是否为叶子节点
    /// </summary>
    public bool is_leaf { get; set; }

    /// <summary>
    ///     键列表
    /// </summary>
    public List<DatabaseKey> keys { get; } = [];

    /// <summary>
    ///     值列表（仅叶子节点）
    /// </summary>
    public List<DatabaseValue> values { get; } = [];

    /// <summary>
    ///     每个键值对的写入序列号（仅叶子节点），用于 MVCC 可见性判断
    /// </summary>
    public List<SequenceNumber> sequences { get; } = [];

    /// <summary>
    ///     子节点引用（仅内部节点）
    /// </summary>
    public List<long> children { get; } = [];

    /// <summary>
    ///     右兄弟节点页面 ID（仅叶子节点）
    /// </summary>
    public long next_sibling { get; set; } = -1;

    /// <summary>
    ///     节点是否已满
    /// </summary>
    /// <param name="order">B+ 树阶数</param>
    /// <returns>是否已满</returns>
    public bool is_full(int order)
    {
        return keys.Count >= order;
    }

    /// <summary>
    ///     节点是否下溢（键数不足）
    /// </summary>
    /// <param name="order">B+ 树阶数</param>
    /// <returns>是否下溢</returns>
    public bool is_underflow(int order)
    {
        return keys.Count < order / 2;
    }

    /// <summary>
    ///     计算节点序列化后的预估字节大小
    ///     格式：IsLeaf(1) + keyCount(4) + Σ(4+keyLen) + [leaf: Σ(4+valLen) + Σ(8) + 8(NextSibling) | internal: 4 + Σ(8)]
    /// </summary>
    /// <returns>序列化后的字节大小</returns>
    public int get_serialized_size()
    {
        var size = 1 + 4;

        foreach (var key in keys) size += 4 + key.length;

        if (is_leaf)
        {
            foreach (var value in values) size += 4 + value.bytes.Length;

            size += sequences.Count * 8;
            size += 8;
        }
        else
        {
            size += 4;
            size += children.Count * 8;
        }

        return size;
    }
}