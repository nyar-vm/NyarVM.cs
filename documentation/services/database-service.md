# 数据库服务

> **归属：Persistent / 结构化（Table）**
> **接口：** `IDatabaseService`
> **语义：** 中心化、跨实例唯一、数据是真相来源
> **实现状态：🟡 部分实现** — `InMemoryDatabase` 已可用，SQL 数据库后端（Sqlite/Postgres/MySQL）尚未实现

## 概述

数据库服务是 Persistent 层的结构化访问模式。适用于需要查询、关联、事务的数据——用户、订单、商品等业务核心实体。

## 接口定义

```csharp
public interface IDatabaseService
{
    Task<T?> FindByIdAsync<T>(object id, CancellationToken ct = default) where T : class, new();
    Task<List<T>> QueryAsync<T>(CancellationToken ct = default) where T : class, new();
    Task<List<T>> QueryAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default) where T : class, new();
    Task<T> AddAsync<T>(T entity, CancellationToken ct = default) where T : class;
    Task<T> UpdateAsync<T>(T entity, CancellationToken ct = default) where T : class;
    Task<bool> DeleteAsync<T>(object id, CancellationToken ct = default) where T : class;
    Task<long> CountAsync<T>(CancellationToken ct = default) where T : class, new();
    Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
```

## 使用示例

```csharp
var db = Sonic.Use<IDatabaseService>();

// 查询
var user = await db.FindByIdAsync<User>(userId);
var activeUsers = await db.QueryAsync<User>(u => u.IsActive);

// 写入
var newUser = await db.AddAsync(new User { Name = "Alice" });

// 事务
await using var tx = await db.BeginTransactionAsync();
try
{
    await db.AddAsync(order, ct);
    await db.UpdateAsync(inventory, ct);
    await tx.CommitAsync(ct);
}
catch
{
    await tx.RollbackAsync(ct);
}
```

## 实现选择

| 实现 | 状态 | 适用场景 | 部署方式 |
|------|------|---------|---------|
| `InMemoryDatabase` | 🟢 已实现 | 开发/测试、单实例 | 进程内 `ConcurrentDictionary` |
| `SqliteDatabase` | 🔴 未实现 | 开发/测试、单实例 | 文件 |
| `PostgresDatabase` | 🔴 未实现 | 生产环境、复杂查询 | 中心化服务 |
| `MySqlDatabase` | 🔴 未实现 | 生产环境、MySQL 生态 | 中心化服务 |

## KV 存储：ISonicDatabase

Sonic 还提供了更轻量的 KV 存储接口，适用于不需要复杂查询的场景：

```csharp
public interface ISonicDatabase : IDisposable
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task PutAsync<T>(string key, T value, CancellationToken ct = default) where T : class;
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SonicEntry>> GetPrefixAsync(string prefix, CancellationToken ct = default);
}
```

`ISonicDatabase` 同样是 Persistent——中心化、跨实例唯一。

## 反模式

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| 用 Database 存二进制文件 | 二进制文件用 `IStorageProvider` |
| 用 Database 做消息队列 | 事件流用 `IStreamService` |
| 先写 Cache 再同步到 Database | 先写 Database 再更新 Cache |

## 相关文档

- [状态管理架构](../systems/state-management.md) — Persistent 三形态全景
- [缓存服务](./cache-service.md) — Database 读取的 Ephemeral 加速
