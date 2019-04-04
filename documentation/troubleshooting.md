# 故障排查

> 常见错误的诊断、原因和解决方案。

## `InvalidOperationException`：没有活跃的 SonicScope

**错误消息：**
```
System.InvalidOperationException: 没有活跃的 SonicScope，请先调用 Sonic.CreateScope()
```

**原因：** 在调用 `Sonic.Use<T>()`、`Sonic.Use<T>(out _)` 或任何便捷方法之前，没有创建 Scope。

**解决方案：**

```csharp
// 在程序入口处（Program.cs 或 Startup.cs）
Sonic.CreateScope();

// 或使用 DI 方式——通过构造函数注入 ISonic
builder.Services.AddSonicDefaults();

public class MyService(ISonic sonic) // 无需手动 CreateScope
{
    public async Task DoWork()
    {
        await sonic.Save("key", value);
    }
}
```

---

## `InvalidOperationException`：服务 Xxx 未注册

**错误消息：**
```
System.InvalidOperationException: 服务 Sonic.ICacheService 未注册或不在有效 scope 内
```

**原因：** 尝试 `Sonic.Use<ICacheService>()` 但该接口从未注册。

**解决方案：**

```csharp
// 方案 A：手动注册
Sonic.Register<ICacheService>(new MemoryCacheService());

// 方案 B：使用默认注册
NetServices.RegisterDefaults();  // 一次性注册所有默认实现

// 方案 C：DI 方式
builder.Services.AddSonicDefaults();  // 全部内存实现
```

---

## `NotSupportedException`：事务功能尚未实现

**错误消息：**
```
System.NotSupportedException: 事务功能尚未实现
```

**原因：** 调用了 `BeginTransactionAsync().CommitAsync()`。当前 `NullTransaction` 不支持提交，因为底层 `InMemoryDatabase` 没有事务机制。

**解决方案：**

```csharp
// 方案 A：不使用事务（单条写入原子性由 ConcurrentDictionary 保证）
await db.AddAsync(entity);  // 单条写入是原子的

// 方案 B：串行化多条写入（简单场景）
await db.AddAsync(order);
await db.AddAsync(auditLog);
// 如果有写入失败，手动清理前面已写入的数据

// 方案 C：等待 SQL 后端支持（路线图中）
```

---

## `InvalidOperationException`：服务没有注册任何提供者

**错误消息：**
```
System.InvalidOperationException: 服务 Sonic.ISonicDatabase 没有注册任何提供者（查询 type='mysql'）
```

**原因：** `SonicProviderRegistry` 中未找到对应 type 的提供者，且连 fallback 默认值也没有。

**解决方案：**

```csharp
// 检查当前已注册的提供者类型
var dbTypes = SonicProviderRegistry.GetRegisteredTypes<ISonicDatabase>();
Console.WriteLine(string.Join(", ", dbTypes));  // → "inmemory"

// 注册新的提供者（如果已实现）
SonicProviderRegistry.Register<ISonicDatabase>("mysql", ctx => new MySqlDatabase(...));

// 或使用现有类型
NetOptions options = ...;
options.Database.Type = "inmemory";  // 而非 "mysql"
```

---

## `JsonException`：JSON 解析失败

**错误消息：**
```
System.Text.Json.JsonException: The JSON value could not be converted to ...
```

**常见情景：**

| 情景 | 原因 | 解决 |
|------|------|------|
| `sonic.Load<User>("user:1")` 返回的是旧版实体结构 | Schema 变更后旧数据与新类型不匹配 | 删除旧数据重写，或实现数据迁移 |
| `SonicEntry.ToObject<T>()` 返回 null | 二进制数据被当作 JSON 解析 | 用 `url.Url()` 直接访问文件 |
| 实体类使用了 `record` 但没有 `required` 属性 | 序列化/反序列化属性名不匹配 | 确保 `[JsonPropertyName("...")]` 或属性名一致 |

**调试方法：**

```csharp
// 查看实际存储的原始数据
var entry = await sonic.Load<JsonElement>("user:1");
Console.WriteLine(entry.GetRawText());
```

---

## 并发问题排查

### `ConcurrentDictionary` 行为差异

`InMemoryDatabase` 使用 `ConcurrentDictionary`，以下操作的行为与 `Dictionary` **不同**：

| 操作 | Dictionary 行为 | ConcurrentDictionary 行为 |
|------|------|------|
| `_store["key"] = value` | 抛异常（键不存在）或覆盖 | 总是 Upsert |
| `_store.Remove("key")` | 键不存在抛异常 | 返回 false，不抛异常 |
| `GetPrefixAsync` 中的 `_store.Values` | 锁保护 | **快照语义**——遍历的是调用时刻的瞬时快照 |

**重要：** `GetPrefixAsync` 返回的是 ConcurrentDictionary 的瞬时快照。在遍历过程中写入的新键不会出现在结果中，已删除的键仍可能在结果中。

### 死锁排查

```csharp
// ❌ 容易死锁的模式
var task1 = Task.Run(async () =>
{
    await sonic.Save(key, value1);
    var v2 = await sonic.Load<T>(key);  // 如果 v2 的写入者也在等待 v1...
});

// ✅ 安全的模式
var data = await PrepareData();
await sonic.Save(key, data);  // 不要在原子写入中包含跨行依赖
```

---

## FileStateManager 问题

### 文件路径过长

**现象：** 键名包含特殊字符或过长时，创建文件失败。

**解决：** `FileStateManager` 自动将 `:` `/` `\` 替换为 `_`。如果键名极长（>255 字符），建议缩短：

```csharp
// 不要用超长键名
await state.SetAsync($"user:{userId}:profile:{guid1}:config:{guid2}:...", value);

// 使用层级合理的键名
await state.SetAsync($"user:{userId}:profile", value);
```

### 读不到刚写入的数据

**现象：** `SetAsync` 后立即 `GetAsync` 返回 null。

**原因：** 文件写入是异步的，如果在外部的 using 作用域内：

```csharp
// ❌ 错误：state 在 using 结束时就 Dispose 了
using (var state = new FileStateManager())
{
    await state.SetAsync("key", "value");
}
// Dispose 清理了写缓存！

var state2 = new FileStateManager();
var v = await state2.GetAsync<string>("key");  // null！文件可能还没写完
```

**解决：** `FileStateManager` 在 `SetAsync` 中同步写文件（`await File.WriteAllBytesAsync`），Dispose 不会删文件，不会有此问题。但写缓存会丢失——重新读取会从文件加载。

---

## MemoryStreamService 问题

### 消费者比生产者晚启动

**现象：** 生产者发布了消息，但消费者启动后收不到之前发布的消息。

**原因：** `MemoryStreamService` 是进程内转发，无持久化。消费者必须在线才能接收消息。

**解决：**

```csharp
// 确保订阅者在生产者之前启动
_ = Task.Run(async () =>
{
    await foreach (var msg in sonic.On<OrderEvent>("order.created"))
    {
        await Handle(msg);
    }
});

// 稍后再发布
await Task.Delay(100);
await sonic.Emit("order.created", new OrderEvent { ... });
```

### 发布速度 > 消费速度（背压）

**现象：** 消费变慢，内存增长。

**原因：** `Channel.CreateUnbounded<StreamMessage>()` 无上限，生产者不会被阻塞。

**解决：** 如果生产速度持续超过消费速度，考虑使用有界 Channel 或外部 Kafka 后备：

```csharp
// 暂不支持—这是 Kafka sidecar 路线图覆盖的场景
```

---

## Provider 切换问题

### 切换 Provider 后数据不可见

**现象：** 从 `local` 切换到 `s3` 后，之前保存的文件找不到。

**原因：** **每个 Provider 是完全独立的后端。** 切换 Provider 不会自动迁移数据。

**解决：**

```csharp
// 迁移数据：遍历旧存储，上传到新存储
var oldStorage = new LocalStorageProvider(new StorageOptions { BasePath = "./storage" });
var newStorage = new S3StorageProvider(new StorageOptions { ... });

foreach (var file in Directory.GetFiles("./storage"))
{
    var key = Path.GetFileName(file);
    var bytes = await oldStorage.DownloadBytesAsync(key);
    await newStorage.UploadAsync(key, bytes);
}
```

### 云厂 SDK 初始化失败

**现象：** 启动时抛出云厂 SDK 的异常（如 `AmazonS3Exception`）。

**原因：** 配置了 `Type = "s3"` 但没有提供有效的 AccessKey / SecretKey / Bucket。

**解决：**

```csharp
// 检查配置完整性
var options = new SonicStorageOptions
{
    Type = "s3",
    Bucket = "my-bucket",           // 必填
    Region = "us-east-1",           // 必填
    Endpoint = null,                // 留空用 AWS 默认，MinIO 填自定义
    AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),     // 必填
    SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")  // 必填
};

builder.Services.AddSonic(new NetOptions
{
    Storage = options
});
```

---

## 快速诊断命令

```csharp
// 打印当前 Scope 的所有服务注册
SonicScope scope = Sonic.CreateScope();
foreach (var type in new[] { typeof(ISonicDatabase), typeof(ICacheService), typeof(IStorageProvider), typeof(IStreamService), typeof(IStateManager) })
{
    var registered = scope.TryResolve(type, out var _);
    Console.WriteLine($"{type.Name,-25} {(registered ? "✅" : "❌")}");
}

// 打印 Provider 注册表状态
Console.WriteLine("ISonicDatabase: " + string.Join(", ", SonicProviderRegistry.GetRegisteredTypes<ISonicDatabase>()));
Console.WriteLine("ICacheService:   " + string.Join(", ", SonicProviderRegistry.GetRegisteredTypes<ICacheService>()));
Console.WriteLine("IStorageProvider: " + string.Join(", ", SonicProviderRegistry.GetRegisteredTypes<IStorageProvider>()));
Console.WriteLine("IStreamService:  " + string.Join(", ", SonicProviderRegistry.GetRegisteredTypes<IStreamService>()));
Console.WriteLine("IStateManager:   " + string.Join(", ", SonicProviderRegistry.GetRegisteredTypes<IStateManager>()));
```

## 相关文档

- [服务参考](./services/index.md) — 各服务的实现状态
- [迁移指南](../migration-guide.md) — 从标准 .NET 架构迁移
- [状态管理架构](./systems/state-management.md) — 理解 Persistent / Ephemeral 的边界
