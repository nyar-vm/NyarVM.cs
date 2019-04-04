using Nyar.IR.Intent;

namespace Nyar.Dialect.Web.Nodes;

#region 前端 UI

[AlgebraNode]
public sealed partial record Element(string tag, IReadOnlyList<Id> attributes, IReadOnlyList<Id> children) : AlgebraNode;

[AlgebraNode]
public sealed partial record TextNode(string content) : AlgebraNode;

[AlgebraNode]
public sealed partial record Fragment(IReadOnlyList<Id> children) : AlgebraNode;

[AlgebraNode]
public sealed partial record Component(string name, IReadOnlyDictionary<string, Id> props, Id body) : AlgebraNode;

[AlgebraNode]
public sealed partial record Attr(string name, Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record Style(IReadOnlyDictionary<string, string> properties) : AlgebraNode;

[AlgebraNode]
public sealed partial record Event(string event_name, Id handler) : AlgebraNode;

[AlgebraNode]
public sealed partial record Cond(Id condition, Id then_branch, Id else_branch) : AlgebraNode;

[AlgebraNode]
public sealed partial record ListRender(Id items, Id render_fn) : AlgebraNode;

#endregion

#region 后端 HTTP

[AlgebraNode]
public sealed partial record Route(string method, string path, Id handler) : AlgebraNode;

[AlgebraNode]
public sealed partial record Middleware(Id handler, Id next) : AlgebraNode;

[AlgebraNode]
public sealed partial record Request : AlgebraNode;

[AlgebraNode]
public sealed partial record Response(int status_code, Id body, IReadOnlyDictionary<string, string> headers) : AlgebraNode;

[AlgebraNode]
public sealed partial record Redirect(int status_code, string location) : AlgebraNode;

[AlgebraNode]
public sealed partial record Json(Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record Guard(Id condition, Id handler) : AlgebraNode;

#endregion

#region 平台 API

[AlgebraNode]
public sealed partial record DomQuery(string selector) : AlgebraNode;

[AlgebraNode]
public sealed partial record DomMutate(Id target, string property, Id value) : AlgebraNode;

[AlgebraNode]
public sealed partial record StorageGet(string storage_type, string key) : AlgebraNode;

[AlgebraNode]
public sealed partial record StorageSet(string storage_type, string key, Id value) : AlgebraNode;

#endregion