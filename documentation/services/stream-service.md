# 流服务

> **归属：Persistent / 瞬态（Queue）**
> **接口：** `IStreamService`
> **语义：** 中心化、跨实例唯一、写入即承诺
> **实现状态：🟡 部分实现** — `MemoryStreamService`（进程内 `Channel<T>`，开发/测试可用）已实现，Kafka sidecar 尚未实现

## 概述

流服务是 Persistent 层的瞬态访问模式。适用于发布-订阅、消费即消失的事件流——订单事件、社交通知、任务状态变更等。

## 为什么 Queue 是 Persistent

消息一旦发布，即使没有消费者在线，消息也不会丢失。这是 Persistent 的核心语义——**写入即承诺**。

消息被消费后消失，这是"瞬态"的含义——但消失是**业务语义**（已处理），不是**技术语义**（缓存淘汰）。这与 Cache 的丢失有本质区别：Cache 丢失是意外，Queue 消费是预期。

## 接口定义

```csharp
public interface IStreamService
{
    Task PublishAsync<T>(string topic, T payload, CancellationToken ct = default);
    IAsyncEnumerable<T> SubscribeAsync<T>(string topic, CancellationToken ct = default);
}
```

## 使用示例

```csharp
var stream = Sonic.Use<IStreamService>();

// 发布事件
await stream.PublishAsync("order.created", new OrderEvent { OrderId = 123 });

// 订阅事件
await foreach (var evt in stream.SubscribeAsync<OrderEvent>("order.created"))
{
    await ProcessOrder(evt);
}
```

## 实现选择

| 实现 | 状态 | 适用场景 | 部署方式 |
|------|------|---------|---------|
| `MemoryStreamService` | 🟢 已实现 | 开发/测试 | 进程内 `Channel<T>` |
| `KafkaStreamService` | 🔴 未实现 | 生产环境 | 中心化集群 |

## 反模式

| ❌ 错误 | ✅ 正确 |
|------|------|------|
| 用 Database 轮询代替 Stream | 使用 Stream 的发布-订阅模式 |
| 用 Cache 做消息传递 | Cache 是 Ephemeral，丢失不可靠 |
| 消费者不处理异常导致消息丢失 | 消费者必须 try-catch，失败时重试或进入死信队列 |

## 相关文档

- [状态管理架构](../systems/state-management.md) — Persistent 三形态全景
- [Hermes 集成](../technical/hermes-integration.md) — Schema 驱动的事件定义
