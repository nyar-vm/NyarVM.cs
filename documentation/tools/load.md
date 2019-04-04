# sonic load

从数据库读取当前表结构，转换为 Hermes Schema 文件。对应运行时 `sonic.Load<T>(key)`。

## 用法

```bash
sonic load [选项]
```

## 选项

| 选项 | 短选项 | 说明 | 默认值 |
|:---|:---|:---|:---|
| `--config` | | 配置文件路径 | `sonic.config.von` |
| `--provider` | `-p` | 数据库提供者 | 配置文件中的 `database` |
| `--connection` | | 数据库连接字符串 | 配置文件中的 `database.main` |
| `--env` | `-e` | 来源环境（test 或 main） | `main` |
| `--output` | `-o` | 输出目录路径 | `schemas/` |

## 说明

`sonic load` 从数据库读取当前表结构，转换为 Hermes Schema 格式，生成本地文件。

### 默认从 main Load

```bash
sonic load              # 从 main 数据库 Load schema
```

### 从 test Load

```bash
sonic load -e test      # 从 test 数据库 Load schema
```

### 典型使用场景

1. **新加入项目**：从 main Load 最新 schema，了解当前数据模型
2. **同步生产**：本地 schema 与 main 不一致时，Load 最新版本
3. **逆向工程**：已有数据库但没有 schema 文件时，生成 schema

## 示例

```bash
sonic load                                      # 从 main 数据库 Load schema
sonic load -e test                              # 从 test 数据库 Load
sonic load -o custom-output/                    # 指定输出目录
```

## 工作流程

```
数据库
      │
      ▼
读取 INFORMATION_SCHEMA / sqlite_master
      │
      ▼
表结构元数据（表名/列名/类型/索引）
      │
      ▼
类型映射（SQL Type → Hermes Type）
      │
      ▼
生成 Hermes Schema 文本
      │
      ▼
写入 schemas/ 目录
```

### 类型映射

| SQL 类型 | Hermes 类型 |
|:---|:---|
| INTEGER / INT | `i32` |
| BIGINT | `i64` |
| REAL / FLOAT | `f32` |
| DOUBLE / DOUBLE PRECISION | `f64` |
| BOOLEAN | `bool` |
| TEXT / VARCHAR / CHAR | `utf8` |
| UUID | `uuid` |
| BLOB | `list<u8>` |
| DATETIME / TIMESTAMP / DATE | `datetime` |

## 与运行时 API 的对应

```csharp
// 运行时（代码中）
var user = await sonic.Load<User>("user:123");

// 开发时（终端中）
// sonic load
// → 从数据库 Load 表结构，转换为 Schema 定义
```

## 与其他命令的关系

| 命令 | 关系 |
|:---|:---|
| `sonic save` | load 从数据库 Load，save 写入数据库，方向相反 |
| `sonic diff` | diff 比较两个数据库的差异 |
| `sonic status` | status 显示 Schema 与数据库的同步状态 |
