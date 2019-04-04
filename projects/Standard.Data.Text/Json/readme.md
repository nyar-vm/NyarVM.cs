# JSON 序列化器

## 定位

JSON 序列化器是 Sonic.Data 的 **人类可读文本序列化实现**。它实现 `ISerializer` 和 `IDeserializer`，将对象转为 JSON 文本。

## 为什么是 JSON？

JSON 不是最快的，不是最紧凑的，但它有一个其他格式永远无法企及的优势： **人与机器共享同一份数据**。

开发者可以直接 `curl` API 看到响应，可以在浏览器 DevTools 中检查请求体，可以用 `jq`
命令行过滤字段，可以用任何文本编辑器打开配置文件。这些场景中，二进制格式瞬间失去意义。

但 JSON 的代价也很明显：

- **键名重复**：每条 `Person` 消息都要写 `"name"`、`"age"`，键名占比可达总字节的 30-50%。
- **数字无类型区分**：`42` 和 `42.0` 在 JSON 中是一样的，但 `int` 和 `double` 的反序列化语义不同。
- **没有二进制原生支持**：`byte[]` 必须 Base64 编码，膨胀 33%。

因此 JSON 的正确场合是： **人需要看的数据**。API 响应、配置文件、日志。RPC 内部通信不应该用 JSON——那是 MessagePack 的位置。

## 为什么基于 `System.Text.Json` 而非自建解析器？

`Utf8JsonReader`/`Utf8JsonWriter` 是 .NET 团队极致优化的 JSON 读写器，零分配、Span-based。自建一个 JSON
解析器不会更快——你会重复解决同样的问题（UTF-8 验证、数字解析、转义处理），且没有 JIT 深度优化的收益。

Sonic JSON 序列化器的价值不在"又一个 JSON 库"，而在于 **它是 `ISerializer` 的一个实现**——这意味着你的 `Person` 类型不需要知道
JSON。投影层（`ITypeSerializer<Person>`）向序列化器输出 `serialize_map → write_field_name → serialize_utf8`，换一个
`MessagePackSerializer` 就是 MessagePack，换一个 `XmlSerializer` 就是 XML。

## 不应该承担什么职责？

- **不应定义类型映射**：JSON 序列化器不知道 `Person`。它只知道"现在有人让我写一个 map，键是 `"name"`，值是 `"Alice"`"
  。类型知识全部在投影层。
- **不应做 JSON Schema 验证**：JSON Schema 验证是 DataContract 维度的事情——验证完整对象的结构和值域，不关心对象是怎么序列化的。

## 与上下游的衔接

- 上游：投影层（`ITypeSerializer<T>`）将类型字段按序拆解。
- 内部：`Utf8JsonWriter` 负责写入 JSON 语法。
- 下游：`IBufferWriter<byte>` 获取完整 JSON 字节。