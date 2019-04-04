# DataStorage — 数据持久化

## 定位

DataStorage 是 Sonic.Data 三维架构中负责"数据如何落地"的维度。它将内存中的对象映射到持久化介质——关系数据库、键值存储、文件系统、对象存储——并提供
CRUD、索引和迁移能力。

## 为什么存储要独立于序列化？

你可以把一个 `Person` 序列化为 JSON 字符串存入 SQLite 的 `TEXT` 列。这能工作，但有两个致命问题：

1. **无法查询**："找出年龄大于 30 的所有用户"需要对每一行进行反序列化 → 检查 → 过滤，而不是 `WHERE age > 30`。存储层不知道
   `age` 是独立字段，因为它看到的只是一个不透明的 JSON blob。

2. **Schema 演化需要手动管理**：加了 `PhoneNumber` 字段后，旧数据的 JSON blob 缺少该字段。如果不做迁移，反序列化时要么报错要么用默认值——两种都不理想。

DataStorage 的 `EntityMapper<T>` 将类型 T 的字段拆解为独立的存储列，让存储引擎理解"这是一个 `age` 列，类型是 `INT32`
，有索引"。源生成器从 `[Data]` 类型自动生成映射，用户只需加几个存储属性。

## 为什么不是 ORM？

DataStorage 比 ORM 弱，这是刻意的。

ORM（Entity Framework、Hibernate）解决的问题远超 DataStorage 的边界：

- 变更追踪（Change Tracker）→ 不属于数据抽象，属于应用层。
- 工作单元（Unit of Work）→ 事务管理，属于应用层。
- 导航属性懒加载 → 对象图管理，属于应用层和 ORM 的领域。
- LINQ 查询翻译 → 查询语言，属于独立领域。

DataStorage 只做一件事： **声明映射 → 生成 DDL → 提供 CRUD**。它假设调用方自己管理事务和变更追踪。这个简化意味着
DataStorage 可以支持 ORM 难以覆盖的场景：

- 键值存储（Redis）——没有 SQL，但同样需要序列化/反序列化。
- 文件系统——对象存为 JSON 文件，用文件路径当 key。
- 对象存储（S3）——序列化后的字节直接上传。

## 为什么 `[Version]` 属于 DataContract 但被 DataStorage 使用？

`[Version]` 的业务语义是"实例版本号"——每行数据的乐观锁版本。它属于 DataContract（数据的契约属性），但 **使用它的是
DataStorage**。

这体现了三维正交但协作的设计哲学：

- DataContract 声明"这个字段是版本号"。
- DataStorage 识别版本字段，在 UPDATE 时自动加入 `WHERE version = @oldVersion`，更新后 `SET version = @oldVersion + 1`。
- DataProcess 无感知——它只是序列化/反序列化一个整数，不知道这是版本号。

如果你把 `[Version]` 放在 DataStorage 中定义，DataContract 就无法独立使用——而版本号在某些场景下也是验证逻辑的一部分（"
版本号不能回退"）。

## 不应该承担什么职责？

- **不应管理事务**：`BEGIN/COMMIT/ROLLBACK` 由调用方控制。DataStorage 的每个操作是原子的，多个操作的事务边界由调用方决定。
- **不应做变更追踪**：DataStorage 不知道哪些字段被修改了。调用方负责构造完整对象，DataStorage 全量更新。
- **不应实现查询语言**：DataStorage 提供 `find_by_key`、简单的条件查询，但不编译 LINQ 表达式树。复杂查询走原生 SQL 或存储后端专用
  API。
- **不应替换 DataProcess**：存入数据库时如果需要把复杂字段序列化（如 `List<string>` 存为一个 JSON 列），DataStorage 委托
  DataProcess 的序列化器完成，不自建序列化。

## 与上下游的衔接

```
上游（业务层）→ 构造类型 T 的实例
    ↓
EntityMapper<T> → 将 T 映射为存储列的集合
RelationalStorage.insert(mapper, entity) → 生成 INSERT INTO ... VALUES (...)
    ↓（复杂字段委托 DataProcess 序列化）
下游（数据库 / Redis / 文件系统）→ 持久化存储
```

## 典型场景

- **用户表自动创建**：`[Data(storage: ...)]` 的 `User` 类 → 生成
  `CREATE TABLE users (id INT PRIMARY KEY, name TEXT, email TEXT UNIQUE)`。
- **乐观锁更新**：`[Version]` 字段自动纳入 UPDATE 的 WHERE 条件，并发冲突时返回零行受影响。
- **配置文件持久化**：`AppConfig` 用 `FileStorage` 存为 `.json` 文件，内部委托 DataProcess 的 JSON 序列化器。