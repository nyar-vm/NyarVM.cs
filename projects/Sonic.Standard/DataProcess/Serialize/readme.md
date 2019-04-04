# Serialize 层 — 结构化数据序列化

## 定位

Serialize 层负责将复合数据结构转换为某种 **格式化表示**——JSON 的大括号和引号、MessagePack 的 fixmap 前缀、CSV
的逗号和引号转义。它是整个管道中第一个处理"结构"的层。

## 为什么只有三种容器？

`tuple`、`sequence`、`map`——乍一看太少，但仔细推敲会发现 **所有复合结构都能归约到这三种**：

- **struct / class**（带字段名的结构）→ `map`：字段名是键，字段值是值。
- **tuple struct**（无字段名的定长结构）→ `tuple`：按顺序排列。
- **List / Set / Array**（同构元素序列）→ `sequence`：重复的同构项。
- **Dictionary / HashMap** → `map`：与 struct 的区别只是键的运行时来源不同——对序列化器来说，都是"写一个键，写一个值，重复"。
- **Enum** → 投影层将变体名作为键名，变体数据作为值，组装为 `map`。

那为什么没有 `list` 和 `set`？因为对序列化器来说，list 和 set 的 **字节表示没有区别**——都是 `[]` 包裹的同构项序列。是否允许重复属于
DataContract 的业务语义，与格式骨架无关。

## 为什么需要子写入器模式？

`serialize_map` 返回 `IObjectSerializer`、`serialize_sequence` 返回 `IArraySerializer`，而不是
`void serialize_map(Action<IMapWriter> callback)`。

子写入器模式的核心价值是 **结构强校验**：

- `IObjectSerializer` 上只有 `write_field_name` 和 `write_value`，强迫键值成对出现。你不可能写出
  `{"name": "Alice", "age"}`（少一个值）——类型系统不让。
- `end()` 是显式的容器终止信号。如果忘了调用，`IDisposable` 的清理路径可以兜底报错。
- 序列化器内部持有对父序列化器的引用，可以维护嵌套深度、验证结构完整性。

对比回调模式（`serialize_map(callback)`）：回调不阻止你在 `{}` 内部直接写 `serialize_i32`
，因为序列化器上同时有标量方法和容器方法——回调模式的类型系统无法区分"在对象内"和"在顶层"。

## 为什么序列化器有状态？

序列化器是 DataProcess 四层抽象中 **唯一有状态的无状态层**——无状态是指它不跨帧记忆数据，有状态是指 **当前帧内部**
需要维护嵌套状态。

原因：JSON 的 `}` 知道闭合哪个 `{`，MessagePack 的 map 结束标记需要知道当前嵌套深度。这是 **格式固有的状态**，不是业务决定的。序列化器在
`end()` 时验证嵌套深度回到零，如果多了一层未闭合容器，这是格式错误。

## 不应该承担什么职责？

- **不应知道具体类型**：序列化器不知道也不应该知道是 `Person` 还是 `Order`。它只看到
  `serialize_map(2) → write_field_name("name") → serialize_utf8(...)`。类型知识全部在投影层。
- **不应处理端序/变长编码**：这些属于编码层。序列化器调用 `serialize_i32` 写明的是格式层面的行为（JSON 写成数字文本、MessagePack
  写成 fixint），具体字节布局由内部委托的编码器决定。
- **不应管理缓冲区**：与编码层一样，输出目标是被动接受的 `IBufferWriter<byte>`。
- **不应做业务验证**：`age: -5` 在序列化层不是错误——格式上它是一个合法的整数。是否是合法年龄是 DataContract 的职责。

## 与上下游的衔接

```
上游（Projec 层）→ 通过 ITypeSerializer<T> 拆解类型 T 为容器操作序列
    ↓
ISerializer.serialize_map() / serialize_sequence()  → 写入格式骨架（括号/前缀）
IObjectSerializer.write_field_name() + write_value() → 写入字段边界（逗号/分隔符）
    ↓
下游（编码层 / IBufferWriter<byte>）→ 基本类型值转换为具体字节
```

## 典型场景

- **API 响应**：`JsonSerializer` 将 DTO 序列化为 JSON，通过 HTTP 返回前端。
- **RPC 调用**：`MessagePackSerializer` 将请求对象编码为紧凑二进制，4 字节长度前缀后发往 TCP。
- **数据导出**：`CsvSerializer` 将 `List<Report>` 序列化为带表头的 CSV 文件。