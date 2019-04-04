# Sonic.Database 0.2.0 完整设计文档

> **版本**：0.2.0  
> **状态**：正式提案  
> **依赖**：Hypersonic 1.0.0（门面契约）  
> **所属生态**：Sonic 标准库嵌入式数据库子系统

---

## 一、引言与设计目标

Sonic.Database 0.2.0 是 Sonic 标准库中负责 **本地嵌入式数据持久化** 的统一抽象层与实现集。它在 Hypersonic
门面的约束下，提供一套极致通用、零领域耦合的键值存储契约，以及三种开箱即用的数据库产品，覆盖从纯内存缓存到安全文件存储、再到模拟测试的全场景需求。同时，通过
Source Generator 扩展，它实现了编译时强类型仓储和查询生成，并为下游生态的 SQL、图、向量等适配器奠定了无侵入的基石。

**核心目标**：

1. **定义最纯粹的持久化原语**：将数据库抽象为 Query → Key → Value（QKV）引擎，剔除所有 SQL、文档、图、向量等高级语义，仅保留事务性键值操作。
2. **提供产品级可用的本地数据库**：MemoryDB、LightDB、MockDB 三大产品，覆盖不同可靠性/性能权衡，且均实现相同的门面接口。
3. **通过 Source Generator 消除样板代码**：结合 Hypersonic 的实体标记属性，自动生成强类型仓储和查询，而无需手动编写重复的
   CRUD。
4. **保障下游无限扩展可能**：允许第三方在不修改核心库的情况下，通过适配器模式接入任何外部数据库，或构建关系、图、向量等高级数据库系统。

---

## 二、我们不做什么（边界明确）

为了保持库的纯净与长久稳定性，Sonic.Database 明确划定以下不作为：

- **不提供任何查询语言解析或执行**。不包含 SQL 解析器、执行计划器、优化器，也不包含 Cypher、Gremlin、SPARQL
  等图或文档查询引擎。所有查询均通过键扫描或光标完成，或由外部扩展包提供。
- **不绑定任何网络协议**。不实现 PostgreSQL、MySQL、Redis 等线协议，也不包含 HTTP/REST 数据服务。这些属于第三方适配器范畴。
- **不实现分布式功能**。不提供分片、复制、一致性协议（如 Raft/Paxos）、自动故障转移等。Sonic.Database 仅面向单节点本地存储。
- **不定义高级数据模型**。不提供关系模型（表、行、列）、图模型（节点、边）、文档模型（集合、嵌套文档）等，仅将数据视为不透明字节序列。
- **不替代 BCL 集合或缓存**。尽管 MemoryDB 功能上类似字典，但它实现了 IDatabase 事务接口，适用于需要事务性、快照隔离等场景；简单的键值缓存仍推荐使用
  `IDistributedCache` 或 `Dictionary<TKey,TValue>`。
- **不进行运行时反射**。所有元数据生成均发生在编译期，运行时零反射。
- **不强行绑定具体存储格式**。索引实现（B+树、LSM、跳表）为内部细节，不暴露在公共契约中。

---

## 三、Hypersonic 中的数据库契约

Hypersonic 1.0.0 新增一级命名空间 `Sonic.Database`，内嵌纯粹的门面类型。

### 3.1 命名空间与接口总览

```
Hypersonic
└── Sonic.Database
    ├── IDatabase.cs                // 核心数据库接口
    ├── ITransaction.cs             // 事务接口
    ├── ICursor.cs                  // 键值游标接口
    ├── ISnapshot.cs                // 快照接口
    ├── IIndexManager.cs            // 索引管理器接口
    ├── IsolationLevel.cs           // 事务隔离级别枚举
    ├── DatabaseOptions.cs          // 数据库通用配置基类（纯属性容器）
    ├── IndexOptions.cs             // 索引选项
    └── Attributes
        ├── IndexedAttribute.cs     // 标记字段需建索引
        └── UniqueIndexAttribute.cs // 标记唯一索引
```

所有类型均为 **接口、枚举或纯数据容器（只有 get/set/init 属性，无任何方法体）**，符合 Hypersonic 零运行时逻辑原则。键与值统一使用
`ReadOnlyMemory<byte>`，确保实现无关。

### 3.2 核心接口详细定义

#### `IDatabase`

```csharp
namespace Sonic.Database
{
    public interface IDatabase
    {
        // 单键操作
        Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);
        Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default);
        Task DeleteAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);
        Task<bool> ContainsKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default);

        // 事务与快照
        ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot);
        ISnapshot CreateSnapshot();

        // 游标
        ICursor CreateCursor();

        // 索引管理
        IIndexManager IndexManager { get; }
    }
}
```

**设计意图**：这是整个数据库子系统的“世界根”。所有操作均为异步，以适应可能的 IO 操作（即便是内存实现也统一异步签名，方便替换）。
`ITransaction` 返回非泛型事务对象，由具体引擎实现决定事务内操作方式。`CreateSnapshot` 返回一个可重复读的视图。

#### `ITransaction`

```csharp
public interface ITransaction : IDisposable
{
    Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key);
    Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value);
    Task DeleteAsync(ReadOnlyMemory<byte> key);
    Task CommitAsync();
    Task RollbackAsync();
    IsolationLevel IsolationLevel { get; }
}
```

事务内部操作无需传递 CancellationToken（由外部发起者控制整个事务生命周期）。提交或回滚必须原子化，且释放资源。

#### `ICursor`

```csharp
public interface ICursor
{
    ValueTask<bool> MoveNextAsync();
    ValueTask<bool> MovePreviousAsync();
    ValueTask SeekAsync(ReadOnlyMemory<byte> key);
    (ReadOnlyMemory<byte> Key, ReadOnlyMemory<byte> Value) Current { get; }
}
```

游标支持双向遍历，以键的字典顺序移动。`SeekAsync` 定位到 >= 给定键的第一个条目。这提供了范围扫描和前缀扫描的基础（结合业务层的前缀判断）。

#### `ISnapshot`

```csharp
public interface ISnapshot
{
    ReadOnlyMemory<byte>? Get(ReadOnlyMemory<byte> key);
    ICursor CreateCursor();
}
```

快照为同步操作，因为其数据已在创建时冻结，不需要异步 IO。实现应保证读取隔离。

#### `IIndexManager`

```csharp
public interface IIndexManager
{
    Task CreateIndexAsync(string name, ReadOnlyMemory<byte> prefix, IndexOptions options);
    Task DropIndexAsync(string name);
    Task<bool> IndexExistsAsync(string name);
}
```

索引名全局唯一。`prefix` 定义索引的键前缀（例如 `user:name`），用于扫描。内部实现可为 B+ 树、哈希等，但不暴露细节。

#### 枚举与配置类

```csharp
public enum IsolationLevel
{
    ReadUncommitted,
    ReadCommitted,
    Snapshot,
    Serializable
}

public sealed class DatabaseOptions
{
    public string? Name { get; init; }
    public int PageSize { get; init; } = 4096;
    public int CacheSize { get; init; } = 1024;
}

public sealed class IndexOptions
{
    public bool Unique { get; init; }
}
```

`DatabaseOptions` 为所有引擎的通用配置基类，特定实现可以继承扩展。

#### 属性

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class IndexedAttribute : Attribute
{
    public string? Name { get; init; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueIndexAttribute : Attribute
{
    public string? Name { get; init; }
}
```

这两个属性用于标记实体类属性，编译时代码生成时会据此创建数据库索引。

---

## 四、Sonic 运行时实现

Sonic 程序集提供 `Sonic.Database` 命名空间下的三个数据库产品，均实现 `IDatabase`。

### 4.1 MemoryDB：纯内存数据库

**适用场景**：单元测试、开发环境、无持久化需求的缓存或中间结果存储。

**实现要点**：

- 底层使用 `ConcurrentDictionary<byte[], byte[]>` 并配合自定义比较器（基于字节序列的 `ReadOnlySpan<byte>` 比较）。
- 事务支持：通过锁或 Copy-on-Write 快照实现多版本。简化版可采用 `ReaderWriterLockSlim` 保护整个字典，事务为单线程批量操作，串行化所有访问。对于
  `Snapshot` 级别，可在事务开始时复制整个字典的快照（内存允许），以提供无锁读。
- 游标实现：因字典无序，需额外维护一个有序的键列表（如 `SortedList<byte[], byte[]>`），或在遍历时对键排序。
- 无持久化，无 WAL，进程退出数据丢失。

**构造函数**：

```csharp
public sealed class MemoryDB : IDatabase
{
    public MemoryDB(string name = null, IServiceProvider? serviceProvider = null) { ... }
}
```

可注入 `ISerializer<T>` 等，但非必需。

### 4.2 LightDB：轻量级文件数据库

这是 Olymp.Athena 重构后的官方嵌入式数据库引擎，提供可靠、事务性、持久化的本地存储。

**核心特性**：

- **多存储引擎支持**：可通过选项选择 FileStream、MemoryMappedFile 或 LSM 存储引擎。默认 FileStorageEngine。
- **MVCC 快照隔离**：通过 B⁺ 树（内部使用）存储键，每个键值对带序列号，配合 `VersionStore` 维护历史版本。支持四种隔离级别。
- **WAL 日志与恢复**：写入前记录日志，提供三级刷盘策略（EveryWrite, Batch, Periodic），使用 Brotli 压缩和 CRC32 校验。崩溃恢复自动重放
  WAL。
- **LRU 页缓存**：基于 `AsyncReaderWriterLock` 的线程安全缓存，支持 pin/unpin 防止淘汰脏页。
- **墓碑删除与 Compaction**：删除操作写入墓碑，后台合并时物理清理，回收空间。
- **SIMD 加速键比较**：内部 `LightKey` 结构使用 `Vector256` 进行前缀匹配和比较。
- **索引**：`IIndexManager` 基于 B⁺ 树实现辅助索引，支持前缀扫描。

**不包含 SQL 引擎**，仅提供基于 `ICursor` 的范围扫描。若需 SQL 支持，由上层扩展包提供。

**配置与构建**：

```csharp
public sealed class LightDB : IDatabase
{
    public LightDB(LightOptions options) { ... }
}

public sealed class LightOptions : DatabaseOptions
{
    public string DirectoryPath { get; init; } = ".";
    public StorageEngineType StorageEngine { get; init; } = StorageEngineType.File;
    public bool EnableCompression { get; init; } = false;
    public WalFlushPolicy WalFlushPolicy { get; init; } = WalFlushPolicy.Batch;
    // ...其他
}
```

通过 `LightOptions` 配置数据目录、页面大小等，并可在 DI 中注册。内部组件（如 `IStorageEngine`、`IWalWriter`）可被替换，但默认实现不暴露。

### 4.3 MockDB：可编程模拟数据库

**适用场景**：单元测试中模拟数据库行为，验证仓储逻辑，避免依赖真实文件系统。

**实现**：

- 内部使用 `Dictionary<byte[], byte[]>` 存储，并提供行为注入点。
- 可以预设特定键的返回值，或者注入延迟、异常。
- 提供 `SetupGet(key, value)`、`SetupPut(handler)` 等方法，风格类似 Moq。
- 事务模拟仅记录操作，`CommitAsync` 可将记录的操作应用到内部字典（如果配置为真实写入），或始终成功。
- 游标模拟有序遍历。

```csharp
public sealed class MockDB : IDatabase
{
    public void SetupGet(byte[] key, byte[]? value) { ... }
    public void SetupPut(Func<byte[], byte[], Task> handler) { ... }
    public void SetupException(Type exceptionType) { ... }
    // ...
}
```

---

## 五、Sonic.SourceGenerator 与强类型仓储

### 5.1 编译时生成目标

Sonic.SourceGenerator 项目（作为 Analyzer 分发）包含 `StorageGenerator`，当检测到类标记了 `[Entity]`（来自 Hypersonic
`Sonic.Data.Attributes`）且项目引用了 `Sonic.Database` 时，会生成以下内容：

1. **键构建器**：根据 `[Key]` 属性自动生成 `ReadOnlyMemory<byte>` 键，常用模式如 `{EntityName}:{Id}`。
2. **仓储接口与实现**：为每个 `[Entity]` 类生成 `I{Entity}Repository` 接口和 `{Entity}Repository` 类，提供 `GetByIdAsync`、
   `PutAsync`、`DeleteAsync` 等。
3. **索引管理**：若属性上有 `[Indexed]`，则生成在数据库初始化时创建索引的代码，并生成 `FindBy{PropertyName}Async`
   方法，利用前缀扫描。
4. **序列化集成**：自动使用 Sonic 为实体生成的 `ISerializer<T>` 和 `IDeserializer<T>`，将实体转换为 `byte[]` 存储。

### 5.2 生成代码示例

用户实体：

```csharp
[Entity]
public partial class User
{
    [Key] public Guid Id { get; init; }
    [Indexed] public string Name { get; init; }
    public int Age { get; init; }
}
```

生成器输出：

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task PutAsync(User user);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<User>> FindByNameAsync(string name);
}

internal sealed class UserRepository : IUserRepository
{
    private readonly IDatabase _db;
    private readonly ISerializer<User> _serializer;
    private readonly IDeserializer<User> _deserializer;

    public UserRepository(IDatabase db, ISerializer<User> serializer, IDeserializer<User> deserializer)
    {
        _db = db;
        _serializer = serializer;
        _deserializer = deserializer;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var key = KeyBuilder.Create(nameof(User), id);
        var bytes = await _db.GetAsync(key);
        return bytes == null ? null : _deserializer.Deserialize(bytes.Value);
    }

    public async Task<IReadOnlyList<User>> FindByNameAsync(string name)
    {
        var prefix = KeyBuilder.Create(nameof(User), name); // 假设 Name 索引前缀
        var cursor = _db.CreateCursor();
        await cursor.SeekAsync(prefix);
        var result = new List<User>();
        while (await cursor.MoveNextAsync())
        {
            var (k, v) = cursor.Current;
            if (!k.Span.StartsWith(prefix.Span))
                break;
            result.Add(_deserializer.Deserialize(v));
        }
        return result;
    }
    // ...其他方法
}
```

注意 `nameof(User)`、`KeyBuilder.Create` 等使用 `typeof` 和 `nameof` 确保重构安全。

---

## 六、下游生态：扩展与高级系统

### 6.1 第三方可以做什么

基于 Hypersonic 的 `IDatabase` 抽象，下游开发者可以构建丰富产品：

#### 6.1.1 数据库协议适配器

实现 `IDatabase` 接口，封装远程数据库客户端，使本地代码以相同 API 访问 PostgreSQL、MySQL、Redis 等。例如：

- **Axum.PGSQL**：内部使用 Npgsql，但对外暴露 `IDatabase`，将 `PutAsync` 映射为 INSERT/UPDATE，`GetAsync` 映射为 SELECT。
- **Spring.MySQL**：类似，封装 MySQL 协议。 这样，业务代码无需感知底层是本地文件还是远程数据库。

#### 6.1.2 查询语言适配器（QL 扩展包）

定义专属属性（如 `[Sql]`、`[Gremlin]`）和 Source Generator，生成强类型查询方法。例如 `Sonic.Database.Sql` 扩展包：

- 属性：`[Sql("SELECT ...")]`
- 生成器解析 SQL，生成方法调用 `ISqlExecutor`。
- `ISqlExecutor` 由适配器实现，可以翻译为 `IDatabase` 的键操作（嵌入式）或直接发送 SQL 到远程服务器。

类似的图查询扩展包 `Sonic.Database.Graph` 可定义 `[Cypher]` 属性，向量扩展包 `Sonic.Database.Vector` 定义
`[VectorSearch]`。

#### 6.1.3 高级数据库系统

利用 `IDatabase` 作为存储底层，构建完整的关系数据库、图数据库、向量数据库、时序数据库等。例如：

- **关系数据库**：自行开发 SQL 解析器、优化器，二级索引使用 `IIndexManager`，行数据序列化后存入 `IDatabase`。
- **图数据库**：节点和边映射为键值对，利用前缀扫描实现高效遍历，事务支持多节点更新。
- **向量数据库**：实现 ANN 索引（如 HNSW），向量本身存于 `IDatabase`，邻接图作为内部数据结构。

由于所有上层系统都依赖同一套 `IDatabase` 抽象，切换底层存储（例如从文件升级到分布式 KV）对它们透明。

#### 6.1.4 工具与诊断

- **性能分析器**：通过 `IDatabase` 接口附加装饰器（如 ProfilingDecorator）收集延迟、吞吐量等指标，输出到 OpenTelemetry。
- **数据迁移工具**：在两个 `IDatabase` 实例间传输数据，或导出为 JSON/CSV。

### 6.2 Sonic 官方提供的扩展包（未来规划）

Sonic 核心团队将陆续发布以下扩展包，以展示最佳实践并丰富生态：

| 包名                         | 功能                                         |
|------------------------------|----------------------------------------------|
| `Sonic.Database.Sql`         | 编译时 SQL 适配器，提供 `[Sql]` 属性和生成器 |
| `Sonic.Database.Pooling`     | 通用连接池抽象与实现                         |
| `Sonic.Database.Migration`   | Schema 迁移框架，基于 `IDatabase` 的版本管理 |
| `Sonic.Database.Profiling`   | 性能计数器与诊断集成                         |
| `Sonic.Database.Lsm`         | LSM 存储引擎插件（可集成到 LightDB）         |
| `Sonic.Database.Compression` | Brotli/Zstd 压缩装饰器（用于存储引擎）       |

这些包与第三方开发处于平等地位，均依赖 Hypersonic 和 Sonic 核心，不享受特权。

---

## 七、与 Sonic 生态其他模块的协作

### 7.1 与 Data 子系统

- `Sonic.Data.Attributes` 中的 `[Entity]`、`[Key]` 等驱动仓储生成。数据库本身不感知实体，仅通过 `ReadOnlyMemory<byte>`
  操作，序列化交由 `Sonic.Data.Serializer` 生成的静态代码。
- `Sonic.Data.Cache` 中的 `IDistributedCache` 可以与 `MemoryDB` 配合：使用 `MemoryDB` 作为高性能本地缓存后端，实现
  `IDistributedCache` 接口，从而在应用中透明切换缓存存储。

### 7.2 与 Config 子系统

- LightDB 的配置可通过 `Sonic.Config` 的强类型绑定从 `appsettings.json` 加载，例如：
  ```csharp
  var lightOptions = config.Get<LightOptions>("LightDB");
  ```
- 数据库的全局选项（如默认存储目录）也可通过配置系统注入。

### 7.3 与 Observability 子系统

- 可以利用 `[Metric]` 属性标记数据库操作，生成计数器（如 `db_reads_total`、`db_writes_total`），与 OpenTelemetry 集成。

### 7.4 与 Concurrency、Stream、Compression 等

- `MemoryDB` 内部可使用 `Sonic.Stream.BufferWriter` 实现零分配序列化。
- `LightDB` 的 WAL 压缩直接调用 `Sonic.Compression.ICompressor` 实现，而页缓存使用 `Sonic.Memory.ArrayPool` 减少分配。

---

## 八、设计原则总结

1. **极致接口分离**：Hypersonic 定义“什么是数据库”，Sonic 提供“如何实现”，扩展包实现“能做什么”。每一层只依赖抽象。
2. **零领域污染**：门面中无 SQL、图、文档等术语，仅存 QKV 原语，保证通用性至少二十年。
3. **编译时安全**：所有元数据操作通过 Source Generator 在编译期完成，使用 `typeof`/`nameof` 实现重构安全。
4. **性能极致**：LightDB 采用 SIMD 加速、零分配序列化、可插拔存储引擎，确保与手写优化代码相当的性能。
5. **可测试性**：MockDB 和 MemoryDB 使得依赖数据库的业务逻辑可以在无外部依赖的情况下快速测试。
6. **开放扩展**：三方可以通过实现 `IDatabase` 或相关执行器接口，自由接入任何外部数据库或高级系统，不受厂商限制。

---

## 九、结语

Sonic.Database 0.2.0 以 Hypersonic 的门面哲学为指引，为 .NET
生态带来了一个干净、高效、无厂商锁定的嵌入式数据库抽象与实现集。它既能让开发者在五分钟内拥有本地持久化能力，又能作为构建分布式、多模态数据库系统的坚固基座。我们期待社区在此基础上创造无限可能。