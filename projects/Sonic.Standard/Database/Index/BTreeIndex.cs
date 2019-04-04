using Std.Database.Core;
using Std.Database.Storage;

namespace Std.Database.Index;

/// <summary>
///     B+ 树索引实现，优化版：内部节点二分查找、零分配序列化
/// </summary>
internal sealed class BTreeIndex : IBTreeIndex
{
    private readonly AsyncReaderWriterLock _index_lock = new();
    private readonly int _order;
    private readonly IPageCache _page_cache;
    private readonly VersionStore? _version_store;

    /// <summary>
    ///     创建 B+ 树索引
    /// </summary>
    /// <param name="pageCache">页面缓存</param>
    /// <param name="name">索引名称</param>
    /// <param name="order">阶数</param>
    /// <param name="versionStore">版本存储</param>
    public BTreeIndex(IPageCache pageCache, string name, int order = 128, VersionStore? versionStore = null)
    {
        _page_cache = pageCache;
        _version_store = versionStore;
        this.name = name;

        var maxOrder = System.Math.Max(4, (_page_cache.page_size - 13) / 48);
        _order = System.Math.Min(order, maxOrder);
        root_page_id = -1;
    }

    /// <summary>
    ///     根页面 ID（用于持久化和恢复）
    /// </summary>
    public long root_page_id { get; private set; } = -1;

    /// <inheritdoc />
    public string name { get; }

    #region 插入

    /// <inheritdoc />
    public async ValueTask insert(DatabaseKey key, DatabaseValue value, SequenceNumber sequence = default)
    {
        await _index_lock.enter_write_lock(CancellationToken.None);
        try
        {
            if (root_page_id < 0)
            {
                var newRoot = await _page_cache.allocate_page();
                var rootId = newRoot.id;
                root_page_id = rootId;

                var rootNode = new BTreeNode { is_leaf = true };
                serialize_node(rootNode, newRoot.data);
                _page_cache.mark_dirty(rootId);
            }

            var currentPageId = root_page_id;
            var parentPageId = -1L;
            var indexInParent = -1;

            while (true)
            {
                var page = await _page_cache.get_page(currentPageId);
                var node = deserialize_node(page.data);

                if (node.is_leaf)
                {
                    var idx = node.keys.BinarySearch(key);
                    var wouldOverflow = would_overflow_page(node, key, value, idx);

                    if (node.keys.Count < _order && !wouldOverflow)
                    {
                        if (idx >= 0)
                        {
                            _version_store?.add_version(key, node.sequences[idx], node.values[idx]);

                            node.values[idx] = value;
                            node.sequences[idx] = sequence;
                        }
                        else
                        {
                            var insertIndex = ~idx;
                            node.keys.Insert(insertIndex, key);
                            node.values.Insert(insertIndex, value);
                            node.sequences.Insert(insertIndex, sequence);
                        }

                        serialize_node(node, page.data);
                        _page_cache.mark_dirty(currentPageId);
                        return;
                    }

                    if (parentPageId < 0)
                    {
                        await split_root();
                        currentPageId = root_page_id;
                        parentPageId = -1;
                        indexInParent = -1;
                        continue;
                    }

                    var parentPage = await _page_cache.get_page(parentPageId);
                    var parentNode = deserialize_node(parentPage.data);

                    if (parentNode.is_full(_order))
                    {
                        await split_root();
                        currentPageId = root_page_id;
                        parentPageId = -1;
                        indexInParent = -1;
                        continue;
                    }

                    await split_child(parentPageId, indexInParent);

                    var updatedParentPage = await _page_cache.get_page(parentPageId);
                    var updatedParentNode = deserialize_node(updatedParentPage.data);
                    var newChildIndex = find_child_index(updatedParentNode, key);
                    currentPageId = updatedParentNode.children[newChildIndex];
                    indexInParent = newChildIndex;
                    continue;
                }

                var childIndex = find_child_index(node, key);

                if (childIndex >= node.children.Count) return;

                var childPageId = node.children[childIndex];
                var childPage = await _page_cache.get_page(childPageId);
                var childNode = deserialize_node(childPage.data);

                if (childNode.is_full(_order))
                {
                    if (node.is_full(_order))
                    {
                        if (parentPageId < 0)
                        {
                            await split_root();
                            currentPageId = root_page_id;
                            parentPageId = -1;
                            indexInParent = -1;
                            continue;
                        }

                        var pp = await _page_cache.get_page(parentPageId);
                        var pn = deserialize_node(pp.data);

                        if (pn.is_full(_order))
                        {
                            await split_root();
                            currentPageId = root_page_id;
                            parentPageId = -1;
                            indexInParent = -1;
                            continue;
                        }

                        await split_child(parentPageId, indexInParent);
                        currentPageId = root_page_id;
                        parentPageId = -1;
                        indexInParent = -1;
                        continue;
                    }

                    await split_child(currentPageId, childIndex);

                    page = await _page_cache.get_page(currentPageId);
                    node = deserialize_node(page.data);
                    if (key.CompareTo(node.keys[childIndex]) > 0) childIndex++;
                }

                parentPageId = currentPageId;
                indexInParent = childIndex;
                currentPageId = node.children[childIndex];
            }
        }
        finally
        {
            _index_lock.exit_write_lock();
        }
    }

    #endregion

    /// <summary>
    ///     设置根页面 ID（仅用于从元数据恢复）
    /// </summary>
    /// <param name="rootPageId">根页面 ID</param>
    public void restore_root_page_id(long rootPageId)
    {
        root_page_id = rootPageId;
    }

    #region 物理删除（旧 API，保留兼容）

    private async ValueTask<bool> delete_node(long pageId, DatabaseKey key)
    {
        var page = await _page_cache.get_page(pageId);
        var node = deserialize_node(page.data);

        if (node.is_leaf)
        {
            var index = node.keys.BinarySearch(key);
            if (index < 0) return false;

            _version_store?.add_version(key, node.sequences[index], node.values[index]);

            node.keys.RemoveAt(index);
            node.values.RemoveAt(index);
            node.sequences.RemoveAt(index);
            serialize_node(node, page.data);
            _page_cache.mark_dirty(pageId);
            return true;
        }

        var childIndex = find_child_index(node, key);

        if (childIndex >= node.children.Count) return false;

        var childPageId = node.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);

        if (childNode.is_underflow(_order))
        {
            await ensure_child_not_underflow(pageId, childIndex);

            page = await _page_cache.get_page(pageId);
            node = deserialize_node(page.data);

            if (childIndex >= node.children.Count) return false;

            childPageId = node.children[childIndex];
            childPage = await _page_cache.get_page(childPageId);
            childNode = deserialize_node(childPage.data);
        }

        var deleted = await delete_node(childPageId, key);
        if (!deleted) return false;

        childPage = await _page_cache.get_page(childPageId);
        childNode = deserialize_node(childPage.data);

        if (childNode.is_underflow(_order)) await handle_underflow(pageId, childIndex);

        return true;
    }

    #endregion

    #region 核心辅助

    /// <summary>
    ///     在内部节点中二分查找键对应的子节点索引
    ///     返回应该前往的子节点在 Children 列表中的位置
    /// </summary>
    private static int find_child_index(BTreeNode node, DatabaseKey key)
    {
        var result = node.keys.BinarySearch(key);
        return result >= 0 ? result + 1 : ~result;
    }

    /// <summary>
    ///     判断在节点中插入或替换键值对后，序列化大小是否会超过页面容量
    /// </summary>
    /// <param name="node">目标节点</param>
    /// <param name="key">要插入或替换的键</param>
    /// <param name="value">要插入或替换的值</param>
    /// <param name="existingIndex">已有键的索引（替换场景），-1 表示新插入</param>
    /// <returns>是否会溢出页面</returns>
    private bool would_overflow_page(BTreeNode node, DatabaseKey key, DatabaseValue value, int existingIndex)
    {
        var newSize = node.get_serialized_size();

        if (existingIndex >= 0)
            newSize = newSize - node.values[existingIndex].bytes.Length + value.bytes.Length;
        else
            newSize += 4 + key.length + 4 + value.bytes.Length + 8;

        return newSize > _page_cache.page_size;
    }

    #endregion

    #region 查找

    /// <inheritdoc />
    public async ValueTask<DatabaseValue?> search(DatabaseKey key, SequenceNumber? asOfSequence = null)
    {
        if (root_page_id < 0) return null;

        await _index_lock.enter_read_lock(CancellationToken.None);
        try
        {
            var (value, sequence) = await search_node_with_sequence(root_page_id, key);

            if (value is null) return null;

            if (!asOfSequence.HasValue) return value is { is_tombstone: true } ? null : value;

            if (sequence.value <= asOfSequence.Value.value) return value is { is_tombstone: true } ? null : value;

            if (_version_store is not null)
            {
                var historicalValue = _version_store.get_visible_value(key, asOfSequence.Value);
                if (historicalValue is not null && historicalValue.Value.is_tombstone) return null;

                return historicalValue;
            }

            return null;
        }
        finally
        {
            _index_lock.exit_read_lock();
        }
    }

    private async ValueTask<(DatabaseValue? Value, SequenceNumber Sequence)> search_node_with_sequence(long pageId,
        DatabaseKey key)
    {
        var page = await _page_cache.get_page(pageId);
        var node = deserialize_node(page.data);
        if (node.is_leaf)
        {
            var index = node.keys.BinarySearch(key);
            return index >= 0 ? (node.values[index], node.sequences[index]) : (null, Zero: SequenceNumber.zero);
        }

        var childIndex = find_child_index(node, key);
        return childIndex < node.children.Count
            ? await search_node_with_sequence(node.children[childIndex], key)
            : (null, Zero: SequenceNumber.zero);
    }

    private async ValueTask<DatabaseValue?> search_node(long pageId, DatabaseKey key)
    {
        var (value, _) = await search_node_with_sequence(pageId, key);
        return value;
    }

    private async ValueTask<long> find_leaf(long pageId, DatabaseKey key)
    {
        var page = await _page_cache.get_page(pageId);
        var node = deserialize_node(page.data);
        if (node.is_leaf) return pageId;

        var childIndex = find_child_index(node, key);
        return childIndex < node.children.Count ? await find_leaf(node.children[childIndex], key) : -1;
    }

    #endregion

    #region 删除

    /// <inheritdoc />
    public async ValueTask<bool> delete(DatabaseKey key, SequenceNumber sequence = default)
    {
        if (root_page_id < 0) return false;

        await _index_lock.enter_write_lock(CancellationToken.None);
        try
        {
            var (oldValue, oldSequence) = await search_node_with_sequence(root_page_id, key);

            if (oldValue is null) return false;

            _version_store?.add_version(key, oldSequence, oldValue.Value);

            await apply_tombstone(root_page_id, key, sequence);

            return true;
        }
        finally
        {
            _index_lock.exit_write_lock();
        }
    }

    /// <summary>
    ///     对指定键应用墓碑标记（软删除），保留键在 BTree 中以支持 MVCC 快照
    /// </summary>
    private async ValueTask apply_tombstone(long pageId, DatabaseKey key, SequenceNumber sequence)
    {
        var page = await _page_cache.get_page(pageId);
        var node = deserialize_node(page.data);

        if (node.is_leaf)
        {
            var index = node.keys.BinarySearch(key);
            if (index >= 0)
            {
                node.values[index] = DatabaseValue.tombstone;
                node.sequences[index] = sequence;
                serialize_node(node, page.data);
                _page_cache.mark_dirty(pageId);
            }

            return;
        }

        var childIndex = find_child_index(node, key);

        if (childIndex < node.children.Count) await apply_tombstone(node.children[childIndex], key, sequence);
    }

    #endregion

    #region 扫描

    /// <inheritdoc />
    public async IAsyncEnumerable<DatabaseEntry> range_scan(DatabaseKey start, DatabaseKey end,
        SequenceNumber? asOfSequence = null)
    {
        if (root_page_id < 0) yield break;

        List<DatabaseEntry> results;
        await _index_lock.enter_read_lock(CancellationToken.None);
        try
        {
            results = await collect_range(start, end, asOfSequence);
        }
        finally
        {
            _index_lock.exit_read_lock();
        }

        foreach (var entry in results) yield return entry;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<DatabaseEntry> prefix_scan(DatabaseKey prefix, SequenceNumber? asOfSequence = null)
    {
        if (root_page_id < 0) yield break;

        List<DatabaseEntry> results;
        await _index_lock.enter_read_lock(CancellationToken.None);
        try
        {
            results = await collect_prefix(prefix, asOfSequence);
        }
        finally
        {
            _index_lock.exit_read_lock();
        }

        foreach (var entry in results) yield return entry;
    }

    private async ValueTask<List<DatabaseEntry>> collect_range(DatabaseKey start, DatabaseKey end,
        SequenceNumber? asOfSequence)
    {
        var results = new List<DatabaseEntry>();
        var leafId = await find_leaf(root_page_id, start);
        while (leafId != -1)
        {
            var page = await _page_cache.get_page(leafId);
            var node = deserialize_node(page.data);
            for (var i = 0; i < node.keys.Count; i++)
            {
                if (node.keys[i].CompareTo(start) >= 0 && node.keys[i].CompareTo(end) <= 0)
                {
                    var value = resolve_mvcc_value(node.keys[i], node.values[i], node.sequences[i], asOfSequence);
                    if (value is not null) results.Add(new DatabaseEntry(node.keys[i], value.Value));
                }

                if (node.keys[i].CompareTo(end) > 0) return results;
            }

            leafId = node.next_sibling;
        }

        return results;
    }

    private async ValueTask<List<DatabaseEntry>> collect_prefix(DatabaseKey prefix, SequenceNumber? asOfSequence)
    {
        var results = new List<DatabaseEntry>();
        var leafId = await find_leaf(root_page_id, prefix);
        while (leafId != -1)
        {
            var page = await _page_cache.get_page(leafId);
            var node = deserialize_node(page.data);
            for (var i = 0; i < node.keys.Count; i++)
                if (node.keys[i].starts_with(prefix))
                {
                    var value = resolve_mvcc_value(node.keys[i], node.values[i], node.sequences[i], asOfSequence);
                    if (value is not null) results.Add(new DatabaseEntry(node.keys[i], value.Value));
                }
                else if (node.keys[i].CompareTo(prefix) > 0)
                {
                    return results;
                }

            leafId = node.next_sibling;
        }

        return results;
    }

    private DatabaseValue? resolve_mvcc_value(DatabaseKey key, DatabaseValue currentValue,
        SequenceNumber currentSequence,
        SequenceNumber? asOfSequence)
    {
        if (!asOfSequence.HasValue)
        {
            if (currentValue.is_tombstone) return null;

            return currentValue;
        }

        if (currentSequence.value <= asOfSequence.Value.value)
        {
            if (currentValue.is_tombstone) return null;

            return currentValue;
        }

        if (_version_store is not null)
        {
            var historicalValue = _version_store.get_visible_value(key, asOfSequence.Value);
            if (historicalValue is not null && historicalValue.Value.is_tombstone) return null;

            return historicalValue;
        }

        return null;
    }

    #endregion

    #region 平衡操作

    private async ValueTask ensure_child_not_underflow(long parentPageId, int childIndex)
    {
        var parentPage = await _page_cache.get_page(parentPageId);
        var parentNode = deserialize_node(parentPage.data);
        var childPageId = parentNode.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);

        if (!childNode.is_underflow(_order)) return;

        BTreeNode? leftSibling = null;
        BTreeNode? rightSibling = null;
        long leftSiblingId = -1;
        long rightSiblingId = -1;

        if (childIndex > 0)
        {
            leftSiblingId = parentNode.children[childIndex - 1];
            var leftPage = await _page_cache.get_page(leftSiblingId);
            leftSibling = deserialize_node(leftPage.data);
        }

        if (childIndex < parentNode.children.Count - 1)
        {
            rightSiblingId = parentNode.children[childIndex + 1];
            var rightPage = await _page_cache.get_page(rightSiblingId);
            rightSibling = deserialize_node(rightPage.data);
        }

        if (leftSibling is not null && !leftSibling.is_underflow(_order))
        {
            await borrow_from_left(parentPageId, childIndex);
            return;
        }

        if (rightSibling is not null && !rightSibling.is_underflow(_order))
        {
            await borrow_from_right(parentPageId, childIndex);
            return;
        }

        if (leftSibling is not null)
            await merge_with_left(parentPageId, childIndex);
        else if (rightSibling is not null) await merge_with_right(parentPageId, childIndex);
    }

    private async ValueTask borrow_from_left(long parentPageId, int childIndex)
    {
        var parentPage = await _page_cache.get_page(parentPageId);
        var parentNode = deserialize_node(parentPage.data);
        var childPageId = parentNode.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);
        var leftSiblingId = parentNode.children[childIndex - 1];
        var leftPage = await _page_cache.get_page(leftSiblingId);
        var leftNode = deserialize_node(leftPage.data);

        if (childNode.is_leaf)
        {
            var borrowedKey = leftNode.keys[^1];
            var borrowedValue = leftNode.values[^1];
            var borrowedSequence = leftNode.sequences[^1];
            leftNode.keys.RemoveAt(leftNode.keys.Count - 1);
            leftNode.values.RemoveAt(leftNode.values.Count - 1);
            leftNode.sequences.RemoveAt(leftNode.sequences.Count - 1);
            childNode.keys.Insert(0, borrowedKey);
            childNode.values.Insert(0, borrowedValue);
            childNode.sequences.Insert(0, borrowedSequence);
            parentNode.keys[childIndex - 1] = borrowedKey;
        }
        else
        {
            var borrowedKey = leftNode.keys[^1];
            var borrowedChild = leftNode.children[^1];
            leftNode.keys.RemoveAt(leftNode.keys.Count - 1);
            leftNode.children.RemoveAt(leftNode.children.Count - 1);
            childNode.keys.Insert(0, parentNode.keys[childIndex - 1]);
            childNode.children.Insert(0, borrowedChild);
            parentNode.keys[childIndex - 1] = borrowedKey;
        }

        serialize_node(leftNode, leftPage.data);
        serialize_node(childNode, childPage.data);
        serialize_node(parentNode, parentPage.data);
        _page_cache.mark_dirty(leftSiblingId);
        _page_cache.mark_dirty(childPageId);
        _page_cache.mark_dirty(parentPageId);
    }

    private async ValueTask borrow_from_right(long parentPageId, int childIndex)
    {
        var parentPage = await _page_cache.get_page(parentPageId);
        var parentNode = deserialize_node(parentPage.data);
        var childPageId = parentNode.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);
        var rightSiblingId = parentNode.children[childIndex + 1];
        var rightPage = await _page_cache.get_page(rightSiblingId);
        var rightNode = deserialize_node(rightPage.data);

        if (childNode.is_leaf)
        {
            var borrowedKey = rightNode.keys[0];
            var borrowedValue = rightNode.values[0];
            var borrowedSequence = rightNode.sequences[0];
            rightNode.keys.RemoveAt(0);
            rightNode.values.RemoveAt(0);
            rightNode.sequences.RemoveAt(0);
            childNode.keys.Add(borrowedKey);
            childNode.values.Add(borrowedValue);
            childNode.sequences.Add(borrowedSequence);
            parentNode.keys[childIndex] = rightNode.keys[0];
        }
        else
        {
            var borrowedKey = rightNode.keys[0];
            var borrowedChild = rightNode.children[0];
            rightNode.keys.RemoveAt(0);
            rightNode.children.RemoveAt(0);
            childNode.keys.Add(parentNode.keys[childIndex]);
            childNode.children.Add(borrowedChild);
            parentNode.keys[childIndex] = borrowedKey;
        }

        serialize_node(rightNode, rightPage.data);
        serialize_node(childNode, childPage.data);
        serialize_node(parentNode, parentPage.data);
        _page_cache.mark_dirty(rightSiblingId);
        _page_cache.mark_dirty(childPageId);
        _page_cache.mark_dirty(parentPageId);
    }

    private async ValueTask merge_with_left(long parentPageId, int childIndex)
    {
        var parentPage = await _page_cache.get_page(parentPageId);
        var parentNode = deserialize_node(parentPage.data);
        var childPageId = parentNode.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);
        var leftSiblingId = parentNode.children[childIndex - 1];
        var leftPage = await _page_cache.get_page(leftSiblingId);
        var leftNode = deserialize_node(leftPage.data);

        if (childNode.is_leaf)
        {
            leftNode.keys.AddRange(childNode.keys);
            leftNode.values.AddRange(childNode.values);
            leftNode.sequences.AddRange(childNode.sequences);
            leftNode.next_sibling = childNode.next_sibling;
        }
        else
        {
            leftNode.keys.Add(parentNode.keys[childIndex - 1]);
            leftNode.keys.AddRange(childNode.keys);
            leftNode.children.AddRange(childNode.children);
        }

        parentNode.keys.RemoveAt(childIndex - 1);
        parentNode.children.RemoveAt(childIndex);

        serialize_node(leftNode, leftPage.data);
        serialize_node(parentNode, parentPage.data);
        _page_cache.mark_dirty(leftSiblingId);
        _page_cache.mark_dirty(parentPageId);
    }

    private async ValueTask merge_with_right(long parentPageId, int childIndex)
    {
        var parentPage = await _page_cache.get_page(parentPageId);
        var parentNode = deserialize_node(parentPage.data);
        var childPageId = parentNode.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);
        var rightSiblingId = parentNode.children[childIndex + 1];
        var rightPage = await _page_cache.get_page(rightSiblingId);
        var rightNode = deserialize_node(rightPage.data);

        if (childNode.is_leaf)
        {
            childNode.keys.AddRange(rightNode.keys);
            childNode.values.AddRange(rightNode.values);
            childNode.sequences.AddRange(rightNode.sequences);
            childNode.next_sibling = rightNode.next_sibling;
        }
        else
        {
            childNode.keys.Add(parentNode.keys[childIndex]);
            childNode.keys.AddRange(rightNode.keys);
            childNode.children.AddRange(rightNode.children);
        }

        parentNode.keys.RemoveAt(childIndex);
        parentNode.children.RemoveAt(childIndex + 1);

        serialize_node(childNode, childPage.data);
        serialize_node(parentNode, parentPage.data);
        _page_cache.mark_dirty(childPageId);
        _page_cache.mark_dirty(parentPageId);
    }

    private async ValueTask handle_underflow(long parentPageId, int childIndex)
    {
        var parentPage = await _page_cache.get_page(parentPageId);
        var parentNode = deserialize_node(parentPage.data);

        if (childIndex >= parentNode.children.Count) return;

        var childPageId = parentNode.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);

        if (!childNode.is_underflow(_order)) return;

        BTreeNode? leftSibling = null;
        BTreeNode? rightSibling = null;
        long leftSiblingId = -1;
        long rightSiblingId = -1;

        if (childIndex > 0)
        {
            leftSiblingId = parentNode.children[childIndex - 1];
            var leftPage = await _page_cache.get_page(leftSiblingId);
            leftSibling = deserialize_node(leftPage.data);
        }

        if (childIndex < parentNode.children.Count - 1)
        {
            rightSiblingId = parentNode.children[childIndex + 1];
            var rightPage = await _page_cache.get_page(rightSiblingId);
            rightSibling = deserialize_node(rightPage.data);
        }

        if (leftSibling is not null && !leftSibling.is_underflow(_order))
        {
            await borrow_from_left(parentPageId, childIndex);
            return;
        }

        if (rightSibling is not null && !rightSibling.is_underflow(_order))
        {
            await borrow_from_right(parentPageId, childIndex);
            return;
        }

        if (leftSibling is not null)
            await merge_with_left(parentPageId, childIndex);
        else if (rightSibling is not null) await merge_with_right(parentPageId, childIndex);
    }

    private async ValueTask split_root()
    {
        var newRoot = await _page_cache.allocate_page();
        var oldRootPage = await _page_cache.get_page(root_page_id);
        var oldRootNode = deserialize_node(oldRootPage.data);
        var newRootNode = new BTreeNode { is_leaf = false };
        newRootNode.children.Add(root_page_id);
        serialize_node(newRootNode, newRoot.data);
        _page_cache.mark_dirty(newRoot.id);
        root_page_id = newRoot.id;
        await split_child(root_page_id, 0);
    }

    private async ValueTask split_child(long parentPageId, int childIndex)
    {
        var parentPage = await _page_cache.get_page(parentPageId);
        var parentNode = deserialize_node(parentPage.data);
        var childPageId = parentNode.children[childIndex];
        var childPage = await _page_cache.get_page(childPageId);
        var childNode = deserialize_node(childPage.data);
        var newChild = await _page_cache.allocate_page();
        var newChildNode = new BTreeNode { is_leaf = childNode.is_leaf };
        var mid = childNode.keys.Count / 2;

        if (childNode.is_leaf)
        {
            for (var i = mid; i < childNode.keys.Count; i++)
            {
                newChildNode.keys.Add(childNode.keys[i]);
                newChildNode.values.Add(childNode.values[i]);
                newChildNode.sequences.Add(childNode.sequences[i]);
            }

            newChildNode.next_sibling = childNode.next_sibling;
            childNode.next_sibling = newChild.id;

            var midKey = childNode.keys[mid];
            parentNode.keys.Insert(childIndex, midKey);
            parentNode.children.Insert(childIndex + 1, newChild.id);

            childNode.keys.RemoveRange(mid, childNode.keys.Count - mid);
            childNode.values.RemoveRange(mid, childNode.values.Count - mid);
            childNode.sequences.RemoveRange(mid, childNode.sequences.Count - mid);
        }
        else
        {
            var midKey = childNode.keys[mid];
            parentNode.keys.Insert(childIndex, midKey);
            parentNode.children.Insert(childIndex + 1, newChild.id);

            for (var i = mid + 1; i < childNode.keys.Count; i++) newChildNode.keys.Add(childNode.keys[i]);

            for (var i = mid + 1; i < childNode.children.Count; i++) newChildNode.children.Add(childNode.children[i]);

            childNode.keys.RemoveRange(mid, childNode.keys.Count - mid);
            childNode.children.RemoveRange(mid + 1, childNode.children.Count - mid - 1);
        }

        serialize_node(childNode, childPage.data);
        serialize_node(newChildNode, newChild.data);
        serialize_node(parentNode, parentPage.data);
        _page_cache.mark_dirty(childPageId);
        _page_cache.mark_dirty(newChild.id);
        _page_cache.mark_dirty(parentPageId);
    }

    #endregion

    #region 压缩

    /// <summary>
    ///     压缩索引，物理删除墓碑标记的键值对并回收空间
    /// </summary>
    public async ValueTask<int> compact(SequenceNumber minSequence, CancellationToken cancellationToken = default)
    {
        if (root_page_id < 0) return 0;

        await _index_lock.enter_write_lock(CancellationToken.None);
        try
        {
            var removed = await compact_node(root_page_id, minSequence);

            await merge_underflow_nodes(root_page_id, minSequence);

            return removed;
        }
        finally
        {
            _index_lock.exit_write_lock();
        }
    }

    private async ValueTask<int> compact_node(long pageId, SequenceNumber minSequence)
    {
        var page = await _page_cache.get_page(pageId);
        var node = deserialize_node(page.data);

        if (node.is_leaf)
        {
            var removedCount = 0;
            for (var i = node.keys.Count - 1; i >= 0; i--)
                if (node.values[i].is_tombstone && node.sequences[i].value < minSequence.value)
                {
                    node.keys.RemoveAt(i);
                    node.values.RemoveAt(i);
                    node.sequences.RemoveAt(i);
                    removedCount++;
                }

            if (removedCount > 0)
            {
                serialize_node(node, page.data);
                _page_cache.mark_dirty(pageId);
            }

            return removedCount;
        }

        var totalRemoved = 0;
        for (var i = 0; i < node.children.Count; i++)
            totalRemoved += await compact_node(node.children[i], minSequence);

        if (totalRemoved > 0)
        {
            page = await _page_cache.get_page(pageId);
            node = deserialize_node(page.data);

            var freedPages = new List<long>();

            for (var i = node.children.Count - 1; i > 0; i--)
            {
                var childPageId = node.children[i];
                var childPage = await _page_cache.get_page(childPageId);
                var childNode = deserialize_node(childPage.data);

                if (childNode.keys.Count == 0 && childNode.is_leaf)
                {
                    node.keys.RemoveAt(i - 1);
                    node.children.RemoveAt(i);
                    freedPages.Add(childPageId);
                }
            }

            if (node.keys.Count == 0 && node.children.Count == 1 && pageId == root_page_id)
            {
                var onlyChildId = node.children[0];
                var childPage = await _page_cache.get_page(onlyChildId);
                var childNode = deserialize_node(childPage.data);

                if (!childNode.is_leaf || childNode.keys.Count > 0)
                {
                    root_page_id = onlyChildId;
                    freedPages.Add(pageId);
                }
            }

            serialize_node(node, page.data);
            _page_cache.mark_dirty(pageId);

            foreach (var freedPageId in freedPages) await _page_cache.deallocate_page(freedPageId);
        }

        return totalRemoved;
    }

    /// <summary>
    ///     合并稀疏节点：将填充率低于一半的叶节点与相邻兄弟合并
    /// </summary>
    private async ValueTask merge_underflow_nodes(long pageId, SequenceNumber minSequence)
    {
        var page = await _page_cache.get_page(pageId);
        var node = deserialize_node(page.data);

        if (node.is_leaf) return;

        for (var i = 0; i < node.children.Count; i++) await merge_underflow_nodes(node.children[i], minSequence);

        page = await _page_cache.get_page(pageId);
        node = deserialize_node(page.data);

        var halfOrder = System.Math.Max(2, _order / 2);
        var freedPages = new List<long>();

        for (var i = 0; i < node.children.Count; i++)
        {
            var childPageId = node.children[i];
            var childPage = await _page_cache.get_page(childPageId);
            var childNode = deserialize_node(childPage.data);

            if (!childNode.is_leaf || childNode.keys.Count >= halfOrder) continue;

            var merged = false;

            if (i > 0)
            {
                var leftSiblingId = node.children[i - 1];
                var leftPage = await _page_cache.get_page(leftSiblingId);
                var leftNode = deserialize_node(leftPage.data);

                if (leftNode.is_leaf && leftNode.keys.Count + childNode.keys.Count <= _order &&
                    would_fit_in_page(leftNode, childNode))
                {
                    leftNode.keys.AddRange(childNode.keys);
                    leftNode.values.AddRange(childNode.values);
                    leftNode.sequences.AddRange(childNode.sequences);
                    leftNode.next_sibling = childNode.next_sibling;

                    serialize_node(leftNode, leftPage.data);
                    _page_cache.mark_dirty(leftSiblingId);

                    node.keys.RemoveAt(i - 1);
                    node.children.RemoveAt(i);
                    freedPages.Add(childPageId);
                    merged = true;
                    i--;
                }
            }

            if (!merged && i < node.children.Count - 1)
            {
                var rightSiblingId = node.children[i + 1];
                var rightPage = await _page_cache.get_page(rightSiblingId);
                var rightNode = deserialize_node(rightPage.data);

                if (rightNode.is_leaf && rightNode.keys.Count + childNode.keys.Count <= _order &&
                    would_fit_in_page(childNode, rightNode))
                {
                    childNode.keys.AddRange(rightNode.keys);
                    childNode.values.AddRange(rightNode.values);
                    childNode.sequences.AddRange(rightNode.sequences);
                    childNode.next_sibling = rightNode.next_sibling;

                    serialize_node(childNode, childPage.data);
                    _page_cache.mark_dirty(childPageId);

                    node.keys.RemoveAt(i);
                    node.children.RemoveAt(i + 1);
                    freedPages.Add(rightSiblingId);
                }
            }
        }

        if (freedPages.Count > 0)
        {
            if (node.keys.Count == 0 && node.children.Count == 1 && pageId == root_page_id)
            {
                root_page_id = node.children[0];
                freedPages.Add(pageId);
            }

            serialize_node(node, page.data);
            _page_cache.mark_dirty(pageId);

            foreach (var freedPageId in freedPages) await _page_cache.deallocate_page(freedPageId);
        }
    }

    /// <summary>
    ///     检查两个叶节点合并后是否能放入一个页面
    /// </summary>
    private bool would_fit_in_page(BTreeNode left, BTreeNode right)
    {
        var totalSize = left.get_serialized_size() - 8 + right.get_serialized_size() - 8 + 8;
        return totalSize <= _page_cache.page_size;
    }

    #endregion

    #region 序列化（优化版：零额外分配）

    /// <summary>
    ///     从页数据反序列化 B+ 树节点
    /// </summary>
    private static BTreeNode deserialize_node(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        var node = new BTreeNode { is_leaf = reader.ReadBoolean() };
        var keyCount = reader.ReadInt32();

        for (var i = 0; i < keyCount; i++)
        {
            var keyLength = reader.ReadInt32();
            var keyBytes = reader.ReadBytes(keyLength);
            node.keys.Add(new DatabaseKey(keyBytes));
        }

        if (node.is_leaf)
        {
            for (var i = 0; i < keyCount; i++)
            {
                var valueLength = reader.ReadInt32();
                var valueBytes = reader.ReadBytes(valueLength);
                node.values.Add(new DatabaseValue(valueBytes));
            }

            for (var i = 0; i < keyCount; i++) node.sequences.Add(new SequenceNumber(reader.ReadUInt64()));

            node.next_sibling = reader.ReadInt64();
        }
        else
        {
            var childCount = reader.ReadInt32();
            if (childCount is < 0 or > 10000)
                throw new InvalidDataException(
                    $"BTreeNode 子节点数量异常: childCount={childCount}, isLeaf={node.is_leaf}, kc={keyCount}, dataLen={data.Length}");

            var remaining = ms.Length - ms.Position;
            if (remaining < childCount * 8L)
                throw new InvalidDataException(
                    $"BTreeNode 页数据不足: 需要={childCount * 8}字节, 剩余={remaining}字节, childCount={childCount}, kc={keyCount}, isLeaf={node.is_leaf}, total={ms.Length}, pos={ms.Position}");

            for (var i = 0; i < childCount; i++) node.children.Add(reader.ReadInt64());
        }

        return node;
    }

    /// <summary>
    ///     序列化 B+ 树节点到页面缓冲区（零额外字节数组分配）
    /// </summary>
    private static void serialize_node(BTreeNode node, byte[] buffer)
    {
        var requiredSize = node.get_serialized_size();
        if (requiredSize > buffer.Length)
            throw new InvalidOperationException(
                $"BTreeNode 序列化溢出: 需要={requiredSize}字节, 缓冲={buffer.Length}字节, isLeaf={node.is_leaf}, kc={node.keys.Count}, cc={(node.is_leaf ? 0 : node.children.Count)}");

        using var ms = new MemoryStream(buffer);
        using var writer = new BinaryWriter(ms);
        writer.Write(node.is_leaf);
        writer.Write(node.keys.Count);

        foreach (var key in node.keys)
        {
            var span = key.bytes.Span;
            writer.Write(span.Length);
            writer.Write(span);
        }

        if (node.is_leaf)
        {
            foreach (var value in node.values)
            {
                var span = value.bytes.Span;
                writer.Write(span.Length);
                writer.Write(span);
            }

            foreach (var sequence in node.sequences) writer.Write(sequence.value);

            writer.Write(node.next_sibling);
        }
        else
        {
            writer.Write(node.children.Count);
            foreach (var child in node.children) writer.Write(child);
        }
    }

    #endregion
}