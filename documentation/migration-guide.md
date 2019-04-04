# 迁移指南：从标准 .NET 架构到 Sonic

> **目标：** 帮助熟悉 ASP.NET Core DI + EF Core + Redis 的开发者快速理解和迁移到 Sonic 架构。

## 核心思维转变

| 标准 .NET 思维 | Sonic 思维 |
|------|------|
| 选择一个数据库，学它的 ORM | 先分 Persistent / Ephemeral，再选后端 |
| Redis 是共享缓存 | Redis 是 sidecar，仅本实例可见 |
| DI 容器在运行时做"组装" | Sonic 提供零配置内存默认，也支持标准 DI |
| 文件存储自己找 S3 SDK | `IStorageProvider` 统一接口，四云厂互换 |

## 一、DI 容器对比

### 标准 ASP.NET Core

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddStackExchangeRedisCache(...);
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();
```

### Sonic：最简版（零配置）

```csharp
Sonic.CreateScope();
Sonic.Register<ISonicDatabase>(new InMemoryDatabase());
Sonic.Register<ICacheService>(new MemoryCacheService());
Sonic.Register<IStorageProvider>(new LocalStorageProvider(new StorageOptions()));

// 使用
Sonic.Use<ISonicDatabase>(out var db);
```

### Sonic：标准 DI 版（推荐新项目）

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSonicDefaults();          // 全内存默认

// 或从 appsettings.json 读取：
builder.Services.AddSonic(builder.Configuration);

// 构造函数注入
public class UserService(ISonic sonic) { ... }

var app = builder.Build();
```

## 二、数据库操作

### EF Core → IDatabaseService + IQueryBuilder

| 操作 | EF Core | Sonic |
|------|------|------|
| 按 ID 查找 | `await _ctx.Users.FindAsync(id)` | `await db.FindByIdAsync<User>(id)` |
| 条件查询 | `await _ctx.Users.Where(u => u.Active).ToListAsync()` | `await sonic.Query<User>().Filter(u => u.Active).ToListAsync()` |
| 计数 | `await _ctx.Users.CountAsync()` | `await sonic.Query<User>().CountAsync()` |
| 检查存在 | `await _ctx.Users.AnyAsync()` | `await sonic.Query<User>().AnyAsync()` |
| 首条 | `await _ctx.Users.FirstOrDefaultAsync()` | `await sonic.Query<User>().FirstOrDefaultAsync()` |
| 插入 | `_ctx.Users.Add(entity); await _ctx.SaveChangesAsync()` | `await db.AddAsync(entity)` |
| 更新 | `_ctx.Entry(entity).State = ...` | `await db.UpdateAsync(entity)` |
| 分页 | `.Skip(n).Take(m).ToListAsync()` | `.Skip(n).Take(m).ToListAsync()`（完全相同） |
| 排序 | `.OrderBy(u => u.Name)` | `.Sort(u => u.Name, descending: false)` |

### 迁移示例

```csharp
// 迁移前：EF Core
public class UserRepository(AppDbContext ctx)
{
    public async Task<User?> GetById(int id)
        => await ctx.Users.FindAsync(id);

    public async Task<List<User>> GetActiveUsers(int page, int size)
        => await ctx.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.CreatedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();
}

// 迁移后：Sonic
public class UserRepository(ISonic sonic)
{
    public async Task<User?> GetById(int id)
        => await sonic.Load<User>($"user:{id}");

    public async Task<List<User>> GetActiveUsers(int page, int size)
        => await sonic.Query<User>()
            .Filter(u => u.IsActive)
            .Sort(u => u.CreatedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();
}
```

## 三、缓存操作

### Redis → ICacheService

| 操作 | StackExchange.Redis | Sonic |
|------|------|------|
| 获取 | `await _cache.GetStringAsync(key)` | `await cache.GetAsync<T>(key)` |
| 设置 | `await _cache.SetStringAsync(key, value, options)` | `await sonic.Put(key, value, ttl: TimeSpan.FromMinutes(30))` |
| 删除 | `await _cache.KeyDeleteAsync(key)` | `await sonic.Delete(key)` |
| 是否存在 | `await _cache.KeyExistsAsync(key)` | `await sonic.Exists(key)` |

### 关键差异

```
迁移前：
所有微服务 → 同一个 Redis 集群  ← 隐式共享，耦合

迁移后：
每个容器有自己的 Cache (Ephemeral)
跨实例数据共享 → Persistent 层（Database / Storage）
```

```csharp
// 迁移前：Redis 反模式——用 Redis 做"真相来源"
var order = await redis.GetStringAsync(orderKey);
if (order == null)
{
    // 从 DB 重建（Redis 宕机就丢数据！）
    order = await db.QueryAsync<Order>(orderId);
    await redis.SetStringAsync(orderKey, order); // 即使丢也没关系，因为是缓存
}

// 迁移后：Sonic 正确处理 Cache miss → DB 回填
var order = await sonic.Get<Order>($"order:{orderId}");
// Sonic 自动：Cache hit → 返回 / Cache miss → DB → 回填 Cache → 返回
```

## 四、文件存储

### S3 SDK / Azure Blob SDK → IStorageProvider

```csharp
// 迁移前：直接使用 S3 SDK
using var client = new AmazonS3Client(accessKey, secretKey, region);
var request = new PutObjectRequest
{
    BucketName = "my-bucket",
    Key = "avatars/user-123.jpg",
    InputStream = stream
};
await client.PutObjectAsync(request);

// 迁移后：Sonic 统一接口
await sonic.Save("avatars/user-123.jpg", stream);  // 自动判断为二进制 → 走 Storage

// 获取访问 URL
var url = sonic.Url("avatars/user-123.jpg");
```

**云厂切换零改动：**

```csharp
// appsettings.json 中改一行，代码零变动
// 本地：{ "Sonic": { "Storage": { "Type": "local" } } }
// S3：  { "Sonic": { "Storage": { "Type": "s3" } } }
// 阿里：{ "Sonic": { "Storage": { "Type": "aliyunoss" } } }
// 腾讯：{ "Sonic": { "Storage": { "Type": "tencentcos" } } }
```

## 五、事件流

### Kafka / RabbitMQ → IStreamService

```csharp
// 迁移前：直接使用 Kafka producer
var message = new Message<string, string> { Key = orderId, Value = json };
await producer.ProduceAsync("order.created", message);

// 迁移后
await sonic.Emit("order.created", new { OrderId = orderId });

// 订阅端
await foreach (var evt in sonic.On<OrderEvent>("order.created"))
{
    await Handle(evt);
}
```

## 六、迁移路径建议

### 阶段 1：低风险试水（1 个模块）

```
1. 选一个独立模块（如文件上传）迁移到 IStorageProvider
2. 保持现有数据库不变，仅在 Sonic 中新增功能使用 IDatabaseService
3. 观察 1-2 周，确认 Sonic 的 Provider 切换和 Cache miss 回填如预期工作
```

### 阶段 2：逐步替换（3-6 个模块）

```
4. 新 Feat 只在 Sonic 中开发
5. 将读多写少的查询迁移到 Sonic 的 Query + Cache put/get 模式
6. 保留 EF Core 的复杂 Join 查询，用 Sonic 替代简单 CRUD
```

### 阶段 3：全面迁移

```
7. 用 IStreamService 替换 Kafka/RabbitMQ 的直接依赖
8. 用 NetOptions + DI 扩展统一配置管理
9. 最后迁移复杂查询（等 Hermes Schema 引擎就绪）
```

## 七、常见阻力及应对

| 阻力 | 应对 |
|------|------|
| "我们已经有了 EF Core" | Sonic 不是替换 EF Core，是**降低耦合**。可以共存——EF Core 负责复杂查询，Sonic 负责统一接口和 Provider 切换 |
| "Redis cluster 我们用了多年" | Sonic 的 Cache 语义不禁止 Redis——只要求 Redis 不跨实例共享。sidecar Redis 完全兼容 |
| "文件存储直接调 S3 SDK 更简洁" | 当你需要在 S3 / Aliyun / Tencent 之间切换时，一个接口比每次换 SDK 强得多 |
| "Sonic 太年轻" | Sonic 的接口薄、实现量少。本质是**契约设计**——定义正确的分层（Persistent / Ephemeral），底层随便换 |

## 相关文档

- [状态管理架构](../systems/state-management.md) — Persistent / Ephemeral 全景
- [服务参考](./services/index.md) — 所有服务的实现状态
- [故障排查](./troubleshooting.md) — 遇到问题看这里
