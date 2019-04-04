# Server-Sent Events (SSE) 协议帧

## 定位

SSE 是一种 **单向推送**协议：服务端通过 HTTP 响应持续向客户端发送事件流。格式极其简单——纯文本，每条事件由 `data:` 行和空行分隔。

Sonic 的 SSE 实现只提供 `SseFramer`（服务端发送方向），因为 SSE 协议没有客户端→服务端的事件帧。

## 为什么 SSE 比 WebSocket 简单？

SSE 是 HTTP 的自然延伸——服务端不发 `Content-Length`，不发 `Transfer-Encoding: chunked` 终止标记，连接一直开着，有事件就写一行
`data: ...`。浏览器原生支持 `EventSource` API。

代价是：

- **只能服务端→客户端**：客户端发消息需要另外的 HTTP 请求。
- **只能是文本**：载荷必须是 UTF-8 文本。二进制数据需要 Base64。
- **自动重连**：浏览器在连接断开后自动重连，发送 `Last-Event-ID` 头。服务端需要记住上次发送的 ID。

但正是这些限制让 SSE 在 **单向推送场景**中远优于 WebSocket：

- 不需要 WebSocket 握手。
- 不需要处理掩码、分片、opcode。
- 天然兼容 HTTP/1.1（不需要 HTTP/2 升级）。
- 天然兼容 CDN/反向代理（就是普通 HTTP 响应）。

## 为什么只有 Framer 没有 Unframer？

SSE 的接收端是浏览器 `EventSource` API。服务端不需要解析 SSE 帧——它只负责 **生成**帧。

如果你需要在服务端解析 SSE 流（比如 SSE 聚合代理），可以用 `DelimiterUnframer("\n\n")` 切帧——SSE 的帧边界就是双换行符。不需要专门的
SSE Unframer。

## 为什么 `SseFramer` 支持 `event_type` 和 `event_id`？

SSE 规范允许三种行类型：

- `data: ...` — 事件数据（必需）。
- `event: ...` — 事件类型（可选）。客户端 `EventSource` 可以按类型注册监听器。
- `id: ...` — 事件 ID（可选）。浏览器断线重连时发送 `Last-Event-ID: xxx`，服务端据此续传。

`SseFramer` 的构造参数允许设置 `eventType` 和 `eventId`。如果省略，只输出 `data:` 行——这是最常见的用法。

## 不应该承担什么职责？

- **不应管理 HTTP 响应生命周期**：SSE 帧写入 `IBufferWriter<byte>`，何时 flush、何时关闭连接是 HTTP 层的事。
- **不应实现重连逻辑**：服务端收到 `Last-Event-ID` 后的续传逻辑是应用层的事。
- **不应做心跳**：SSE 规范建议服务端定期发注释行（`: keepalive\n\n`）防止代理超时。这是应用层策略。

## 与上下游的衔接

```
上游（业务层）→ 构造事件数据（JSON 字符串 / 纯文本）
    ↓
SseFramer(eventType: "update", eventId: "42").frame(payload, writer)
    ↓ 输出格式：
    id: 42\n
    event: update\n
    data: {"price": 100.5}\n
    \n
下游（HTTP 响应流）→ 持续写入，客户端 EventSource 自动解析
```

## 典型场景

- **股票行情推送**：服务端持续推送价格变动，客户端 `EventSource` 监听 `event: price`。
- **日志流**：CI/CD 构建日志实时输出，SSE 比 WebSocket 简单且兼容性更好。
- **通知推送**：用户的通知列表实时更新，SSE 天然兼容 HTTP 缓存和 CDN。