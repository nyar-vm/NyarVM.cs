# Sonic Changelog

All notable changes to the Sonic framework will be documented in this file.

## [1.0.0] - 2026-05-04

### ✨ 亮点

Sonic.Net 1.0 正式发布——Valkyrie 生态中的后端应用框架，定位为 Spring Boot / ASP.NET Core 级别的企业级后端运行时。

### 🎉 新增功能

#### 子领域 A — 应用核心

- **依赖注入容器**：构造器注入 / 属性注入 / 生命周期管理（Singleton / Scoped / Transient）
- **应用主机**：`NetHost` + `NetApplicationBuilder` + `INetModule` 自动发现机制
- **配置系统**：VON 格式 / 环境变量 / 命令行参数多源合并
- **中间件管道**：`IMiddleware` + `MiddlewareDelegate` + `MiddlewarePipelineBuilder` 链式构建

#### 子领域 B — Web 能力

- **HTTP 服务器**：`HttpServer` 基于 Valkyrie Lexer/Parser 的原始 HTTP 解析
- **RESTful 路由**：属性路由 `[Route]` / `[HttpGet/POST/PUT/DELETE]` / 参数绑定 / 模型验证
- **JWT 认证**：自建 HMAC-SHA256 引擎（零外部依赖），支持 `ParseAndValidate` / `ParseFromHeader`
- **API Key 认证**：`X-Api-Key` 请求头 + 查询字符串双模式
- **角色授权**：`[RequireRole]` 标注 + 401/403 响应
- **WebSocket 支持**：RFC 6455 帧编解码 / 连接生命周期管理 / 广播 / JSON + 二进制序列化

#### 子领域 C — 企业级能力

- **结构化日志**：`ConsoleLogOutputProvider`（彩色分级输出） + `FileLogOutputProvider`（滚动日志）
- **健康检查**：`/health` / `/health/liveness` / `/health/readiness` 三端点，支持自定义探针
- **缓存抽象**：`MemoryCacheService` + `GetOrSetAsync` 缓存模式
- **Cron 调度器**：`CronExpressionParser`（5 字段解析） + `CronScheduler`（Timer 驱动）
- **事件总线**：`InMemoryEventBus` 发布订阅 + `SubscriptionToken` 取消订阅

#### 子领域 D — 整合层

- **Hermes ORM 整合**：`IHermesRepository<T>` 仓储自动注册 + `[HermesEntity]` / `[HermesKey]` 标注
- **Hermes RPC 整合**：`RpcProxyGenerator`（DispatchProxy 代理） + `HermesRpcModule` 自动扫描
- **Iris CLI 整合**：`[SonicCommand]` 命令标注 + `ISonicCommand` 接口 + `CommandContext`
- **Iris 终端适配**：`IIrisTerminal` 契约 + `IrisLogAdapter` 日志渲染
- **项目脚手架**：`ProjectScaffolder` + `BuiltInTemplates`（webapi / websocket / minimal 三模板）

#### 企业级增强

- **响应压缩**：GZip / Deflate 双算法，MIME 类型过滤 + 可配置压缩阈值
- **速率限制**：令牌桶 + 固定窗口双策略，IP 识别 + Retry-After 响应头
- **静态文件服务**：`StaticFileMiddleware` + MIME 类型映射 + 目录浏览选项
- **请求超时控制**：`TimeoutMiddleware` 超时 504 响应
- **Cookie 工具**：`CookieParser` 解析 + `BuildSetCookie` 构造

### 🧪 测试

- MiddlewarePipelineTests: 18 测试
- RoutingTests: 22 测试
- AuthenticationTests: 16 测试
- HermesIntegrationTests: 13 测试
- LoggingTests: 7 测试
- HealthCheckTests: 7 测试
- CacheTests: 8 测试
- CronSchedulerTests: 6 测试
- EventBusTests: 7 测试
- IrisIntegrationTests: 12 测试
- WebSocketTests: 16 测试
- EnterpriseFeaturesTests: 15 测试
- SonicInfrastructureTests: 18 测试
- **合计：165 测试，0 诊断**

### 🔒 依赖约束

- Sonic 不自建 CLI/TUI 框架（委托 Iris）
- Sonic 不自建 ORM/RPC 引擎（委托 Hermes）
- Sonic 不自建数据库（委托 Data & Infra）
- Sonic 不自建文本编解码（委托 Oak）
- Sonic 不自建二进制编解码（委托 Acorn）

### 📦 依赖项

| 包 | 版本 | 说明 |
|:---|:---|:---|
| Microsoft.Extensions.Configuration.Binder | 8.0.2 | 配置绑定 |
| Microsoft.Extensions.Configuration.Abstractions | 8.0.0 | 配置接口 |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | DI 容器 |
| Microsoft.Extensions.Hosting.Abstractions | 8.0.1 | 主机抽象 |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | 日志抽象 |
| AWSSDK.S3 | 3.7.401 | S3 存储 |
| Hermes.ORM | (源码引用) | ORM/RPC 引擎 |
