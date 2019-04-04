# API 参考

> **层次归属：** 本文档中的 API 按状态管理层次组织。详见 [服务参考](../services/index.md)。

## Sonic 核心类

### Sonic

静态服务定位器，提供服务注册和获取功能。

| 方法 | 说明 |
|------|------|
| `Initialize()` | 初始化 Sonic |
| `Register<TService>(TService instance)` | 注册服务实例 |
| `Register<TService>(Func<TService> factory)` | 注册服务工厂 |
| `Register<TService, TImplementation>()` | 注册服务实现 |
| `Register<TService>(string name, TService instance)` | 注册命名服务实例 |
| `Register<TService>(string name, Func<TService> factory)` | 注册命名服务工厂 |
| `Use<TService>()` | 获取服务实例（未注册时抛异常） |
| `Use<TService>(string name)` | 获取命名服务实例 |
| `TryUse<TService>()` | 安全获取服务实例（未注册时返回 null） |
| `IsRegistered<TService>()` | 检查服务是否已注册 |
| `IsRegistered<TService>(string name)` | 检查命名服务是否已注册 |
| `Unregister<TService>()` | 注销服务 |
| `Unregister<TService>(string name)` | 注销命名服务 |
| `Clear()` | 清除所有注册 |

### SonicBootstrap

一键配置入口。

| 方法 | 说明 |
|------|------|
| `Configure(Action<SonicConfig> configure)` | 配置 Sonic 服务 |

### SonicConfig

| 属性 | 类型 | 说明 |
|------|------|------|
| `Storage` | `StorageConfig` | 存储配置 |
| `Cache` | `CacheConfig` | 缓存配置 |

### StorageConfig

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Provider` | `string` | `"local"` | 存储提供者 |
| `Bucket` | `string` | `""` | 存储桶 |
| `Region` | `string` | `""` | 地域 |
| `Endpoint` | `string` | `""` | 自定义端点 |
| `AccessKey` | `string` | `""` | 访问密钥 |
| `SecretKey` | `string` | `""` | 秘密密钥 |
| `BasePath` | `string` | `"./storage"` | 本地存储路径 |

### CacheConfig

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Provider` | `string` | `"memory"` | 缓存提供者 |
| `ConnectionString` | `string` | `""` | Redis 连接字符串 |
| `DefaultExpireMinutes` | `int` | `60` | 默认过期时间 |

## 缓存服务

### ICacheService

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `GetAsync<T>(string key, CancellationToken ct)` | `Task<T?>` | 获取缓存值 |
| `SetAsync<T>(string key, T value, TimeSpan? expire, CancellationToken ct)` | `Task` | 设置缓存值 |
| `DeleteAsync(string key, CancellationToken ct)` | `Task<bool>` | 删除缓存 |
| `ExistsAsync(string key, CancellationToken ct)` | `Task<bool>` | 检查缓存是否存在 |
| `GetKeysAsync(string pattern, CancellationToken ct)` | `Task<List<string>>` | 模式匹配获取 key |
| `ClearAsync(CancellationToken ct)` | `Task` | 清除所有缓存 |
| `ExecuteWithLockAsync<T>(string lockKey, Func<Task<T>> action, TimeSpan lockTimeout, CancellationToken ct)` | `Task<T>` | 分布式锁执行 |

### CacheOptions

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ConnectionString` | `string?` | `null` | Redis 连接字符串 |
| `DefaultExpireMinutes` | `int` | `60` | 默认过期时间 |
| `MaxMemoryItems` | `int` | `10000` | 内存缓存最大条目数 |
| `KeyPrefix` | `string` | `"sonic:"` | 缓存 key 前缀 |

## 存储服务

### IStorageProvider

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `UploadAsync(string key, Stream data, string? contentType, CancellationToken ct)` | `Task<StorageFileInfo>` | 上传文件（流） |
| `UploadAsync(string key, byte[] data, string? contentType, CancellationToken ct)` | `Task<StorageFileInfo>` | 上传文件（字节） |
| `DownloadAsync(string key, CancellationToken ct)` | `Task<Stream>` | 下载文件（流） |
| `DownloadBytesAsync(string key, CancellationToken ct)` | `Task<byte[]>` | 下载文件（字节） |
| `ExistsAsync(string key, CancellationToken ct)` | `Task<bool>` | 检查文件是否存在 |
| `DeleteAsync(string key, CancellationToken ct)` | `Task` | 删除文件 |
| `GetFileInfoAsync(string key, CancellationToken ct)` | `Task<StorageFileInfo?>` | 获取文件信息 |
| `GetUrl(string key, TimeSpan? expire)` | `string` | 获取访问 URL |
| `ListAsync(string? prefix, CancellationToken ct)` | `IAsyncEnumerable<string>` | 列出文件 |
| `CopyAsync(string sourceKey, string destKey, CancellationToken ct)` | `Task<StorageFileInfo>` | 复制文件 |

### StorageProviderType

| 值 | 说明 |
|----|------|
| `Local` | 本地文件系统 |
| `S3` | AWS S3 |
| `TencentCos` | 腾讯云 COS |
| `AliyunOss` | 阿里云 OSS |

### StorageFileInfo

| 属性 | 类型 | 说明 |
|------|------|------|
| `Key` | `string` | 文件 key |
| `Url` | `string` | 访问 URL |
| `Size` | `long` | 文件大小 |
| `ContentType` | `string?` | 内容类型 |
| `LastModified` | `DateTimeOffset?` | 最后修改时间 |
| `ETag` | `string` | ETag |

## 状态服务

### IStateManager

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `GetAsync<T>(string key, string? partitionKey, CancellationToken ct)` | `Task<T?>` | 获取状态值 |
| `SetAsync<T>(string key, T value, string? partitionKey, TimeSpan? expire, CancellationToken ct)` | `Task` | 设置状态值 |
| `DeleteAsync(string key, string? partitionKey, CancellationToken ct)` | `Task<bool>` | 删除状态 |
| `ExistsAsync(string key, string? partitionKey, CancellationToken ct)` | `Task<bool>` | 检查状态是否存在 |
| `GetKeysAsync(string pattern, string? partitionKey, CancellationToken ct)` | `Task<List<string>>` | 模式匹配获取 key |
| `CountAsync(string? partitionKey, CancellationToken ct)` | `Task<long>` | 计数 |
| `ClearAsync(string? partitionKey, CancellationToken ct)` | `Task` | 清除状态 |
| `ExecuteWithLockAsync<T>(string lockKey, Func<Task<T>> action, TimeSpan lockTimeout, CancellationToken ct)` | `Task<T>` | 分布式锁执行 |

## 扩展方法

### SonicRepositoryExtensions

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `GetByIdAsync<T>(this IStateManager, object id)` | `Task<T?>` | 按 ID 获取实体 |
| `SaveAsync<T>(this IStateManager, T entity, object id)` | `Task<T>` | 保存实体 |
| `DeleteAsync<T>(this IStateManager, object id)` | `Task` | 按 ID 删除实体 |
| `GetAllAsync<T>(this IStateManager)` | `Task<List<T>>` | 获取所有实体 |

### StorageExtensions

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `UseLocalStorage(this StorageOptions)` | `void` | 使用本地存储 |
| `UseS3Storage(this StorageOptions)` | `void` | 使用 S3 存储 |
| `UseAliyunOssStorage(this StorageOptions)` | `void` | 使用阿里云 OSS |
| `UseTencentCosStorage(this StorageOptions)` | `void` | 使用腾讯云 COS |
| `UploadStringAsync(this IStorageProvider, string key, string content)` | `Task<StorageFileInfo>` | 上传字符串 |
| `DownloadStringAsync(this IStorageProvider, string key)` | `Task<string>` | 下载字符串 |
