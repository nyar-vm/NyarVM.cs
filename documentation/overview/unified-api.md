# 统一 API

Sonic 的核心理念：**用户只说"放"和"取"，Sonic 自己知道往哪放、从哪取。**

Db / Cache / Storage / Stream 是 Sonic 的内脏，不是 Sonic 的脸面。用户不应该被迫做基础设施的选择——那是框架的职责。

## 设计原则

### 两个轴

Sonic 的全部操作可以由两个轴完全描述：

| 轴 | 维度 | 含义 |
|:---|:---|:---|
| **寿命** | 临时 / 持久 | 数据活多久 |
| **方向** | 来 / 去 | 数据往哪流 |

交叉结果：

| | 持久 | 临时 |
|:---|:---|:---|
| **来（取）** | `Load` | `Get`（自动降级） |
| **去（放）** | `Save` | `Put` |

加上查询、删除、发布、订阅，共 **8 个操作**覆盖全部场景。

### 路由策略

用户不选后端，Sonic 根据数据特征自动路由：

| 信号 | 路由目标 | 示例 |
|:---|:---|:---|
| 调用 `Save` / `Load` | 持久层 | `Save("user:123", user)` → 数据库 |
| 调用 `Put` / `Get` | 临时层（缓存） | `Put("search:hello", results, ttl: 5min)` → 缓存 |
| 值类型为 `byte[]` / `Stream` | 对象存储 | `Save("img.png", bytes)` → S3/本地文件 |
| 值类型为其他 | 数据库 | `Save("user:123", user)` → KV 数据库 |
| 调用 `Get` 未命中缓存 | 自动降级到持久层 | `Get<User>("user:123")` → 缓存 → 数据库 → 回填缓存 |

## API 总览

```csharp
public interface ISonic : IDisposable
{
    // 持久化操作
    Task Save<T>(string address, T value, CancellationToken ct = default);
    Task<T?> Load<T>(string address, CancellationToken ct = default);
    IQueryBuilder<T> Query<T>() where T : class;

    // 临时操作
    Task Put<T>(string address, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task<T?> Get<T>(string address, CancellationToken ct = default);

    // 通用操作
    Task<bool> Delete(string address, CancellationToken ct = default);
    Task<bool> Exists(string address, CancellationToken ct = default);

    // 消息流
    Task Emit<T>(string topic, T payload, CancellationToken ct = default);
    IAsyncEnumerable<T> On<T>(string topic, CancellationToken ct = default);

    // 辅助
    string Url(string address);
}
```

## 操作详解

### Save — 持久化写入

数据写入持久层，重启不丢失。Sonic 根据值类型自动选择载体：

- **结构化对象**（非 `byte[]`/`Stream`）→ 写入数据库
- **二进制数据**（`byte[]` 或 `Stream`）→ 写入对象存储

```csharp
// 结构化 → 数据库
await sonic.Save($"user:{userId}", new User { Name = "Alice" });

// 二进制 → 对象存储
await sonic.Save($"artwork/{id}/preview.png", imageBytes);
```

### Load — 持久化读取

从持久层读取数据。路由规则与 `Save` 对称：

- **泛型参数为非二进制类型** → 从数据库读取
- **泛型参数为 `byte[]`** → 从对象存储读取

```csharp
// 从数据库
var user = await sonic.Load<User>($"user:{userId}");

// 从对象存储
var bytes = await sonic.Load<byte[]>($"artwork/{id}/preview.png");
```

### Put — 临时写入

数据写入临时层（缓存），带可选 TTL。过期或实例回收即丢失。

```csharp
// 带过期时间
await sonic.Put($"search:{keyword}", results, ttl: TimeSpan.FromMinutes(5));

// 不带过期（使用默认 TTL）
await sonic.Put($"session:{sessionId}", sessionData);
```

### Get — 临时读取（自动降级）

从临时层读取，**未命中时自动降级到持久层并回填缓存**。

```
Get<User>("user:123")
  → 缓存命中？直接返回
  → 缓存未命中 → Load("user:123") → 回填缓存 → 返回
```

这是经典的 Read-Through Cache 模式，用户无需手动编排。

```csharp
// 先查缓存，未命中自动查数据库并回填
var user = await sonic.Get<User>($"user:{userId}");
```

### Query — 结构化查询

对持久层进行条件查询，返回 `IQueryBuilder<T>` 流畅 API：

```csharp
var activeUsers = await sonic.Query<User>()
    .Filter(u => u.Active)
    .Sort(u => u.CreatedAt, descending: true)
    .Skip(0).Take(20)
    .ToListAsync();
```

`IQueryBuilder<T>` 支持：

| 方法 | 说明 |
|:---|:---|
| `Filter(expr)` | 条件过滤 |
| `Sort(expr, desc)` | 排序 |
| `Set(expr)` | 更新赋值（配合 `UpdateAsync`） |
| `Skip(n)` / `Take(n)` | 分页 |
| `ToListAsync()` | 查询多条 |
| `FirstOrDefaultAsync()` | 查询一条 |
| `CountAsync()` | 计数 |
| `AnyAsync()` | 是否存在 |
| `InsertAsync(entity)` | 插入 |
| `UpdateAsync()` | 更新（配合 `Set`） |
| `DeleteAsync()` | 删除 |

### Delete — 删除

同时删除持久层和临时层中的数据。

```csharp
await sonic.Delete($"user:{userId}");
```

### Exists — 存在性检查

检查持久层中是否存在指定地址的数据。

```csharp
var exists = await sonic.Exists($"user:{userId}");
```

### Emit — 发布消息

向指定主题发布消息。消息是方向性临时写入，不持久化，实例回收即丢失。

```csharp
await sonic.Emit("artwork.created", new { Id = artworkId });
```

### On — 订阅消息

订阅指定主题的消息。返回 `IAsyncEnumerable<T>`，支持 `await foreach`。

```csharp
await foreach (var e in sonic.On<ArtworkEvent>("artwork.created"))
{
    // 处理事件
}
```

### Url — 获取资源地址

获取持久层中资源的访问 URL。对于对象存储，返回预签名 URL 或公共 URL。

```csharp
var url = sonic.Url($"artwork/{id}/preview.png");
// → "https://cdn.example.com/artwork/abc/preview.png?X-Amz-Expires=3600..."
```

## 地址约定

Sonic 使用统一的地址格式标识数据，无论底层存储是什么：

```
类型:标识[/路径]
```

| 地址 | 路由目标 | 说明 |
|:---|:---|:---|
| `user:123` | 数据库 | 结构化对象，按类型前缀索引 |
| `user:123/avatar.png` | 对象存储 | 二进制数据，路径式寻址 |
| `search:hello` | 缓存 | 临时数据，按功能前缀分组 |
| `session:abc` | 缓存 | 会话数据，临时 |

路由规则：

1. **`Save` / `Load` + 非 `byte[]` 类型** → 数据库，地址作为 KV 的 key
2. **`Save` / `Load` + `byte[]` / `Stream` 类型** → 对象存储，地址作为对象 key
3. **`Put` / `Get`** → 缓存，地址作为缓存 key
4. **`Emit` / `On`** → 消息流，地址作为 topic

## 完整示例

```csharp
public class ArtworkService(ISonic sonic)
{
    public async Task<Artwork> CreateArtwork(string prompt, byte[] imageData)
    {
        var artwork = new Artwork
        {
            Id = Guid.NewGuid().ToString(),
            Prompt = prompt,
            CreatedAt = DateTime.UtcNow
        };

        // 持久化元数据 → 数据库
        await sonic.Save($"artwork:{artwork.Id}", artwork);

        // 持久化图片 → 对象存储
        await sonic.Save($"artwork/{artwork.Id}/preview.png", imageData);

        // 缓存热门作品 → 临时层
        await sonic.Put($"artwork:recent", artwork, ttl: TimeSpan.FromMinutes(10));

        // 通知其他服务 → 消息流
        await sonic.Emit("artwork.created", new { ArtworkId = artwork.Id });

        return artwork;
    }

    public async Task<Artwork?> GetArtwork(string id)
    {
        // 自动降级：缓存 → 数据库 → 回填缓存
        return await sonic.Get<Artwork>($"artwork:{id}");
    }

    public async Task<List<Artwork>> SearchArtworks(string keyword)
    {
        // 先查缓存
        var cached = await sonic.Get<List<Artwork>>($"search:{keyword}");
        if (cached != null) return cached;

        // 缓存未命中，查数据库
        var results = await sonic.Query<Artwork>()
            .Filter(a => a.Prompt.Contains(keyword))
            .Sort(a => a.CreatedAt, descending: true)
            .Take(20)
            .ToListAsync();

        // 回填缓存
        await sonic.Put($"search:{keyword}", results, ttl: TimeSpan.FromMinutes(5));

        return results;
    }

    public async Task DeleteArtwork(string id)
    {
        await sonic.Delete($"artwork:{id}");
        await sonic.Delete($"artwork/{id}/preview.png");
    }

    public string GetImageUrl(string id)
    {
        return sonic.Url($"artwork/{id}/preview.png");
    }
}
```

## 内部架构

用户只看到 `ISonic`，内部由四个能力层支撑：

```
ISonic（用户接口）
├── Save/Load/Query → ISonicDatabase（持久层 - 结构化）
│                    → IStorageProvider（持久层 - 文件）
├── Put/Get         → ICacheService（临时层）
├── Emit/On         → IStreamService（消息流）
└── Delete/Exists   → 联合操作（持久层 + 临时层）
```

`ISonicDatabase`、`IStorageProvider`、`ICacheService`、`IStreamService` 是内部实现，用户不应直接注入。所有操作通过 `ISonic` 统一入口。

## 与旧 API 的对照

| 旧 API | 新 API | 变化 |
|:---|:---|:---|
| `IDatabaseService.AddAsync(entity)` | `sonic.Save(addr, entity)` | 统一持久化写入 |
| `IDatabaseService.FindByIdAsync<T>(id)` | `sonic.Load<T>(addr)` | 统一持久化读取 |
| `IDatabaseService.QueryAsync<T>(pred)` | `sonic.Query<T>().Filter(...)` | 流畅 API |
| `ICacheService.SetAsync(key, val, ttl)` | `sonic.Put(addr, val, ttl)` | 统一临时写入 |
| `ICacheService.GetAsync<T>(key)` | `sonic.Get<T>(addr)` | 自动降级 |
| `IStorageProvider.UploadAsync(key, data)` | `sonic.Save(addr, data)` | 类型自动路由 |
| `IStorageProvider.DownloadBytesAsync(key)` | `sonic.Load<byte[]>(addr)` | 类型自动路由 |
| `IStreamService.PublishAsync(topic, val)` | `sonic.Emit(topic, val)` | 语义更清晰 |
| `IStreamService.SubscribeAsync<T>(topic)` | `sonic.On<T>(topic)` | 语义更清晰 |
| 手动 cache-aside 编排 | `sonic.Get<T>(addr)` | 自动降级，零编排 |
| 4 个接口注入 | 1 个 `ISonic` 注入 | 极简依赖 |
