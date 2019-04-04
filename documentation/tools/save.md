# sonic save

将 Hermes Schema 持久化写入到数据库。对应运行时 `sonic.Save(key, obj)`。

## 用法

```bash
sonic save [选项]
```

## 选项

| 选项 | 短选项 | 说明 | 默认值 |
|:---|:---|:---|:---|
| `--config` | | 配置文件路径 | `sonic.config.von` |
| `--schema` | `-s` | Schema 文件路径 | 配置文件中的 `schema` |
| `--provider` | `-p` | 数据库提供者 | 配置文件中的 `database` |
| `--connection` | | 数据库连接字符串 | 配置文件中的 `database.test` |
| `--env` | `-e` | 目标环境（test 或 main） | `test` |
| `--force` | `-f` | 跳过确认提示 | `false` |
| `--dry-run` | `-d` | 只生成 SQL 不执行 | `false` |

## 说明

`sonic save` 是开发阶段最常用的命令。它将 Hermes Schema 中定义的数据模型同步到数据库：

1. 读取 `sonic.config.von` 配置
2. 编译 `schemas/` 目录中的 `.hermes` 文件为 SchemaIR
3. 从 SchemaIR 生成 SQL DDL 语句
4. 连接数据库，执行 SQL（建表/加列/索引）
5. 显示执行结果

### 默认 Save 到 test

```bash
sonic save              # Save 到 test 数据库
```

### Save 到 main（谨慎使用）

```bash
sonic save -e main      # Save 到 main 数据库
```

> ⚠️ 通常不需要直接 `sonic save -e main`，应该使用 `sonic publish` 来操作 main 数据库。

## 示例

```bash
sonic save                                      # Save schema 变更到 test 数据库
sonic save --dry-run                            # 只预览 SQL，不实际执行
sonic save --force                              # 跳过确认提示
sonic save --schema custom-path/schemas         # 指定 Schema 路径
```

## 工作流程

```
schemas/
      │
      ▼
HermesCompiler.Compile()
      │
      ▼
SchemaIR
      │
      ▼
SqlDdlGenerator.Generate()
      │
      ▼
SQL DDL 语句
      │
      ▼
连接数据库 → 执行 SQL
```

## 与运行时 API 的对应

```csharp
// 运行时（代码中）
await sonic.Save("user:123", new User { Name = "Alice" });

// 开发时（终端中）
// sonic save
// → 将 Schema 中定义的所有 model 持久化写入到数据库
```

## 与其他命令的关系

| 命令 | 关系 |
|:---|:---|
| `sonic load` | load 从 main 拉取，save 写入 test，方向相反 |
| `sonic diff` | 先 diff 看差异，再 save 执行 |
| `sonic publish` | publish = diff 预览 + 确认 + save 到 main + deploy |
| `sonic dev` | dev 内部自动调用 save |
