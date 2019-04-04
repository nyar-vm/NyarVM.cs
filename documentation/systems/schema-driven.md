# Schema-driven 的日常体验

Schema 是 Sonic 的**唯一真相源**。不是代码优先，不是数据库优先，不是微服务优先，是 Schema 优先。

## 定义即一切

```
开发者定义 Schema
      │
      ├─→ 数据库表自动创建/维护
      ├─→ C# / TS / Go 实体代码自动生成
      ├─→ HTTP / gRPC / WS 接口自动暴露
      └─→ 迁移 SQL 自动生成
```

## 日常体验

### 添加字段

```
1. 改动 .her 文件，添加字段
       avatar_url: utf8?,

2. sonic dev --watch 自动：
       ✅ C# 实体 User 新增属性 AvatarUrl
       ✅ TS 接口 IUser 新增字段 avatarUrl
       ✅ ALTER TABLE user ADD COLUMN avatar_url
```

### 创建新模型

```
1. 新增 class Artwork { ... }

2. sonic save ：
       ✅ CREATE TABLE artworks (...)
       ✅ C# record Artwork { ... } 已生成
       ✅ TS interface IArtwork 已生成
```

### 删除废弃字段

```
1. 在 .her 中注释或删除 legacy_field

2. sonic diff：
       [~] Item 即将删除列 legacy_field  ← 兼容性警告

3. sonic publish：
       确认后执行 ALTER TABLE item DROP COLUMN legacy_field
```

## 与 Prisma / Entity Framework 的区别

| 工具 | Schema 在哪里 | 语言绑定 | 多存储 | 发布流程 |
|:---|:---|:---|:---|:---|
| Prisma | `schema.prisma` | Node.js | SQL only | `prisma migrate` |
| EF Core | C# 代码 | .NET | SQL only | `dotnet ef migration` |
| **Sonic** | **`.hermes`（语言无关）** | **C# / TS / Go / Rust / Java** | **SQL / Redis / S3 / OSS / Cos / Kafka / RabbitMQ** | **sonic dev → save → diff → publish** |

Sonic 的独特优势：同一个 `.hermes` 文件生成所有语言的实体，且一个模型可同时映射到 SQL 表和 Redis 键。

## Hermes 作为 Schema 引擎

Sonic 使用 Hermes 实现 Schema-driven 能力。Hermes 管 Schema 编译和代码生成；Sonic 管数据库连接和运行时。详见 [Hermes 集成](../technical/hermes-integration.md)。

对于纯 Schema 需求（只需代码生成），直接使用 [Hermes CLI](https://example.com/hermes)。
