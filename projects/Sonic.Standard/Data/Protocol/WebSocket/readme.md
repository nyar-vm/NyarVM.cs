# WebSocket 协议帧

## 定位

WebSocket（RFC 6455）定义了在 TCP 上的全双工通信帧格式。Sonic 的 WebSocket 实现提供帧封装/解帧，支持文本帧、二进制帧、关闭帧和
Ping/Pong 帧。

## 为什么 WebSocket 帧比 TCP "帧"复杂？

TCP 是字节流，没有消息边界。WebSocket 帧在 TCP 之上定义了消息边界，但它的帧头设计比简单的长度前缀复杂得多：

1. **操作码**：区分 text (0x01)、binary (0x02)、close (0x08)、ping (0x09)、pong (0x0A)。接收方需要根据操作码决定"
   这段载荷怎么处理"。
2. **掩码**：客户端发送的帧 **必须**掩码，服务端发送的帧 **不**掩码。这是 RFC 6455 的安全要求（防止缓存投毒攻击）。
3. **变长载荷长度**：125 以下 7 bit 编码，126-65535 用 2 字节扩展，65535 以上用 8 字节扩展。三级长度编码。
4. **分片**：大消息可以拆成多个帧（FIN=0 表示后续还有），最后一片 FIN=1。

## 为什么掩码是协议层的事而不是安全层的事？

掩码不是加密——它只是 XOR 一个 4 字节密钥，密钥就写在帧头里。任何人都可以 unmask。它的设计目的是防止 **代理缓存投毒**
：攻击者构造一个看起来像 HTTP GET 请求的 WebSocket 帧，让透明代理缓存"恶意响应"。掩码让攻击者无法控制帧的实际字节内容。

`WebSocketUnframer` 在 `is_server=true` 时自动 unmask 客户端帧——这是协议合规要求，不是可选的安全增强。

## 为什么 `WebSocketFramer` 不实现分片？

分片涉及 **应用层决策**：什么时候分？每片多大？正在发送的消息如何知道总长度？

如果帧层自动分片，调用方就无法控制分片边界——而 WebSocket 协议允许接收方在收到 FIN=0 的帧时就开始处理（流式消费）。自动分片会剥夺调用方的流式控制权。

当前 `WebSocketFramer` 总是设置 FIN=1（完整帧）。需要分片时，调用方自行构造多个帧。

## 不应该承担什么职责？

- **不应实现 WebSocket 握手**：HTTP Upgrade 请求/响应是 HTTP 层的事。
- **不应做分片重组**：FIN=0 的后续帧重组是上层状态机的事。
- **不应管理连接生命周期**：心跳（Ping/Pong）、超时、重连是应用层逻辑。
- **不应做 TLS**：WSS（WebSocket Secure）是 TLS 层的事。

## 与上下游的衔接

```
上游（业务层）→ 构造载荷字节
    ↓
WebSocketFramer(is_text: false).frame(payload, writer) → 加帧头（opcode + 长度 + 可选 mask）
    ↓
下游（Socket.Send）→ 发送 TCP 字节

接收方向：
上游（Socket.Receive）→ 喂入 TCP 字节
    ↓
WebSocketUnframer(is_server: true).feed(data) → 累积 + 自动 unmask
    ↓（通过 current_opcode 判断帧类型）
下游（业务层）→ opcode=0x01 → JSON 反序列化，opcode=0x02 → 二进制还原，opcode=0x08 → 关闭连接
```

## 典型场景

- **实时聊天**：客户端发文本帧（opcode=0x01），服务端广播给其他客户端。
- **游戏状态同步**：客户端发二进制帧（opcode=0x02），载荷是 Protobuf 编码的位置信息。
- **WebSocket 代理**：透明转发 WebSocket 帧，只解帧头检查 opcode，不解析载荷。