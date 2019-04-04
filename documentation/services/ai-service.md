# AI 服务

> **归属：Layer 4 / 外围能力**
> **接口：** `IAiService`
> **定位：** 不属于状态管理层，是建立在状态管理之上的业务能力
> **实现状态：🟢 已实现** — `OpenAiService`、`AzureOpenAiService`、`BaiduWenxinService`、`AlibabaTongyiService` 均已就绪

## 概述

AI 服务是 Sonic 的外围能力，封装了 LLM（大语言模型）的调用。它不属于状态管理层——它消费状态（从 Database 读取上下文），产出状态（将生成结果写入 Database/Storage），但本身不管理状态。

## 在架构中的位置

```
Layer 4: 外围能力
┌─────────────────────────────────────────┐
│  AI 服务 / Schema 驱动 / CLI / 灰度发布  │
└──────────────────┬──────────────────────┘
                   │ 消费状态、产出状态
┌──────────────────┴──────────────────────┐
│  Layer 1: 状态管理                       │
│  Persistent / Ephemeral                 │
└─────────────────────────────────────────┘
```

AI 服务是一个典型的 Layer 4 消费者：
- 从 `IDatabaseService` 读取对话历史（Persistent）
- 从 `ICacheService` 读取用户会话（Ephemeral 加速）
- 将生成结果写入 `IDatabaseService`（Persistent）
- 将生成的图片上传到 `IStorageProvider`（Persistent）

## 接口定义

```csharp
public interface IAiService
{
    Task<string> CompleteAsync(string prompt, AiOptions? options = null, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamCompleteAsync(string prompt, AiOptions? options = null, CancellationToken ct = default);
}
```

## 使用示例

```csharp
var ai = Sonic.Use<IAiService>();

// 同步调用
var result = await ai.CompleteAsync("写一首关于春天的诗");

// 流式调用
await foreach (var chunk in ai.StreamCompleteAsync("写一首关于春天的诗"))
{
    Console.Write(chunk);
}
```

## 相关文档

- [状态管理架构](../systems/state-management.md) — AI 服务消费的底层状态
- [数据库服务](./database-service.md) — 对话历史的持久化
