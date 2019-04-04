# 状态管理架构

> Layer 1：底层，不可动摇。基础不牢，地动山摇。

## 全景图

```
┌─────────────────────────────────────────────────────────────────┐
│  Layer 1: 状态管理                                               │
│                                                                 │
│  ┌───────────────────────────────┐  ┌─────────────────────────┐ │
│  │  Persistent（持久化）          │  │  Ephemeral（临时状态）   │ │
│  │                               │  │                         │ │
│  │  中心化，跨实例唯一            │  │  sidecar，随实例生灭     │ │
│  │  数据是真相来源               │  │  数据是加速副本          │ │
│  │                               │  │                         │ │
│  │  ┌─────────────────────────┐  │  │  ┌───────────────────┐  │ │
│  │  │ 结构化（Table）          │  │  │  │ 内存（Memory）     │  │ │
│  │  │                         │  │  │  │                   │  │ │
│  │  │ IDatabaseService        │  │  │  │ ICacheService     │  │ │
│  │  │ ├─ SqliteDatabase       │  │  │  │ ├─ MemoryCache    │  │ │
│  │  │ ├─ PostgresDatabase     │  │  │  │ └─ Redis sidecar  │  │ │
│  │  │ └─ MySqlDatabase        │  │  │  │                   │  │ │
│  │  └─────────────────────────┘  │  │  └───────────────────┘  │ │
│  │  ┌─────────────────────────┐  │  │  ┌───────────────────┐  │ │
│  │  │ 文件（File）             │  │  │  │ 本地盘（Local）    │  │ │
│  │  │                         │  │  │  │                   │  │ │
│  │  │ IStorageProvider        │  │  │  │ IStateManager     │  │ │
│  │  │ ├─ LocalStorageProvider │  │  │  │ └─ SQLite 本地    │  │ │
│  │  │ ├─ S3StorageProvider    │  │  │  │                   │  │ │
│  │  │ ├─ OssStorageProvider   │  │  │  └───────────────────┘  │ │
│  │  │ └─ CosStorageProvider   │  │  │                         │ │
│  │  └─────────────────────────┘  │  │                         │ │
│  │  ┌─────────────────────────┐  │  │                         │ │
│  │  │ 瞬态（Queue）            │  │  │                         │ │
│  │  │                         │  │  │                         │ │
│  │  │ IStreamService          │  │  │                         │ │
│  │  │ ├─ MemoryStreamService  │  │  │                         │ │
│  │  │ └─ KafkaStreamService   │  │  │                         │ │
│  │  └─────────────────────────┘  │  │                         │ │
│  └───────────────────────────────┘  └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Persistent 三形态

Persistent 的核心语义：**中心化、跨实例唯一、数据是真相来源。**

### 结构化（Table）— IDatabaseService

**何时使用：** 需要查询、关联、事务的结构化数据。

```csharp
var db = Sonic.Use<IDatabaseService>();

// CRUD
var user = await db.FindByIdAsync<User>(userId);
var users = await db.QueryAsync<User>(u => u.IsActive);
var newUser = await db.AddAsync(new User { Name = "Alice" });
await db.UpdateAsync(user);
await db.DeleteAsync<User>(userId);

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

**实现选择：**

| 实现 | 适用场景 | 部署方式 |
|------|---------|---------|
| `SqliteDatabase` | 开发/测试、单实例部署 | 文件 |
| `PostgresDatabase` | 生产环境、复杂查询 | 中心化服务 |
| `MySqlDatabase` | 生产环境、MySQL 生态 | 中心化服务 |

**关键约束：** 无论哪种实现，Database 都是 Persistent——中心化部署，跨实例共享。

### 文件（File）— IStorageProvider

**何时使用：** 需要上传、下载、CDN 分发的二进制文件。

```csharp
var storage = Sonic.Use<IStorageProvider>();

// 上传
var info = await storage.UploadAsync("avatars/2024/01/abc123.png", imageData, "image/png");

// 获取 URL
var url = storage.GetUrl("avatars/2024/01/abc123.png");

// 删除
await storage.DeleteAsync("avatars/2024/01/abc123.png");

// 检查存在
var exists = await storage.ExistsAsync("avatars/2024/01/abc123.png");
```

**实现选择：**

| 实现 | 适用场景 | 部署方式 |
|------|---------|---------|
| `LocalStorageProvider` | 开发/测试、单机部署 | 本地文件系统 |
| `S3StorageProvider` | AWS 生产环境 | 中心化服务 |
| `OssStorageProvider` | 阿里云生产环境 | 中心化服务 |
| `CosStorageProvider` | 腾讯云生产环境 | 中心化服务 |

**关键约束：** 无论哪种实现，Storage 都是 Persistent——文件一旦上传，跨实例可访问。

### 瞬态（Queue）— IStreamService

**何时使用：** 需要发布-订阅、消费即消失的事件流。

```csharp
var stream = Sonic.Use<IStreamService>();

// 发布
await stream.PublishAsync("order.created", new OrderEvent { OrderId = 123 });

// 订阅
await foreach (var evt in stream.SubscribeAsync<OrderEvent>("order.created"))
{
    await ProcessOrder(evt);
}
```

**为什么 Queue 是 Persistent？** 消息一旦发布，即使没有消费者在线，消息也不会丢失。这是 Persistent 的核心语义——写入即承诺。消息被消费后消失，这是"瞬态"的含义——但消失是业务语义（已处理），不是技术语义（缓存淘汰）。

**实现选择：**

| 实现 | 适用场景 | 部署方式 |
|------|---------|---------|
| `MemoryStreamService` | 开发/测试 | 进程内 |
| `KafkaStreamService` | 生产环境 | 中心化集群 |

## Ephemeral 两形态

Ephemeral 的核心语义：**sidecar 加载、随实例生灭、数据是加速副本。**

### 内存（Memory）— ICacheService

**何时使用：** 需要极速读取、可丢失的加速缓存。

```csharp
var cache = Sonic.Use<ICacheService>();

// 读取（可能为 null）
var user = await cache.GetAsync<User>("user:123");

// 写入（带过期时间）
await cache.SetAsync("user:123", user, TimeSpan.FromMinutes(30));

// 删除
await cache.DeleteAsync("user:123");

// 检查存在
var exists = await cache.ExistsAsync("user:123");
```

**实现选择：**

| 实现 | 适用场景 | 部署方式 |
|------|---------|---------|
| `MemoryCacheService` | 开发/测试、简单场景 | 进程内字典 |
| `Redis sidecar` | 生产环境 | 容器内 sidecar |

**关键约束：** Redis 必须以 sidecar 模式部署，仅本实例可见。跨实例共享 Redis 是反模式。

### 本地盘（Local）— IStateManager

**何时使用：** 需要容器重启后保留、但容器销毁后可丢失的本地状态。

```csharp
var state = Sonic.Use<IStateManager>();

// 读写本地状态
await state.SetAsync("session:abc", sessionData);
var session = await state.GetAsync<Session>("session:abc");
```

**为什么需要本地盘？** 有些状态需要跨进程重启保留（如长时间运行的计算中间态），但不需要跨实例共享。本地 SQLite 提供了这种中间态——比内存持久，比 Persistent 轻量。

**关键约束：** 本地盘的数据不可被其他实例依赖。如果其他实例需要这些数据，必须写入 Persistent 层。

## 数据流向

```
写入路径：
请求 → 业务逻辑 → Persistent（Database/Storage/Stream）
                  ↓
              Ephemeral（Cache/State）可选更新

读取路径：
请求 → 业务逻辑 → Ephemeral（Cache）→ 命中？→ 返回
                                    → 未命中 → Persistent → 写入 Cache → 返回
```

**写入必须先落 Persistent，读取可以走 Ephemeral 加速。** 这是 Persistent/Ephemeral 二分原则的直接推论。

## 与 Layer 2 的关系

状态管理是 Layer 1，服务控制反转是 Layer 2。Layer 2 不改变状态管理的语义，只改变服务的注册和发现方式：

```csharp
// Layer 2：Sonic.Use<T>() 自动解析 Schema 中的服务配置
var db = Sonic.Use<IDatabaseService>();    // 自动选择 Sqlite/Postgres/MySQL
var cache = Sonic.Use<ICacheService>();    // 自动选择 Memory/Redis
var storage = Sonic.Use<IStorageProvider>(); // 自动选择 Local/S3/OSS/COS
```

Layer 2 让你不需要关心技术实现，但**你必须关心语义**——你用的是 Persistent 还是 Ephemeral，决定了你能用这些数据做什么。

## 下一步

- [服务控制反转](./service-inversion.md) — Layer 2 详解
- [中间件与上层建筑](./middleware.md) — Layer 3 详解
