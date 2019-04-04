# Sonic 文档

## 一句话定义

> **后端即状态管理。** Sonic 是以状态管理为底层、Persistent/Ephemeral 二分为原则的后端基础设施。

## 核心架构

```
┌─────────────────────────────────────────────────────────────────┐
│  Layer 4: 外围能力                                               │
│  AI 服务 / Schema 驱动 / CLI 工具 / 灰度发布 / 部署运维          │
└──────────────────────────┬──────────────────────────────────────┘
                           │ 依赖
┌──────────────────────────┴──────────────────────────────────────┐
│  Layer 3: 上层建筑                                               │
│  中间件 / 洋葱模型 / 微服务 / API 网关 / 认证授权 / 路由          │
└──────────────────────────┬──────────────────────────────────────┘
                           │ 依赖
┌──────────────────────────┴──────────────────────────────────────┐
│  Layer 2: 服务控制反转                                           │
│  Sonic.Use<T>() / Schema 自动注册 / Scope 生命周期               │
└──────────────────────────┬──────────────────────────────────────┘
                           │ 依赖
┌──────────────────────────┴──────────────────────────────────────┐
│  Layer 1: 状态管理（底层，不可动摇）                               │
│                                                                 │
│  ┌─────────────────────────┐  ┌─────────────────────────┐      │
│  │  Persistent（持久化）    │  │  Ephemeral（临时状态）   │      │
│  │                         │  │                         │      │
│  │  中心化，跨实例唯一      │  │  sidecar，随实例生灭     │      │
│  │  数据是真相来源          │  │  数据是加速副本          │      │
│  │                         │  │                         │      │
│  │  ┌───────────────────┐  │  │  ┌───────────────────┐  │      │
│  │  │ 结构化（Table）    │  │  │  │ 内存（Memory）     │  │      │
│  │  │ IDatabaseService  │  │  │  │ ICacheService      │  │      │
│  │  └───────────────────┘  │  │  └───────────────────┘  │      │
│  │  ┌───────────────────┐  │  │  ┌───────────────────┐  │      │
│  │  │ 文件（File）       │  │  │  │ 本地盘（Local）    │  │      │
│  │  │ IStorageProvider  │  │  │  │ IStateManager      │  │      │
│  │  └───────────────────┘  │  │  └───────────────────┘  │      │
│  │  ┌───────────────────┐  │  │                         │      │
│  │  │ 瞬态（Queue）      │  │  │                         │      │
│  │  │ IStreamService    │  │  │                         │      │
│  │  └───────────────────┘  │  │                         │      │
│  └─────────────────────────┘  └─────────────────────────┘      │
│                                                                 │
│  ⚠️ Persistent/Ephemeral 是用法，不是技术实现                     │
│  同一个 Redis：中心化用 = Persistent，sidecar 加载 = Ephemeral    │
└─────────────────────────────────────────────────────────────────┘
```

**基础不牢，地动山摇。** 理解 Layer 1，就理解了 Sonic 的一切。

## 文档导航

### 必读：底层原则

| 文档 | 描述 |
|------|------|
| [什么是后端](./introduction.md) | 后端即状态管理——Sonic 的核心定义 |
| [设计哲学](./design-philosophy.md) | Persistent/Ephemeral 二分原则，与技术实现无关 |
| [快速开始](./quick-start.md) | 从零搭建一个 Sonic 后端服务 |

### 核心系统

| 文档 | 描述 |
|------|------|
| [状态管理架构](../systems/state-management.md) | Layer 1 详解：Persistent 三形态 + Ephemeral 两形态 |
| [服务控制反转](../systems/service-inversion.md) | Layer 2：`Sonic.Use<T>()` 与 Schema 自动注册 |
| [中间件与上层建筑](../systems/middleware.md) | Layer 3：洋葱模型、微服务、API 网关等 |
| [Schema 驱动](../systems/schema-driven.md) | Hermes Schema 如何驱动 Sonic 全层 |
| [部署与发布](../systems/deployment.md) | test/main 双环境 + 四条命令 |

### 服务参考

| 文档 | 描述 | 归属 |
|------|------|------|
| [数据库服务](../services/database-service.md) | `IDatabaseService` — 结构化持久化 | Persistent / Table |
| [存储服务](../services/storage-service.md) | `IStorageProvider` — 文件持久化 | Persistent / File |
| [流服务](../services/stream-service.md) | `IStreamService` — 瞬态持久化 | Persistent / Queue |
| [缓存服务](../services/cache-service.md) | `ICacheService` — 内存临时状态 | Ephemeral / Memory |
| [状态服务](../services/state-service.md) | `IStateManager` — 本地盘临时状态 | Ephemeral / Local |
| [AI 服务](../services/ai-service.md) | `IAiService` — 外围能力 | Layer 4 |

### CLI 工具

| 文档 | 描述 |
|------|------|
| [CLI 概览](../tools/index.md) | 四条命令覆盖开发到发布 |

### 技术细节

| 文档 | 描述 |
|------|------|
| [Hermes 集成](../technical/hermes-integration.md) | Schema 编译管线与代码生成 |
| [Serverless 部署](../technical/serverless-deployment.md) | 容器化与 Serverless 部署 |
| [灰度发布](../technical/grayscale-release.md) | 蓝绿部署与灰度策略 |

## 版本信息

| 项目 | 版本 | 状态 |
|------|------|------|
| Sonic | 0.2.0 | 预研版 |
