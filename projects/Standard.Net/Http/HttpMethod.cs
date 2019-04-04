namespace Std.Net.Http;

/// <summary>
///     HTTP 请求方法�?///
/// </summary>
public enum HttpMethod
{
    /// <summary>
    ///     GET 方法，获取资源�?    ///
    /// </summary>
    get,

    /// <summary>
    ///     POST 方法，提交数据�?    ///
    /// </summary>
    post,

    /// <summary>
    ///     PUT 方法，替换资源�?    ///
    /// </summary>
    put,

    /// <summary>
    ///     DELETE 方法，删除资源�?    ///
    /// </summary>
    delete,

    /// <summary>
    ///     PATCH 方法，部分更新资源�?    ///
    /// </summary>
    patch,

    /// <summary>
    ///     HEAD 方法，获取资源元信息�?    ///
    /// </summary>
    head,

    /// <summary>
    ///     OPTIONS 方法，获取通信选项�?    ///
    /// </summary>
    options,

    /// <summary>
    ///     TRACE 方法，回环测试�?    ///
    /// </summary>
    trace
}