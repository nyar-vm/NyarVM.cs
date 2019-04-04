using Std.Category;

namespace Std.Collection.Map;

/// <summary>
///     鍩轰簬鑷�?2-3-4 鏍戯紙order=4锛夌殑鏈夊簭閿€煎鏄犲皠锛屽疄鐜?<see cref="IMap{K, V}" /> 鎺ュ彛銆?///
/// </summary>
/// <typeparam name="K">
///     閿被鍨嬶紝蹇呴』鍙瘮杈冦€?/typeparam>
///     <typeparam name="V">鍊肩被鍨嬨€?/typeparam>
public class BTreeMap<K, V> : IMap<K, V> where K : IComparable<K>
{
    /// <summary>B 鏍戠殑鏍硅妭鐐广�?/summary>
    private BTreeNode<K, V> _root;

    /// <summary>
    ///     鍒濆鍖栦竴涓┖鐨?<see cref="BTreeMap{K, V}" /> 瀹炰緥銆?    ///
    /// </summary>
    public BTreeMap()
    {
        _root = new BTreeNode<K, V>(true);
    }

    /// <inheritdoc />
    public int count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count_keys(_root);
    }

    /// <inheritdoc />
    public bool is_empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _root.keys.Count == 0;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void clear()
    {
        _root = new BTreeNode<K, V>(true);
    }

    /// <inheritdoc />
    public Option<V> get(K key)
    {
        return search_node(_root, key);
    }

    /// <inheritdoc />
    public Option<V> insert(K key, V value)
    {
        if (_root.keys.Count == 4)
        {
            var newRoot = new BTreeNode<K, V>(false);
            newRoot.children.Add(_root);
            split_child(newRoot, 0);
            _root = newRoot;
            return insert_non_full(newRoot, key, value);
        }

        return insert_non_full(_root, key, value);
    }

    /// <inheritdoc />
    public Option<V> remove(K key)
    {
        throw new NotSupportedException("BTreeMap.Remove 尚未实现。");
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool contains_key(K key)
    {
        return get(key).is_some;
    }

    /// <inheritdoc />
    public void for_each(Action<K, V> action)
    {
        iterate_node(_root, action);
    }

    /// <inheritdoc />
    public IEnumerable<K> keys
    {
        get
        {
            var result = new List<K>();
            for_each((k, _) => result.Add(k));
            return result;
        }
    }

    /// <inheritdoc />
    public IEnumerable<V> values
    {
        get
        {
            var result = new List<V>();
            for_each((_, v) => result.Add(v));
            return result;
        }
    }

    /// <summary>
    ///     鍦ㄨ妭鐐逛腑閫掑綊鏌ユ壘鎸囧畾閿搴旂殑鍊笺€?    ///
    /// </summary>
    /// <param name="node">
    ///     褰撳墠鎼滅储鑺傜偣銆?/param>
    ///     <param name="key">
    ///         瑕佹煡鎵剧殑閿€?/param>
    ///         <returns>閿搴旂殑鍊硷紝鑻ヤ笉瀛樺湪鍒欒繑�?<see cref="Option{T}.none" />�?/returns>
    private static Option<V> search_node(BTreeNode<K, V> node, K key)
    {
        var i = 0;

        while (i < node.keys.Count)
        {
            var comparison = key.CompareTo(node.keys[i]);

            if (comparison == 0) return Option<V>.some(node.values[i]);

            if (comparison < 0)
            {
                if (node.is_leaf) return Option<V>.none;

                return search_node(node.children[i], key);
            }

            i++;
        }

        if (node.is_leaf) return Option<V>.none;

        return search_node(node.children[i], key);
    }

    /// <summary>
    ///     鍦ㄩ潪婊¤妭鐐逛腑鎻掑叆閿€煎�?    ///
    /// </summary>
    /// <param name="node">
    ///     褰撳墠鑺傜偣锛屼繚璇佽鑺傜偣鐨勯敭鏁板皬�?4�?/param>
    ///     <param name="key">
    ///         瑕佹彃鍏ョ殑閿€?/param>
    ///         <param name="value">
    ///             瑕佹彃鍏ョ殑鍊笺�?/param>
    ///             <returns>鑻ラ敭宸插瓨鍦ㄥ垯杩斿洖鏃у€硷紝鍚﹀垯杩斿洖 <see cref="Option{T}.none" />�?/returns>
    private static Option<V> insert_non_full(BTreeNode<K, V> node, K key, V value)
    {
        var i = node.keys.Count - 1;

        if (node.is_leaf)
        {
            while (i >= 0 && key.CompareTo(node.keys[i]) < 0) i--;

            i++;

            if (i > 0 && node.keys[i - 1].CompareTo(key) == 0)
            {
                var oldValue = node.values[i - 1];
                node.values[i - 1] = value;
                return Option<V>.some(oldValue);
            }

            node.keys.Insert(i, key);
            node.values.Insert(i, value);
            return Option<V>.none;
        }

        while (i >= 0 && key.CompareTo(node.keys[i]) < 0) i--;

        i++;

        if (i > 0 && node.keys[i - 1].CompareTo(key) == 0)
        {
            var oldValue = node.values[i - 1];
            node.values[i - 1] = value;
            return Option<V>.some(oldValue);
        }

        var child = node.children[i];

        if (child.keys.Count == 4)
        {
            split_child(node, i);

            if (key.CompareTo(node.keys[i]) > 0) i++;
        }

        return insert_non_full(node.children[i], key, value);
    }

    /// <summary>
    ///     鍒嗚鐖惰妭鐐逛腑鎸囧畾浣嶇疆鐨勬弧瀛愯妭鐐广€?    ///
    /// </summary>
    /// <param name="parent">
    ///     鐖惰妭鐐广€?/param>
    ///     <param name="idx">瑕佸垎瑁傜殑瀛愯妭鐐瑰湪鐖惰妭鐐瑰瓙鍒楄〃涓殑绱㈠紩�?/param>
    private static void split_child(BTreeNode<K, V> parent, int idx)
    {
        var child = parent.children[idx];
        var newChild = new BTreeNode<K, V>(child.is_leaf);
        var mid = 2;

        var i = mid;
        while (i < child.keys.Count)
        {
            newChild.keys.Add(child.keys[i]);
            newChild.values.Add(child.values[i]);
            i++;
        }

        while (child.keys.Count > mid)
        {
            child.keys.RemoveAt(mid);
            child.values.RemoveAt(mid);
        }

        if (!child.is_leaf)
        {
            i = mid;
            while (i < child.children.Count)
            {
                newChild.children.Add(child.children[i]);
                i++;
            }

            while (child.children.Count > mid) child.children.RemoveAt(mid);
        }

        parent.keys.Insert(idx, child.keys[mid - 1]);
        parent.values.Insert(idx, child.values[mid - 1]);
        child.keys.RemoveAt(mid - 1);
        child.values.RemoveAt(mid - 1);
        parent.children.Insert(idx + 1, newChild);
    }

    /// <summary>
    ///     閫掑綊缁熻鑺傜偣鍙婂叾瀛愭爲涓墍鏈夐敭鐨勬暟閲忋€?    ///
    /// </summary>
    /// <param name="node">
    ///     褰撳墠鑺傜偣�?/param>
    ///     <returns>閿殑鎬绘暟銆?/returns>
    private static int count_keys(BTreeNode<K, V> node)
    {
        var count = node.keys.Count;

        if (!node.is_leaf)
            for (var i = 0; i < node.children.Count; i++)
                count += count_keys(node.children[i]);

        return count;
    }

    /// <summary>
    ///     涓簭閬嶅巻鑺傜偣鍙婂叾瀛愭爲锛屽姣忎釜閿€煎鎵ц鎸囧畾鎿嶄綔�?    ///
    /// </summary>
    /// <param name="node">
    ///     褰撳墠鑺傜偣�?/param>
    ///     <param name="action">瀵规瘡涓敭鍊煎鎵ц鐨勬搷浣溿€?/param>
    private static void iterate_node(BTreeNode<K, V> node, Action<K, V> action)
    {
        var i = 0;

        while (i < node.keys.Count)
        {
            if (!node.is_leaf) iterate_node(node.children[i], action);

            action(node.keys[i], node.values[i]);
            i++;
        }

        if (!node.is_leaf) iterate_node(node.children[i], action);
    }

    /// <summary>
    ///     B 鏍戝唴閮ㄨ妭鐐癸紝瀛樺偍閿€煎拰瀛愯妭鐐瑰紩鐢ㄣ�?    ///
    /// </summary>
    /// <typeparam name="TKey">
    ///     閿被鍨嬨�?/typeparam>
    ///     <typeparam name="TValue">鍊肩被鍨嬨€?/typeparam>
    private class BTreeNode<TKey, TValue> where TKey : IComparable<TKey>
    {
        /// <summary>瀛愯妭鐐瑰垪琛锛屽彾瀛愯妭鐐逛负绌哄垪琛�?/summary>
        public readonly List<BTreeNode<TKey, TValue>> children;

        /// <summary>鏄惁涓哄彾瀛愯妭鐐广€?/summary>
        public readonly bool is_leaf;

        /// <summary>鑺傜偣涓殑閿垪琛锛岄敭涓ユ牸閫掑鎺掑垪銆?/summary>
        public readonly List<TKey> keys;

        /// <summary>鑺傜偣涓殑鍊煎垪琛锛屼笌閿竴涓€瀵瑰簲銆?/summary>
        public readonly List<TValue> values;

        /// <summary>
        ///     鍒涘缓涓€涓寚瀹氱被鍨嬬殑 B 鏍戣妭鐐广€?        ///
        /// </summary>
        /// <param name="isLeaf">鏄惁涓哄彾瀛愯妭鐐广€?/param>
        public BTreeNode(bool isLeaf)
        {
            is_leaf = isLeaf;
            keys = [];
            values = [];
            children = [];
        }
    }
}