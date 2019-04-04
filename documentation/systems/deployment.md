# Sonic CLI 部署 / 发布模型

## 环境模型

Sonic 有**两个逻辑环境**：test 和 main。它们不是 Docker 容器或服务器名，而是一对**逻辑数据库**的别名。

| 环境 | 用途 | database URL 配置 | 谁操作 |
|:---|:---|:---|:---|
| test | 本地开发测试，随意重写 | `database.test` | `sonic save` |
| main | 线上生产数据库 | `database.main` | `sonic publish`（只写一次，走确认流程） |

## 核心命令

Sonic CLI 的动词与 `ISonic` 运行时 API 完全对齐：

| 命令 | 对应运行时 API | 说明 |
|:---|:---|:---|
| `sonic dev` | — | 开发模式：编译+生成+DB同步+运行+热重载 |
| `sonic save` | `sonic.Save(key, obj)` | Schema → 数据库（持久化写入） |
| `sonic load` | `sonic.Load<T>(key)` | 数据库 → Schema（持久化读取） |
| `sonic diff` | — | Schema 差异预览 |
| `sonic publish` | — | test → main 发布 |
| `sonic deploy` | — | 只部署应用代码 |
| `sonic status` | — | 状态仪表盘 |
| `sonic env` | — | 环境管理 |

## 两个发布维度

### 1. Schema 发布

```bash
sonic save              # Schema 变更写入 test 数据库
sonic diff              # 预览 test vs main 差异
sonic publish           # 发布 Schema 到 main + 部署
```

### 2. 应用部署

```bash
sonic deploy            # 只部署前后端代码（不含 Schema 变更）
```

## 典型工作流

### 日常开发

```bash
sonic dev --watch       # 启动，3 秒后开始写代码
# 修改 hermes schema, sonic 自动同步
```

### 发布日

```bash
sonic diff              # 「生产多了什么？」
sonic publish           # 「确认，发布。」
```

### 新成员加入

```bash
sonic load              # 从 main 取回生产环境 schema
sonic dev               # 开始本地开发
```

## 安全模型

| 操作 | 安全机制 |
|:---|:---|
| `sonic save` | 只写 test，不写 main |
| `sonic publish` | 必须先跑 diff 并人工确认 |
| `sonic load` | 从 main 只读 |
| `sonic env use main` | 警告提示，save 仍不写 main |
