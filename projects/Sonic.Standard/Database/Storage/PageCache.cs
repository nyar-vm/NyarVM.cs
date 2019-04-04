using Std.Database.Core;

namespace Std.Database.Storage;

/// <summary>
///     页面缓存，使用 LRU 置换策略与异步读写锁
/// </summary>
internal sealed class PageCache : IPageCache
{
    #region 构造函数

    /// <summary>
    ///     创建页面缓存
    /// </summary>
    /// <param name="storage">底层存储</param>
    /// <param name="capacity">缓存容量（页面数）</param>
    public PageCache(IStorageEngine storage, int capacity)
    {
        _storage = storage;
        this.capacity = capacity;
        _pages = new Dictionary<long, Page>();
        _node_map = new Dictionary<long, LinkedListNode<long>>();
        _lru_list = [];
    }

    #endregion

    #region 刷盘

    /// <summary>
    ///     刷盘所有脏页
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask flush(CancellationToken cancellationToken = default)
    {
        List<Page> dirtyPages;

        _lock.enter_read_lock();
        try
        {
            dirtyPages = [.. _pages.Values.Where(p => p.is_dirty)];
        }
        finally
        {
            _lock.exit_read_lock();
        }

        foreach (var page in dirtyPages)
        {
            await _storage.write(page.id, page.data, cancellationToken);
            page.is_dirty = false;
        }

        await _storage.flush(cancellationToken);
    }

    #endregion

    #region 页面释放

    /// <inheritdoc />
    public ValueTask deallocate_page(long pageId, CancellationToken cancellationToken = default)
    {
        _lock.enter_write_lock();
        try
        {
            if (_node_map.TryGetValue(pageId, out var node))
            {
                _lru_list.Remove(node);
                _node_map.Remove(pageId);
            }

            _pages.Remove(pageId);
        }
        finally
        {
            _lock.exit_write_lock();
        }

        _storage.free_page(pageId);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region 内部方法

    private async ValueTask evict_page(CancellationToken cancellationToken)
    {
        var node = _lru_list.Last;
        while (node is not null)
        {
            if (_pages.TryGetValue(node.Value, out var page) && page.pin_count == 0)
            {
                if (page.is_dirty) await _storage.write(page.id, page.data, cancellationToken);

                _pages.Remove(node.Value);
                _node_map.Remove(node.Value);
                _lru_list.Remove(node);
                return;
            }

            node = node.Previous;
        }
    }

    #endregion

    #region 字段

    private readonly IStorageEngine _storage;
    private readonly Dictionary<long, Page> _pages;
    private readonly Dictionary<long, LinkedListNode<long>> _node_map;
    private readonly LinkedList<long> _lru_list;
    private readonly AsyncReaderWriterLock _lock = new();

    #endregion

    #region 属性

    /// <summary>
    ///     底层存储的页面大小
    /// </summary>
    public int page_size => _storage.page_size;

    /// <summary>
    ///     当前缓存页面数
    /// </summary>
    public int count => _pages.Count;

    /// <summary>
    ///     缓存命中次数
    /// </summary>
    public long hit_count { get; private set; }

    /// <summary>
    ///     缓存未命中次数
    /// </summary>
    public long miss_count { get; private set; }

    /// <summary>
    ///     缓存容量（页面数）
    /// </summary>
    public int capacity { get; }

    #endregion

    #region 读取

    /// <inheritdoc />
    public bool try_get_page(long pageId, out Page page)
    {
        _lock.enter_read_lock();
        try
        {
            if (_pages.TryGetValue(pageId, out var cachedPage))
            {
                hit_count++;
                page = cachedPage;
                return true;
            }

            page = null!;
            return false;
        }
        finally
        {
            _lock.exit_read_lock();
        }
    }

    /// <summary>
    ///     检查页面是否在缓存中
    /// </summary>
    public bool contains(long pageId)
    {
        _lock.enter_read_lock();
        try
        {
            return _pages.ContainsKey(pageId);
        }
        finally
        {
            _lock.exit_read_lock();
        }
    }

    /// <summary>
    ///     将指定页面移动到 LRU 链表头部
    /// </summary>
    private void move_to_front(long pageId)
    {
        if (_node_map.TryGetValue(pageId, out var node))
        {
            _lru_list.Remove(node);
            _lru_list.AddFirst(node);
        }
    }

    /// <summary>
    ///     获取页面
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>页面实例</returns>
    public async ValueTask<Page> get_page(long pageId, CancellationToken cancellationToken = default)
    {
        _lock.enter_read_lock();
        try
        {
            if (_pages.TryGetValue(pageId, out var cachedPage))
            {
                hit_count++;
                return cachedPage;
            }
        }
        finally
        {
            _lock.exit_read_lock();
        }

        miss_count++;

        var page = new Page(pageId, _storage.page_size);
        await _storage.read(pageId, page.data, cancellationToken);

        _lock.enter_write_lock();
        try
        {
            if (_pages.TryGetValue(pageId, out var existingPage))
            {
                hit_count++;
                return existingPage;
            }

            if (_pages.Count >= capacity) await evict_page(cancellationToken);

            _pages[pageId] = page;
            var node = _lru_list.AddFirst(pageId);
            _node_map[pageId] = node;

            return page;
        }
        finally
        {
            _lock.exit_write_lock();
        }
    }

    #endregion

    #region 写入

    /// <summary>
    ///     分配新页面
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新页面实例</returns>
    public async ValueTask<Page> allocate_page(CancellationToken cancellationToken = default)
    {
        var pageId = _storage.allocate_page();
        var page = new Page(pageId, _storage.page_size);
        page.is_dirty = true;

        _lock.enter_write_lock();
        try
        {
            if (_pages.Count >= capacity) await evict_page(cancellationToken);

            _pages[pageId] = page;
            var node = _lru_list.AddFirst(pageId);
            _node_map[pageId] = node;
        }
        finally
        {
            _lock.exit_write_lock();
        }

        return page;
    }

    /// <summary>
    ///     标记页面为脏页
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    public void mark_dirty(long pageId)
    {
        _lock.enter_read_lock();
        try
        {
            if (_pages.TryGetValue(pageId, out var page)) page.is_dirty = true;
        }
        finally
        {
            _lock.exit_read_lock();
        }
    }

    /// <summary>
    ///     写入页面到缓存
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    /// <param name="page">页面实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask put_page(long pageId, Page page, CancellationToken cancellationToken = default)
    {
        _lock.enter_write_lock();
        try
        {
            if (_pages.ContainsKey(pageId))
            {
                _pages[pageId] = page;
                page.is_dirty = true;
            }
            else
            {
                if (_pages.Count >= capacity) await evict_page(cancellationToken);

                _pages[pageId] = page;
                page.is_dirty = true;
                var node = _lru_list.AddFirst(pageId);
                _node_map[pageId] = node;
            }
        }
        finally
        {
            _lock.exit_write_lock();
        }
    }

    #endregion
}