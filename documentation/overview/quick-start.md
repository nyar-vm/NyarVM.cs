# Sonic 快速开始

## 安装

```bash
dotnet tool install -g Nyar.Sonic.CLI
```

## 创建项目

```bash
sonic init my-app
cd my-app
```

生成的项目结构：

```
my-app/
├── sonic.config.von       # Sonic 配置（数据库连接等）
├── schemas/
│   ├── app.hermes         # 业务模型定义
│   └── config.hermes      # 配置/环境
└── generated/             # 生成的代码（C# 实体等）
```

## 定义业务模型

编辑 `schemas/app.hermes`：

```hermes
namespace! app {
    class User {
        @@ uuid user_id,
        utf8 display_name,
        utf8 email,
        datetime created_at,
    }

    class Artwork {
        @@ uuid artwork_id,
        utf8 title,
        @ &User owner,
        json? tags,
    }
}
```

## 启动开发模式

```bash
sonic dev --watch
```

一条命令完成：

- ✅ 编译 `.hermes` → SchemaIR
- ✅ 生成 C# 实体代码
- ✅ 创建数据库表（`users`, `artworks`）
- ✅ 启动本地应用
- ✅ 监听文件变化，自动热重载

终端输出：

```
🔄 Sonic Dev 启动（test 环境）

✅ Schema 编译完成 — 2 个 model
✅ 代码已生成 — generated/
✅ 数据库已同步 — 2 张表创建成功
🚀 应用已启动 — http://localhost:8080
👀 正在监听 Schema 文件变化...
```

## 使用 Sonic API

生成的代码提供了完整的 `ISonic` 接口：

```csharp
var sonic = app.Services.GetRequiredService<ISonic>();

// 持久化：存到数据库
var user = new User { DisplayName = "Alice", Email = "alice@example.com" };
await sonic.Save($"user:{user.UserId}", user);

// 加载：从数据库取回
var loaded = await sonic.Load<User>($"user:{user.UserId}");

// 查询
var artworks = await sonic.Query<Artwork>()
    .Filter(a => a.Title.Contains("Mona"))
    .Execute();

// 临时存储（缓存）
await sonic.Put("search:popular", popularArtworks, TimeSpan.FromMinutes(5));

// 发送消息
await sonic.Emit("artwork.created", newArtwork);

// 订阅消息
await sonic.On<ArtworkCreated>("artwork.created", msg =>
{
    Console.WriteLine($"新作品: {msg.Title}");
});
```

## 发布

查看差异并发布：

```bash
sonic diff          # 预览 test vs main 的 SQL 差异
sonic publish       # 确认后发布
```

## 下一步

- [设计哲学](./design-philosophy.md) — Persistent / Ephemeral 二分
- [统一 API 参考](./unified-api.md) — 完整 API 文档
- [Schema-driven 开发](../systems/schema-driven.md) — Schema 即真相源
- [CLI 工具](../tools/index.md) — 命令详解
