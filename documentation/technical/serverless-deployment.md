# Serverless 部署

> **归属：Layer 4 — 外围能力（运维）**
> **定位：** Serverless 是部署形态，不影响 Persistent/Ephemeral 的语义

Sonic 的后端服务默认部署到 Serverless 平台，前端部署到 CDN。

## 部署架构

```
┌──────────────────────────────────────────────────────────────┐
│                       用户请求                                │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                    CDN (前端静态资源)                          │
│                                                              │
│   /assets/app.20260429.js    ← 版本化 URL                   │
│   /assets/app.20260429.css   ← 版本化 URL                   │
│   /index.html                ← 入口（指向最新版本）           │
└──────────────────────────┬───────────────────────────────────┘
                           │
                           ▼ API 请求
┌──────────────────────────────────────────────────────────────┐
│                  API 网关 / 负载均衡器                         │
│                                                              │
│   X-App-Version: 20260429.1  ← 版本标记                     │
│   流量分配: Blue 90% / Green 10%  ← 灰度控制                │
└──────────────────────────┬───────────────────────────────────┘
                           │
              ┌────────────┼────────────┐
              ▼                         ▼
┌──────────────────────┐   ┌──────────────────────┐
│   Blue (v1.0)        │   │   Green (v2.0)        │
│   Serverless 实例     │   │   Serverless 实例     │
│                      │   │                      │
│   Cache (进程级)      │   │   Cache (进程级)      │
│   ↓                  │   │   ↓                  │
│   Storage (main DB)  │   │   Storage (main DB)  │
└──────────────────────┘   └──────────────────────┘
```

## 部署命令

### sonic deploy

```bash
# 部署到 staging
sonic deploy

# 部署到生产
sonic deploy -e production
```

### sonic publish

```bash
# 一站式发布：schema 同步 + 部署
sonic publish

# 灰度发布
sonic publish --canary 10
```

## 前端部署

### 构建流程

```bash
# Sonic 自动执行
1. npm run build
2. 注入 APP_VERSION 环境变量
3. 上传到 CDN / 对象存储
4. 切换 CDN 版本指针
```

### 版本化策略

| 资源 | URL 格式 | 缓存策略 |
|------|---------|---------|
| JS/CSS | `/assets/app.{version}.js` | 永久缓存 |
| HTML | `/index.html` | 短缓存（5 分钟） |
| 图片/字体 | `/assets/{hash}.png` | 永久缓存 |

### 前端版本检查

```typescript
axios.interceptors.response.use(response => {
  const backendVersion = response.headers['x-app-version'];
  if (backendVersion !== process.env.APP_VERSION) {
    console.warn(`版本不匹配: 前端${process.env.APP_VERSION} ≠ 后端${backendVersion}`);
  }
  return response;
});
```

## 后端部署

### 构建流程

```bash
# Sonic 自动执行
1. dotnet publish -c Release
2. 注入 Version 属性
3. 打包为 Docker 镜像 / Serverless 函数
4. 上传到 Serverless 平台 / K8s
5. 切换流量
```

### 健康检查

```csharp
[HttpGet("/health")]
public IActionResult HealthCheck()
{
    return Ok(new
    {
        Version = _configuration["Version"],
        Status = "Healthy",
        Timestamp = DateTime.UtcNow
    });
}
```

### 响应头

每个 API 响应携带版本号：

```http
HTTP/1.1 200 OK
X-App-Version: 20260429.1
Content-Type: application/json
```

## 环境配置

| 环境 | 数据库 | 前端 | 后端 |
|------|--------|------|------|
| 本地开发 | test | `npm run dev` | `dotnet run` |
| staging | test | CDN (staging) | Serverless (staging) |
| production | main | CDN (production) | Serverless (production) |

## 容器化

Sonic 后端服务支持 Docker 容器化部署：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/ .
ENV ASPNETCORE_URLS=http://+:8080
ENV UseMainDatabase=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
```

关键环境变量：

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `UseMainDatabase` | 使用 main 数据库 | `false`（使用 test） |
| `ASPNETCORE_URLS` | 监听地址 | `http://+:8080` |
| `SonicConfig` | 配置文件路径 | `sonic.config.von` |
