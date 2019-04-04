# Protobuf 序列化

## 定位

Protobuf（Protocol Buffers）是 Google 的**结构化二进制序列化格式**。Sonic 的 Protobuf 序列化器实现 `ISerializer`/`IDeserializer`，让你用与 JSON/MessagePack 相同的投影层代码产出 Protobuf 线格式字节，同时保留了字段标签（`field_number`）的显式控制。

## 为什么需要 Protobuf 同时还有 MessagePack？

MessagePack 和 Protobuf 都是紧凑二进制格式但哲学完全不同：

| | MessagePack | Protobuf |
|:---|:---|:---|
| Schema 依赖 | **否**，自描述 | **是**，需要 `.proto` |
| 字段标识 | 字符串键名 | 整数 field_number |
| 整数编码 | 固定宽度（fixint/int8/16/32/64） | **Varint**（小整数 1 字节） |
| 未知字段 | 正常反序列化（有键名） | **自动跳过**（无键名也有 wire_type） |
| 线上字节量 | 中等（键名是字符串） | 最小（键名是 varint 数字） |
| gRPC 兼容 | 否 | 是 |

**选 Protobuf 的核心场景**：
1. **gRPC**：Protobuf 是 gRPC 的唯一线格式。不做 Protobuf = 不能用 gRPC。
2. **极致带宽优化**：字段名叫 `"user_profile_image_url"` 在 MessagePack 里每次传输都是 25 字节；Protobuf 里是 1 字节 varint（field_number ≤ 15，tag = 1 byte）。
3. **Schema 强约束**：`.proto` 文件同时作为文档和契约，跨团队协作时比"口头约定字段名"更可靠。

## 为什么 `write_field_tag` 在 ProtobufSerializer 上而不是 `IObjectSerializer` 上？

这是 Protobuf 与 MessagePack/JSON 最本质的接口差异。

在 MessagePack/JSON 中，`write_field_name("age")` 写入键名字符串，然后值紧跟着写入。对于 Protobuf，你需要先写入 `varint(field_number << 3 | wire_type)`，然后写入值。

但 `write_field_name` 的参数是 `string name`——Protobuf 需要的是 field_number（整数）和 wire_type（由值的类型决定）。把这两个信息塞进字符串（如 `"1:0"`）在类型层面失去了静态检查。

因此把 `write_field_tag` 放在 `ProtobufSerializer` 上（而非 `IObjectSerializer`），让投影层在调用标量方法前显式写入：
```csharp
serializer.write_field_tag(1, 0);  // field 1, varint
serializer.serialize_i32(42);      // value
```

对于嵌套消息（wire_type=2），`write_field_name("1")` 在 `IObjectSerializer` 上写入 wire_type=2 的标签——因为嵌套消息总是 length-delimited，wire_type 固定。

## 为什么 Varint 是 Protobuf 的默认整数编码？

Varint（变长整数）的核心洞察：**大多数整数很小**。

- `age: 25` → 1 字节 varint（而是 4 字节 fixed int32）
- `user_id: 1538291` → 2 字节 varint
- `timestamp_ms: 1715846400000` → 6 字节 varint（仍是 8 字节 fixed int64 的 75%）

代价：Varint 编码每个字节用 1 bit 做 MSB（是否还有后续字节），有效位只有 7 bits/byte。对于均匀分布在 64 位范围的大整数（如 UUID），Varint 反而比固定宽度多 12.5% 字节。

Protobuf 同时提供了 `fixed32`/`fixed64`（wire_type 1/5）给大整数场景，由 `.proto` 中的 `fixed32`/`sfixed32` 声明。Sonic 的 `serialize_f32`/`serialize_f64` 使用固定 4/8 字节——对于浮点，varint 编码语义不适用。

## 不应该承担什么职责？

- **不应编译 `.proto` 文件**：Protobuf 序列化器是线格式引擎，不是 IDL 编译器。`.proto` → C# 类型的代码生成器是独立工具。
- **不应做类型校验**：Varint 解码为负数 `int32` 在 Protobuf 中是合法的（`int32` 使用 varint 编码，负值需要 10 字节）。值的语义正确性由 DataContract 验证。
- **不应处理 gRPC 帧格式**：gRPC 在 Protobuf 载荷外包了一层 5 字节的 gRPC 帧头（1B 压缩标志 + 4B BE 长度）。那是 gRPC 协议的职责。

## 与上下游的衔接

```
上游（投影层）→ write_field_tag(field_number, wire_type) → serialize_xxx(value)
    ↓
ProtobufSerializer → 内部 ArrayBufferWriter<byte>
    ↓
to_bytes() → byte[] → 由 gRPC / 文件 / 消息队列消费
```

## 典型场景

- **gRPC 服务间通信**：Protobuf 载荷 + gRPC 帧头（另见 `Data/Protocol/Grpc/`，待建）。
- **Kafka 消息最小化**：topic 的 key/value 用 Protobuf 编码，比 JSON 减少 60-80% 体积。结合 Schema Registry 做版本演进。
- **移动端 API（节省带宽）**：服务端用 JSON 响应 Web 前端，同时对 App 端提供 Protobuf 端点。App 的弱网环境下，Protobuf 的字节节省 = 更少的重传。