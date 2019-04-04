namespace Std.DL.Flux;

/// <summary>
///     KV Cache 分页 —— 固定大小的 KV Cache 存储单元
///     每个 Page 存储 PageSize 个 token 的 Key 和 Value
///     多个 Page 通过 PageTable 逻辑拼接为完整的 KV Cache
/// </summary>
public sealed class KVPage
{
    private readonly float[] _keyData;
    private readonly float[] _valueData;

    /// <summary>
    ///     创建 KV Cache 分页
    /// </summary>
    /// <param name="pageId">物理页 ID</param>
    /// <param name="pageSize">每页 token 数</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dK">每个头的维度</param>
    public KVPage(int pageId, int pageSize, int numHeads, int dK)
    {
        PageId = pageId;
        PageSize = pageSize;
        UsedSlots = 0;
        RefCount = 1;

        var elementsPerPage = pageSize * numHeads * dK;
        _keyData = new float[elementsPerPage];
        _valueData = new float[elementsPerPage];
    }

    /// <summary>
    ///     每个 Page 存储的 token 数
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    ///     当前 Page 中已使用的 token 槽位数
    /// </summary>
    public int UsedSlots { get; private set; }

    /// <summary>
    ///     Page 中剩余可用槽位数
    /// </summary>
    public int FreeSlots => PageSize - UsedSlots;

    /// <summary>
    ///     引用计数（用于 Prefix Cache 共享）
    /// </summary>
    public int RefCount { get; set; }

    /// <summary>
    ///     物理页 ID
    /// </summary>
    public int PageId { get; }

    /// <summary>
    ///     向 Page 追加 KV 数据
    /// </summary>
    /// <param name="newKeys">新 Key 数据 [numHeads, newLen, dK]</param>
    /// <param name="newValues">新 Value 数据 [numHeads, newLen, dK]</param>
    /// <param name="srcOffset">源数据中的 token 偏移</param>
    /// <param name="count">要追加的 token 数</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dK">每个头的维度</param>
    public void Append(ReadOnlySpan<float> newKeys, ReadOnlySpan<float> newValues,
        int srcOffset, int count, int numHeads, int dK)
    {
        if (UsedSlots + count > PageSize)
            throw new InvalidOperationException($"Page 已满：UsedSlots={UsedSlots}, count={count}, PageSize={PageSize}");

        var dstBase = UsedSlots * numHeads * dK;
        var srcBase = srcOffset * numHeads * dK;
        var copyLen = count * numHeads * dK;

        newKeys.Slice(srcBase, copyLen).CopyTo(_keyData.AsSpan(dstBase, copyLen));
        newValues.Slice(srcBase, copyLen).CopyTo(_valueData.AsSpan(dstBase, copyLen));

        UsedSlots += count;
    }

    /// <summary>
    ///     读取 Page 中的 KV 数据
    /// </summary>
    /// <param name="dstKeys">目标 Key 缓冲区</param>
    /// <param name="dstValues">目标 Value 缓冲区</param>
    /// <param name="dstOffset">目标缓冲区中的 token 偏移</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dK">每个头的维度</param>
    public void Read(Span<float> dstKeys, Span<float> dstValues,
        int dstOffset, int numHeads, int dK)
    {
        var copyLen = UsedSlots * numHeads * dK;
        var srcOffset = 0;
        var dstBase = dstOffset * numHeads * dK;

        _keyData.AsSpan(srcOffset, copyLen).CopyTo(dstKeys.Slice(dstBase, copyLen));
        _valueData.AsSpan(srcOffset, copyLen).CopyTo(dstValues.Slice(dstBase, copyLen));
    }

    /// <summary>
    ///     重置 Page（清空数据，但保留分配）
    /// </summary>
    public void Reset()
    {
        UsedSlots = 0;
        Array.Clear(_keyData);
        Array.Clear(_valueData);
    }

    /// <summary>
    ///     深拷贝 Page 数据到目标 Page（用于 Copy-On-Write）
    /// </summary>
    /// <param name="target">目标 Page</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dK">每个头的维度</param>
    public void DeepCopyInto(KVPage target, int numHeads, int dK)
    {
        var copyLen = UsedSlots * numHeads * dK;
        if (copyLen > 0)
        {
            _keyData.AsSpan(0, copyLen).CopyTo(target._keyData.AsSpan(0, copyLen));
            _valueData.AsSpan(0, copyLen).CopyTo(target._valueData.AsSpan(0, copyLen));
        }

        target.UsedSlots = UsedSlots;
    }
}

/// <summary>
///     页表 —— 每个请求的虚拟页到物理页映射
///     虚拟页按序列顺序排列，物理页可以不连续
/// </summary>
public sealed class PageTable
{
    private readonly List<int> _physicalPages;

    /// <summary>
    ///     创建页表
    /// </summary>
    public PageTable()
    {
        _physicalPages = [];
    }

    /// <summary>
    ///     虚拟页到物理页的映射（索引=虚拟页序号，值=物理页 ID）
    /// </summary>
    public IReadOnlyList<int> PhysicalPages => _physicalPages;

    /// <summary>
    ///     当前映射的虚拟页数量
    /// </summary>
    public int PageCount => _physicalPages.Count;

    /// <summary>
    ///     追加一个物理页映射
    /// </summary>
    /// <param name="physicalPageId">物理页 ID</param>
    public void AppendPage(int physicalPageId)
    {
        _physicalPages.Add(physicalPageId);
    }

    /// <summary>
    ///     获取指定虚拟页对应的物理页 ID
    /// </summary>
    /// <param name="virtualPageIdx">虚拟页索引</param>
    /// <returns>物理页 ID</returns>
    public int GetPhysicalPageId(int virtualPageIdx)
    {
        return _physicalPages[virtualPageIdx];
    }

    /// <summary>
    ///     替换指定虚拟页的物理页映射（用于 Copy-On-Write）
    /// </summary>
    /// <param name="virtualPageIdx">虚拟页索引</param>
    /// <param name="newPhysicalPageId">新的物理页 ID</param>
    public void ReplacePage(int virtualPageIdx, int newPhysicalPageId)
    {
        _physicalPages[virtualPageIdx] = newPhysicalPageId;
    }

    /// <summary>
    ///     移除最后一个虚拟页映射
    /// </summary>
    public void RemoveLastPage()
    {
        if (_physicalPages.Count > 0) _physicalPages.RemoveAt(_physicalPages.Count - 1);
    }

    /// <summary>
    ///     截断到指定虚拟页数量
    /// </summary>
    /// <param name="pageCount">目标虚拟页数量</param>
    public void Truncate(int pageCount)
    {
        while (_physicalPages.Count > pageCount) _physicalPages.RemoveAt(_physicalPages.Count - 1);
    }

    /// <summary>
    ///     清空所有映射
    /// </summary>
    public void Clear()
    {
        _physicalPages.Clear();
    }
}

/// <summary>
///     块管理器 —— 管理物理页池的分配、释放和引用计数
///     维护一个空闲页列表，按需分配和回收
/// </summary>
public sealed class BlockManager
{
    private readonly int _dK;
    private readonly Stack<int> _freePageIds;
    private readonly int _numHeads;
    private readonly int _pageSize;
    private readonly List<KVPage> _pages;
    private int _nextPageId;

    /// <summary>
    ///     创建块管理器
    /// </summary>
    /// <param name="pageSize">每页 token 数</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dK">每个头的维度</param>
    /// <param name="initialPages">初始预分配页数</param>
    public BlockManager(int pageSize, int numHeads, int dK, int initialPages = 256)
    {
        _pageSize = pageSize;
        _numHeads = numHeads;
        _dK = dK;
        _pages = [];
        _freePageIds = new Stack<int>();
        _nextPageId = 0;

        for (var i = 0; i < initialPages; i++) AllocateNewPage();
    }

    /// <summary>
    ///     总物理页数（含已分配和空闲）
    /// </summary>
    public int TotalPages => _pages.Count;

    /// <summary>
    ///     空闲页数
    /// </summary>
    public int FreePageCount => _freePageIds.Count;

    /// <summary>
    ///     已分配页数
    /// </summary>
    public int AllocatedPageCount => _pages.Count - _freePageIds.Count;

    /// <summary>
    ///     分配一个物理页
    /// </summary>
    /// <returns>分配的物理页</returns>
    public KVPage Allocate()
    {
        if (_freePageIds.Count > 0)
        {
            var pageId = _freePageIds.Pop();
            var page = _pages[pageId];
            page.RefCount = 1;
            page.Reset();
            return page;
        }

        return AllocateNewPage();
    }

    /// <summary>
    ///     释放一个物理页（引用计数 -1，归零时回收）
    /// </summary>
    /// <param name="pageId">物理页 ID</param>
    public void Free(int pageId)
    {
        if (pageId < 0 || pageId >= _pages.Count) return;

        var page = _pages[pageId];
        page.RefCount--;
        if (page.RefCount <= 0)
        {
            page.Reset();
            _freePageIds.Push(pageId);
        }
    }

    /// <summary>
    ///     增加物理页的引用计数（用于 Prefix Cache 共享）
    /// </summary>
    /// <param name="pageId">物理页 ID</param>
    public void AddRef(int pageId)
    {
        if (pageId >= 0 && pageId < _pages.Count) _pages[pageId].RefCount++;
    }

    /// <summary>
    ///     获取指定 ID 的物理页
    /// </summary>
    /// <param name="pageId">物理页 ID</param>
    /// <returns>物理页</returns>
    public KVPage GetPage(int pageId)
    {
        return _pages[pageId];
    }

    /// <summary>
    ///     Copy-On-Write：复制一个物理页，返回新页 ID
    ///     当共享页需要修改时，创建副本，原页引用计数减一
    /// </summary>
    /// <param name="srcPageId">源物理页 ID</param>
    /// <returns>新物理页的 ID</returns>
    public int CopyOnWrite(int srcPageId)
    {
        var srcPage = _pages[srcPageId];
        srcPage.RefCount--;

        var newPage = Allocate();
        srcPage.DeepCopyInto(newPage, _numHeads, _dK);

        return newPage.PageId;
    }

    /// <summary>
    ///     扩展页池（预分配更多页）
    /// </summary>
    /// <param name="count">要新增的页数</param>
    public void Expand(int count)
    {
        for (var i = 0; i < count; i++) AllocateNewPage();
    }

    private KVPage AllocateNewPage()
    {
        var page = new KVPage(_nextPageId, _pageSize, _numHeads, _dK);
        _pages.Add(page);
        _freePageIds.Push(_nextPageId);
        _nextPageId++;
        return page;
    }
}

/// <summary>
///     请求句柄 —— 标识一个推理请求在 PagedKVCache 中的状态
/// </summary>
public sealed class RequestHandle
{
    /// <summary>
    ///     创建请求句柄
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    /// <param name="numLayers">Transformer 层数</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    public RequestHandle(int requestId, int numLayers, int maxSeqLen)
    {
        RequestId = requestId;
        PageTables = new PageTable[numLayers];
        for (var i = 0; i < numLayers; i++) PageTables[i] = new PageTable();
        CurrentLength = 0;
        MaxSeqLen = maxSeqLen;
        IsCompleted = false;
        SharedPrefixPages = 0;
    }

    /// <summary>
    ///     请求唯一 ID
    /// </summary>
    public int RequestId { get; init; }

    /// <summary>
    ///     该请求的页表（每层一个）
    /// </summary>
    public PageTable[] PageTables { get; }

    /// <summary>
    ///     当前已缓存的 token 数
    /// </summary>
    public int CurrentLength { get; set; }

    /// <summary>
    ///     最大序列长度
    /// </summary>
    public int MaxSeqLen { get; init; }

    /// <summary>
    ///     是否已完成生成
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    ///     前缀共享的页数（与系统提示共享的虚拟页数量）
    /// </summary>
    public int SharedPrefixPages { get; set; }
}

/// <summary>
///     PagedAttention KV Cache —— vLLM 风格分页 KV Cache 管理
///     将 KV Cache 按虚拟内存页管理，解决 KV Cache 碎片化问题
///     支持多请求并发、Copy-On-Write、Prefix Cache 复用
/// </summary>
public sealed class PagedKVCache
{
    private readonly int _dK;
    private readonly int _numHeads;
    private readonly int _numLayers;
    private readonly Dictionary<int, RequestHandle> _requests;
    private int _nextRequestId;

    /// <summary>
    ///     创建 PagedAttention KV Cache
    /// </summary>
    /// <param name="numLayers">Transformer 层数</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="dK">每个头的维度</param>
    /// <param name="pageSize">每页 token 数（默认 16）</param>
    /// <param name="initialPages">初始预分配页数（默认 256）</param>
    public PagedKVCache(int numLayers, int numHeads, int dK, int pageSize = 16, int initialPages = 256)
    {
        _numLayers = numLayers;
        _numHeads = numHeads;
        _dK = dK;
        PageSize = pageSize;
        Blocks = new BlockManager(pageSize, numHeads, dK, initialPages);
        _requests = new Dictionary<int, RequestHandle>();
        _nextRequestId = 0;
    }

    /// <summary>
    ///     每页 token 数
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    ///     当前活跃请求数
    /// </summary>
    public int ActiveRequestCount => _requests.Count;

    /// <summary>
    ///     块管理器（用于监控内存使用）
    /// </summary>
    public BlockManager Blocks { get; }

    /// <summary>
    ///     创建新的推理请求
    /// </summary>
    /// <param name="maxSeqLen">最大序列长度</param>
    /// <returns>请求句柄</returns>
    public RequestHandle CreateRequest(int maxSeqLen = 2048)
    {
        var requestId = _nextRequestId++;
        var handle = new RequestHandle(requestId, _numLayers, maxSeqLen);
        _requests[requestId] = handle;
        return handle;
    }

    /// <summary>
    ///     创建共享前缀的推理请求
    ///     新请求与已有请求共享系统提示的 KV Cache Page
    /// </summary>
    /// <param name="prefixHandle">已有请求（包含系统提示的 KV Cache）</param>
    /// <param name="prefixTokenCount">前缀 token 数</param>
    /// <param name="maxSeqLen">最大序列长度</param>
    /// <returns>新请求句柄</returns>
    public RequestHandle CreateRequestWithPrefix(RequestHandle prefixHandle, int prefixTokenCount, int maxSeqLen = 2048)
    {
        var handle = CreateRequest(maxSeqLen);

        var prefixPages = (prefixTokenCount + PageSize - 1) / PageSize;
        handle.SharedPrefixPages = prefixPages;
        handle.CurrentLength = prefixTokenCount;

        for (var layer = 0; layer < _numLayers; layer++)
        {
            var srcTable = prefixHandle.PageTables[layer];
            var dstTable = handle.PageTables[layer];

            for (var p = 0; p < prefixPages && p < srcTable.PageCount; p++)
            {
                var physicalPageId = srcTable.GetPhysicalPageId(p);
                Blocks.AddRef(physicalPageId);
                dstTable.AppendPage(physicalPageId);
            }
        }

        return handle;
    }

    /// <summary>
    ///     向请求追加 KV 数据（预填充或自回归生成）
    /// </summary>
    /// <param name="handle">请求句柄</param>
    /// <param name="layerIdx">层索引</param>
    /// <param name="newKeys">新 Key [numHeads, newSeqLen, dK]</param>
    /// <param name="newValues">新 Value [numHeads, newSeqLen, dK]</param>
    public void Append(RequestHandle handle, int layerIdx, ArrayND newKeys, ArrayND newValues)
    {
        var newSeqLen = newKeys.Shape[1];
        var spanK = newKeys.AsSpan();
        var spanV = newValues.AsSpan();

        var pageTable = handle.PageTables[layerIdx];
        var currentInPage = handle.CurrentLength % PageSize;

        var srcOffset = 0;
        var remaining = newSeqLen;

        if (currentInPage > 0 && pageTable.PageCount > 0)
        {
            var lastPageId = pageTable.GetPhysicalPageId(pageTable.PageCount - 1);
            var lastPage = Blocks.GetPage(lastPageId);

            if (lastPage.RefCount > 1)
            {
                var newPageId = Blocks.CopyOnWrite(lastPageId);
                pageTable.ReplacePage(pageTable.PageCount - 1, newPageId);
                lastPage = Blocks.GetPage(newPageId);
            }

            var fillCount = System.Math.Min(remaining, lastPage.FreeSlots);
            lastPage.Append(spanK, spanV, srcOffset, fillCount, _numHeads, _dK);
            srcOffset += fillCount;
            remaining -= fillCount;
        }

        while (remaining > 0)
        {
            var page = Blocks.Allocate();
            pageTable.AppendPage(page.PageId);

            var fillCount = System.Math.Min(remaining, PageSize);
            page.Append(spanK, spanV, srcOffset, fillCount, _numHeads, _dK);
            srcOffset += fillCount;
            remaining -= fillCount;
        }

        if (layerIdx == 0) handle.CurrentLength += newSeqLen;
    }

    /// <summary>
    ///     获取请求的完整 KV Cache（从非连续页中收集）
    /// </summary>
    /// <param name="handle">请求句柄</param>
    /// <param name="layerIdx">层索引</param>
    /// <returns>完整 Key 和 Value [numHeads, totalLen, dK]</returns>
    public (ArrayND keys, ArrayND values) Get(RequestHandle handle, int layerIdx)
    {
        var totalLen = handle.CurrentLength;
        var pageTable = handle.PageTables[layerIdx];

        var fullKeys = ArrayND.Zeros(_numHeads, totalLen, _dK);
        var fullValues = ArrayND.Zeros(_numHeads, totalLen, _dK);

        var spanK = fullKeys.AsWriteSpan();
        var spanV = fullValues.AsWriteSpan();

        var tokenOffset = 0;
        for (var p = 0; p < pageTable.PageCount; p++)
        {
            var pageId = pageTable.GetPhysicalPageId(p);
            var page = Blocks.GetPage(pageId);

            if (page.UsedSlots > 0)
            {
                page.Read(spanK, spanV, tokenOffset, _numHeads, _dK);
                tokenOffset += page.UsedSlots;
            }
        }

        return (fullKeys, fullValues);
    }

    /// <summary>
    ///     释放请求的所有 KV Cache 页
    /// </summary>
    /// <param name="handle">请求句柄</param>
    public void ReleaseRequest(RequestHandle handle)
    {
        for (var layer = 0; layer < _numLayers; layer++)
        {
            var pageTable = handle.PageTables[layer];
            for (var p = 0; p < pageTable.PageCount; p++)
            {
                var pageId = pageTable.GetPhysicalPageId(p);
                Blocks.Free(pageId);
            }

            pageTable.Clear();
        }

        handle.CurrentLength = 0;
        handle.IsCompleted = true;
        _requests.Remove(handle.RequestId);
    }

    /// <summary>
    ///     截断请求的 KV Cache 到指定长度
    /// </summary>
    /// <param name="handle">请求句柄</param>
    /// <param name="length">目标长度</param>
    public void Truncate(RequestHandle handle, int length)
    {
        if (length >= handle.CurrentLength) return;

        var targetPages = (length + PageSize - 1) / PageSize;

        for (var layer = 0; layer < _numLayers; layer++)
        {
            var pageTable = handle.PageTables[layer];

            while (pageTable.PageCount > targetPages)
            {
                var pageId = pageTable.GetPhysicalPageId(pageTable.PageCount - 1);
                Blocks.Free(pageId);
                pageTable.RemoveLastPage();
            }
        }

        handle.CurrentLength = length;
    }

    /// <summary>
    ///     获取指定请求的句柄
    /// </summary>
    /// <param name="requestId">请求 ID</param>
    /// <returns>请求句柄，不存在则返回 null</returns>
    public RequestHandle? GetRequest(int requestId)
    {
        return _requests.GetValueOrDefault(requestId);
    }

    /// <summary>
    ///     计算当前内存利用率
    /// </summary>
    /// <returns>利用率（0.0 ~ 1.0）</returns>
    public float MemoryUtilization()
    {
        if (Blocks.AllocatedPageCount == 0) return 0.0f;

        var totalSlots = Blocks.AllocatedPageCount * PageSize;
        var usedSlots = 0;
        foreach (var handle in _requests.Values) usedSlots += handle.CurrentLength;

        return totalSlots > 0 ? (float)usedSlots / totalSlots : 0.0f;
    }
}