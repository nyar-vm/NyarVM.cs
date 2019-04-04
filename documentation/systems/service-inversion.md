# 服务控制反转

> **归属：Layer 2 — 服务注册与发现**
> **依赖：** Layer 1（状态管理）的接口定义
> **被依赖：** Layer 3（中间件/洋葱模型）和 Layer 4（外围能力）

## 核心思想

Sonic 的所有服务通过控制反转（IoC）获取。开发者不直接构造服务实例，而是通过 `Sonic.Use<TService>(out service)` 获取强类型接口，在 scope 内部调用。

> 服务由 Schema 定义，由 Sonic 注册，由开发者使用。

## API 设计

### `Sonic.Use<T>()` — 获取服务

```csharp
if (Sonic.Use<ICacheService>(out var cache))
{
    await cache.SetAsync("key", value);
}
```

| 返回值 | 说明 |
|--------|------|
| `true` | 服务已注册，`service` 可用 |
| `false` | 服务未注册，`service` 为 null |

### `Sonic.Use<T>(name)` — 获取命名服务

```csharp
if (Sonic.Use<ICacheService>("session", out var sessionCache))
{
    await sessionCache.SetAsync("session:abc", userId);
}
```

### `Sonic.TryUse<T>()` — 安全获取

```csharp
var cache = Sonic.TryUse<ICacheService>();
if (cache != null)
{
    await cache.SetAsync("key", value);
}
```

### `Sonic.Register<T>()` — 注册服务

```csharp
Sonic.Register<ICacheService>(new MemoryCacheService(options));
Sonic.Register<ICacheService>(() => new MemoryCacheService(options));
Sonic.Register<ICacheService, MemoryCacheService>();
```

## 服务注册流程

Sonic 的服务注册由 Schema 驱动，开发者不需要手动注册大部分服务：

```
schema.he
    │
    ↓ Hermes Compiler
SchemaIR
    │
    ↓ Sonic Runtime
┌──────────────────────────────────────────────┐
│ 自动注册                                       │
│                                              │
│ SchemaIR.storage.models  → IStorageService   │
│ SchemaIR.storage.caches  → ICacheService     │
│ SchemaIR.storage.streams → IStreamService    │
│ SchemaIR.services        → IServiceEndpoint  │
│ SchemaIR.micros          → IMicroService     │
└──────────────────────────────────────────────┘
    │
    ↓ SonicBootstrap.Configure()
┌──────────────────────────────────────────────┐
│ 手动配置                                       │
│                                              │
│ config.Cache.Provider = "redis"              │
│ config.Storage.Provider = "s3"               │
│ config.Database.Provider = "mysql"           │
└──────────────────────────────────────────────┘
```

## Scope 语义

### 什么是 Scope

Scope 是服务的生命周期边界。在 Sonic 中，scope 对应一个 Serverless 函数调用的生命周期。

```
HTTP 请求进入
    │
    ↓ 创建 Scope
┌──────────────────────────────────────┐
│ Scope                                │
│                                      │
│ Sonic.Use<ICacheService>(out cache)  │
│   → 获取当前 scope 的 Cache 实例     │
│                                      │
│ Sonic.Use<IStorageService>(out stor) │
│   → 获取当前 scope 的 Storage 实例   │
│                                      │
│ 业务逻辑执行...                       │
│                                      │
└──────────────────────────────────────┘
    │
    ↓ Scope 结束
资源释放
```

### Scope 内的服务行为

| 服务类型 | Scope 内行为 | 跨 Scope 行为 |
|---------|-------------|--------------|
| `ICacheService` | 每次请求获取当前容器的缓存实例 | 不同容器有不同的缓存实例 |
| `IStorageService` | 每次请求获取共享的存储实例 | 所有容器共享同一个存储 |
| `IStateManager` | 每次请求获取共享的状态管理器 | 所有容器共享同一个状态 |

## 与传统 DI 框架的对比

### ASP.NET Core DI

```csharp
public class UserController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IStorageService _storage;

    public UserController(ICacheService cache, IStorageService storage)
    {
        _cache = cache;
        _storage = storage;
    }
}
```

### Sonic IoC

```csharp
public class UserController
{
    public async Task<User> GetUser(int id)
    {
        if (Sonic.Use<ICacheService>(out var cache))
        {
            var cached = await cache.GetAsync<User>($"user:{id}");
            if (cached != null) return cached;
        }

        if (Sonic.Use<IStorageService>(out var storage))
        {
            var user = await storage.GetAsync<User>($"users/{id}");
            return user;
        }

        throw new InvalidOperationException("No storage service available");
    }
}
```

### 关键区别

| 特性 | ASP.NET Core DI | Sonic IoC |
|------|----------------|-----------|
| 注册方式 | 手动 `services.AddScoped<T>()` | Schema 自动 + 手动配置 |
| 获取方式 | 构造函数注入 | `Sonic.Use<T>(out service)` |
| 依赖声明 | 构造函数参数 | 代码内按需获取 |
| 框架绑定 | 绑定 ASP.NET Core | 框架无关 |
| 可选依赖 | 需要 nullable | `if (Sonic.Use<T>(out var s))` |
| 生命周期 | DI 容器管理 | Scope 管理 |

## 设计理由

### 为什么不用构造函数注入

1. **Serverless 场景**：函数入口不一定是控制器，可能是 Lambda、Worker 等
2. **Schema 驱动**：服务由 Schema 定义，不是由代码声明
3. **可选依赖**：`Sonic.Use<T>()` 天然支持可选依赖，不需要 nullable 注入
4. **框架无关**：Sonic 不绑定任何特定框架

### 为什么用 `out` 参数而非直接返回

```csharp
// 方案 A：直接返回，可能为 null
var cache = Sonic.Use<ICacheService>();

// 方案 B：out 参数 + bool 返回（Sonic 选择）
if (Sonic.Use<ICacheService>(out var cache))
{
    // cache 一定不为 null
}
```

方案 B 的优势：
- 编译时明确区分"有服务"和"没服务"的代码路径
- 不需要 null 检查
- 符合 Try-Parse 模式，C# 开发者熟悉
