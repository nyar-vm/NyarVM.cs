# Deserialize 层 — 结构化数据反序列化

## 定位

Deserialize 层是 Serialize 层的读取侧对称操作：从格式化字节中按格式骨架"剥开"容器结构，读取键名和值。

## 为什么反序列化器比序列化器更难设计？

序列化是你 **主导**的——字段顺序、嵌套深度、是否跳过 null 值，全部由你决定。反序列化是你 **被迫适应**
的——你不知道对方按什么顺序发字段，不知道对方发了 5 个字段还是 50 个，不知道第 7 层嵌套里有没有你认识的路径。

因此反序列化器需要更多的"容错"原语：

- `try_read_null()` — "探测一下是不是 null，不是的话我还要读别的"，不能消耗字节。
- `read_field_name() → Result<string, Error>` — "给我下一个键名；如果对象结束了，告诉我"。

## 为什么对象遍历用 Result 而非 Try 模式？

`read_field_name` 返回 `Result<string, DeserializeError>` 而不是 `bool try_read_field_name(out string name)`。原因：

对象结束时不是"异常"——它是预期内的终止信号。但在读取键名时，有三种可能：

1. 读到合法键名 → 返回键名。
2. 对象结束 → 不再有键名，应退出循环。
3. 格式错误（非法字符）→ 真正的错误。

如果用 `TryXxx` 模式：`try_read_field_name(out name)` 返回 `false`——到底是"对象结束了"还是"格式错误我没法告诉你"
？调用方必须再调用一个 API 区分，增加复杂度和出错概率。

`Result<string, Error>` 天然表达三者：`Ok(name)` / `Err(ObjectEnd)` / `Err(OtherError)`。

## 为什么反序列化器不需要 `bytesConsumed`？

与解码层不同，反序列化器消费的是 **完整帧载荷**——Unframe 层已经保证了数据完整性。解码层需要 `bytesConsumed`
是因为它在字节级操作，而反序列化器操作的是格式化语义级别。如果一个 JSON 对象声称有 3 个字段但实际只有 2 个（缺 `}`
），这是格式错误，不是流控——帧已经完整了，是内容非法。

## 不应该承担什么职责？

- **不应区分缺失字段与 null 字段**：这是还原层的决策。反序列化器只负责告诉你"下一项是键 `"age"`，值是 `18`"，不负责判断
  `"age"` 是否必需。
- **不应跳过未知字段**：同样属于还原层。反序列化器忠实地报告每一个键名，由还原层的 `switch` 决定哪些 case 消费、哪些 default
  跳过。
- **不应做值域验证**：`age: -5` 在反序列化层不是错误。验证是 DataContract 的职责。
- **不应处理版本兼容**：字段在 v2 中消失了，v1 的序列化器发了，v2 的还原器应该忽略 unknown key。这是还原层的逻辑。

## 与上下游的衔接

```
上游（Unframe 输出的帧载荷）→ ReadOnlySpan<byte> 完整帧
    ↓
IDeserializer.deserialize_map() → 格式化字节 → 容器结构
IMapDeserializer.read_field_name() / deserialize_value() → 逐键值遍历
    ↓
下游（还原层 / ITypeDeserializer<T>）→ 根据键名还原到具体类型 T 的字段
```

## 典型场景

- **Web API 请求解析**：HTTP body 是 JSON，反序列化为 DTO。`read_field_name` 按字段名分发，未知字段跳过（向前兼容）。
- **配置文件加载**：`.json` 文件一帧到底（整个文件就是一个 map），反序列化为配置对象。
- **流式日志消费**：Kafka 消息反序列化，配合 `DelimiterUnframer` 逐行解析 JSON。