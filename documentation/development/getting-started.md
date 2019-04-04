# 入门指南

> **前置阅读：** [什么是后端](../overview/introduction.md) 和 [设计哲学](../overview/design-philosophy.md) — 理解 Persistent/Ephemeral 二分原则

## 环境要求

| 工具 | 版本 | 说明 |
|------|------|------|
| .NET SDK | 11.0+ | Sonic 运行时和构建工具 |
| Sonic CLI | 最新 | Schema 管理和部署工具 |
| 数据库 | MySQL 8.0+ / PostgreSQL 14+ | 可选，开发环境可用 SQLite |

## 安装 Sonic CLI

```bash
dotnet tool install --global Sonic.Tools
```

## 创建项目

### 1. 创建解决方案

```bash
dotnet new console -n MyApp
cd MyApp
dotnet add package Sonic
```

### 2. 定义 Schema

创建 `schema.he` 文件：

```hermes
namespace myapp;

class User {
    id: i32,
    name: utf8,
    email: utf8,
    status: UserStatus,
}

enums UserStatus {
    Inactive = 0,
    Active = 1,
    Suspended = 2,
}
```hermes
[index(email)]
model UserModel {
    user: &User,
}
```

缓存使用 `[cache]` 属性的独立命名空间：

```hermes
[cache(main="redis_main")]
namespace! my_redis;

[ttl(3600)]
model SessionCache {
    key: utf8,
    value: User.id,
}
```
service UserService {
    [path("/users/{id}"), json]
    get get_user(id: i32) -> User

    [path("/users"), json]
    post create_user(user: User) -> User
}
```

### 3. 同步数据库

```bash
sonic push --schema schema.he --backend sqlite:///./myapp.db
```

### 4. 编写业务代码

```csharp
using Sonic;

SonicBootstrap.Configure(config =>
{
    config.Cache.Provider = "memory";
    config.Cache.DefaultExpireMinutes = 30;
    config.Storage.Provider = "local";
    config.Storage.BasePath = "./data";
});

if (Sonic.Use<Cache.ICacheService>(out var cache))
{
    await cache.SetAsync("hello", "world");
    var value = await cache.GetAsync<string>("hello");
    Console.WriteLine($"Cache: {value}");
}

if (Sonic.Use<Storage.IStorageProvider>(out var storage))
{
    await storage.UploadAsync("test.txt", "Hello Sonic"u8.ToArray());
    var data = await storage.DownloadBytesAsync("test.txt");
    Console.WriteLine($"Storage: {System.Text.Encoding.UTF8.GetString(data)}");
}
```

### 5. 运行

```bash
dotnet run
```

## 项目结构

推荐的项目结构：

```
MyApp/
├── schema.he                # Hermes Schema 定义
├── sonic.json              # Sonic 配置文件
├── MyApp.csproj            # 项目文件
├── Program.cs              # 入口文件
├── Services/               # 业务服务
│   ├── UserService.cs
│   └── OrderService.cs
└── Models/                 # 生成的数据模型
    └── (由 Hermes Generator 自动生成)
```

## 添加缓存

```bash
dotnet add package Sonic.Cache
```

使用内存缓存：

```csharp
SonicBootstrap.Configure(config =>
{
    config.Cache.Provider = "memory";
});
```

使用 Redis 缓存：

```bash
dotnet add package Sonic.Cache.Redis
```

```csharp
SonicBootstrap.Configure(config =>
{
    config.Cache.Provider = "redis";
    config.Cache.ConnectionString = "localhost:6379";
});
```

## 添加存储

```bash
dotnet add package Sonic.Storage
```

使用本地存储：

```csharp
SonicBootstrap.Configure(config =>
{
    config.Storage.Provider = "local";
    config.Storage.BasePath = "./data";
});
```

使用 S3 存储：

```csharp
SonicBootstrap.Configure(config =>
{
    config.Storage.Provider = "s3";
    config.Storage.Bucket = "myapp-prod";
    config.Storage.Region = "us-east-1";
    config.Storage.AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY")!;
    config.Storage.SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY")!;
});
```

## 添加 AI 服务

```bash
dotnet add package Sonic.AI
```

```csharp
var aiOptions = new AiServiceOptions();
aiOptions.AddOpenAI(apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);

Sonic.Register<IAiServiceProvider>(new AiServiceProvider(aiOptions));
```

## 部署

```bash
# 同步 Schema
sonic push --schema schema.he --backend mysql://prod-db:3306/myapp

# 部署新版本
sonic deploy --version v1.0.0 --env production

# 灰度发布
sonic publish --version v1.0.0 --canary 10
```

## 下一步

| 文档 | 描述 |
|------|------|
| [API 参考](./api-reference.md) | Sonic 完整 API 文档 |
| [设计哲学](../overview/design-philosophy.md) | 理解核心设计理念 |
| [Hermes 集成](../technical/hermes-integration.md) | 深入了解 Hermes Schema 集成 |
