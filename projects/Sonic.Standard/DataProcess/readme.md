# Sonic.DataProcess 2.0 设计文档

**版本**：2.0.0  
**状态**：正式发布  
**语言风格**：Rust-like C#（`snake_case` 方法，`Result<T>` 错误处理）  
**定位**：Sonic 标准库的核心组件，负责数据的在线表示与转换。

---

## 1. 引言

Sonic.DataProcess
是一个严格分层的序列化框架，它将数据处理拆分为五个正交的抽象层：I/O、编码、分帧、序列化、类型投射。每一层只使用自己的动词，绝不跨层污染。本设计文档详细阐述各层的职责、接口、协作方式，并解释如何通过源生成器消除手写冗余，以及如何与独立的
Schema 库协作。

### 1.1 为什么需要新的序列化框架？

现有序列化方案（包括 .NET 自带的 `System.Text.Json` 以及 Rust 的 Serde）普遍存在概念混合的问题：

- 序列化器需要理解 `Option`、`enum` 变体等类型系统知识，导致接口臃肿。
- 编码（如 VarInt）与格式化（如 JSON 语法）往往耦合在同一实现中。
- 分帧（framing）通常由用户自己拼装，库不提供统一抽象。
- 错误模型混乱：有时抛异常，有时用 `out` 参数。
- 动词不统一：`WriteInt32` 在 I/O、编码、序列化层都有出现，含义不清。

Sonic.DataProcess 从根本上解决这些问题：它从现实世界的网络协议栈和文件格式中提取出最小、最通用的抽象，让每一层各司其职，并通过源码生成在保持整洁分层的同时达到手写代码的性能。

### 1.2 设计目标

- **正交性**：每层只做一件事，可独立替换。
- **动词唯一性**：`read/write`、`encode/decode`、`frame/unframe`、`serialize/deserialize`、`project/restore` 互不重叠。
- **Fail-fast 错误**：遇到格式破坏立即中止，不收集多个错误。
- **零拷贝与高性能**：充分利用 `Span<byte>`、`ReadOnlySequence<byte>`，避免不必要分配。
- **代码生成友好**：通过源生成器将投影层自动生成，保持接口整洁。
- **标准库内聚**：完全基于 Sonic 标准库（不依赖 `System`），所有属性无冗余前缀。

### 1.3 文档结构

- 第 2 节：五层抽象总览与动词表
- 第 3 节：I/O 层
- 第 4 节：编码层
- 第 5 节：分帧层
- 第 6 节：序列化层
- 第 7 节：投影层与源生成器
- 第 8 节：与 Schema 库的边界
- 第 9 节：性能考量
- 第 10 节：FAQ
- 第 11 节：总结

---

## 2. 五层抽象总览

数据处理管道被划分为五个严格正交的层次，每一层都通过一组特定动词与上下层交互。

| 层     | 动词                        | 职责                                           | 输入 → 输出                        |
|--------|-----------------------------|------------------------------------------------|------------------------------------|
| I/O    | `read` / `write`            | 搬运原始字节，不识别任何类型                   | 设备/缓冲区 ↔ `ReadOnlySpan<byte>` |
| 编码   | `encode` / `decode`         | 基本类型 ↔ 字节表示（VarInt、UTF-8、IEEE 754） | 基本类型值 ↔ 字节序列              |
| 分帧   | `frame` / `unframe`         | 从无结构字节流中切出完整帧载荷                 | 字节流 ↔ 完整载荷块                |
| 序列化 | `serialize` / `deserialize` | 复合结构 ↔ 连续的格式化表示（JSON、二进制）    | 对象 ↔ 格式化字节序列              |
| 投射   | `project` / `restore`       | 具体类型 T 映射到序列化器的通用调用序列        | 类型 T ↔ 中性模型操作序列          |

### 动词唯一性原则

每一层的方法名前缀只能是该层定义的动词。例如：

- I/O 层：`write(span)`, `read(span)`
- 编码层：`encode_i32(v, writer)`, `decode_i32(buf) -> Result<(i32, int)>`
- 分帧层：`frame(payload, writer)`, `feed(data)`, `try_unframe() -> Option<ReadOnlySpan<byte>>`
- 序列化层：`serialize_i32(v)`, `deserialize_i32() -> Result<i32>`, `serialize_map() -> IMapSerializer`
- 投射层：`project(value, serializer)`, `restore(deserializer) -> Result<T>`

绝对不允许出现 `write_i32`（编码层越权 I/O）或 `encode_field_name`（序列化越权编码）等混杂动词。

---

## 3. I/O 层

### 3.1 职责

I/O 层负责从外部设备（网络、磁盘、内存缓冲区）中读取原始字节，或向外部写入原始字节。它是整个处理管道的起点和终点。

### 3.2 核心接口

Sonic.DataProcess 不重新发明 I/O 抽象，而是直接使用 Sonic 标准库定义的基础 trait：

```rust
// 相当于 Rust 的 std::io::Write 但属于 Sonic 自有标准库
trait IWrite {
    fn write(&mut self, buf: &[u8]) -> Result<usize>;
    fn write_all(&mut self, buf: &[u8]) -> Result<()>;
    fn flush(&mut self) -> Result<()>;
}

trait IRead {
    fn read(&mut self, buf: &mut [u8]) -> Result<usize>;
    fn read_exact(&mut self, buf: &mut [u8]) -> Result<()>;
}
```

任何实现了 `IWrite` / `IRead` 的类型都可以作为序列化管道的底层传输。内置实现包括：

- `BufWriter<T: IWrite>`：带缓冲的写入器
- `BufReader<T: IRead>`：带缓冲的读取器
- `PipeWriter` / `PipeReader`：异步管道支持零拷贝

### 3.3 与上层的关系

- 编码层通过 `IWrite` 写出基本类型编码后的字节。
- 分帧层通过 `IRead` 读取原始字节流，通过 `IWrite` 写出封装好的帧。

I/O 层对上层完全透明，替换 I/O 实现不影响任何编码或序列化逻辑。

---

## 4. 编码层

### 4.1 职责

编码层定义基本数据类型到其字节表示的转换规则。这是最底层的类型感知层，但仅限于 **单个基本类型**，不处理任何复合结构。

### 4.2 核心接口

```rust
trait IEncoder {
    fn encode_null(writer: &mut dyn IWrite) -> Result<()>;
    fn encode_bool(val: bool, writer: &mut dyn IWrite) -> Result<()>;
    fn encode_i32(val: i32, writer: &mut dyn IWrite) -> Result<()>;
    fn encode_i64(val: i64, writer: &mut dyn IWrite) -> Result<()>;
    fn encode_u64(val: u64, writer: &mut dyn IWrite) -> Result<()>;
    fn encode_f32(val: f32, writer: &mut dyn IWrite) -> Result<()>;
    fn encode_f64(val: f64, writer: &mut dyn IWrite) -> Result<()>;
    fn encode_string(utf8: &[u8], writer: &mut dyn IWrite) -> Result<()>;
    fn encode_raw(bytes: &[u8], writer: &mut dyn IWrite) -> Result<()>;
}

trait IDecoder {
    fn decode_bool(buf: &[u8]) -> Result<(bool, usize)>;
    fn decode_i32(buf: &[u8]) -> Result<(i32, usize)>;
    fn decode_i64(buf: &[u8]) -> Result<(i64, usize)>;
    fn decode_u64(buf: &[u8]) -> Result<(u64, usize)>;
    fn decode_f32(buf: &[u8]) -> Result<(f32, usize)>;
    fn decode_f64(buf: &[u8]) -> Result<(f64, usize)>;
    fn decode_string_bytes(buf: &[u8]) -> Result<(&[u8], usize)>;
    fn decode_raw(buf: &[u8], len: usize) -> Result<(&[u8], usize)>;
}
```

每个解码方法返回 `(解码后的值, 消耗的字节数)`。若缓冲区不足，返回 `Error::BufferUnderflow`，不抛异常。

### 4.3 内置实现

- `VarIntEncoder` / `VarIntDecoder`：LEB128 变长整数
- `NetworkEncoder` / `NetworkDecoder`：固定大小、大端序
- `Utf8Encoder` / `Utf8Decoder`：UTF-8 字符串，可配置是否带长度前缀
- `RawBinaryEncoder` / `RawBinaryDecoder`：原样拷贝字节块

### 4.4 性能注意

编码方法是热路径，应设计为可静态分发。在 C# 中通过泛型约束（`where TEncoder : IEncoder`）或直接使用结构体实例，避免虚调用开销。

---

## 5. 分帧层

### 5.1 职责

在无结构的字节流中，依据某种协议划分出完整的消息帧（payload），解决 TCP 粘包/半包问题。分帧操作完全基于字节，不解析内部格式。

### 5.2 核心接口

```rust
trait IFramer {
    fn frame(payload: &[u8], writer: &mut dyn IWrite) -> Result<()>;
}

trait IUnframer {
    fn feed(&mut self, data: &[u8]) -> Result<()>;
    fn try_unframe(&mut self) -> Result<Option<&[u8]>>;
    fn reset(&mut self);
}
```

- `IFramer`：将一段载荷包装为帧（例如添加长度前缀），写入 I/O。
- `IUnframer`：接收源源不断的字节流，内部维护缓冲区。`try_unframe` 尝试提取一个完整的帧载荷，若无足够数据返回 `Ok(None)`
  ，格式错误返回 `Err`。
- 返回的 `&[u8]` 是内部缓冲区的切片，在下次 `feed` 或 `reset` 前有效。

### 5.3 内置实现

- `LengthPrefixFramer`：1/2/4 字节长度前缀 + 载荷
- `DelimiterUnframer`：以特定分隔符（如 `\r\n`）切帧
- `HttpChunkedUnframer`：HTTP/1.1 分块传输
- `WebSocketFramer` / `WebSocketUnframer`：WebSocket 帧处理
- `ZipUnframer`：从 Zip 字节流中提取条目

### 5.4 状态管理

分帧器内部拥有可变状态，但接口不暴露状态。调用者只需循环 `feed` → `try_unframe` → 处理 payload → 继续，分帧器自己负责缓冲不完整的帧头。

---

## 6. 序列化层

### 6.1 职责

序列化层将复合数据结构（对象、列表、字典等）转换为一种连续的格式化表示。它定义格式的“骨架”：大括号、方括号、属性名、键值分隔符、元素分隔符等。对于基础字段，序列化层调用自身的标量方法直接写入，或委托给编码层（取决于具体实现）。

### 6.2 设计思路

序列化层提供最通用的容器抽象，不包含任何具体类型知识。所有类型相关的决策都由上层投射层负责。因此接口极其精简：只有三个容器原语——
`tuple`、`sequence`、`map`。

- **Tuple**：固定长度、异构元素序列。例如 `(i32, string)`。
- **Sequence**：可变长度、同构元素序列。例如 JSON 数组、Protobuf repeated。
- **Map**：键值对集合，键和值可以是任意类型。例如 JSON 对象、MessagePack map。

为什么不区分 `list` / `set`？因为对于序列化器来说，它们都是同构序列，差异只在语义（是否允许重复），该语义由投射层或 Schema
管理，与字节表示无关。

为什么不区分 `dict` / `struct`？因为结构体本质上就是一个键为字符串的 map。投射层负责将结构体字段名映射为字符串键，序列化器只看到
`serialize_entry("field_name", value)`。

### 6.3 核心接口（简化伪代码）

```rust
trait ISerializer {
    // 标量
    fn serialize_null() -> Result<()>;
    fn serialize_bool(v: bool) -> Result<()>;
    fn serialize_i32(v: i32) -> Result<()>;
    fn serialize_i64(v: i64) -> Result<()>;
    fn serialize_u64(v: u64) -> Result<()>;
    fn serialize_f32(v: f32) -> Result<()>;
    fn serialize_f64(v: f64) -> Result<()>;
    fn serialize_string(utf8: &[u8]) -> Result<()>;
    fn serialize_raw(bytes: &[u8]) -> Result<()>;

    // 容器入口
    fn serialize_tuple(len: usize) -> Result<ITupleSerializer>;
    fn serialize_sequence(expected: Option<usize>) -> Result<ISequenceSerializer>;
    fn serialize_map(expected: Option<usize>) -> Result<IMapSerializer>;
}

trait ITupleSerializer {
    fn serialize_element<T>(val: &T, projector: &dyn IProjector<T>) -> Result<()>;
    fn end() -> Result<()>;
}

trait ISequenceSerializer {
    fn serialize_element<T>(val: &T, projector: &dyn IProjector<T>) -> Result<()>;
    fn end() -> Result<()>;
}

trait IMapSerializer {
    fn serialize_entry<K, V>(
        key: &K, key_projector: &dyn IProjector<K>,
        val: &V, val_projector: &dyn IProjector<V>
    ) -> Result<()>;
    fn end() -> Result<()>;
}
```

反序列化侧对称：

```rust
trait IDeserializer {
    fn try_deserialize_null() -> Result<bool>;
    fn deserialize_bool() -> Result<bool>;
    fn deserialize_i32() -> Result<i32>;
    fn deserialize_i64() -> Result<i64>;
    fn deserialize_u64() -> Result<u64>;
    fn deserialize_f32() -> Result<f32>;
    fn deserialize_f64() -> Result<f64>;
    fn deserialize_string_bytes() -> Result<&[u8]>;
    fn deserialize_raw(len: usize) -> Result<&[u8]>;

    fn deserialize_tuple(len: usize) -> Result<ITupleDeserializer>;
    fn deserialize_sequence(expected: Option<usize>) -> Result<ISequenceDeserializer>;
    fn deserialize_map(expected: Option<usize>) -> Result<IMapDeserializer>;
}

trait ITupleDeserializer {
    fn deserialize_element<T>(restorer: &dyn IRestorer<T>) -> Result<T>;
    fn end() -> Result<()>;
}

trait ISequenceDeserializer {
    fn try_deserialize_element<T>(restorer: &dyn IRestorer<T>) -> Result<Option<T>>;
    fn end() -> Result<()>;
}

trait IMapDeserializer {
    fn try_deserialize_entry<K, V>(
        key_restorer: &dyn IRestorer<K>,
        val_restorer: &dyn IRestorer<V>
    ) -> Result<Option<(K, V)>>;
    fn end() -> Result<()>;
}
```

### 6.4 格式实现示例

JSON 序列化器实现 `ISerializer`，内部维护一个 `StringWriter` 或直接写字节。

```rust
impl ISerializer for JsonSerializer {
    fn serialize_map(expected: Option<usize>) -> Result<IMapSerializer> {
        self.write_char('{')?;
        Ok(JsonMapSerializer { serializer: self, first: true })
    }
}

impl IMapSerializer for JsonMapSerializer {
    fn serialize_entry<K, V>(...) -> Result<()> {
        if !self.first { self.serializer.write_char(',')?; }
        self.first = false;
        key_projector.project(key, self.serializer)?; // 键必须序列化为字符串
        self.serializer.write_char(':')?;
        val_projector.project(val, self.serializer)?;
        Ok(())
    }
    fn end() -> Result<()> {
        self.serializer.write_char('}')
    }
}
```

MessagePack 序列化器则根据 map 的大小写入 `fixmap`、`map16` 或 `map32` 前缀，键可以是非字符串类型。

---

## 7. 投影层与源生成器

### 7.1 职责

投影层负责将任意的领域类型 `T` 映射到序列化层的通用调用序列上（`project`），以及从通用反序列化调用序列还原出类型 `T`（
`restore`）。

这是 Sonic.DataProcess 与 Serde 最大的不同之处： **投影层把类型知识完全从序列化器剥离出来**
，使得序列化器极简，同时仍能支持复杂的类型系统（枚举、Option、newtype 等）。

### 7.2 核心接口

```rust
trait IProjector<T> {
    fn project(val: &T, serializer: &mut dyn ISerializer) -> Result<()>;
}

trait IRestorer<T> {
    fn restore(deserializer: &mut dyn IDeserializer) -> Result<T>;
}
```

标量类型的投影器由标准库预提供单例：

```rust
struct I32Projector;
impl IProjector<i32> for I32Projector {
    fn project(val: &i32, ser: &mut dyn ISerializer) -> Result<()> {
        ser.serialize_i32(*val)
    }
}
impl IRestorer<i32> for I32Projector {
    fn restore(de: &mut dyn IDeserializer) -> Result<i32> {
        de.deserialize_i32()
    }
}
```

对于用户自定义类型，编写投影器通常是繁琐且重复的，因此 Sonic.DataProcess 提供了 **源生成器**，通过几个轻量属性自动生成投影/还原代码。

### 7.3 源生成器属性

| 属性        | 用途                                     |
|-------------|------------------------------------------|
| `[Project]` | 标记类/结构为可投影，触发源生成器        |
| `[Field]`   | 配置成员映射细节（名称、顺序、默认值等） |
| `[Ignore]`  | 排除成员                                 |

`[Project]` 的可选参数：

- `Mode`：`Map`（默认）或 `Tuple`。`Map` 将字段序列化为键值对（适用于 JSON、MessagePack map）；`Tuple` 按顺序序列化元素（适用于紧凑二进制元组）。
- `RenameAll`：自动将字段名转换为指定风格（`CamelCase`、`snake_case` 等）。

`[Field]` 的可选参数：

- `Name`：序列化时使用的键名（仅 Map 模式）。
- `Order`：Tuple 模式下的位置顺序。
- `Required`：若为 `true`，反序列化缺失该字段将报错。
- `DefaultValue`：反序列化缺失时使用的默认值（编译时常量）。
- `SkipWhenNull`：序列化时若值为 `null` 则跳过。
- `SkipWhenDefault`：序列化时若值等于 `default` 则跳过。

所有属性均无 `Sonic` 前缀，因为它们位于 `Sonic.DataProcess` 命名空间下，默认就是标准库权威属性。

### 7.4 生成代码示例

给定定义：

```rust
[Project(Mode = Map, RenameAll = CamelCase)]
struct Person {
    [Field(Name = "email_address", Required = true)]
    email: String,
    
    [Field(SkipWhenNull = true)]
    bio: Option<String>,
    
    [Field(DefaultValue = 18)]
    age: i32,
    
    [Ignore]
    internal_id: String,
}
```

源生成器将生成类似如下伪代码：

```rust
impl IProjector<Person> for PersonProjector {
    fn project(val: &Person, ser: &mut dyn ISerializer) -> Result<()> {
        let map = ser.serialize_map(Some(3))?;
        // email (required)
        map.serialize_entry(
            &"email_address", &DataProcessStringProjector,
            &val.email, &DataProcessStringProjector
        )?;
        // bio (skip if null)
        if let Some(ref bio) = val.bio {
            map.serialize_entry(
                &"bio", &DataProcessStringProjector,
                bio, &DataProcessStringProjector
            )?;
        }
        // age (default 18)
        if val.age != 18 {
            map.serialize_entry(
                &"age", &DataProcessStringProjector,
                &val.age, &DataProcessI32Projector
            )?;
        }
        // internal_id ignored
        map.end()?;
        Ok(())
    }
}

impl IRestorer<Person> for PersonProjector {
    fn restore(de: &mut dyn IDeserializer) -> Result<Person> {
        let map = de.deserialize_map(None)?;
        let mut email = None;
        let mut bio = None;
        let mut age = Some(18);
        while let Some((key, _)) = map.try_deserialize_entry::<String, DataProcessValue>(...)? {
            match key.as_str() {
                "email_address" => email = Some(String::restore(de)?),
                "bio" => bio = Some(String::restore(de)?),
                "age" => age = Some(i32::restore(de)?),
                _ => { /* skip unknown */ }
            }
        }
        map.end()?;
        Ok(Person {
            email: email.ok_or(Error::MissingRequiredField("email_address"))?,
            bio,
            age: age.unwrap(),
            internal_id: String::new(),
        })
    }
}
```

生成的代码是 `partial` 类的另一部分，允许用户手动补充自定义逻辑。

### 7.5 类型系统消化

通过投影层，所有 Rust/Serde 中需序列化器特化的类型都被优雅处理：

- **Option<T>**：投影器检查值是否为 `None`，若是则调用 `serialize_null()`；否则递归投影内部值。
- **Enum 变体**：投影器根据枚举变体构建一个内部 map（如 `{"variant": "A", "data": ...}`），然后委托给 map 序列化器。
- **Newtype**：投影器直接提取内部值调用标量序列化方法，序列化器根本不感知包装类型。
- **元组结构体**：投影器使用 `serialize_tuple`，按顺序投影各字段。

因此，序列化层无需提供 `serialize_enum`、`serialize_newtype_struct` 等方法，保持极简。

---

## 8. 与 Schema 库的边界

Sonic.DataProcess 处理 **数据如何转换**，Schema 处理 **数据应该是什么**。两者正交，协作方式如下：

| 关注点       | DataProcess                | Schema                                       |
|--------------|----------------------------|----------------------------------------------|
| 字段名、顺序 | `[Field]` 属性             | 读取同一属性用于文档生成                     |
| 结构完整性   | `Required`、`DefaultValue` | 无                                           |
| 值有效性     | 不关心                     | 提供 `[Range]`、`[Regex]` 等属性，多错误收集 |
| 加密/脱敏    | 不感知 `[Secret]`          | 可标记 `[Secret]` 用于日志、API 输出         |
| 版本演化     | 不处理                     | 管理字段增删、兼容性                         |
| ORM、RPC     | 不涉及                     | 映射到数据库、生成服务桩                     |
| 错误模式     | 单次 `Result`，快速失败    | 聚合所有错误，完整报告                       |

管道示例：

```
网络字节 → IUnframer → IDeserializer → IRestorer<T> → T 对象 (DataProcess 完成)
        → IValidator<T> (Schema) → 验证结果 → 业务处理
```

若 DataProcess 阶段失败（帧截断、非法 UTF-8），直接返回 `Error`，跳过验证。若 DataProcess 成功，Schema
可以进一步检查业务规则，收集所有问题后返回多错误报告。

---

## 9. 性能考量

### 9.1 分层不会造成性能损耗

每一层的方法调用在 Release 模式下都会被 JIT 内联，尤其是使用结构体泛型约束时。最终生成的机器码相当于手写字节拼接。

### 9.2 零拷贝路径

- 分帧器返回的 `payload` 直接指向内部缓冲区，无需复制。
- 解码器读取字符串时，可返回指向输入缓冲区的 `&[u8]`，由用户决定是否拷贝。
- 序列化器直接向 `IWrite` 的 `Span<byte>` 写入，避免中间缓冲区。

### 9.3 源生成器的优化

生成的投影器代码对每个字段直接调用序列化器的对应方法，无反射、无动态分配。对于固定结构，JIT 可完全展开循环并内联标量序列化，达到与手写优化代码相同的性能。

### 9.4 缓冲策略

- 编码器直接写入 `IWrite` 的缓冲区，不产生临时分配。
- 序列化器内部可使用线程本地缓冲区来累积小片段，达到阈值后一次性 `write_all`。

---

## 10. FAQ

**Q1: 为什么动词这么严格？`write_i32` 不是挺直观吗？**

直观但容易混淆。如果 `write_i32` 出现在序列化器上，它到底是将 `i32` 转换为格式的文本（如 JSON 数字），还是将 `i32`
编码为二进制字节？前者属于序列化，后者属于编码。强行使用同一动词会让实现者不确定谁负责转换，最终各层职责模糊。严格区分动词后，接口语义一目了然。

**Q2: 为什么序列化器只有 tuple、sequence、map 三种容器？list 和 set 呢？**

对于序列化器（格式）而言，list 和 set 都是“同构元素序列”，字节表示没有任何区别。是否允许重复、是否有序是业务语义，由投影层和
Schema 负责。序列化层只关注字节布局，因此统一为 `sequence`。

**Q3: 为什么没有 `serialize_struct` 或 `serialize_enum`？**

因为有了投影层，结构体和枚举都可以映射到已有的三种容器上。结构体是“键为字符串的 map”，元组结构体是 tuple，枚举变体可以用 map
或 tuple 表示。这极大简化了序列化器接口，并且让新格式的实现只需实现最少的几个方法。

**Q4: 为什么错误模型采用单次 Result 而非聚合多个错误？**

序列化/反序列化是流式操作，一旦遇到格式错误（如非法的
UTF-8、截断的帧），后续字节已无法信任，继续解析只会产生海量虚假错误。快速失败是最安全、最高效的策略。需要聚合多个错误的场景（如业务验证）由
Schema 库处理，DataProcess 不涉足。

**Q5: 如果我想序列化为 JSON，又想序列化为二进制，需要两个不同的投影器吗？**

是的，但通常由源生成器自动生成。你可以为一个类型添加 `[Project(Mode = Map)]` 用于 JSON，同时再写一个
`[Project(Mode = Tuple)]` 的配置用于二进制。由于投影层仅仅是调用序列化器接口，不同的序列化器实现（`JsonSerializer` vs
`MessagePackSerializer`）决定了最终格式。

**Q6: 分帧层为什么要独立？我可以把长度前缀直接写在序列化器里吗？**

不行，因为这会导致协议与格式耦合。如果长度前缀写在序列化器里，那么你的序列化器就只能用于该分帧协议，无法直接写到文件或另一个分帧器（如
WebSocket 帧）。分离后，你可以将同一个 JSON 序列化器与 `LengthPrefixFramer` 组合用于 TCP，或直接写入 `FileStream`，完全解耦。

**Q7: Sonic.DataProcess 和 System.Text.Json 有什么区别？**

`System.Text.Json` 是一个具体的 JSON 序列化器，混合了 I/O、编码、序列化、分帧（如流式 JSON 的 `\n` 分隔）等职责，且与 JSON
格式深度绑定。Sonic.DataProcess 是分层框架，JSON 只是 `ISerializer` 的一个实现，你可以随时替换成 MessagePack
或自定义二进制格式，无需修改业务类型或投影器。

**Q8: 我可以只使用 Sonic.DataProcess 中的某一层吗？**

完全可以。每层都通过标准接口定义，你可以单独使用 `IEncoder/IDecoder` 进行整数编码，或单独使用 `IUnframer` 处理粘包，其余层用其他库。

---

## 11. 总结

Sonic.DataProcess 2.0
通过严格的五层抽象、动词唯一性和正交接口，重新定义了序列化框架的构成方式。它将序列化器从类型系统的泥潭中解放出来，通过投射层实现任意类型与通用容器之间的映射，同时保持了高性能、零拷贝和代码生成的友好性。它与
Schema 库清晰分工，共同构成 Sonic 标准库中数据处理的核心支柱。

这种设计不仅让实现者更容易开发新的格式和协议支持，也让使用者在面对不同传输需求时拥有前所未有的组合自由度。Sonic.DataProcess
的目标不是成为另一个 JSON 库，而是成为所有数据在线交互的基石。

# Sonic.DataProcess 2.1 修正案

本修正案基于 Sonic.DataProcess 2.0 设计文档，针对接口细节、命名准确性和语义清晰度进行小幅修正，不改变五层抽象架构。

---

## 修正清单

| 编号 | 修正项            | 旧设计（2.0）                                                                  | 新设计（2.1）                                                                         | 修正理由                                                                              |
|------|-------------------|--------------------------------------------------------------------------------|---------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------|
| 1    | 容器元素命名      | `ITupleSerializer.serialize_element` / `ISequenceSerializer.serialize_element` | `ITupleSerializer.serialize_element` / `ISequenceSerializer.serialize_item`           | 消除不同容器下相同方法名的歧义，强调 tuple 固定元素与 sequence 可重复项的区别         |
| 2    | Null 反序列化接口 | `fn try_deserialize_null() -> Result<bool>`                                    | `fn is_null(&self) -> Result<bool>` + `fn deserialize_null(&mut self) -> Result<()>`  | 解决旧接口语义模糊问题（返回 `false` 时不消耗输入但无法回退），明确“探测”与“消耗”分离 |
| 3    | 原始字节方法名    | `serialize_raw` / `deserialize_raw`                                            | `serialize_bytes` / `deserialize_bytes`                                               | 避免 “raw” 的宽泛歧义，与 Serde 的 `serialize_bytes` 对齐，明确为字节数组的写入/读取  |
| 4    | 格式化参数位置    | 无说明                                                                         | 明确 `JsonSerializer` 等实现的构造参数（如 `indent`）不属于序列化接口，由实现自身管理 | 澄清接口纯净化边界，防止用户误以为 `ISerializer` 需携带格式化参数                     |
| 5    | FAQ 大小端说明    | 未提及                                                                         | 新增 FAQ 条目“为什么不允许 I/O 层 `write_i32`？”                                      | 突出编码层与 I/O 层的字节序分工，强化分层价值                                         |

---

## 修正理由详解

### 修正 1：Tuple → `element`，Sequence → `item`

**理由**：虽然两者接口方法签名形式上都是传入一个投影器和值，但语义不同：

- Tuple 强调“固定位置的结构成员”，用 `element` 体现其不可变顺序和异构性。
- Sequence 强调“可遍历的同构项”，用 `item` 体现其重复性和可迭代特性。

在源码中能够直接通过方法名区分当前正在构建的容器类型，避免阅读混淆。反序列化侧对应改为 `deserialize_element` 与
`try_deserialize_item`，保持对称。

---

### 修正 2：拆分 null 反序列化为 `is_null` 和 `deserialize_null`

**理由**：旧设计 `try_deserialize_null() -> Result<bool>` 存在两个问题：

1. **语义模糊**：返回 `Ok(true)` 表示成功消耗了一个 null；返回 `Ok(false)` 表示当前值不是
   null，但该方法不能消耗任何字节，否则后续读取将错位。这意味着调用者需要“回退”，而多数解析器无法回退。
2. **与 `Result` 错误混淆**：`Ok(false)` 表达非 null，`Err(...)` 表达读取错误，两者语义完全不同。

修正后：

- `is_null(&self) -> Result<bool>`：只探测而不消耗输入，支持 peek 能力的格式可以正确返回；对于不支持 peek 的格式可直接返回
  `Err(NotSupported)`，此时投影层可用兜底策略（尝试反序列化并在失败时回退或默认处理）。
- `deserialize_null(&mut self) -> Result<()>`：消耗当前 null 标记，若当前不是 null 则返回错误。

这个拆分符合 Rust 标准库中 `Iterator::peek` 或 `is_some` 的惯例，清晰表达“探测 vs 消费”的语义。

---

### 修正 3：`raw` → `bytes`

**理由**：`raw` 过于模糊，可能被误解为“原始格式数据块”或“未处理的二进制”。这里实际语义是“写入一段不透明的字节序列”，对标
Serde 的 `serialize_bytes`。使用 `bytes` 更加精确，与 `encode_raw`（编码层的原样拷贝）区分开来——编码层是 `encode_raw`
原样拷贝字节，序列化层是 `serialize_bytes` 将一段字节当作格式允许的字节块（例如 JSON 中的 base64 字符串或二进制格式中的直接嵌入）。

保持编码层仍可使用 `encode_raw` 表示“不做转换、直接写入字节”，因为编码层处理的就是字节块，`raw`
在此语境下合理（未编码/未处理）。序列化层因其抽象层级更高，`bytes` 更适合。

---

### 修正 4：格式化参数归实现管理

**理由**：`JsonSerializer` 的缩进、编码选项（如 `indent_text`、`escape_unicode`）是具体格式实现的行为偏好，不应污染
`ISerializer` 接口。接口保持最小化，仅定义序列化操作骨架。用户通过构造参数配置具体序列化器，例如：

```rust
let mut ser = JsonSerializer::new(JsonConfig { indent: true });
```

这样既保持接口纯粹性，又不会限制实现方扩展。

---

### 修正 5：FAQ 大小端职责说明

**新增问答**：

**Q: 如果支持 `write_i32` 之类的接口，大小端问题怎么处理？**

**A:** Sonic.DataProcess 严格禁止 I/O 层携带类型化写入方法，大小端问题完全由 **编码层**负责。若出现 `write_i32`
，则必然暗中决定了一种字节序，无法灵活切换。

在 Sonic.DataProcess 中：

- 编码器 `NetworkEncoder` 内部实现大端转换，调用 `writer.write_all(&[0x00, 0x00, 0x00, 0x2A])` 写出大端字节。
- 若需要小端，只需替换为 `LittleEndianEncoder`，I/O 无感知。

I/O 层只搬运字节，编码层决定字节布局，分帧层决定边界，序列化层决定格式化骨架，投影层映射类型。每个层的职责独立，大小端不过是编码层的一个可插拔策略，与其余层完全解耦。

---

## 其他说明

- 投影层属性 `[Project]`、`[Field]`、`[Ignore]` 保持不变。
- 序列化层容器仍为 `tuple`、`sequence`、`map` 三种，无需增加新容器。
- 错误模型保持 `Result<T>` 单错误快速失败。
- 与 Schema 库的边界定义不变。

---

本修正案即日起生效，纳入 Sonic.DataProcess 2.1 标准。已有 2.0 实现者仅需调整上述接口名称和 null 处理逻辑，架构无变动。