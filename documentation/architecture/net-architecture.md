# Sonic 总体架构设计

> **文档状态**：M0 产出，对应路线图 [14-sonic-team.md](file:///e:/RiderProjects/.trae/roadmaps/14-sonic-team.md)

## 一、架构概览

### 1.1 定位

Sonic 是公司的**后端应用框架**，整合 Iris 的 CLI 框架和 Hermes 的 ORM/RPC，提供类似 Spring Boot / ASP.NET 级别的后端开发体验，但基于纯 Valkyrie 生态。

### 1.2 核心原则

| 原则 | 说明 |
|:---|:---|
| **薄组装层** | Sonic 只做组装和编排，不重写底层能力 |
| **委托分工** | CLI/TUI → Iris、ORM/RPC → Hermes、数据库 → Data & Infra |
| **统一 DI** | 所有服务通过依赖注入容器管理生命周期 |
| **中间件管道** | 请求经过标准化管道：认证 → 授权 → 路由 → 业务 → 响应 |

---

## 二、四大子领域架构

### 子领域 A：应用核心（Application Core）

```
┌─────────────────────────────────────────────────────┐
│                    NetHost (应用主机)                │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │ 启动/关闭 │  │ 优雅停机  │  │ 生命周期事件      │  │
│  └──────────┘  └──────────┘  └──────────────────┘  │
├─────────────────────────────────────────────────────┤
│               DependencyInjection (依赖注入)          │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │ 服务注册  │  │ 构造器注入 │  │ 生命周期(S/T/S)  │  │
│  └──────────┘  └──────────┘  └──────────────────┘  │
├─────────────────────────────────────────────────────┤
│               MiddlewarePipeline (中间件管道)         │
│  Request → [CORS] → [Auth] → [Log] → Router → Response │
├─────────────────────────────────────────────────────┤
│               Configuration (配置系统)                │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │ VON 文件  │  │ 环境变量  │  │ 命令行参数        │  │
│  └──────────┘  └──────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────┘
```

#### A1. Sonic 应用主机

**职责**：管理应用的完整生命周期。

| 阶段 | 操作 | 说明 |
|:---|:---|:---|
| 启动 | `NetHost.StartAsync()` | 初始化 DI 容器 → 加载配置 → 注册中间件 → 启动 HTTP 服务器 |
| 运行 | 处理请求循环 | 中间件管道分发请求 |
| 关闭 | `NetHost.StopAsync()` | 拒绝新请求 → 完成进行中请求 → 释放资源 |

**依赖**：`Microsoft.Extensions.Hosting.Abstractions` 提供 `IHost` / `IHostedService` 基础设施。

#### A2. 依赖注入容器

**职责**：管理所有服务的注册、解析和生命周期。

| 生命周期 | 说明 | 适用场景 |
|:---|:---|:---|
| `Singleton` | 全局唯一实例 | 配置、缓存、日志工厂 |
| `Scoped` | 每个请求/作用域一个实例 | DbContext、UnitOfWork |
| `Transient` | 每次解析创建新实例 | 轻量无状态服务 |

**依赖**：`Microsoft.Extensions.DependencyInjection` 提供底层容器。

**Sonic 扩展**：
- 自动扫描注册（按约定注册服务）
- 模块化注册（`INetModule` 接口，每个模块自描述注册逻辑）
- Hermes 仓储/RPC 客户端自动注入（见子领域 D）

#### A3. 中间件管道

**职责**：定义请求处理的标准管道。

```
HTTP Request
    │
    ▼
┌─────────────┐
│   CORS      │  ← 跨域处理
├─────────────┤
│   AuthN     │  ← 身份认证（JWT / API Key）
├─────────────┤
│   AuthZ     │  ← 权限授权（角色 / 策略）
├─────────────┤
│   Logging   │  ← 请求日志记录
├─────────────┤
│   Router    │  ← 路由匹配 + 参数绑定 + 模型验证
├─────────────┤
│   Business  │  ← 业务逻辑处理
├─────────────┤
│   Response  │  ← 响应序列化
└─────────────┘
    │
    ▼
HTTP Response
```

**内置中间件**：

| 中间件 | 说明 | 配置 |
|:---|:---|:---|
| `CorsMiddleware` | 跨域资源共享 | `Sonic:Cors` |
| `AuthenticationMiddleware` | 身份认证 | `Sonic:Auth` |
| `AuthorizationMiddleware` | 权限校验 | 按策略配置 |
| `RequestLoggingMiddleware` | 请求日志 | `Sonic:Logging` |
| `ExceptionHandlingMiddleware` | 全局异常处理 | — |
| `StaticFileMiddleware` | 静态文件服务 | `Sonic:StaticFiles` |

#### A4. 配置系统

**职责**：多环境、多来源的配置管理。

| 配置来源 | 优先级 | 说明 |
|:---|:---|:---|
| 命令行参数 | 最高 | `--key=value` 格式 |
| 环境变量 | 中 | `SONIC__Section__Key` 格式 |
| VON 配置文件 | 低 | `sonic.von` 或 `appsettings.von` |
| 默认值 | 最低 | 代码中定义的默认值 |

**配置结构约定**：

```yaml
Sonic:
  Host:
    Urls: "http://localhost:5000"
    ShutdownTimeoutSeconds: 30

  Database:      # → Hermes.Schema → Hermes.ORM
    Type: "postgresql"
    ConnectionString: "..."

  Cache:         # → Sonic.ICacheService
    Type: "redis"
    ConnectionString: "localhost:6379"

  Storage:       # → Sonic.IStorageProvider
    Type: "s3"
    Bucket: "my-bucket"

  Auth:          # → Sonic.Authorization
    Jwt:
      Issuer: "sonic"
      SecretKey: "..."

  Logging:       # → Sonic.ILogger
    Level: "Information"
    Outputs: ["console", "file"]
```

---

### 子领域 B：Web 能力（Web Capabilities）

```
┌──────────────────────────────────────────┐
│            HttpServer (HTTP 服务器)    │
│  ┌────────────────────────────────────┐  │
│  │  基于 Valkyrie 的 HTTP 协议实现      │  │
│  │  HTTP/1.1 + HTTP/2 + HTTPS         │  │
│  └────────────────────────────────────┘  │
├──────────────────────────────────────────┤
│          RESTful Routing (路由系统)       │
│  ┌──────────┐  ┌─────────┐  ┌────────┐ │
│  │ 属性路由  │  │ 参数绑定 │  │ 模型验证│ │
│  └──────────┘  └─────────┘  └────────┘ │
├──────────────────────────────────────────┤
│       Auth & AuthZ (认证与授权)           │
│  ┌──────┐  ┌────────┐  ┌─────────────┐ │
│  │ JWT  │  │ OAuth2 │  │ API Key     │ │
│  └──────┘  └────────┘  └─────────────┘ │
├──────────────────────────────────────────┤
│         WebSocket (实时通信)              │
└──────────────────────────────────────────┘
```

#### B1. HttpServer — HTTP 服务器

**职责**：基于 Valkyrie 的纯 C# 实现的 HTTP 服务器。

**特性**：
- HTTP/1.1 和 HTTP/2 协议支持
- HTTPS/TLS（通过 Valkyrie 加密模块）
- Keep-Alive 连接复用
- 请求管道与中间件集成

**注意**：HttpServer 不依赖 ASP.NET Core Kestrel，是 Sonic 自建的 HTTP 层。

#### B2. RESTful 路由

**职责**：将 HTTP 请求映射到业务处理方法。

**路由定义**：
```csharp
[SonicRoute("/api/users")]
public class UsersController
{
    [HttpGet("{id}")]
    public Task<User> GetById(int id) { ... }

    [HttpPost]
    public Task<User> Create([FromBody] CreateUserRequest req) { ... }
}
```

**参数绑定**：

| 绑定源 | 属性 | 说明 |
|:---|:---|:---|
| 路由参数 | `[FromRoute]` | URL 路径中的 `{id}` |
| 查询字符串 | `[FromQuery]` | `?page=1&size=10` |
| 请求体 | `[FromBody]` | JSON/XML 反序列化 |
| 请求头 | `[FromHeader]` | `Authorization: Bearer ...` |
| 表单 | `[FromForm]` | multipart/form-data |

**模型验证**：

```csharp
public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required, Email]
    public string Email { get; set; }

    [Range(1, 150)]
    public int Age { get; set; }
}
```

#### B3. 认证与授权

**职责**：验证用户身份和控制访问权限。

**认证方式**：

| 方式 | 说明 | 依赖 |
|:---|:---|:---|
| JWT Bearer | 无状态 Token 认证 | Valkyrie 加密模块 |
| API Key | 简单密钥认证 | 配置系统 |
| OAuth2 | 第三方授权 | 外部 OAuth 提供方 |

**授权模型**：
- 基于角色（`[HasRole("Admin")]`）
- 基于权限（`[Permission("users:write")]`）
- 基于资源（`[ResourceAction("Post", "delete")]`）

**依赖**：`Sonic.Authorization` 项目，消费 `05-Valkyrie` 的加密模块。

#### B4. WebSocket 支持

**职责**：支持全双工实时通信。

**路由方式**：`[WebSocketRoute("/ws/chat")]`

---

### 子领域 C：企业级能力（Enterprise Capabilities）

#### C1. 健康检查

**职责**：提供应用健康状态探针。

| 探针类型 | 端点 | 说明 |
|:---|:---|:---|
| Liveness | `/health/live` | 应用是否存活（进程级） |
| Readiness | `/health/ready` | 应用是否就绪（依赖就绪） |
| 自定义 | `/health/{name}` | 任意自定义健康检查 |

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckAsync()
    {
        // 检查数据库连接
    }
}
```

#### C2. 日志与追踪

**职责**：结构化日志 + 分布式追踪。

| 输出目标 | 说明 |
|:---|:---|
| 控制台 | 开发环境 |
| 文件 | 滚动文件日志 |
| 远程 | 发送到集中式日志平台 |

**日志级别**：`Trace` < `Debug` < `Information` < `Warning` < `Error` < `Critical`

**分布式追踪**：基于 W3C Trace Context 标准，生成 `trace-id` 和 `span-id`。

#### C3. 定时任务

**职责**：Cron 表达式驱动的定时任务调度。

```csharp
[Cron("0 0 * * *")]  // 每小时执行
public class CleanupJob : ICronJob
{
    public Task ExecuteAsync(CancellationToken ct) { ... }
}
```

#### C4. 缓存抽象

**职责**：统一的缓存访问接口，屏蔽底层实现差异。

| 缓存类型 | 实现 | 适用场景 |
|:---|:---|:---|
| 本地内存 | `MemoryCacheService` | 单机高频数据 |
| 分布式 | `RedisCacheService` (LightDB) | 集群共享数据 |

**现有实现**：`Sonic.ICacheService`（Sonic 中已有完整实现）。

#### C5. 消息队列

**职责**：事件总线 + 异步消息处理。

| 实现 | 说明 |
|:---|:---|
| 内存队列 | `MemoryStreamService`，单机开发/测试 |
| Kafka | `KafkaStreamService`，生产环境 |

**现有实现**：`Sonic.IStreamService`（Sonic 中已有完整实现）。

---

### 子领域 D：整合层（Integration Layer）

#### D1. Sonic + Hermes 整合

详见 [Sonic-Hermes 整合方案](file:///e:/RiderProjects/Sonic/documentation/architecture/sonic-hermes-integration.md)。

**核心机制**：
- Hermes Schema 定义实体模型 → Sonic 代码生成器生成仓储 → DI 容器自动注册
- RPC 客户端代理自动注入到 DI 容器

#### D2. Sonic + Iris 整合

详见 [Sonic-Iris 整合方案](file:///e:/RiderProjects/Sonic/documentation/architecture/sonic-iris-integration.md)。

**核心机制**：
- Iris CLI 命令自动注册到 Sonic 应用
- `sonic new` 脚手架通过 DejaVu 模板引擎生成项目

#### D3. Sonic + DejaVu 整合

- 使用 DejaVu 模板引擎生成项目脚手架
- 项目模板（`sonic new`）、代码生成模板（仓储/服务/控制器）

---

## 三、目录结构

```
Sonic/
├── Sonic.slnx
├── projects/
│   ├── Sonic/                       # 应用核心（Sonic.Core.dll）
│   │   ├── Cache/                   # 缓存抽象
│   │   ├── Database/                # 数据库服务
│   │   ├── Storage/                 # 存储抽象
│   │   ├── Stream/                  # 消息流
│   │   ├── State/                   # 状态管理
│   │   ├── Middleware/              # 中间件管道 ← M0 新建
│   │   ├── Routing/                 # RESTful 路由 ← M0 新建
│   │   ├── HealthCheck/             # 健康检查 ← M0 新建
│   │   ├── Logging/                 # 日志与追踪 ← M0 新建
│   │   ├── Scheduling/              # 定时任务 ← M0 新建
│   │   ├── Sonic.cs                 # 应用主机入口
│   │   ├── SonicDiExtensions.cs     # DI 注册扩展
│   │   ├── NetOptions.cs          # 配置选项
│   │   └── ISonic.cs               # 核心接口
│   │
│   ├── Sonic.Authorization/         # 认证与授权
│   ├── Sonic.AI/                    # AI 服务
│   ├── Sonic.CLI/                   # CLI 工具（sonic 命令）
│   └── Sonic.Generator/            # 代码生成器
│
├── examples/
│   ├── Sonic.Tests/                 # 单元测试
│   └── Sonic.Benchmarks/           # 性能基准
│
└── documentation/
    ├── overview/                    # 概览文档
    ├── services/                    # 服务文档
    ├── systems/                     # 系统设计
    ├── technical/                   # 技术文档
    ├── tools/                       # 工具文档
    └── architecture/               # 架构设计 ← M0 新建
```

---

## 四、上下游关系

| 方向 | 对接团队 | 交付物 | 接口契约 |
|:---|:---|:---|:---|
| **上游** | 12-Hermes & DejaVu | ORM 实体 + RPC 代理 | Hermes 代码生成器 |
| **上游** | 13-Iris | CLI 框架 | Iris CLI + Iris.Interactive |
| **上游** | 05-Valkyrie | 编译器 + 标准库 + 加密 | ValkyrieRuntime |
| **上游** | 16-Data & Infra | 数据库 + LightDB 缓存 | OlympOS + LightDB |
| **上游** | 01-Acorn | 数据序列化 | Acorn 编码 |
| **下游** | 11-VOA | BFF API 层 | RESTful API |
| **下游** | lingames-* | 外包项目验证 | 完整后端 API |

---

## 五、红线约束

| 红线 | 委托团队 | 实施方式 |
|:---|:---|:---|
| 不做 CLI/TUI 框架 | 13-Iris | 引用 Iris CLI + Iris.Interactive，仅做命令注册和输出美化 |
| 不做 ORM/RPC 引擎 | 12-Hermes & DejaVu | 引用 Hermes.ORM，仅做仓储注入和代理注册 |
| 不做数据库引擎 | 16-Data & Infra | 引用 Hermes 数据库适配层，不做存储引擎实现 |
| 不做前端框架 | 11-VOA | 提供 RESTful API 供 VOA BFF 消费 |
| Sonic 不绕过 Hermes/Iris | — | 所有底层能力通过上游团队接口访问 |
