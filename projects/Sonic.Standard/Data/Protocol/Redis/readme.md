# Redis RESP 协议帧

## 定位

Redis RESP（REdis Serialization Protocol）是 Redis 客户端与服务端之间的 **线协议**。它不是序列化框架——它定义的是"如何在 TCP
字节流中表达命令和响应"。

Sonic 的 Redis 实现是一套 `IFramer`/`IUnframer`，让你能够通过 `Socket` 直接与 Redis 服务端通信，不依赖第三方客户端。

## 为什么需要 RESP 的 Framer/Unframer？

Redis 生态中最流行的 `StackExchange.Redis` 是一个完整的客户端库——连接池、命令队列、Pipeline、事务全部打包在内。但它带来的代价是：

1. **依赖重**：`StackExchange.Redis` 引入管道调度器、IO 多路复用、物理连接管理等复杂组件。如果你只是想"缓存一下计算结果"
   ，这些全是你不想要的开销。
2. **阻塞你的 I/O 模型**：`StackExchange.Redis` 自己管理 Socket 和 I/O 循环，不与你的 `Socket`/`Pipe` 共享 I/O
   线程。这在你已经拥有自己的事件循环时是冲突的。

Sonic 的 Redis 层只给帧封装——把 RESP 命令格式化为字节，把 RESP 响应切帧。调用方自己管理 Socket 和重连。
**控制权全在你手上**。

## RESP 的五种消息类型

RESP 的巧妙在于 **每种类型第一字节自标识**：

| 首字节 | 类型          | 示例                               | 语义                         |
|:-------|:--------------|:-----------------------------------|:-----------------------------|
| `+`    | Simple String | `+OK\r\n`                          | 状态回复                     |
| `-`    | Error         | `-ERR unknown command\r\n`         | 错误                         |
| `:`    | Integer       | `:1000\r\n`                        | 整数                         |
| `$`    | Bulk String   | `$5\r\nhello\r\n`                  | 二进制安全字符串（长度前缀） |
| `*`    | Array         | `*2\r\n$3\r\nGET\r\n$3\r\nkey\r\n` | 数组（各元素递归解析）       |

## 为什么 `RedisUnframer` 不返回"已解析的结构"而是返回原始帧？

`RedisUnframer` 返回整个 RESP 消息的原始字节作为 `current_frame`。它判断帧边界（`$5\r\nhello\r\n` 从 `$` 到最后一个 `\n`
），但 **不解析语义**。

原因：RESP 是最容易被直接解析的协议之一。把 `$` 解析为 Bulk String 的工作留给上层——它可能直接做
`payload[1..payload.IndexOf("\r\n")]` 取长度，或 `switch(payload[0])` 分发到不同处理器。拆出一个中间对象（`RedisMessage`
）反而增加了分配开销。

## 不应该承担什么职责？

- **不应管理 Redis 连接**：重连、认证、`SELECT db` 是应用层逻辑。
- **不应提供命令映射**：没有 `SET(key, value)` 方法。你需要自己拼 `*3\r\n$3\r\nSET\r\n...` 然后用 `RedisFramer` 封帧（或直接用
  `RedisFramer` 包装预格式化的 RESP 字符串）。
- **不应实现 Pub/Sub、事务、Pipeline**：这些都是"发了多条 RESP 命令后收到多条 RESP 响应"的模式，应由上层管理命令-响应的配对。

## 与上下游的衔接

```
上游（应用层）→ 构造 RESP 格式的字符串或字节（如 "*3\r\n$3\r\nSET\r\n..." ）
    ↓
RedisFramer.frame(payload, writer)  → 包装为 Bulk String 帧（$N\r\npayload\r\n）
    ↓
下游（Socket.Send）→ 发送 TCP 字节

上游（Socket.Receive）→ 喂入 TCP 字节
    ↓
RedisUnframer.feed(data) → 内部累积
RedisUnframer.try_get_next_frame() → 返回完整 RESP 消息的原始字节
    ↓
下游（应用层）→ 按首字节分发：+/-/: → 简单处理，$ → 取长度+内容，* → 递归解析
```

## 典型场景

- **轻量缓存层**：`SET key value`、`GET key` —— 不需要 `StackExchange.Redis` 的完整重量级客户端。
- **嵌入式 Redis 代理**：你的服务作为 Redis 协议的透明代理，收到 RESP 原封不动转发给后端 Redis。
- **RESP 协议测试工具**：手动构造畸形 RESP 帧，测试 Redis 的容错能力。