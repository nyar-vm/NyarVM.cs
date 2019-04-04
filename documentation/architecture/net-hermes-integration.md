# Sonic + Hermes 整合方案

> **文档状态**：M0 产出，对应路线图 [14-sonic-team.md](file:///e:/RiderProjects/.trae/roadmaps/14-sonic-team.md) 子领域 D-14

## 一、整合目标

使 Sonic 应用能够**自动注入 Hermes 生成的 ORM 仓储和 RPC 客户端代理**，开发者无需手动注册每个仓储或代理类。

## 二、整合架构

```
┌──────────────────────────────────────────────────────┐
│                    Sonic 应用                         │
│  ┌────────────────────────────────────────────────┐  │
│  │              DI 容器 (IServiceCollection)       │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────────┐ │  │
│  │  │ 仓储实例  │  │ RPC 代理  │  │  其他服务     │ │  │
│  │  │ (自动注入)│  │ (自动注入) │  │              │ │  │
│  │  └──────────┘  └──────────┘  └──────────────┘ │  │
│  └────────────────────────────────────────────────┘  │
│                         ▲                             │
│                         │ 自动注册                    │
│  ┌────────────────────────────────────────────────┐  │
│  │         Sonic.Generator (代码生成器)             │  │
│  │  ┌──────────────┐  ┌────────────────────────┐  │  │
│  │  │ 仓储生成器    │  │ RPC 代理生成器          │  │  │
│  │  │ (Repository  │  │ (RpcClientGenerator)   │  │  │
│  │  │  Generator)  │  │                        │  │  │
│  │  └──────┬───────┘  └───────────┬────────────┘  │  │
│  └─────────┼──────────────────────┼────────────────┘  │
│            │                      │                    │
├────────────┼──────────────────────┼────────────────────┤
│            ▼                      ▼                    │
│  ┌─────────────────────────────────────────────────┐  │
│  │            Hermes Schema (.he 文件)               │  │
│  │  ┌──────────────┐  ┌────────────────────────┐   │  │
│  │  │ 实体定义      │  │ RPC 服务定义             │   │  │
│  │  │ entity User {}│  │ rpc UserService {}     │   │  │
│  │  └──────────────┘  └────────────────────────┘   │  │
│  └─────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

## 三、Hermes ORM 仓储自动注入

### 3.1 流程

```
1. 开发者编写 Hermes Schema (.he)
       │
2. Hermes 编译器 → 生成实体类 (.cs)
       │
3. Sonic.Generator → 扫描实体 → 生成仓储类
       │
4. Sonic DI 容器 → 自动注册仓储
       │
5. 开发者构造函数注入即可使用
```

### 3.2 仓储生成规则

| Schema 实体 | 生成的仓储 | 注册生命周期 |
|:---|:---|:---|
| `entity User` | `UserRepository : IRepository<User>` | `Scoped` |
| `entity Team` | `TeamRepository : IRepository<Team>` | `Scoped` |

### 3.3 自动注册实现

```csharp
// Sonic DiExtensions 中的自动扫描注册
public static IServiceCollection AddHermesRepositories(
    this IServiceCollection services,
    Assembly assembly)
{
    var repositoryTypes = assembly.GetTypes()
        .Where(t => t.IsClass
                    && !t.IsAbstract
                    && t.Name.EndsWith("Repository"));

    foreach (var repoType in repositoryTypes)
    {
        var entityType = GetEntityType(repoType);
        var interfaceType = typeof(IRepository<>).MakeGenericType(entityType);
        services.AddScoped(interfaceType, repoType);
    }

    return services;
}
```

### 3.4 使用示例

```csharp
public class UserService
{
    private readonly IRepository<User> _users;

    // 构造器注入，Sonic DI 自动解析
    public UserService(IRepository<User> users)
    {
        _users = users;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _users.FindByIdAsync(id);
    }
}
```

## 四、Hermes RPC 客户端自动注册

### 4.1 流程

```
1. Hermes Schema 中定义 RPC 服务
       │  rpc UserService {
       │      method GetUser(id: int) -> User
       │  }
       │
2. Hermes 编译器 → 生成 RPC 接口 + 代理类
       │
3. Sonic.Generator → 扫描 RPC 服务 → 生成客户端代理
       │
4. Sonic DI 容器 → 自动注册代理
       │
5. 开发者构造函数注入即可调用
```

### 4.2 RPC 代理注册规则

| Schema RPC 定义 | 生成的接口 | 代理实现 | 生命周期 |
|:---|:---|:---|:---|
| `rpc UserService` | `IUserService` | `UserServiceRpcProxy` | `Scoped` |

### 4.3 自动注册实现

```csharp
public static IServiceCollection AddHermesRpcClients(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var rpcConfig = configuration.GetSection("Sonic:Rpc");
    var serviceEndpoints = rpcConfig.GetSection("Services").Get<Dictionary<string, string>>();

    var rpcInterfaces = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => t.IsInterface
                    && t.GetCustomAttribute<HermesRpcServiceAttribute>() != null);

    foreach (var rpcInterface in rpcInterfaces)
    {
        var proxyType = typeof(RpcClientProxy<>).MakeGenericType(rpcInterface);
        var endpoint = serviceEndpoints.GetValueOrDefault(rpcInterface.Name, "http://localhost:5000");
        services.AddScoped(rpcInterface, sp =>
            Activator.CreateInstance(proxyType, endpoint));
    }

    return services;
}
```

### 4.4 使用示例

```csharp
public class TeamService
{
    private readonly IUserService _userService;

    public TeamService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task AddMemberAsync(int teamId, int userId)
    {
        var user = await _userService.GetUser(userId);
        // ...
    }
}
```

## 五、代码生成器协作流程

### 5.1 架构

```
Hermes.Compiler (编译 Schema)
    │
    ├── Hermes.Generator (生成实体 + 接口)
    │       │
    │       └── C# 源文件写入磁盘
    │
    └── Sonic.Generator (生成仓储 + 代理)
            │
            ├── 读取 Hermes 生成的实体和接口
            ├── 生成仓储实现 (SonicRepository<T>)
            ├── 生成 RPC 代理 (RpcClientProxy<T>)
            └── 生成 DI 注册引导代码
```

### 5.2 文件生成目录约定

```
backends/
└── MyProject/
    ├── schema.he                    # Hermes Schema
    ├── generated/
    │   ├── Hermes/
    │   │   ├── Entities/
    │   │   │   ├── User.cs          # Hermes 生成
    │   │   │   └── Team.cs          # Hermes 生成
    │   │   └── Services/
    │   │       └── IUserService.cs  # Hermes 生成
    │   └── Sonic/
    │       ├── Repositories/
    │       │   ├── UserRepository.cs # Sonic 生成
    │       │   └── TeamRepository.cs # Sonic 生成
    │       └── Proxies/
    │           └── UserServiceRpcProxy.cs # Sonic 生成
    └── src/
        └── Services/
            └── UserService.cs       # 开发者编写
```

## 六、与 Hermes & DejaVu 团队的接口契约

| 接口 | 提供方 | 消费方 | 说明 |
|:---|:---|:---|:---|
| `Hermes.Compiler.ICompiler` | Hermes | Sonic.CLI | Schema 编译 |
| `Hermes.ORM.IRepository<T>` | Hermes | Sonic | 通用仓储接口 |
| `Hermes.ORM.IQueryExecutor` | Hermes | Sonic | 查询执行器 |
| `[HermesRpcService]` | Hermes | Sonic.Generator | RPC 服务标记 |
| `[HermesEntity]` | Hermes | Sonic.Generator | 实体标记 |

## 七、红线约束

| 红线 | 遵守方式 |
|:---|:---|
| Sonic 不做 ORM 引擎 | 仓储仅封装 `IRepository<T>` 的调用，不实现查询翻译、连接管理等核心逻辑 |
| Sonic 不做 RPC 引擎 | 代理仅封装序列化/传输，底层协议由 Hermes 提供 |
| Sonic 不绕过 Hermes | 所有数据操作必须通过 Hermes 生成的仓储或代理 |
