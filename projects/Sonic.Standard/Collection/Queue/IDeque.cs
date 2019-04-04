namespace Std.Collection.Queue;

/// <summary>
///     双端队列接口
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IDeque<T>
{
    /// <summary>
    ///     获取前端元素
    /// </summary>
    T front();

    /// <summary>
    ///     获取后端元素
    /// </summary>
    T back();

    /// <summary>
    ///     在前端插入元�?    ///
    /// </summary>
    /// <param name="value">要插入的元素</param>
    void push_front(T value);

    /// <summary>
    ///     在后端插入元�?    ///
    /// </summary>
    /// <param name="value">要插入的元素</param>
    void push_back(T value);

    /// <summary>
    ///     从前端弹出元�?    ///
    /// </summary>
    T pop_front();

    /// <summary>
    ///     从后端弹出元�?    ///
    /// </summary>
    T pop_back();
}