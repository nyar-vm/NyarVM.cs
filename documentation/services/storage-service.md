# 存储服务

> **归属：Persistent / 文件（File）**
> **接口：** `IStorageProvider`
> **语义：** 中心化、跨实例唯一、数据是真相来源
> **实现状态：🟢 已实现** — `LocalStorageProvider`、`S3StorageProvider`、`AliyunOssStorageProvider`、`TencentCosStorageProvider` 和 `StorageProviderFactory` 均已就绪

## 概述

存储服务是 Persistent 层的文件访问模式。适用于二进制文件——图片、视频、文档等需要上传、下载、CDN 分发的资产。

## 接口定义

```csharp
public interface IStorageProvider
{
    Task<StorageInfo> UploadAsync(string key, byte[] data, string contentType, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    string GetUrl(string key);
}

public sealed class StorageInfo
{
    public required string Key { get; init; }
    public required string Url { get; init; }
}
```

## 使用示例

```csharp
var storage = Sonic.Use<IStorageProvider>();

// 上传
var info = await storage.UploadAsync("avatars/2024/01/abc123.png", imageData, "image/png");

// 获取访问 URL
var url = storage.GetUrl("avatars/2024/01/abc123.png");

// 删除
await storage.DeleteAsync("avatars/2024/01/abc123.png");

// 检查存在
var exists = await storage.ExistsAsync("avatars/2024/01/abc123.png");
```

## 实现选择

| 实现 | 适用场景 | 部署方式 |
|------|---------|---------|
| `LocalStorageProvider` | 开发/测试、单机 | 本地文件系统 |
| `S3StorageProvider` | AWS 生产环境 | 中心化服务 |
| `OssStorageProvider` | 阿里云生产环境 | 中心化服务 |
| `CosStorageProvider` | 腾讯云生产环境 | 中心化服务 |

通过 `StorageProviderFactory` 根据配置自动选择：

```csharp
var options = new StorageOptions
{
    Provider = config["Storage:Provider"] ?? "local",
    LocalPath = config["Storage:LocalPath"],
    S3Bucket = config["Storage:S3Bucket"],
    // ...
};
var factory = new StorageProviderFactory(options);
var storage = factory.Create();
```

## 为什么文件是 Persistent

文件上传后，其他实例必须能访问到（如 CDN 回源、其他服务下载）。这意味着文件存储必须中心化、跨实例唯一。即使用 `LocalStorageProvider`，在单实例场景下也满足 Persistent 语义——数据是真相来源。

## 反模式

| ❌ 错误 | ✅ 正确 |
|---------|---------|
| 文件存在容器内本地目录（多实例） | 使用 S3/OSS/COS 等中心化存储 |
| 文件 URL 硬编码外部域名 | 使用 `GetUrl()` 获取动态 URL |
| 大文件直接存 Database | 大文件用 Storage，Database 只存引用 |

## 相关文档

- [状态管理架构](../systems/state-management.md) — Persistent 三形态全景
- [数据库服务](./database-service.md) — 文件元数据的结构化存储
