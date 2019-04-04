# Sonic CLI 工具

Sonic CLI 提供 8 个核心命令，覆盖从开发到发布的完整工作流。命令动词与 `ISonic` 运行时 API 完全对齐：你运行时怎么写代码，CLI 就怎么下命令。

## 命令概览

| 命令 | 说明 | 对应运行时 API | 目标环境 |
|:---|:---|:---|:---|
| [`sonic dev`](./dev.md) | 开发模式：一键启动编译+生成+DB同步+运行+热重载 | 组合命令 | test |
| [`sonic save`](./save.md) | 持久化写入：Schema → 数据库 | `sonic.Save(key, obj)` | test（默认） |
| [`sonic load`](./load.md) | 持久化读取：数据库 → Schema | `sonic.Load<T>(key)` | main（默认） |
| [`sonic diff`](./diff.md) | Schema 差异预览 | — | test ⇄ main |
| [`sonic publish`](./publish.md) | 发布：test → main（diff 预览 + 确认 + save + deploy） | — | test → main |
| [`sonic deploy`](./deploy.md) | 部署前后端到目标环境 | — | staging / production |
| [`sonic status`](./status.md) | 项目状态仪表盘 | — | 全部 |
| [`sonic env`](./env.md) | 环境管理 | — | 全部 |

## 设计原则

**CLI 动词 = 运行时动词。** `sonic save` 就是运行时的 `sonic.Save()`，`sonic load` 就是运行时的 `sonic.Load()`。不需要翻译，不需要记忆两套词汇。

## 典型工作流

```
开发阶段:
  sonic dev --watch    # 启动开发模式，自动处理一切
  修改 schemas/ 中的 .hermes 文件
  # 自动编译 → 自动生成代码 → 自动同步 test 数据库 → 热重载

查看状态:
  sonic status         # 查看 Schema 与数据库的同步状态
  sonic diff           # 预览 test 和 main 的差异

生产发布:
  sonic publish        # 一站式发布

单独部署:
  sonic deploy         # 只部署应用，不涉及数据库 schema
```

## 环境与命令的关系

```
                    ┌──────────────┐
                    │  开发者本地    │
                    │  schema.her   │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
        sonic load     sonic save   sonic publish
              │            │            │
              ▼            ▼            ▼
         ┌────────┐  ┌────────┐  ┌──────────────┐
         │  main  │  │  test  │  │ test → main  │
         │ 数据库  │  │ 数据库  │  │ + 前后端部署  │
         └────────┘  └────────┘  └──────────────┘
                                      ▲
                                      │
                               sonic deploy
                              （只部署，不改 schema）
```

## 配置文件

所有命令共享 `sonic.config.von` 配置文件：

```von
SonicConfig {
    database: {
        test: "mysql://user:pass@host:3306/myapp_test",
        main: "mysql://user:pass@host:3306/myapp_main",
    },
    schema: "schemas",
    generators: ["csharp", "sql-ddl"],
    output: "generated",
}
```

| 字段 | 说明 |
|:---|:---|
| `database.test` | test 数据库连接 URL（本地开发用） |
| `database.main` | main 数据库连接 URL（生产环境用） |
| `schema` | Hermes Schema 文件路径 |
| `generators` | 代码生成器列表 |
| `output` | 生成代码输出目录 |
