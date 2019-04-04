在聚焦单机性能、使用 C# 打造的背景下， **Athena** 的架构可以设计得高度模块化，且能充分利用 .NET 的
Span、硬件加速（Vector512/AVX2）、与托管内存管理等现代特性。以下是推荐的 **基本架构**，以核心模块和关键类为主线。

---

## 🏛️ 整体分层架构

```
┌─────────────────────────────────────────────┐
│   Athena Query Layer (SQL/Extension)        │
│   Parser → Binder → Optimizer → Executor    │
├─────────────────────────────────────────────┤
│   Pluggable Index Engine                    │
│   Vector ANN │ Graph CSR │ Inverted Index   │
├─────────────────────────────────────────────┤
│   Unified Micro-Partition Columnar Storage  │
│   (PAX Layout, Encoding, Compression)       │
├─────────────────────────────────────────────┤
│   Dual-Mode Cache & Buffer Manager          │
│   (DRAM → PMem → NVMe tiering, UDF-driven)  │
├─────────────────────────────────────────────┤
│   I/O & File Abstraction                     │
│   (Async I/O, Direct I/O, Memory-Mapped)    │
└─────────────────────────────────────────────┘
```

---

## 🧩 核心命名空间与关键类

### 1. `Athena` – 基础类型与公共抽象

- `DataValue` – 统一的值类型（支持 long, double, string, byte[], vector<float>, Guid 等）
- `Schema`, `ColumnDescriptor`, `TableDefinition`
- `UUIDv7` – 原生时间有序 UUID 生成器（基于 `Guid` 封装或自定义 128-bit 结构）

```csharp
public readonly struct UUIDv7 : IComparable<UUIDv7>
{
    private readonly long _timestamp;    // 48-bit unix毫秒
    private readonly short _sequence;    // 12-bit 序列
    private readonly ulong _random;      // 随机性保证全局唯一
    // 提供从 Guid / 字节数组构造、比较、以及 Unix 时间戳提取等
}
```

---

### 2. `Athena.Storage` – 统一列式微分区存储引擎

- `MicroPartition` – 逻辑存储单元，内部使用 PAX 布局
  - 包含多个 `ColumnChunk`（列块）
  - 记录每个列块的元数据：Zone Map（min/max/null count），偏移量，编码方式
- `ColumnChunk<T>` – 泛型列数据块，内部为连续内存块 `byte[]` / `Memory<byte>`
  - 支持自适应编码：`Delta`, `VarInt`, `Dictionary`, `Plain`
- `MicroPartitionReader` / `MicroPartitionWriter` – 负责列式数据的扫描与批量写

```csharp
public class MicroPartition
{
    public Guid PartitionId;            // UUIDv7，时间有序
    public PartitionMetadata Metadata;  // ZoneMaps per column
    internal Dictionary<int, ColumnChunk> Columns; // key=列索引
}

public class ColumnChunk<T> where T : unmanaged
{
    private Memory<byte> _data;
    private EncodingType _encoding;     // Delta, VarInt, Dictionary...
    public ReadOnlySpan<T> GetValues();
    public void Append(ReadOnlySpan<T> values);
}
```

---

### 3. `Athena.Index` – 可插拔的多模索引

- `IVectorIndex` – 向量近似最近邻搜索接口
  - `HnswIndex` – 基于 HNSW 的内存索引，结合双模缓存可换入换出部分层
  - `DiskAnnIndex` – 固态硬盘友好的图索引变种
- `IGraphIndex` – 图遍历索引
  - `CsrNeighborIndex` – 基于压缩稀疏行（CSR）的邻居索引，支持快速入边/出边遍历
- `IInvertedIndex` – 文档倒排索引
  - 使用 `RoaringBitmap` 做倒排列表交集/并集
- `ZoneMapIndex` – 列式分区的 min/max 剪枝统计，自动由 `MicroPartition` 维护

```csharp
public interface IVectorIndex
{
    void Build(IReadOnlyList<float[]> vectors, IReadOnlyList<long> ids);
    (long[] ids, float[] distances) Search(float[] query, int k);
}

public class CsrNeighborIndex : IGraphIndex
{
    // 邻接数组 + 偏移数组，支持快速邻居迭代
    public IReadOnlyList<long> GetNeighbors(long vertexId, string edgeType);
}
```

---

### 4. `Athena.Query` – SQL/扩展查询编译器与执行器

- `Lexer`, `Parser` – 解析 Athena SQL 超集（支持 `MATCH`, `ORDER BY distance`, `json_contains` 等）
- `Binder` – 将 AST 绑定到表、列、函数，解析类型
- `Optimizer` – 基于规则的优化（谓词下推、列裁剪、分区裁剪、索引选择）
- `ExecutionEngine` – 向量化执行引擎
  - `VectorizedOperator` 基类：`Filter`, `Project`, `Aggregate`, `HashJoin`, `TopK`
  - 针对 `Span<T>` 和 SIMD 指令（`Vector256<float>` 等）优化的批量处理
- `QueryCoordinator` – 单机查询入口，生成执行计划并驱动执行

```csharp
public abstract class VectorizedOperator
{
    public abstract void Execute();
    protected void FilterBatch<T>(Span<T> input, Span<T> output, Predicate<T> pred);
    protected void AggregateBatch<T>(Span<T> input, Span<long> groupIds, Span<T> result);
}
```

---

### 5. `Athena.Cache` – 双模缓存与智能驱逐

- `CacheManager` – 多级缓存管理器
  - 使用 `MemoryCache` + 自定义 `DiskCache` 实现分层
- `IEvictionPolicy` – 可插拔驱逐策略接口
  - 预置 `LRU`, `LFU`, `TtlPolicy`
  - **重点**：`UdfDrivenPolicy` – 允许用户注册 UDF 来决定缓存对象的权重/存活时间
- `CacheEntry` – 缓存单元，可以是微分区、向量索引节点、图邻居热点等
- 利用 `UUIDv7` 时间戳，实现基于时间序的预热和因果一致性提示

```csharp
public interface IEvictionPolicy
{
    CacheEntry SelectVictim(IReadOnlyList<CacheEntry> entries);
}

public class UdfDrivenPolicy : IEvictionPolicy
{
    private readonly Func<CacheEntry, double> _weightFunction; // 用户自定义
}
```

---

### 6. `Athena.Udf` – 用户自定义函数运行时

- `UdfRegistry` – 注册、管理 UDF
- `UdfInvoker` – 安全沙箱内执行 UDF（可基于 `System.Reflection` 或表达式树编译）
- 支持标量 UDF、聚合 UDF、以及特殊的“缓存策略 UDF”

```csharp
public interface IScalarUdf
{
    DataValue Execute(ReadOnlySpan<DataValue> arguments);
}
```

---

### 7. `Athena.Serialization` – 零拷贝序列化与编码

- `BinaryEncoder` / `BinaryDecoder` – 直接读写 `Span<byte>` 的二进制协议
- 用于微分区落盘、缓存交换、以及未来可能的网络传输
- 所有编码/解码均为零分配、一次拷贝

---

### 8. `Athena.Server` (可选) – 嵌入或独立进程

- `AthenaEngine` – 内嵌使用的顶级门面，类似 `SQLiteConnection`
- 可选的 gRPC/HTTP 接口，用于远程管理或调试
- 提供 `await engine.ExecuteAsync("SELECT ...")` 等 API

---

## 🔗 关键交互流程示例：一条混合查询

假设收到查询：

```sql
SELECT friend.name
FROM users
WHERE user.embedding <-> @queryVec < 0.8
MATCH (user)-[:KNOWS]->(friend)
WHERE friend.city = 'Beijing'
```

1. **Parser** 生成联合 AST。
2. **Binder** 绑定 `users` 表、`embedding` 列、图边 `KNOWS`。
3. **Optimizer** 决定：先用向量 ANN 索引过滤出 user 候选集（索引选择），再以 CSR 邻居索引遍历 friend，最后用列存 Zone Map 跳过
   `city != 'Beijing'` 的微分区。
4. **Executor** 向量化执行：
  - `VectorIndex.Search` 得到 TopK user id。
  - `GraphIndex.GetNeighbors` 批量获取这些 user 的 friend ids。
  - `MicroPartitionReader` 加载 `friend.name` 和 `friend.city` 列块，利用 `VectorizedOperator.FilterBatch` 做 SIMD 字符串比较。
5. **Cache** 在过程中自动缓存热的向量高层节点、图邻居关系及对应的微分区数据。

---

## 🧪 技术栈与依赖

- **.NET 8/9**：充分利用 `System.Numerics.Vector512`、硬件加速、`ref struct` Span 等。
- **内存管理**：尽量使用 `ArrayPool<byte>` 和 `MemoryOwner<byte>`，减少 GC 压力。
- **I/O**：异步文件 I/O (`System.IO.FileStream`)，可选 `io_uring`（通过 `Syscall` 或第三方库）。
- **压缩/编码**：自实现轻量级方案，或集成 `Zstd` 非托管库作通用块压缩。

---

这个架构体现了 **“一座神庙建到极致”** 的哲学：所有模块服务于极致的单机多模性能，没有分布式带来的网络与共识开销，只靠智能的列式存储、向量化执行、和
UDF 驱动的缓存，让 Athena 在 AI 时代的嵌入式数据库领域独树一帜。
