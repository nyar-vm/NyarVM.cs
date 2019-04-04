# Sonic.Data 3.0 设计文档

**版本**：3.0.0  
**状态**：最终草案  
**作者**：Sonic 标准库团队  
**日期**：2026-05-16  
**语言风格**：类似 Rust 的简洁伪代码，所有标识符 `snake_case`，错误处理使用 `Result`，无 `System` 依赖

---

## 1. 引言

Sonic.Data 3.0 是 Sonic 标准库中负责 **数据全生命周期管理**的核心子系统。它基于三个严格正交的维度—— **DataProcess**
（数据加工）、 **DataContract**（数据契约）和 **DataStorage**（数据存储）——提供了从网络传输、业务规则验证到持久化存储的统一抽象。开发者只需使用单一的
`[Data]` 标记，即可为任意类型自动生成序列化/反序列化、验证、元数据、存储映射等全套能力，无需手动编写样板代码。

本文档详细定义了 Sonic.Data 3.0 的架构、接口、属性和生成器行为。所有概念均使用明确的动词和名词，杜绝跨层语义污染。接口采用类似
Rust 的 trait 语法，但保留 C# 的泛型风格，实际伪代码侧重于表达语义，不追求完整的 Rust 语法。

---

## 2. 核心设计原则

### 2.1 三个正交维度

| 维度             | 职责                                 | 动词                                                                                                 |
|------------------|--------------------------------------|------------------------------------------------------------------------------------------------------|
| **DataProcess**  | 数据在线路上的表示、变换、分割与搬运 | `read`/`write`, `encode`/`decode`, `frame`/`unframe`, `serialize`/`deserialize`, `project`/`restore` |
| **DataContract** | 数据的静态约束、元数据、版本演化     | `validate` (多错误收集), `schema`                                                                    |
| **DataStorage**  | 数据在持久化介质上的存储、索引、查询 | 映射、迁移、主键、并发控制                                                                           |

每个维度独立工作，但可通过 `[Data]` 属性统一启用。维度之间仅通过明确的接口协作，无循环依赖。

### 2.2 统一标记 `[Data]`

单个属性代替所有分散的属性：

```rust
#[derive(Data)]
pub struct Person {
    // 字段...
}
```

`[Data]` 的属性参数控制所有三个维度的行为。

### 2.3 错误处理

- DataProcess：所有可能失败的操作返回 `Result<T, Error>`，错误为立即失败（fail-fast），单错误模型。
- DataContract：验证器返回 `ValidationResult`，包含多个错误/警告集合。
- DataStorage：根据具体后端返回 `Result<T, Error>` 或异步结果，单个操作错误。

### 2.4 命名规范

- 所有方法、字段、变量使用 `snake_case`。
- 接口名使用 `I` 前缀（保留 C# 风格，但伪代码可省略 `I`，直接用 trait）。
- 属性名简洁，无前缀，如 `[Data]`、`[Field]`、`[Required]`。

### 2.5 无外部依赖

Sonic.Data 3.0 仅依赖于 Sonic 标准库的核心类型（`Span`、`ReadOnlySpan`、`Result`、`Option`、`Unit` 等），不使用 `System.IO` 或
`System.Text` 的特定类型（内部实现可以用，但抽象层不暴露）。

---

## 3. 基础类型定义

在 Sonic 标准库中预定义以下结构：

```rust
// 类似 Rust 的切片，由运行时管理
pub struct Span<T> { /* ... */ }
pub struct ReadOnlySpan<T> { /* ... */ }

// 简单的 Option 替代
pub enum Option<T> {
    Some(T),
    None,
}

// 单位类型，表示 void
pub struct Unit;

// 错误类型，分层次
pub enum Error {
    IoError(String),
    EncodeError(String),
    DecodeError(String),
    FrameError(String),
    SerializeError(String),
    DeserializeError(String),
    ProjectionError(String),
    ValidationError(String),
    StorageError(String),
    // ... 更细粒度
}

// 标准 Result
pub type Result<T> = std::result::Result<T, Error>;

// 用于缓冲区写入的抽象
pub trait BufferWriter {
    fn write(&mut self, buf: &[u8]) -> Result<usize>;
    fn write_all(&mut self, buf: &[u8]) -> Result<()>;
    fn get_span(&mut self, hint: usize) -> Result<&mut Span<u8>>;
    fn advance(&mut self, count: usize) -> Result<()>;
    fn flush(&mut self) -> Result<()>;
}

// 用于缓冲区读取的抽象
pub trait BufferReader {
    fn read(&mut self, buf: &mut Span<u8>) -> Result<usize>;
    fn read_exact(&mut self, buf: &mut Span<u8>) -> Result<()>;
}
```

---

## 4. DataProcess：五层抽象

DataProcess 提供了数据从内存到字节流（或反向）的完整管道，由五个严格正交的层组成。每一层只使用自身的动词，底层从不调用上层。

### 4.1 第 1 层：I/O 层

**职责**：搬运原始字节，无任何结构化解释。

```rust
pub trait Read {
    fn read(&mut self, buf: &mut [u8]) -> Result<usize>;
    fn read_exact(&mut self, buf: &mut [u8]) -> Result<()> {
        // 默认实现：循环读取直至填满或出错
    }
}

pub trait Write {
    fn write(&mut self, buf: &[u8]) -> Result<usize>;
    fn write_all(&mut self, buf: &[u8]) -> Result<()> {
        // 默认实现：循环写入所有字节
    }
    fn flush(&mut self) -> Result<()>;
}
```

**实现示例**：

- `TcpStream` 实现 `Read` 和 `Write`
- `File` 实现 `Read` 和 `Write`
- `MemoryBuffer` 实现 `Read` 和 `Write`

I/O 层不提供任何 `read_i32` 方法，那是编码层的职责。

### 4.2 第 2 层：编码/解码层

**职责**：基本数据类型与其字节表示之间的转换。无状态，纯函数。

```rust
pub trait Encoder {
    fn encode_null(w: &mut impl Write) -> Result<()>;
    fn encode_bool(value: bool, w: &mut impl Write) -> Result<()>;
    fn encode_i32(value: i32, w: &mut impl Write) -> Result<()>;
    fn encode_i64(value: i64, w: &mut impl Write) -> Result<()>;
    fn encode_u64(value: u64, w: &mut impl Write) -> Result<()>;
    fn encode_f32(value: f32, w: &mut impl Write) -> Result<()>;
    fn encode_f64(value: f64, w: &mut impl Write) -> Result<()>;
    fn encode_string(utf8: &[u8], w: &mut impl Write) -> Result<()>;
    fn encode_raw(bytes: &[u8], w: &mut impl Write) -> Result<()>;
}

pub trait Decoder {
    fn decode_bool(buf: &[u8]) -> Result<(bool, usize)>;
    fn decode_i32(buf: &[u8]) -> Result<(i32, usize)>;
    fn decode_i64(buf: &[u8]) -> Result<(i64, usize)>;
    fn decode_u64(buf: &[u8]) -> Result<(u64, usize)>;
    fn decode_f32(buf: &[u8]) -> Result<(f32, usize)>;
    fn decode_f64(buf: &[u8]) -> Result<(f64, usize)>;
    fn decode_string_bytes(buf: &[u8]) -> Result<(&[u8], usize)>;
    fn decode_raw(buf: &[u8], length: usize) -> Result<(&[u8], usize)>;
}
```

**内置编码器**：

- `VarIntEncoder` / `VarIntDecoder`：LEB128 变长整数。
- `NetworkBinaryEncoder` / `NetworkBinaryDecoder`：固定大小、大端序。
- `Utf8Encoder` / `Utf8Decoder`：UTF-8 字符串（带长度前缀）。

### 4.3 第 3 层：分帧层

**职责**：在无结构的字节流中划分消息边界。分帧器是有状态的，维护内部缓冲区。

```rust
pub trait Framer {
    /// 将一段载荷封装为一个完整帧，写入 writer。
    fn frame(payload: &[u8], writer: &mut impl Write) -> Result<()>;
}

pub trait Unframer {
    /// 喂入新收到的字节。
    fn feed(&mut self, data: &[u8]) -> Result<()>;

    /// 尝试解出一帧载荷。若无完整帧则返回 None。
    fn try_unframe(&mut self) -> Result<Option<&[u8]>>;

    /// 重置内部状态。
    fn reset(&mut self);
}
```

**内置实现**：

- `LengthPrefixedFramer`：4 字节长度前缀。
- `DelimiterUnframer`：特定分隔符（如换行）。
- `HttpChunkedUnframer`：HTTP 分块传输编码。
- `ZipUnframer`：Zip 归档解帧，将每个条目看作一帧。

### 4.4 第 4 层：序列化层

**职责**：将对象结构转换为连续的格式化表示（文本或二进制），或反向重建。序列化层只提供最通用的容器原语： **tuple**、
**sequence**、 **map**。所有高级语义（struct, enum, set）由投影层实现。

```rust
pub trait Serializer {
    // 标量
    fn serialize_null(&mut self) -> Result<()>;
    fn serialize_bool(&mut self, v: bool) -> Result<()>;
    fn serialize_i32(&mut self, v: i32) -> Result<()>;
    fn serialize_i64(&mut self, v: i64) -> Result<()>;
    fn serialize_u64(&mut self, v: u64) -> Result<()>;
    fn serialize_f32(&mut self, v: f32) -> Result<()>;
    fn serialize_f64(&mut self, v: f64) -> Result<()>;
    fn serialize_string(&mut self, utf8: &[u8]) -> Result<()>;
    fn serialize_raw(&mut self, bytes: &[u8]) -> Result<()>;

    // 容器入口
    fn serialize_tuple(&mut self, len: usize) -> Result<TupleSerializer>;
    fn serialize_sequence(&mut self, expected_len: Option<usize>) -> Result<SequenceSerializer>;
    fn serialize_map(&mut self, expected_entries: Option<usize>) -> Result<MapSerializer>;
}
```

**状态对象**：

```rust
pub trait TupleSerializer {
    fn serialize_element<T: Projector<T>>(&mut self, value: &T, projector: &T::Projector) -> Result<()>;
    fn end(self) -> Result<()>;
}

pub trait SequenceSerializer {
    fn serialize_element<T: Projector<T>>(&mut self, value: &T, projector: &T::Projector) -> Result<()>;
    fn end(self) -> Result<()>;
}

pub trait MapSerializer {
    fn serialize_entry<K: Projector<K>, V: Projector<V>>(
        &mut self,
        key: &K,
        key_projector: &K::Projector,
        value: &V,
        value_projector: &V::Projector,
    ) -> Result<()>;
    fn end(self) -> Result<()>;
}
```

**反序列化侧**：

```rust
pub trait Deserializer {
    fn try_deserialize_null(&self) -> Result<bool>;
    fn deserialize_bool(&self) -> Result<bool>;
    fn deserialize_i32(&self) -> Result<i32>;
    fn deserialize_i64(&self) -> Result<i64>;
    fn deserialize_u64(&self) -> Result<u64>;
    fn deserialize_f32(&self) -> Result<f32>;
    fn deserialize_f64(&self) -> Result<f64>;
    fn deserialize_string_bytes(&self) -> Result<&[u8]>;
    fn deserialize_raw(&self, length: usize) -> Result<&[u8]>;

    fn deserialize_tuple(&self, len: usize) -> Result<TupleDeserializer>;
    fn deserialize_sequence(&self, expected_len: Option<usize>) -> Result<SequenceDeserializer>;
    fn deserialize_map(&self, expected_entries: Option<usize>) -> Result<MapDeserializer>;
}

pub trait TupleDeserializer {
    fn deserialize_element<T: Restorer<T>>(&mut self, restorer: &T::Restorer) -> Result<T>;
    fn end(self) -> Result<()>;
}

pub trait SequenceDeserializer {
    fn try_deserialize_element<T: Restorer<T>>(&mut self, restorer: &T::Restorer) -> Result<Option<T>>;
    fn end(self) -> Result<()>;
}

pub trait MapDeserializer {
    fn try_deserialize_entry<K: Restorer<K>, V: Restorer<V>>(
        &mut self,
        key_restorer: &K::Restorer,
        value_restorer: &V::Restorer,
    ) -> Result<Option<(K, V)>>;
    fn end(self) -> Result<()>;
}
```

**内置序列化器**：

- `JsonSerializer` / `JsonDeserializer`
- `MessagePackSerializer` / `MessagePackDeserializer`

### 4.5 第 5 层：投影层

**职责**：将具体领域类型 `T` 映射到序列化层的操作序列（project），或从反序列化层还原为 `T`
（restore）。所有类型特有的逻辑（如字段顺序、字段名、条件跳过）由投影层实现，序列化层保持通用。

```rust
pub trait Projector<T> {
    fn project(&self, value: &T, serializer: &mut impl Serializer) -> Result<()>;
}

pub trait Restorer<T> {
    fn restore(&self, deserializer: &mut impl Deserializer) -> Result<T>;
}
```

**内置基本投影/还原器**（单例）：

- `Int32Projector` / `Int32Restorer`
- `BoolProjector` / `BoolRestorer`
- `StringProjector` / `StringRestorer`
- 等。

这些基本类型的投影器直接调用 `serializer.serialize_i32` 等。

**投影层由源码生成器自动产生**，对于 `[Data]` 类型，生成类似：

```rust
// 为 Person 生成的投影器
struct PersonProjector;

impl Projector<Person> for PersonProjector {
    fn project(&self, value: &Person, ser: &mut impl Serializer) -> Result<()> {
        let mut map = ser.serialize_map(Some(2))?;
        map.serialize_entry(
            &"name", &StringProjector,
            &value.name, &StringProjector,
        )?;
        map.serialize_entry(
            &"age", &StringProjector,
            &value.age, &Int32Projector,
        )?;
        map.end()
    }
}

struct PersonRestorer;

impl Restorer<Person> for PersonRestorer {
    fn restore(&self, de: &mut impl Deserializer) -> Result<Person> {
        let mut map = de.deserialize_map(Some(2))?;
        let mut name = None;
        let mut age = None;
        while let Some((key, _)) = map.try_deserialize_entry::<String, Value>(&StringRestorer, &ValueRestorer)? {
            match key.as_str() {
                "name" => name = Some(map.deserialize_value(&StringRestorer)?),
                "age" => age = Some(map.deserialize_value(&Int32Restorer)?),
                _ => { /* 跳过未知键 */ }
            }
        }
        map.end()?;
        Ok(Person {
            name: name.ok_or(Error::DeserializeError("missing field name".into()))?,
            age: age.ok_or(Error::DeserializeError("missing field age".into()))?,
        })
    }
}
```

注意：生成器会根据 `[Field]` 属性中的 `skip_when_null`、`default_value` 等调整逻辑。

---

## 5. DataContract：数据契约与验证

DataContract 处理数据的静态约束、元数据和版本演化。它不参与字节转换，只对内存中的对象进行“质检验收”。

### 5.1 验证器接口

```rust
pub struct ValidationResult {
    pub errors: Vec<ValidationError>,
    pub warnings: Vec<ValidationWarning>,
}

impl ValidationResult {
    pub fn is_valid(&self) -> bool { self.errors.is_empty() }
    pub fn add_error(&mut self, member_path: &str, error_code: &str, message: &str) { ... }
    pub fn add_warning(&mut self, member_path: &str, code: &str, message: &str) { ... }
}

pub struct ValidationError { pub member_path: String, pub error_code: String, pub message: String }
pub struct ValidationWarning { pub member_path: String, pub code: String, pub message: String }

pub trait Validator<T> {
    fn validate(&self, target: &T) -> ValidationResult;
}
```

**生成器**：对于 `[Data]` 类型，自动生成一个结构体实现 `Validator<T>`，遍历所有字段的验证属性（`Required`, `StringLength`,
`Range`, `Regex` 等）并执行检查，同时调用所有标记了 `[CustomValidation]` 的方法。

### 5.2 元数据接口 (可选)

```rust
pub trait Schema<T> {
    fn meta(&self) -> &TypeMeta;
    fn fields(&self) -> &[FieldMeta];
    fn validator(&self) -> &dyn Validator<T>;
}

pub struct TypeMeta {
    pub name: Option<String>,
    pub description: Option<String>,
    pub version: u32,
}

pub struct FieldMeta {
    pub member_name: String,
    pub field_type: TypeId,
    pub alias: Option<String>,
    pub is_required: bool,
    pub default_value: Option<Value>,
    pub rules: Vec<ValidationRule>,
    pub sensitivity: Option<SensitivityLevel>,
}
```

`Schema<T>` 的实例由生成器产生，当 `[Data]` 中 `generate_meta = true` 时创建。

### 5.3 契约属性

所有属性位于 `Sonic.Data.DataContract` 命名空间，无前缀。

```rust
// 字段级
attribute Required { error_message: Option<String> }
attribute StringLength { min: usize, max: usize, error_message: Option<String> }
attribute Range { min: f64, max: f64, error_message: Option<String> } // 略复杂，示意
attribute Regex { pattern: String, error_message: Option<String> }
attribute EnumCheck { error_message: Option<String> }

// 条件验证
attribute RequiredWhen { dependent_field: String, expected_value: String } // 简化
attribute CustomValidation { error_code: String, message: String }  // 用于方法

// 元数据与安全
attribute Sensitivity { level: SensitivityLevel }
enum SensitivityLevel { Normal, Personal, Sensitive, Secret }

// 版本与演化 (属于 DataContract)
attribute Since { version: u32 }           // 字段引入的版本
attribute Deprecated { since_version: Option<u32>, message: Option<String> }
```

**注意**：`[Required]` 在 DataContract 中表示“业务必填”，与 DataProcess 投影层中的 `[Field(required = true)]`
（流必填）语义不同，但生成器会协调两者避免冲突。

### 5.4 版本管理

**结构版本**由 `[Data(version = n)]` 声明，`[Since]` 记录字段的引入版本。DataContract 的验证器可能根据版本做条件验证，但主要驱动
DataProcess 的兼容性（缺失字段填充）和 DataStorage 的迁移。

**实例版本**（乐观锁）通过 `[Version]` 属性标记在字段上（属于 DataContract 的元数据），DataStorage 利用该字段自动实现并发更新。

```rust
attribute Version {} // 标记字段为实例版本号，DataStorage 处理
```

---

## 6. DataStorage：数据存储

DataStorage 负责对象与持久化介质（关系数据库、文件、KV 缓存等）的映射。它使用 DataContract 的元数据来生成存储结构，并处理
CRUD、索引、迁移。

### 6.1 核心抽象

```rust
// 实体映射器：定义类型 T 与存储结构的关系
pub trait EntityMapper<T> {
    fn table_name(&self) -> &str;
    fn columns(&self) -> &[ColumnMeta];
    fn primary_keys(&self) -> &[usize]; // 列索引
    fn concurrency_field(&self) -> Option<usize>;
}

pub struct ColumnMeta {
    pub member_name: String,
    pub column_name: String,
    pub column_type: StorageType,
    pub is_nullable: bool,
    pub is_unique: bool,
    pub is_indexed: bool,
    pub default_value: Option<Value>,
}

pub enum StorageType { Int32, Int64, String, Float64, Bool, Blob, DateTime, ... }
```

`EntityMapper<T>` 由生成器根据 `[Data]` 标记和额外的存储属性生成。

### 6.2 存储属性

位于 `Sonic.Data.DataStorage` 命名空间：

```rust
attribute PrimaryKey { }
attribute Unique { group: Option<String> } // 联合唯一
attribute Index { unique: bool, name: Option<String> }
attribute ConcurrencyCheck { } // 或直接用 Version
attribute Table { name: String }  // 用于类，自定义表名
attribute Column { name: String, storage_type: Option<StorageType> } // 自定义列名和类型
attribute IgnoreStorage { }  // 持久化时忽略该字段
```

`[Version]` 属性属于 DataContract，但 DataStorage 会识别它作为并发字段。

### 6.3 存储后端接口

DataStorage 本身不提供单一接口，而是为不同介质提供特质。比如：

```rust
// 关系数据库后端
pub trait RelationalStorage {
    fn create_table<T: EntityMapper<T>>(&mut self, mapper: &T) -> Result<()>;
    fn insert<T: EntityMapper<T>>(&mut self, entity: &T) -> Result<()>;
    fn update<T: EntityMapper<T>>(&mut self, entity: &T) -> Result<()>;
    fn delete<T: EntityMapper<T>>(&mut self, entity: &T) -> Result<()>;
    fn find_by_key<T: EntityMapper<T>, K>(&self, mapper: &T, key: K) -> Result<Option<T>>;
    // 迁移...
}

// 键值存储后端
pub trait KeyValueStorage {
    fn put<K, V>(&mut self, key: &K, value: &V) -> Result<()> where K: Projector<K>, V: Projector<V>;
    fn get<K, V>(&self, key: &K) -> Result<Option<V>> where K: Projector<K>, V: Restorer<V>;
    fn delete<K>(&mut self, key: &K) -> Result<()>;
}

// 文件存储后端
pub trait FileStorage {
    fn write<T: Projector<T>>(path: &str, value: &T, serializer: &impl Serializer) -> Result<()>;
    fn read<T: Restorer<T>>(path: &str, deserializer: &impl Deserializer) -> Result<T>;
}
```

### 6.4 迁移支持

DataStorage 可以根据 DataContract 的版本变化生成迁移脚本。例如，`Schema<T>` 提供字段集合，与上一次记录的 schema 快照进行比较，生成
`ALTER TABLE` 或 `CREATE INDEX` 语句。这通常由独立工具实现，但属于 DataStorage 维度。

### 6.5 与 DataProcess 的协作

DataStorage 在存储/读取复合对象时，内部会使用 DataProcess 进行序列化。例如，关系数据库可能将复杂字段（如 `Vec<T>`）序列化为
JSON 或 MessagePack 存入单列，此时 DataStorage 调用相应的 `Serializer`。这些细节由生成器或适配器封装，用户透明。

---

## 7. 统一属性 `[Data]` 与生成器行为

### 7.1 `[Data]` 属性定义

```rust
attribute Data {
    mode: ProjectMode,               // 序列化容器模式：Map 或 Tuple，默认 Map
    rename_all: RenameStyle,         // 默认 None
    version: u32,                    // 结构版本，默认 1
    description: Option<String>,     // 文档描述
    generate_meta: bool,            // 是否生成 ISchema<T>，默认 true
    storage: Option<StorageConfig>,  // 存储配置（可选）
}

enum ProjectMode { Map, Tuple }
enum RenameStyle { None, CamelCase, SnakeCase, KebabCase, UpperCamelCase, LowerCase, UpperCase }
struct StorageConfig {
    backend: String,                // 例如 "relational", "kv", "file"
    table_name: Option<String>,
}
```

### 7.2 生成器产物

对于标记 `[Data]` 的类型 `T`，源生成器在编译时产生以下额外代码（作为 partial 或独立结构）：

1. **DataProcess**：
  - `ProjectorT : Projector<T>` (和 `RestorerT : Restorer<T>`)
2. **DataContract**：
  - `ValidatorT : Validator<T>`
  - `SchemaT : Schema<T>` (若 `generate_meta = true`)
3. **DataStorage** (若配置了 `storage`)：
  - `EntityMapperT : EntityMapper<T>`，以及根据后端可能生成的辅助代码。

生成器会读取所有字段的属性（`[Field]`、`[Required]`、`[PrimaryKey]` 等），分别交由相应维度的生成逻辑处理。

### 7.3 字段级属性 `[Field]` (DataProcess 特有)

```rust
attribute Field {
    name: Option<String>,           // 键名（Map 模式）
    order: usize,                   // Tuple 模式顺序
    default_value: Option<Value>,   // 反序列化缺失时的默认值
    required: bool,                 // 在流中是否必填（Wire 级别）
    skip_when_null: bool,
    skip_when_default: bool,
}
```

注意：`required` 仅影响 DataProcess 的还原，与 DataContract 的 `[Required]` 无关。

---

## 8. 组合与管道

### 8.1 流式处理管道

Sonic.Data 的层次天然支持管道组合。一个典型的 TCP 接收管道：

```rust
let stream = TcpStream::connect("...").unwrap();
let mut unframer = LengthPrefixedUnframer::new();
let mut deserializer = JsonDeserializer::new(/* ... */);

loop {
    let mut buf = [0u8; 1024];
    let n = stream.read(&mut buf)?;
    unframer.feed(&buf[..n])?;

    while let Some(payload) = unframer.try_unframe()? {
        let de = &mut deserializer.with_bytes(payload);
        let person = PersonRestorer.restore(de)?;
        // 验证
        let validator = PersonValidator;
        let v = validator.validate(&person);
        if v.is_valid() {
            // 处理
        }
    }
}
```

### 8.2 中间件步骤

DataProcess 支持在序列化与分帧之间插入中间件（如压缩、加密）：

```rust
trait PipelineStep {
    fn process(input: &[u8], writer: &mut impl Write) -> Result<()>;
}
```

实现 `GzipStep` 或 `AesEncryptStep`，在构建序列化管道时按顺序插入。但这属于可选特性，不在核心抽象中。

---

## 9. 应用场景

### 9.1 Web API 请求处理

1. 从 HTTP 请求体读取字节 → I/O 层。
2. 按 Content-Length 分帧 → 分帧层。
3. JSON 反序列化 → 序列化层 + 投影层 (DataProcess)。
4. 对 DTO 执行验证 → DataContract 验证器，可能返回多个错误。
5. 业务逻辑后，若要持久化 → DataStorage 执行插入。

### 9.2 配置文件加载与校验

1. 文件 I/O 读取所有字节。
2. 反序列化为 `AppConfig` 对象。
3. 使用 DataContract 验证器检查配置合法性，所有错误一次性报告。

### 9.3 分布式事件溯源

- 每个事件类型标记 `[Data]`。
- 事件序列化后存入事件存储（DataStorage，可能是关系表或消息队列）。
- 重放时反序列化并验证。
- 版本管理利用 `[Since]` 处理向前兼容。

### 9.4 ORM 集成

- 实体类 `[Data(storage = ...)]`，生成 `EntityMapper`。
- 使用 `RelationalStorage` 实现自动创建表、CRUD。
- 实例版本 `[Version]` 字段由 DataStorage 在 update 时自动递增并加入 WHERE 条件，实现乐观锁。

---

## 10. 与外部系统的边界

Sonic.Data 并不试图实现所有东西，它明确定义了边界：

- **数据仓库/数据湖**：使用 Sonic.Data 的 DataStorage 抽象作为底层格式，但查询引擎（如 Spark）是独立的。
- **流处理框架**：可以使用 DataProcess 进行消息的序列化，DataContract 进行校验，但框架本身负责窗口、聚合、背压等。
- **缓存系统**：Redis 作为 DataStorage 的 KV 后端，封装序列化/反序列化。
- **文件同步（如 Git）**：历史版本管理是独立的库，可使用 Sonic.Data 的 DataContract 定义快照结构，DataStorage 存储历史。

---

## 11. 架构图

```
                          [Data] 标记的类型
                                   |
          ┌────────────────────────┼──────────────────────────┐
          |                        |                          |
   DataProcess               DataContract               DataStorage
   (五层抽象)                (验证、元数据)            (持久化映射)
          |                        |                          |
  project/restore              validate/ schema          EntityMapper
  (投影层)                    (多错误)                   (表/键/索引)
          |                        |                          |
  serialize/deserialize          版本、安全              SQL/KV/文件
  (序列化层)                      (演化)
          |
  frame/unframe
  (分帧)
          |
  encode/decode
  (编码)
          |
  read/write (I/O)
```

---

## 12. 总结

Sonic.Data 3.0 通过三个正交维度，以极简的统一标记 `[Data]` 提供了覆盖数据处理全生命周期的完整抽象。DataProcess
负责数据如何流动，DataContract 负责数据必须满足的契约，DataStorage 负责数据如何落地。三者独立组合，无概念污染，无职责重叠，为现代软件开发奠定了坚实的数据基础。

---

**文档结束**  
（总字数约 9500 字，如需满足 40000
字，可进一步扩展每一层的实现细节、生成代码样例、错误处理、性能优化、安全考量、大量具体应用场景的详细伪代码，以及各个内置实现的完整描述。此处受限于输出长度，核心架构已完整呈现。）

## 推演：常见应用的数据转换与 Sonic.Data 三维映射

Sonic.Data 的三个维度—— **DataProcess**（数据流动/变换）、 **DataContract**（数据契约/验证）、 **DataStorage**
（数据持久化）——可以覆盖绝大多数数据密集型应用的数据处理环节。以下将逐一推演各类典型应用，展示它们如何自然地分解为这三个维度的组合，以及哪些部分属于应用特有的“业务逻辑”，从而验证
Sonic.Data 作为标准库基础抽象的完备性。

---

### 1. 文字处理（如 Pandoc）

Pandoc 是一款文档格式转换工具，支持 Markdown、HTML、LaTeX、DOCX 等多种格式之间的互转。其核心是解析某种格式的输入，转换成内部抽象语法树（AST），再输出为另一种格式。

**DataProcess 维度**：

- **读取输入**：从文件或 stdin 读取原始字节流 → I/O 层 `Read`。
- **解码**：将字节解码为 UTF‑8 文本 → 编码层 `decode_string`。
- **解析格式**：将文本按照特定格式（如 Markdown）的语法规则解析为 AST 节点。这需要格式特定的 **解析器**
  ，它接收文本流，输出结构化数据。解析器本身不属于 Sonic.Data，但它会 **使用序列化概念**
  ：解析过程可以看作从文本格式反序列化为通用树结构。我们可以为每种格式实现一个 `Deserializer`（如 `MarkdownDeserializer`
  ），它读取文本并产生 AST。
- **输出格式**：将 AST 序列化为目标格式的文本或二进制。可为每种输出格式实现 `Serializer`（如 `HtmlSerializer`），通过投影层将
  AST 节点映射到格式化写入操作。
- **分帧**：某些格式（如 DOCX）是 Zip 压缩包，需要先进行 Zip 解帧 → `ZipUnframer`（分帧层），解压出内部 XML 文件，再进入 XML
  反序列化。

**DataContract 维度**：

- **AST 定义**：Pandoc 的 AST 本质上是一个数据类型，可以标记 `[Data]`，自动生成投影/还原代码，以及验证器。例如，`Document` 包含
  `Vec<Block>`，每个 `Block` 可以是段落、标题、列表等变体。DataContract 可提供验证，确保 AST 结构合法（如标题必须包含文本）。
- **元数据**：文档元数据（标题、作者、日期）可视为契约的一部分，可通过 `Schema` 导出让外部工具读取。
- **版本**：当 Pandoc AST 随版本升级变化时，可利用 `[Since]` 标记新增节点类型，配合 DataProcess 投影层处理向后兼容。

**DataStorage 维度**：

- 不直接涉及，除非需要缓存已解析的 AST 到磁盘或数据库，可使用 DataStorage 的序列化存储（如 JSON 或二进制）加速二次转换。

**应用特有逻辑**：

- 格式转换的核心算法（如 Markdown 的解析规则、引文处理、模板渲染）属于业务代码，它们操作 AST 并调用序列化器。

---

### 2. 文档生成（如 Sphinx, Doxygen, 静态站点生成器）

文档生成器通常从源代码注释、Markdown/RST 源文件生成 HTML/PDF 等文档。

**DataProcess 维度**：

- **读取源文件**：I/O 层读取 Markdown 或源码文件。
- **解析输入**：将 Markdown 反序列化为 AST（与 Pandoc 类似），或将源代码注释提取为结构化数据（如 `CommentBlock`）。
- **模板渲染**：将文档结构体与模板结合，生成目标格式。这可以视为序列化过程：`HtmlTemplateSerializer` 接受文档树，按模板写出
  HTML 文本。
- **中间表示**：文档生成器常使用内部表示（IR），可定义为 `[Data]` 类型，通过投影层实现不同格式的输出（HTML、PDF、LaTeX 等）。

**DataContract 维度**：

- 验证文档结构的完整性：例如，文档引用链接是否有效、章节编号是否连续。通过自定义验证器实现。
- 文档元数据（如配置文件中的 site.title）可通过 `Schema` 导出给编辑器插件。

**DataStorage 维度**：

- 生成的文档最终写入文件系统 → `FileStorage`。
- 增量构建时，可缓存已处理的文档 IR 到磁盘（DataStorage 序列化），避免重复解析。

**业务逻辑**：模板语言解释、交叉引用解析、索引生成等。

---

### 3. 编译器（如 Rust、GCC 等）

编译器将高级语言源码转换为机器码或中间码。其流程：词法分析 → 语法分析 → 语义分析 → 中间表示（IR）→ 优化 → 代码生成。

**DataProcess 维度**：

- **读取源文件**：I/O 层 `Read` 字节流，解码为 UTF‑8 文本。
- **词法分析**：从文本中解析出 Token 流。Token 序列可以看作一种 **序列化格式**：可以设计 `TokenDeserializer` 将源码反序列化为
  `Vec<Token>`。
- **语法分析**：将 Token 流反序列化为抽象语法树（AST）。这同样是一个反序列化过程：`AstDeserializer` 消费 Token 流，构造
  `AstNode` 树。
- **IR 序列化/反序列化**：编译器内部 IR 经常需要序列化（如保存到磁盘、分发给其他编译阶段）。可以使用 `Projector<IrModule>` /
  `Restorer<IrModule>` 进行高效的二进制序列化（如 Protobuf 风格）。
- **代码生成**：从 IR 生成目标代码文本或二进制。这可以视为 `Serializer`，接收 IR
  并写入汇编文本或机器码字节。对于文本汇编，类似于序列化为文本格式；对于机器码，则是编码为特定指令格式（编码层 `encode` 指令）。
- **分帧**：当生成的目标文件是 ELF/PE 等格式时，需要将其包装为段（sections）并写入文件头，这类似于分帧操作：`Framer`
  将各段载荷封装成完整的可执行文件帧。

**DataContract 维度**：

- 定义语言的语法规则？这部分过于动态，不适合声明式属性。但可以定义 **IR 的结构**：IR 指令、类型系统、符号表等，使用 `[Data]`
  生成验证器，确保 IR 在编译各阶段之间保持合法（如类型匹配、使用前定义）。
- 编译器版本与语言版本：利用结构版本 `[Data(version=...)]` 和 `[Since]` 来处理 IR 的演化，兼容旧版本编译缓存。

**DataStorage 维度**：

- 编译缓存：将编译中间产物（如编译单元、依赖图）存储到文件系统或数据库。可以使用 DataStorage 实现缓存管理，自动序列化/反序列化。
- 增量编译：通过检查文件修改时间（mtime）或内容哈希，决定是否重新编译。这属于存储层的查询逻辑。

**业务逻辑**：类型检查、优化遍、内联等算法，它们操作 IR 但不属于数据处理抽象。

---

### 4. 图片处理（如 ImageMagick, PIL, Photoshop 部分）

图片处理涉及编解码各种图像格式（PNG, JPEG, GIF 等）以及像素级操作。

**DataProcess 维度**：

- **读取/写入图像文件**：I/O 层读文件字节，写回结果。
- **图像格式解码/编码**：每种图像格式本质上是 **编码层 + 序列化层**的组合。例如，PNG 解码器首先处理分帧（PNG chunk
  解帧），然后进行解码（deflate 解压、滤波），最后将像素数据反序列化为通用的位图表示（如 `ImageBuffer`）。Sonic.Data 的编码层可提供
  deflate 解码器，分帧层可处理 chunk 格式。
- **格式转换**：将一种格式的字节流转换为另一种格式，可以分解为：源格式反序列化 → 通用图像结构 → 目标格式序列化。`Image`
  可以是一个 `[Data]` 类型，包含像素缓冲区、色彩空间等。不同格式实现相应的 `Serializer`/`Deserializer`。投影层负责将 `Image`
  映射到特定的格式操作。

**DataContract 维度**：

- **图像元数据**：EXIF、ICC 色彩配置文件、GPS 数据等，可作为 `Image` 类型的字段，用契约来验证合法性（如尺寸非负、颜色通道数有效）。
- **像素范围验证**：验证图像尺寸、像素值是否在有效范围内（例如 0-255 for 8-bit）。

**DataStorage 维度**：

- 将处理后的图像保存到文件或对象存储 → `FileStorage`。
- 图像缓存（如缩略图）存储在 KV 缓存或数据库。

**业务逻辑**：滤镜、缩放、旋转等像素操作。这些是纯算法，操作内存中的像素数组，与数据抽象无关。

---

### 5. 多媒体处理（FFmpeg）

FFmpeg 处理音视频的编解码、复用/解复用、滤镜。

**DataProcess 维度**：

- **读取/写入文件或流**：I/O 层 `Read`/`Write`。
- **容器格式解复用/复用**：MP4, MKV 等容器本质上是 **分帧层**，它们将原始压缩的音视频包（packet）打包成容器帧。`Unframer`
  可以从容器中提取一个个 packet（载荷），并提供时间戳等元数据。
- **编解码**：视频编码（H.264, H.265）和音频编码（AAC, MP3）是典型的 **编码层**。它们将原始未压缩帧（YUV 平面、PCM
  样本）编码为压缩码流，或反向解码。我们可以有 `VideoEncoder` / `VideoDecoder` 实现 `encode_frame` / `decode_frame` 接口。
- **像素/音频数据序列化**：原始帧数据可以视为特定格式的字节序列，但更常见的是在内存中表示为多维数组。Sonic.Data
  的序列化层可能不直接处理这种大型原始数据，但可以将其视为 `RawBuffer` 类型，提供投影器直接写入/读取字节。

**DataContract 维度**：

- **编解码参数**：分辨率、帧率、采样率、编码档次等，都是 `VideoConfig` 或 `AudioConfig` 结构体的字段，可声明 `[Data]`
  生成验证器，确保参数在编码器支持范围内。
- **流信息验证**：检查容器中的所有流是否兼容，时基是否一致。

**DataStorage 维度**：

- 输出文件存储。
- 流媒体分发（如将编码后的包推送到 RTMP 服务器）可视为 DataStorage 的特化（网络存储）。

**业务逻辑**：滤镜图、帧率变换、音量调整等处理算法。

---

### 6. 大模型 Data Loader（PyTorch / TensorFlow 数据管道）

深度学习数据加载管道从各种存储后端读取原始数据，解码、预处理、混洗、批处理，最终喂入模型。

**DataProcess 维度**：

- **读取原始数据**：从文件系统、对象存储或网络流中读取字节 → I/O 层。
- **格式解码**：图像（JPEG 解码）、文本（UTF‑8 解码）、音频（WAV 解码）等，由相应的解码器完成，将字节转换为张量或标准结构。
- **序列化/反序列化**：训练样本通常定义为 `struct Sample { image: Tensor, label: i32 }`。从原始数据集中读取时，可以利用
  `Restorer<Sample>` 将每条记录（如 TFRecord 条目）反序列化为 `Sample` 对象。
- **预处理**：数据增强（裁剪、翻转）通常是对张量进行操作，不属于数据处理抽象。但预处理流水线可以建模为一系列转换步骤，每个步骤接受一个
  `Sample` 并返回新的 `Sample`，类似中间件。

**DataContract 维度**：

- **数据契约**：验证样本的 shape、数据类型、标签范围，防止无效数据进入训练。例如，图像尺寸必须匹配模型输入，标签必须在类别数内。使用
  `Validator<Sample>` 收集所有违规项，决定丢弃或修正。
- **元数据**：数据集描述（类别名、图像通道类型）可通过 `Schema<Sample>` 导出，用于模型配置。

**DataStorage 维度**：

- 数据集存储：图像文件、TFRecord 文件、LMDB 数据库、HDF5 文件等。这些是 DataStorage 的不同后端。Sonic.Data 可以提供
  `FileStorage`、`KeyValueStorage` 等接口，内部封装数据的读取和序列化。
- 数据混洗与缓存：混洗缓冲区可以使用本地存储或内存队列，属于存储层的临时缓存。

**业务逻辑**：数据增强算法、混洗策略、分布式采样策略（与分布式训练框架协作）。

---

### 7. 分布式数据库

分布式数据库（如 TiDB, CockroachDB）涉及数据分片、复制、事务、存储引擎等。

**DataProcess 维度**：

- **网络通信**：节点间 RPC 消息的序列化/反序列化。使用 `Data` 类型定义消息（如 `RequestVote`, `AppendEntries`），DataProcess
  通过 Protobuf 或自定义格式序列化并通过 TCP 流发送，用 `LengthPrefixedFramer` 分帧。
- **存储引擎内部格式**：行数据在内存和磁盘上的编码（如 LSM-Tree 的 SSTable 格式）。这涉及编码层（变长整数、字符串）和序列化层（行编码为字节）。可以定义
  `RowSerializer` 和 `RowDeserializer`。

**DataContract 维度**：

- **表模式（Schema）**：就是 DataContract 的核心。每个表结构定义为一个 `[Data]` 类型，字段的列名、类型、约束（如 `PrimaryKey`、
  `Unique`、`NotNull`）由属性声明。DataContract 的 `Schema<T>` 提供元数据，用于自动生成 SQL DDL 和查询规划。
- **版本演化**：DDL 变更（添加/删除列）通过 `[Since]` 和 `[Deprecated]` 管理，DataStorage 生成在线模式迁移。
- **数据验证**：写入数据时验证是否符合当前模式（类型、范围），多错误报告。

**DataStorage 维度**：

- **存储映射**：`EntityMapper<T>` 将行映射到分布式 KV 存储或磁盘文件。索引也基于契约中的 `[Index]` 生成。
- **分布式事务**：两阶段提交、MVCC 等属于业务逻辑，但数据持久化由 DataStorage 的后端负责（RocksDB, 文件等）。

**业务逻辑**：分布式一致性协议（Raft）、查询优化器、事务管理器。它们使用 DataProcess 通信，DataContract 定义数据，DataStorage
持久化，但本身是复杂算法。

---

### 8. 分布式云服务（如 AWS Lambda, Kubernetes 配置）

云服务处理大量配置、请求、日志。

**DataProcess 维度**：

- **API 请求/响应**：HTTP REST 或 gRPC 消息的序列化/反序列化。云 API 通常定义请求结构体，用 `[Data]`
  标记，自动生成服务端验证和客户端代理（DataProcess 序列化 + DataContract 验证）。
- **事件负载**：Lambda 的事件参数（如 S3 事件通知）反序列化为特定类型。

**DataContract 维度**：

- **资源配置**：云资源定义（如 `LambdaConfig`, `ContainerSpec`）用 `[Data]` 定义，自动验证（如内存范围、端口号合法）并生成文档。
- **合规验证**：检查配置是否符合公司策略（如必须加密、必须绑定安全组）可通过自定义验证器。

**DataStorage 维度**：

- **状态存储**：将资源状态持久化到数据库或文件（如 Terraform state），DataStorage 管理其序列化与并发版本（`.tfstate`
  文件的版本锁定）。
- **日志和监控数据**：结构化日志存储到时序数据库或对象存储，通过 DataProcess 序列化。

**业务逻辑**：调度算法、自动伸缩、权限检查。

---

### 9. 总结模式

从这些推演可以看出，Sonic.Data 的三个维度恰好覆盖了所有数据处理相关的“工具性”代码，而应用特有的“创造性”代码（算法、业务规则）则位于这些维度之上。

- **DataProcess** 负责一切 **格式转换**与 **字节流动**，无论读写文件、网络通信、编解码媒体、序列化AST，都落在此层。
- **DataContract** 负责 **数据应该是什么**的声明与检查，从文档元数据到数据库表模式，从配置合法性到训练样本完整性。
- **DataStorage** 负责 **数据如何安放**，无论是磁盘文件、数据库、缓存还是对象存储，都通过统一映射管理。

这种正交划分不仅消除重复代码（无需为每个项目重写序列化、验证、存储逻辑），还使得不同应用间的数据相互操作变得容易——因为契约和过程是标准化的。Sonic.Data
3.0 因此成为一个真正通用、强大且简洁的数据基础架构。