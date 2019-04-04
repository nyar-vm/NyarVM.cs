using System.Collections;
using Std.Category;

namespace Std.Collection.Queue;

/// <summary>
///     鍙岀闃熷垪锛圖eque锛夛紝鏀寔鍦ㄤ袱绔珮鏁堟彃鍏ュ拰鍒犻櫎銆?///
/// </summary>
/// <typeparam name="T">鍙岀闃熷垪涓厓绱犵殑绫诲瀷銆?/typeparam>
public class Deque<T> : IEnumerable<T>
{
    private readonly List<T> _data;

    /// <summary>
    ///     鍒濆鍖栦竴涓┖鍙岀闃熷垪�?    ///
    /// </summary>
    public Deque()
    {
        _data = [];
    }

    /// <summary>
    ///     鑾峰彇鍙岀闃熷垪涓殑鍏冪礌鏁伴噺銆?    ///
    /// </summary>
    public int count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Count;
    }

    /// <summary>
    ///     鑾峰彇涓€涓€硷紝鎸囩ず鍙岀闃熷垪鏄惁涓虹┖�?/summary>
    public bool is_empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Count == 0;
    }

    /// <summary>
    ///     杩斿洖浠庡墠绔悜鍚庣閬嶅巻鐨勬灇涓惧櫒銆?    ///
    /// </summary>
    /// <returns>姝ｅ簭閬嶅巻鐨勬灇涓惧櫒�?/returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     鍦ㄩ槦鍒楀墠绔彃鍏ュ厓绱犮€?    ///
    /// </summary>
    /// <param name="value">瑕佹彃鍏ョ殑鍏冪礌銆?/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void push_front(T value)
    {
        _data.Insert(0, value);
    }

    /// <summary>
    ///     鍦ㄩ槦鍒楀悗绔彃鍏ュ厓绱犮€?    ///
    /// </summary>
    /// <param name="value">瑕佹彃鍏ョ殑鍏冪礌銆?/param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void push_back(T value)
    {
        _data.Add(value);
    }

    /// <summary>
    ///     浠庨槦鍒楀墠绔脊鍑哄厓绱犮€?    ///
    /// </summary>
    /// <returns>濡傛灉闃熷垪闈炵┖鍒欒繑鍥炲寘鍚墠绔厓绱犵殑 <see cref="Option{T}" />锛屽惁鍒欒繑�?<see cref="Option{T}.none" />�?/returns>
    public Option<T> pop_front()
    {
        if (_data.Count == 0) return Option<T>.none;

        var value = _data[0];
        _data.RemoveAt(0);
        return Option<T>.some(value);
    }

    /// <summary>
    ///     浠庨槦鍒楀悗绔脊鍑哄厓绱犮€?    ///
    /// </summary>
    /// <returns>濡傛灉闃熷垪闈炵┖鍒欒繑鍥炲寘鍚悗绔厓绱犵殑 <see cref="Option{T}" />锛屽惁鍒欒繑�?<see cref="Option{T}.none" />�?/returns>
    public Option<T> pop_back()
    {
        if (_data.Count == 0) return Option<T>.none;

        var lastIndex = _data.Count - 1;
        var value = _data[lastIndex];
        _data.RemoveAt(lastIndex);
        return Option<T>.some(value);
    }

    /// <summary>
    ///     鏌ョ湅闃熷垪鍓嶇鍏冪礌浣嗕笉寮瑰嚭�?    ///
    /// </summary>
    /// <returns>濡傛灉闃熷垪闈炵┖鍒欒繑鍥炲寘鍚墠绔厓绱犵殑 <see cref="Option{T}" />锛屽惁鍒欒繑�?<see cref="Option{T}.none" />�?/returns>
    public Option<T> peek_front()
    {
        if (_data.Count == 0) return Option<T>.none;

        return Option<T>.some(_data[0]);
    }

    /// <summary>
    ///     鏌ョ湅闃熷垪鍚庣鍏冪礌浣嗕笉寮瑰嚭�?    ///
    /// </summary>
    /// <returns>濡傛灉闃熷垪闈炵┖鍒欒繑鍥炲寘鍚悗绔厓绱犵殑 <see cref="Option{T}" />锛屽惁鍒欒繑�?<see cref="Option{T}.none" />�?/returns>
    public Option<T> peek_back()
    {
        if (_data.Count == 0) return Option<T>.none;

        return Option<T>.some(_data[_data.Count - 1]);
    }

    /// <summary>
    ///     娓呯┖鍙岀闃熷垪涓殑鎵€鏈夊厓绱犮�?    ///
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void clear()
    {
        _data.Clear();
    }

    /// <summary>
    ///     浠庡墠绔悜鍚庣閬嶅巻姣忎釜鍏冪礌�?    ///
    /// </summary>
    /// <param name="action">瑕佸姣忎釜鍏冪礌鎵ц鐨勬搷浣溿�?/param>
    public void for_each(Action<T> action)
    {
        foreach (var item in _data) action(item);
    }
}