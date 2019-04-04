namespace Std.Database.Storage;

/// <summary>
///     数据库页面，存储引擎的基本单位
/// </summary>
internal sealed class Page
{
    private int _pin_count;

    /// <summary>
    ///     创建页面
    /// </summary>
    /// <param name="id">页面 ID</param>
    /// <param name="size">页面大小</param>
    public Page(long id, int size)
    {
        this.id = id;
        data = new byte[size];
        is_dirty = false;
    }

    /// <summary>
    ///     页面 ID
    /// </summary>
    public long id { get; }

    /// <summary>
    ///     页面数据
    /// </summary>
    public byte[] data { get; }

    /// <summary>
    ///     是否脏页（已被修改但未刷盘）
    /// </summary>
    public bool is_dirty { get; set; }

    /// <summary>
    ///     引用计数
    /// </summary>
    public int pin_count => _pin_count;

    /// <summary>
    ///     固定页面（防止被缓存置换）
    /// </summary>
    public void pin()
    {
        Interlocked.Increment(ref _pin_count);
    }

    /// <summary>
    ///     解除固定
    /// </summary>
    public void unpin()
    {
        Interlocked.Decrement(ref _pin_count);
    }
}