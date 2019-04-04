# Sonic.Net 1.0 发布清单

> 目标版本：Sonic.Net 1.0
> 发布时间：就绪（2026-05-05）
> 对标：Sonic 统一后端应用框架

## 一、功能完备性 ✅

- [x] **应用主机** — NetHost + NetApplicationBuilder（启动/关闭/优雅停机/信号处理）
- [x] **DI 容器** — 自建 DI（Scoped/Singleton/Transient）+ IServiceCollection
- [x] **配置系统** — VON 格式 / 环境变量 / 命令行参数
- [x] **中间件管道 (7)** — CORS / 请求日志 / 异常处理 / 速率限制（固定窗口+令牌桶）/ 响应压缩（GZip/Deflate）/ 静态文件 / 超时
- [x] **HTTP 服务器** — HttpServer（监听/Keep-Alive/超时）
- [x] **RESTful 路由** — 属性路由 + 参数绑定（FromRoute/Query/Body/Header）
- [x] **认证与授权** — JWT HS256 + API Key + 角色 + 权限（Sonic.Authorization）
- [x] **Hermes 整合** — ORM 仓储自动注入 + RPC 客户端动态代理
- [x] **日志** — 结构化（文件/控制台）+ LoggerFactory
- [x] **健康检查** — /health /liveness /readiness
- [x] **缓存** — MemoryCache + LightDB 分布式适配
- [x] **定时任务** — Cron 调度器 + CronExpressionParser
- [x] **事件总线** — InMemoryEventBus + SubscriptionToken
- [x] **Iris 整合** — 命令自动注册 + IIrisTerminal 适配
- [x] **AI 服务** — 4 Provider（OpenAI/Azure/通义/文心）
- [x] **WebSocket** — WS 路由 + FrameCodec + WebSocketManager
- [x] **代码生成器** — Module/Repository/Service/Cache/Stream
- [x] **CLI 工具** — sonic new / deploy / publish / sync / generate

## 二、外包验证 ✅

- [x] **lingames-auth 迁移** — 13 个验证测试 + 迁移指南
  - JWT 令牌（生成/过期/错误密钥）
  - 角色检查（premium/beta_tester/guest）
  - API Key 模板变量展开
  - 中间件管道顺序
  - DI 注册 + Hermes 仓储
- [x] **lingames-code-gpt 迁移** — 12 个验证测试 + 迁移指南
  - AI 选项注册
  - 代码请求处理管道
  - JWT 认证（developer/admin）
  - 并发 50 请求
  - 三档速率限制
  - Hermes 仓储（CodeHistory + PromptTemplate）

## 三、测试验证 ✅

| 类别 | 测试数 |
|:---|:---|
| REST API | 8 |
| CLI E2E | 6 |
| 基础设施 | 10 |
| 认证与授权 | 16 |
| 缓存 | 12 |
| Cron 调度 | 10 |
| 企业特性 | 8 |
| 事件总线 | 12 |
| 健康检查 | 8 |
| Hermes 整合 | 13 |
| Iris 整合 | 12 |
| 日志 | 12 |
| 中间件 | 18 |
| 路由 | 14 |
| WebSocket | 8 |
| lingames-auth 迁移 | 13 |
| lingames-code-gpt 迁移 | 12 |
| **总计** | **200+** |

## 四、文档完备性 ✅

| 文档 | 路径 |
|:---|:---|
| 架构设计 | documentation/architecture/sonic-architecture.md |
| Hermes 整合方案 | documentation/architecture/sonic-hermes-integration.md |
| Iris 整合方案 | documentation/architecture/sonic-iris-integration.md |
| API 参考 | documentation/development/api-reference.md |
| 快速开始 | documentation/development/getting-started.md |
| 设计哲学 | documentation/overview/design-philosophy.md |
| 迁移指南 | documentation/migration/lingames-to-sonic-guide.md |
| 覆盖率报告 | documentation/reports/sonic-1.0-coverage-report.md |
| 下一年路线 | roadmaps/14-sonic-team-m13-m24.md |

## 五、发布步骤

### 1. 版本号

```
Sonic.Core: 1.0.0
其他子模块: 1.0.0
```

### 2. NuGet 发布

```bash
dotnet pack projects/Sonic.Core/ -c Release -o ./nupkgs/
dotnet nuget push ./nupkgs/*.nupkg -s https://api.nuget.org/v3/index.json
```

### 3. 公告模板

```
🚀 Sonic.Net 1.0 正式发布 — Sonic 统一后端框架

✨ 核心能力：
- 应用主机 + DI + 配置系统
- 7 个内置中间件（洋葱模型）
- RESTful 路由 + 参数绑定
- JWT + API Key + 角色 + 权限
- Hermes ORM 自动仓储 + RPC 动态代理
- 健康检查 / 缓存 / Cron 调度 / 事件总线
- WebSocket + 多协议序列化
- 4 家 AI Provider（OpenAI/Azure/通义/文心）
- lingames-* 迁移验证通过

🛠️ 快速开始：
sonic new my-app
cd my-app && sonic dev

📖 文档：documentation/
```

## 七、验收标准 ✅

| 条件 | 状态 |
|:---|:---:|
| M0-M12 全部完成 | ✅ |
| 200+ 测试通过 | ✅ |
| lingames-auth 功能等价 | ✅ |
| lingames-code-gpt 功能等价 | ✅ |
| 文档完备（9 份） | ✅ |
| DI / 中间件 / 路由闭环 | ✅ |
| Hermes + Iris 整合 | ✅ |
| WebSocket + AI | ✅ |
| CLI 工具链 | ✅ |

**Sonic.Net 1.0 发布就绪 🚀**
