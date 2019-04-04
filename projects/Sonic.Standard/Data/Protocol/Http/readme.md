# HTTP 协议帧

## 定位

HTTP 协议帧实现了一套完整的 `IFramer` / `IUnframer`，将 HTTP/1.1 的 Content-Length 和响应状态行抽象为帧边界。

## 为什么 HTTP 分帧值得独立实现？

乍一想"HTTP 不就是 TCP 上发的文本吗"。事实上 HTTP 的分帧规则是协议栈中最复杂的之一：

- **Content-Length**：固定长度，最直接。但需要先解析头部才能知道 body 在哪。
- **Transfer-Encoding: chunked**：每个 chunk 自带长度前缀，最后一个 chunk 长度为 0。解析器需要在 chunked 状态机和 body
  累积之间切换。
- **Connection: close**：没有长度声明，以 TCP 关闭为帧结束。不适合连接复用。
- **多部分（multipart）**：boundary 分隔符切帧。

`HttpFrameFramer` 和 `HttpFrameUnframer` 当前实现了最简单的 Content-Length 模式——生成一个固定的 `HTTP/1.1 200 OK`
响应包装载荷。这是"把 Sonic 序列化结果通过 HTTP 返回"的最短路径。

## 为什么 HTTP 分帧不包含 header 解析？

HTTP header 的解析（`Content-Type: application/json`、`Authorization: Bearer xxx`）是协议层的事情，不属于分帧层。
`HttpFrameUnframer` 只需要知道"body 有多少字节"，所以它只需要解析 `Content-Length`。

完整的 HTTP 语义（方法、路径、头部集合、Cookie）需要一个独立的 HTTP 库来处理。`HttpFrameFramer`/`Unframer` 的目的是连接 Sonic
序列化管道到 HTTP 协议，而不是实现一个 HTTP 服务器。

## 不应该承担什么职责？

- **不应实现完整的 HTTP 语义**：没有 URI 路由、没有 Header 字典、没有 `Transfer-Encoding` 选择。
- **不应处理 HTTPS**：TLS 在分帧层之下，是 I/O 层的职责。
- **不应做 HTTP/2 帧复用**：HTTP/2 的分帧是二进制协议，与 HTTP/1.1 的文本协议设计完全不同，需要独立实现。

## 典型场景

- **快速搭建 HTTP API**：序列化 → HttpFrameFramer → Socket.Send。不需要引入 ASP.NET Core。
- **嵌入式 HTTP 响应**：IoT 设备返回传感器数据，HTTP 帧包装 MessagePack 载荷。