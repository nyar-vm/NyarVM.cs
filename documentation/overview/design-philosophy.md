# 设计哲学

## 核心原则：Persistent/Ephemeral 二分

Sonic 的一切设计决策，都源于一条不可动摇的原则：

> **状态分为 Persistent（持久化）和 Ephemeral（临时状态）两类。这是用法，不是技术实现。**

这条原则的含义：

1. **二分是概念层面的，与技术无关。** 同一个 Redis，中心化用就是 Persistent，sidecar 加载就是 Ephemeral。同一个 SQLite，NFS 共享就是 Persistent，容器内本地文件就是 Ephemeral。
2. **用法决定语义，语义决定架构。** Persistent 的语义是"数据是真相来源"，因此必须中心化、跨实例唯一。Ephemeral 的语义是"数据是加速副本"，因此可以随实例生灭。
3. **语义不可混淆。** 如果把 Ephemeral 当 Persistent 用（跨实例共享缓存），或者把 Persistent 当 Ephemeral 用（本地文件存关键数据），系统的状态一致性就建立在沙子上。

## 五条派生规则

从 Persistent/Ephemeral 二分原则，可以推导出五条具体的架构规则：

### 规则 1：Cache 不跨实例

```
❌ 反模式：
容器 A ──→ Redis ←── 容器 B   ← 隐式耦合！

✅ 正确做法：
容器 A → Cache A → Storage ← Cache B ← 容器 B
```

Cache 是 Ephemeral，语义是"加速副本"。如果两个实例共享同一个 Cache，就等于把 Ephemeral 当 Persistent 用——实例 A 写入的缓存，实例 B 依赖它，这就形成了隐式耦合。一旦 Cache 清空（Ephemeral 的正常行为），B 就会读到过时数据或空数据。

**正确做法：** Cache 是实例私有的，跨实例的数据共享必须走 Persistent 层（Storage 或 Database）。

### 规则 2：Persistent 是唯一的真相来源

```
❌ 反模式：
写入 Cache → 异步同步到 Database   ← Cache 是真相来源？

✅ 正确做法：
写入 Database → Cache 失效/更新   ← Database 是真相来源
```

Persistent 层（Database、Storage）是系统的 Source of Truth。所有写入必须先落 Persistent，Cache 只是读取时的加速。Cache 的数据可以随时丢弃，因为可以从 Persistent 重建。

### 规则 3：Ephemeral 的丢失不影响正确性

```
✅ 设计检验：
如果所有 Ephemeral 同时丢失，系统是否仍然正确？
- 如果是 → 架构正确
- 如果否 → 你把 Ephemeral 当 Persistent 用了
```

这是一个简单但强大的检验标准。如果 Redis sidecar 崩溃导致业务出错，说明你的业务逻辑错误地依赖了 Ephemeral 的持久性。

### 规则 4：Persistent 内部按访问模式分，不按技术分

```
❌ 按技术分：
Database（MySQL） / Storage（S3） / Queue（Kafka）

✅ 按访问模式分：
结构化（Table）/ 文件（File）/ 瞬态（Queue）
```

Database、Storage、Stream 不是三个独立的服务，而是 Persistent 的三种访问模式：
- **结构化（Table）：** 需要查询、关联、事务 → `IDatabaseService`
- **文件（File）：** 需要上传、下载、CDN 分发 → `IStorageProvider`
- **瞬态（Queue）：** 需要发布、订阅、消费即消失 → `IStreamService`

它们共享 Persistent 的核心语义（中心化、跨实例唯一、真相来源），只是访问模式不同。

### 规则 5：Ephemeral 内部按落盘与否分

```
内存（Memory）  → ICacheService   ← 纯内存，重启即丢失
本地盘（Local） → IStateManager   ← 容器内落盘，重启保留，销毁丢失
```

两者都是 Ephemeral，都随实例生灭。区别只是实现策略：
- **内存：** 最快，但进程重启即丢失
- **本地盘：** 稍慢，但容器重启后还在（容器销毁才丢失）

选择哪种取决于性能需求和重启容忍度，但**不改变 Ephemeral 的语义**。

## 反模式清单

| 反模式 | 违反规则 | 后果 |
|--------|---------|------|
| 跨实例共享 Redis Cache | 规则 1 | 隐式耦合，缓存失效时数据不一致 |
| 先写 Cache 再同步 Database | 规则 2 | Cache 崩溃时数据丢失 |
| 业务逻辑依赖 Cache 中的数据 | 规则 3 | Cache 丢失时业务出错 |
| 用本地文件存关键业务数据 | 规则 2 | 容器销毁时数据丢失 |
| 把 Redis 当消息队列持久化用 | 规则 4 | 语义错位，可靠性不足 |

## 下一步

- [状态管理架构](../systems/state-management.md) — Layer 1 五种形态的完整详解
- [中间件与上层建筑](../systems/middleware.md) — 底层之上的洋葱模型、微服务等
