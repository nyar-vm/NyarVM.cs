using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Web;

/// <summary>
///     Web 方言的 OA 接口定义。
///     该接口声明 UI 结构、HTTP 交互与宿主平台相关的 Web 操作。
/// </summary>
[Dialect("web")]
public interface IWeb<T>
{
    /// <summary>
    ///     创建元素节点。
    /// </summary>
    [Operator("element")]
    Term<T> Element(string tag, IReadOnlyList<Term<T>> attributes, IReadOnlyList<Term<T>> children);

    /// <summary>
    ///     创建文本节点。
    /// </summary>
    [Operator("text_node")]
    Term<T> TextNode(string content);

    /// <summary>
    ///     创建片段节点。
    /// </summary>
    [Operator("fragment")]
    Term<T> Fragment(IReadOnlyList<Term<T>> children);

    /// <summary>
    ///     创建组件节点。
    /// </summary>
    [Operator("component")]
    Term<T> Component(string name, IReadOnlyDictionary<string, Term<T>> props, Term<T> body);

    /// <summary>
    ///     创建属性节点。
    /// </summary>
    [Operator("attr")]
    Term<T> Attr(string name, Term<T> value);

    /// <summary>
    ///     创建样式节点。
    /// </summary>
    [Operator("style")]
    Term<T> Style(IReadOnlyDictionary<string, string> properties);

    /// <summary>
    ///     创建事件节点。
    /// </summary>
    [Operator("event")]
    Term<T> Event(string event_name, Term<T> handler);

    /// <summary>
    ///     创建条件节点。
    /// </summary>
    [Operator("cond")]
    Term<T> Cond(Term<T> condition, Term<T> then_branch, Term<T> else_branch);

    /// <summary>
    ///     创建列表渲染节点。
    /// </summary>
    [Operator("list_render")]
    Term<T> ListRender(Term<T> items, Term<T> render_fn);

    /// <summary>
    ///     创建路由节点。
    /// </summary>
    [Operator("route")]
    Term<T> Route(string method, string path, Term<T> handler);

    /// <summary>
    ///     创建中间件节点。
    /// </summary>
    [Operator("middleware")]
    Term<T> Middleware(Term<T> handler, Term<T> next);

    /// <summary>
    ///     创建请求节点。
    /// </summary>
    [Operator("request")]
    Term<T> Request();

    /// <summary>
    ///     创建响应节点。
    /// </summary>
    [Operator("response")]
    Term<T> Response(int status_code, Term<T> body, IReadOnlyDictionary<string, string> headers);

    /// <summary>
    ///     创建重定向节点。
    /// </summary>
    [Operator("redirect")]
    Term<T> Redirect(int status_code, string location);

    /// <summary>
    ///     创建 JSON 响应节点。
    /// </summary>
    [Operator("json")]
    Term<T> Json(Term<T> value);

    /// <summary>
    ///     创建守卫节点。
    /// </summary>
    [Operator("guard")]
    Term<T> Guard(Term<T> condition, Term<T> handler);

    /// <summary>
    ///     查询 DOM 节点。
    /// </summary>
    [Operator("dom_query")]
    Term<T> DomQuery(string selector);

    /// <summary>
    ///     修改 DOM 节点。
    /// </summary>
    [Operator("dom_mutate")]
    Term<T> DomMutate(Term<T> target, string property, Term<T> value);

    /// <summary>
    ///     读取存储项。
    /// </summary>
    [Operator("storage_get")]
    Term<T> StorageGet(string storage_type, string key);

    /// <summary>
    ///     写入存储项。
    /// </summary>
    [Operator("storage_set")]
    Term<T> StorageSet(string storage_type, string key, Term<T> value);
}