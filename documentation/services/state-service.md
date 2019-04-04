# 状态服务

> **归属：Ephemeral / 本地盘（Local）**
> **接口：** `IStateManager`
> **语义：** sidecar、随实例生灭、数据是加速副本（但容器重启后保留）
> **实现状态：🟢 已实现** — `IStateManager` + `FileStateManager`（本地 JSON 文件，零外部依赖）已可用

## 概述

状态服务是 Ephemeral 层的本地盘访问模式。适用于需要跨进程重启保留、但不需要跨实例共享的本地状态——长时间运行的计算中间态、本地配置缓存等。

## 为什么 State 是 Ephemeral

State 使用容器内本地 JSON 文件落盘，容器重启后数据还在。但**容器销毁后数据就没了**——这是 Ephemeral 的核心语义：随实例生灭。

State 和 Cache 的区别只是实现策略：
- **Cache（内存）：** 最快，进程重启即丢失
- **State（本地盘）：** 稍慢，容器重启保留，容器销毁丢失

两者都是 Ephemeral，都不可被其他实例依赖。

## 接口定义

```csharp
public interface IStateManager
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
```

## 使用示例

```csharp
var state = Sonic.Use<IStateManager>();

// 保存计算中间态
await state.SetAsync("compute:progress", new Progress { Percent = 45, Step = "training" });

// 读取（容器重启后仍在）
var progress = await state.GetAsync<Progress>("compute:progress");

// 清理
await state.DeleteAsync("compute:progress");
```

## 何时用 State 而非 Cache

| 场景 | 选择 | 原因 |
|------|------|------|
| 查询结果缓存 | Cache | 丢失可接受，速度优先 |
| 会话数据 | Cache | 丢失可接受，用户重新登录即可 |
| 长时间计算中间态 | State | 进程重启后需要恢复 |
| 本地配置缓存 | State | 避免每次启动都从 Persistent 读取 |
| 关键业务数据 | **都不行** | 必须用 `IDatabaseService`（Persistent） |

## 反模式

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| 用 State 存关键业务数据 | 关键数据走 `IDatabaseService` |
| 其他实例依赖 State 中的数据 | 跨实例数据走 Persistent |
| State 和 Cache 混用 | 明确选择：需要重启保留用 State，否则用 Cache |

## 相关文档

- [状态管理架构](../systems/state-management.md) — Ephemeral 两形态全景
- [缓存服务](./cache-service.md) — Ephemeral 的另一种实现策略
