# Web 方言

## 概述

Web 方言覆盖前后端同构开发，包含前端 UI 描述与后端 HTTP 服务两部分。

## 前端方言 (Nyar.Web.Frontend)

### 节点定义

#### UI 结构

| 节点 | 描述 | 示例 | 效应 |
|------|------|------|------|
| Element | 创建 DOM 元素 | `(Element "div" (Attr "class" "container") children)` | 纯 |
| Text | 创建文本节点 | `(Text "Hello")` | 纯 |
| Fragment | 虚拟容器 | `(Fragment children)` | 纯 |
| Component | 用户定义组件 | `(Component MyApp props)` | 取决于内部 |

#### 属性/样式

| 节点 | 描述 | 示例 |
|------|------|------|
| Attr | 静态属性 | `(Attr "id" "main")` |
| Style | CSS 样式对象 | `(Style "color: red;")` |
| Class | CSS 类名（可动态） | `(Class "btn" (Cond active "btn-active" ""))` |
| Event | 事件监听器 | `(Event "click" handler)` |

#### 控制流

| 节点 | 描述 | 示例 |
|------|------|------|
| Cond | 条件渲染 | `(Cond show (Element ...) (Text ""))` |
| List | 列表渲染（带 key） | `(List items (lambda (item) (Element "li" ...)))` |
| Portal | 渲染到其他 DOM 节点 | `(Portal target children)` |

#### 生命周期

| 节点 | 描述 |
|------|------|
| OnMount | 组件挂载后执行 |
| OnUnmount | 组件卸载前执行 |

#### 平台 API

| 节点 | 描述 | 效应 |
|------|------|------|
| DomQuery | 查询 DOM 节点 | 读 |
| DomMutate | 直接修改 DOM | 写 |
| StorageGet | 读取 localStorage | 读 |
| StorageSet | 写入 localStorage | 写 |

### 重写规则

| 规则名 | 模式 | 重写目标 | 触发条件 |
|--------|------|----------|----------|
| 静态属性合并 | `(Element tag (Attr k1 v1) (Attr k2 v2))` | 合并属性 | 属性名不冲突 |
| 常量折叠 | `(Cond true then else)` | `then` | 编译时已知条件 |
| 列表扁平化 | `(List arr (lambda (x) (Fragment ...)))` | `(Fragment (map lambda arr))` | 减少层级 |
| 相邻文本合并 | `(Fragment (Text a) (Text b))` | `(Text (concat a b))` | 纯文本 |
| 样式对象提取 | 多次出现的相同 Style 字面量 | 提升为常量引用 | 减少重复字符串 |

### 降级路径

- **Nyar VM 模式**：UI 意图由 VM 中的轻量级虚拟 DOM 运行时解释执行
- **AOT 编译模式**：编译为命令式 JavaScript/Wasm 代码

## 后端方言 (Nyar.Web.Backend)

### 节点定义

#### HTTP 语义

| 节点 | 描述 | 示例 |
|------|------|------|
| Route | 匹配路径和方法 | `(Route "GET" "/users/:id" handler)` |
| Middleware | 请求/响应拦截器 | `(Middleware auth (Route ...))` |
| Request | 传入请求对象 | `(Request)` |
| Response | 构造 HTTP 响应 | `(Response 200 body headers)` |
| Redirect | 重定向响应 | `(Redirect 302 "/new")` |
| Json | JSON 序列化 | `(Json value)` |

#### 业务逻辑

| 节点 | 描述 |
|------|------|
| Guard | 条件守卫 |
| Validate | 请求体验证 |

#### 副作用

| 节点 | 描述 | 效应 |
|------|------|------|
| Database | 数据库查询 | 读/写 |
| FileRead | 读取文件 | 读 |
| FileWrite | 写入文件 | 写 |
| Fetch | 外部 HTTP 请求 | 网络效应 |
| Sleep | 异步等待 | 定时效应 |

### 重写规则

| 规则名 | 模式 | 重写目标 | 说明 |
|--------|------|----------|------|
| 路由合并 | 嵌套 Route | 合并为路由表 | 构建路由树 |
| 中间件扁平化 | 嵌套 Middleware | 组合中间件 | 减少调用栈深度 |
| 提前返回优化 | Guard 条件静态为 false | 直接返回错误 | 常量折叠 |
| JSON Schema 编译 | Validate schema | 编译为高效校验函数 | 避免运行时解析 |
| 数据库查询下推 | 静态查询 | 提升到模块初始化 | 缓存查询计划 |

### 降级路径

- **Nyar VM 模式**：效应处理器对接 libuv / epoll / IOCP
- **AOT 编译模式**：生成原生可执行文件或 Node.js 兼容脚本

## 跨方言协同：SSR 示例

```
(Route "GET" "/posts/:id"
  (lambda (req)
    (let* ((id    (PathParam req "id"))
           (post  (Perform DatabaseQuery (Query "SELECT * FROM posts WHERE id = ?" id)))
           (html  (RenderToString (Component PostPage (Props post)))))
      (Response 200 full (Attr "Content-Type" "text/html")))))
```

优化动作：

- 前端优化：静态属性合并、常量折叠
- 数据库优化：检查索引，标记高效访问
- 渲染优化：静态子树提前生成字符串常量
- 跨层融合：数据库结果直接映射到 HTML 片段
