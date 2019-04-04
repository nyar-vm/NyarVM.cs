# gRPC 协议帧

## 定位

gRPC 在 HTTP/2 之上定义了自己的帧格式： **5 字节帧头**（1 字节压缩标志 + 4 字节大端序消息长度）+ Protobuf 载荷。Sonic 的
gRPC 实现提供这 5 字节帧头的封装/解帧，与 `Data/Binary/Protobuf` 配合使用。

## 为什么 gRPC 帧头是 5 字节？

HTTP/2 本身已经有帧头（9 字节：3 字节长度 + 1 字节类型 + 1 字节标志 + 4 字节流 ID）。gRPC 为什么还要再加一层？

因为 HTTP/2 的 DATA 帧可能被 **分片**——一个 gRPC 消息可能跨越多个 HTTP/2 DATA 帧。gRPC 的 5 字节 Length-Prefixed Message
头让接收方知道"这条消息到底有多长"，从而在 HTTP/2 帧碎片之上重组出完整的 Protobuf 消息。

```
HTTP/2 DATA 帧 1: [gRPC 头 5B][Protobuf 前 1000B]
HTTP/2 DATA 帧 2: [Protobuf 后 500B]
HTTP/2 DATA 帧 3: [gRPC 头 5B][下一条 Protobuf 消息...]
```

## 为什么压缩标志只是 1 字节 bool？

gRPC 的压缩协商在 HTTP 头部（`grpc-encoding: gzip` / `identity`）完成，帧头的压缩标志只是"这条消息是否被压缩了"
的标记。实际的压缩/解压算法由 `grpc-encoding` 头决定——帧头不关心你用的是 gzip 还是 snappy。

`GrpcFramer` 的 `compressed` 参数只设置这个标志位。如果你启用了 gRPC 压缩中间件，中间件会：1) 压缩载荷，2) 用
`GrpcFramer(compressed: true)` 封帧。解压方向同理。

## 为什么 gRPC 帧与 HTTP/2 帧是分离的？

Sonic 的 `GrpcFramer`/`GrpcUnframer` 只处理 gRPC 的 Length-Prefixed Message 格式。HTTP/2 的帧解析（HPACK
头压缩、流复用、流量控制）是另一个完整的协议层。

分离的原因：

1. **gRPC-over-HTTP/2 和 gRPC-over-WebSocket** 都用同样的 5 字节帧头。HTTP/2 不是唯一的传输层。
2. **测试隔离**：你可以不启动 HTTP/2 服务器，直接用 TCP 传输 gRPC 帧——这在嵌入式场景和单元测试中非常有用。
3. **HTTP/2 实现复杂度远超帧封装**：HPACK、流优先级、服务器推送——这些不属于 Sonic.Data 的职责。

## 不应该承担什么职责？

- **不应实现 HTTP/2 帧解析**：HPACK、SETTINGS、WINDOW_UPDATE 等帧类型是 HTTP/2 层的事。
- **不应做 Protobuf 编解码**：gRPC 帧只管 5 字节头 + 载荷边界。载荷的 Protobuf 解码由 `ProtobufDeserializer` 处理。
- **不应管理 gRPC 流**：Unary、Server Streaming、Client Streaming、Bidirectional Streaming 四种调用模式是上层状态机的事。
- **不应做压缩/解压**：只设置标志位，实际压缩由中间件处理。

## 与上下游的衔接

```
上游（ProtobufSerializer.to_bytes()）→ Protobuf 载荷
    ↓
GrpcFramer.frame(payload, writer)  → 5B 头 + 载荷
    ↓
下游（HTTP/2 DATA 帧或 WebSocket 帧）→ 传输

接收方向：
上游（HTTP/2 DATA 帧或 WebSocket 帧）→ 喂入字节
    ↓
GrpcUnframer.feed(data) → 累积
GrpcUnframer.try_get_next_frame() → 完整 gRPC 帧
    ↓
下游（ProtobufDeserializer）→ 解码 Protobuf 载荷
```

## 典型场景

- **gRPC 服务端**：收 HTTP/2 DATA 帧 → GrpcUnframer 解帧 → ProtobufDeserializer 反序列化请求。
- **gRPC 客户端**：ProtobufSerializer 序列化 → GrpcFramer 封帧 → 发送 HTTP/2 DATA 帧。
- **gRPC-Web 代理**：浏览器发 gRPC-Web（非 HTTP/2），代理转码为标准 gRPC 帧后转发。