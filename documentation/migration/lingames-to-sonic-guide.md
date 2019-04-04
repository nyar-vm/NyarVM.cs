# lingames-* 迁移到 Sonic 指南

> Sonic 是 Sonic 统一后端框架，提供 DI 容器、中间件管道、HTTP 服务器、认证授权、定时任务、消息总线等企业级能力。
> 本指南帮助 lingames-* 项目从独立基建迁移到 Sonic 平台。

## 一、lingames-auth 迁移方案

### 1.1 当前架构

```
lingames-auth/
├── src/
│   ├── jwt/          # JWT 签发与验证
│   ├── oauth/        # OAuth2 流程
│   ├── session/      # 会话管理
│   └── api-key/      # API Key 管理
├── middleware/        # 认证中间件
└── db/                # 用户/会话存储
```

### 1.2 Sonic 对应能力

| lingames-auth 模块 | Sonic 能力 | 迁移方式 |
|:---|:---|:---|
| JWT 签发/验证 | `Sonic.Auth.Jwt.JwtHandler` | 直接替换，HS256 签名/验证 |
| OAuth2 流程 | `Sonic.Auth` 扩展点 | 注册自定义 AuthHandler |
| 会话管理 | Sonic DI Scoped 服务 | `ISessionStore` 接口 |
| API Key 管理 | `ApiKeyMiddleware` | 直接使用 |
| 认证中间件 | `MiddlewarePipelineBuilder` | 注册到管道 |
| 用户/会话存储 | `Hermes ORM` 仓储 | `IHermesRepository<T>` |

### 1.3 迁移步骤

1. **依赖注入改造**：将自建 DI 替换为 `NetApplicationBuilder`
2. **认证中间件接入**：
   ```csharp
   builder.UseMiddleware<ApiKeyMiddleware>();
   builder.UseMiddleware<JwtAuthMiddleware>();
   ```
3. **用户模型迁移**：将 User / Session 模型标注 `[HermesEntity]`，注册到 `HermesModule`
4. **配置迁移**：从自建 config.json 迁移到 Sonic VON 配置
5. **测试验证**：运行 LingamesAuthMigrationTests（13 个测试）

### 1.4 验收标准
- ✅ JWT 令牌生成与验证
- ✅ 角色检查（premium / beta_tester / guest）
- ✅ API Key 验证
- ✅ 中间件管道（CORS → Auth → RateLimit 顺序正确）
- ✅ DI 容器注册
- ✅ Hermes 仓储集成（Player + GameSession）

---

## 二、lingames-code-gpt 迁移方案

### 2.1 当前架构

```
lingames-code-gpt/
├── api/
│   ├── completion.go     # 代码补全接口
│   ├── chat.go           # 聊天接口
│   └── template.go       # 提示词模板接口
├── ai/
│   ├── openai.go         # OpenAI 适配器
│   └── azure.go          # Azure 适配器
├── middleware/
│   ├── auth.go           # 认证
│   └── rate_limit.go     # 速率限制
└── db/
    ├── history.go        # 代码历史
    └── template.go       # 模板存储
```

### 2.2 Sonic 对应能力

| lingames-code-gpt 模块 | Sonic 能力 | 迁移方式 |
|:---|:---|:---|
| API 路由 | RESTful 路由 + 参数绑定 | 属性路由 `[HttpGet]` / `[HttpPost]` |
| OpenAI 适配器 | `Sonic.AI` 服务抽象 | `IAiService` 接口 |
| Azure 适配器 | `Sonic.AI.AzureOpenAiService` | 切换 Provider 配置 |
| 认证 | `JwtHandler` | 直接使用 |
| 速率限制 | `RateLimitMiddleware` | 直接使用 |
| 代码历史 | `Hermes` 仓储 | `IHermesRepository<CodeGenerationHistory>` |
| 模板存储 | `Hermes` 仓储 | `IHermesRepository<PromptTemplate>` |

### 2.3 迁移步骤

1. **路由迁移**：Go gin → Sonic 属性路由
2. **AI 服务接入**：
   ```csharp
   services.AddSingleton<IAiService, OpenAiService>();
   ```
3. **速率限制配置**：Free 10 req/min → Pro 500 req/min → Enterprise 5000 req/min
4. **历史记录存储**：标注 `[HermesEntity]`，自动生成 CRUD 仓储
5. **测试验证**：运行 LingamesCodeGptMigrationTests（12 个测试）

### 2.4 验收标准
- ✅ AI 服务选项注册
- ✅ 中间件管道（rate_limit → auth → prompt → ai_call）
- ✅ JWT 认证（developer / org_admin）
- ✅ Hermes 仓储（CodeHistory + PromptTemplate）
- ✅ 50 并发请求独立处理
- ✅ 三档速率限制（Free / Pro / Enterprise）

---

## 三、二者共用的 Sonic 能力

| 能力 | Sonic 模块 | lingames-auth | lingames-code-gpt |
|:---|:---|:---|:---|
| DI 容器 | `Sonic.Hosting` | ✅ | ✅ |
| 中间件管道 | `Sonic.Middleware` | ✅ | ✅ |
| JWT 认证 | `Sonic.Auth.Jwt` | ✅ | ✅ |
| API Key | `Sonic.Auth.ApiKeyMiddleware` | ✅ | — |
| ORM 仓储 | `Hermes` via `HermesModule` | ✅ | ✅ |
| 速率限制 | `RateLimitMiddleware` | — | ✅ |
| AI 服务 | `Sonic.AI` | — | ✅ |

## 四、迁移检查清单

- [ ] lingames-auth 所有 API 在 Sonic 上可运行
- [ ] lingames-code-gpt 所有 API 在 Sonic 上可运行
- [ ] 13 个 auth 迁移测试通过
- [ ] 12 个 code-gpt 迁移测试通过
- [ ] 中间件管道集成正确
- [ ] Hermes ORM 仓储集成正确
- [ ] 配置文件迁移到 VON 格式
- [ ] DI 容器正确注册所有服务
