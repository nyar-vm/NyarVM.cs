# MessagePack 序列化器

## 定位

MessagePack 是 Sonic.Data 的**紧凑二进制序列化实现**。它实现 `ISerializer` 和 `IDeserializer`，将对象转为 MessagePack 规范定义的二进制格式。

## 为什么选择 MessagePack？

在二进制序列化格式中（MessagePack vs Protobuf vs BSON vs Avro），MessagePack 的取舍是：

- **比 JSON 小 30-50%**：整数 0-127 只需要 1 字节（fixint），JSON 需要 1-3 字节。字符串没有引号和转义开销。
- **不需要 schema 文件**：与 Protobuf 不同，MessagePack 是自描述的。你可以解析一段 MessagePack 字节而无需 `.proto` 文件。代价是每条消息携带类型标记（首字节），字节效率略低于 Protobuf。
- **比 BSON 快**：BSON 在 MongoDB 中使用，设计偏向遍历速度（包含长度前缀、null 终止符），导致编码比 MessagePack 大。MessagePack 的设计偏向紧凑性。
- **比 Avro 简单**：Avro 需要 schema 才能读写。MessagePack 不需要——它更适合"通用消息队列中的通用消息"场景。

**选 MessagePack 的场景**：需要二进制紧凑性，但又不想引入 schema 依赖的内网 RPC、消息队列、Redis 缓存序列化。

## 为什么类型支持表看似"不完整"？

MessagePack 规范支持的类型和 Sonic `ISerializer` 接口的容器映射之间存在间隙。MessagePack 的 `ext` 类型（扩展类型）、`timestamp` 扩展在当前实现中未支持——这些场景极少出现在通用序列化中，属于 MessagePack 协议的特定领域。

## 与上下游的衔接

- 上游：投影层（`ITypeSerializer<T>`）将类型的字段按序拆解，传入 `ISerializer`。
- 下游：编码层（`IEncoder`）负责具体基本类型的字节表示。MessagePack 序列化器主要处理容器的 type marker（`fixmap` vs `map16`）和格式化逻辑。
- 最终输出：`IBufferWriter<byte>` 中的完整 MessagePack 帧。

## 与 JSON 序列化器的对比

|  | JSON | MessagePack |
|:---|:---|:---|
| 人类可读 | 是 | 否 |
| 字节大小 | 大（键名重复、引号、逗号） | 小（fixint、无引号键名） |
| 跨语言 | 是，所有语言一流支持 | 是，主流语言有库 |
| Schema 依赖 | 无 | 无 |
| 调试 | 轻松 | 需要工具 |
| 适用场景 | API、配置文件、日志 | RPC、消息队列、缓存