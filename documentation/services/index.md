# 服务参考

> **全局实现状态：🟡 部分实现** — 核心入口 `ISonic` 和所有接口定义均已就绪；Storage 层已全量实现；Database/Cache/Stream 的生产级后端（SQL/Redis/Kafka）尚未实现。

Sonic 的所有服务按状态管理层次组织。理解每个服务的归属，是正确使用 Sonic 的前提。

> **核心原则：** Persistent/Ephemeral 是用法，不是技术。详见 [设计哲学](../overview/design-philosophy.md)。

## Layer 1：状态管理

### Persistent（持久化）— 中心化，跨实例唯一，数据是真相来源

| 服务 | 接口 | 访问模式 | 说明 |
|------|------|---------|------|
| [数据库服务](./database-service.md) | `IDatabaseService` | 结构化（Table） | 关系型数据 CRUD + 事务 |
| [存储服务](./storage-service.md) | `IStorageProvider` | 文件（File） | 二进制文件上传/下载/CDN |
| [流服务](./stream-service.md) | `IStreamService` | 瞬态（Queue） | 发布-订阅事件流 |

### Ephemeral（临时状态）— sidecar，随实例生灭，数据是加速副本

| 服务 | 接口 | 实现策略 | 说明 |
|------|------|---------|------|
| [缓存服务](./cache-service.md) | `ICacheService` | 内存（Memory） | 极速读取，重启即丢失 |
| [状态服务](./state-service.md) | `IStateManager` | 本地盘（Local） | 容器内落盘，重启保留 |

## Layer 4：外围能力

| 服务 | 接口 | 说明 |
|------|------|------|
| [AI 服务](./ai-service.md) | `IAiService` | LLM 调用封装，不属于状态管理层 |

## 快速决策树

```
你的数据需要跨实例共享吗？
├── 是 → Persistent
│   ├── 需要查询/关联/事务？ → IDatabaseService（结构化）
│   ├── 是二进制文件？ → IStorageProvider（文件）
│   └── 是事件/消息？ → IStreamService（瞬态）
└── 否 → Ephemeral
    ├── 进程重启后需要保留？ → IStateManager（本地盘）
    └── 不需要，越快越好？ → ICacheService（内存）
```

## 反模式警告

| ❌ 错误用法 | ✅ 正确做法 |
|------------|-----------|
| 跨实例共享 `ICacheService` | 跨实例数据走 `IDatabaseService` 或 `IStorageProvider` |
| 先写 `ICacheService` 再同步到 `IDatabaseService` | 先写 `IDatabaseService` 再更新 `ICacheService` |
| 用 `IStateManager` 存关键业务数据 | 关键数据走 `IDatabaseService` |
| 业务逻辑依赖 `ICacheService` 中的数据 | `ICacheService` 只是加速，丢失不影响正确性 |
