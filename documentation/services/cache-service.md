# 缓存服务

> **归属：Ephemeral / 内存（Memory）**
> **接口：** `ICacheService`
> **语义：** sidecar、随实例生灭、数据是加速副本
> **实现状态：🟡 部分实现** — `MemoryCacheService`（进程内 `ConcurrentDictionary`）已可用，Redis sidecar 尚未实现

## 概述

缓存服务是 Ephemeral 层的内存访问模式。适用于极速读取、可丢失的加速缓存——查询结果、会话数据、配置缓存等。

## 核心约束

> **Cache 不跨实例。** 这是 Sonic 最重要的一条规则。

```
❌ 反模式：
容器 A ──→ Redis ←── 容器 B   ← 隐式耦合！

✅ 正确做法：
容器 A → Cache A → Storage ← Cache B ← 容器 B
```

Redis 在 Sonic 中必须以 sidecar 模式部署，仅本实例可见。跨实例的数据共享必须走 Persistent 层。

## 接口定义

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
```

## 使用示例

```csharp
var cache = Sonic.Use<ICacheService>();

// 读取（可能为 null——这是正常的！）
var user = await cache.GetAsync<User>("user:123");
if (user == null)
{
    // Cache miss → 从 Persistent 读取
    user = await db.FindByIdAsync<User>(123);
    // 回填 Cache
    await cache.SetAsync("user:123", user, TimeSpan.FromMinutes(30));
}

// 写入时更新 Cache
await db.UpdateAsync(user);
await cache.SetAsync("user:123", user, TimeSpan.FromMinutes(30));

// 主动失效
await cache.DeleteAsync("user:123");
```

## 实现选择

| 实现 | 状态 | 适用场景 | 部署方式 |
|------|------|---------|---------|
| `MemoryCacheService` | 🟢 已实现 | 开发/测试、简单场景 | 进程内 `ConcurrentDictionary` |
| `Redis sidecar` | 🔴 未实现 | 生产环境 | 容器内 sidecar |

## 设计检验

> 如果所有 Cache 同时丢失，系统是否仍然正确？
> - 如果是 → 架构正确 ✅
> - 如果否 → 你把 Ephemeral 当 Persistent 用了 ❌

## 反模式

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| 跨实例共享 Redis Cache | 跨实例数据走 `IDatabaseService` 或 `IStorageProvider` |
| 先写 Cache 再同步到 Database | 先写 Database 再更新 Cache |
| 业务逻辑依赖 Cache 中的数据 | Cache 只是加速，丢失不影响正确性 |
| 用 Cache 做消息传递 | 消息传递用 `IStreamService` |

## 相关文档

- [状态管理架构](../systems/state-management.md) — Ephemeral 两形态全景
- [数据库服务](./database-service.md) — Cache 加速的目标
- [状态服务](./state-service.md) — Ephemeral 的另一种实现策略
