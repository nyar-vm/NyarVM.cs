# Hermes Schema 集成

Sonic 基于 Hermes Schema 引擎实现 Schema-driven 开发。Hermes 令 Sonic 能编译 `.hermes` 为底层运行时数据结构（SchemaIR），并驱动代码生成和迁移。

## Sonic 如何使用 Hermes

```
sonic dev / save / publish
      │
      ▼
Sonic CLI
      │
      ▼
Hermes.Compiler.HermesCompiler      # .her → SchemaIR
      │
      ▼
Hermes.Generator.GeneratorDispatcher # SchemaIR → C# / TS / Go / SQL etc.
      │
      ▼
SchemaIR
      │
      ├─→ HermesPlugin.SqlDdl        # DDL 生成
      ├─→ Sonic.CLI                  # 连接数据库，执行 DDL
      └─→ SonicCodeGenerator         # ISonic API 包装器生成
```

## Hermes Schema 文件示例

```hermes
namespace! app {
    model {
        @@ uuid user_id,
        utf8 display_name,
        datetime created_at,
    }
}

namespace log {
    model {
        @@ uuid event_id,
        utf8 event_type,
        @ utf8 source,
        json? detail,
    }
}
```

## Schema 到 Sonic 的映射

HermesNamespace！/ model → Sonic Storage → Sonic 数据库表：

```
Hermes namespacce！app → Sonic Storage "app" → 数据库 myapp_test（或 myapp_main）
Hermes class User       → Sonic Entity       → SQL table users
```

## 代码生成

Sonic 通过 Hermes Generator 驱动多语言生成：

```bash
sonic generate
# → 内部调用 Hermes.Generator 及其插件：
#   - Hermes.Plugin.CSharp     → C# record / class
#   - Hermes.Plugin.TypeScript → TS interface
#   - Hermes.Plugin.SqlDdl     → MySQL / PostgreSQL DDL
```

## 迁移
Sonic 内部使用 Hermes Migrator：

```bash
sonic diff
# → 内部调用 Hermes.Migrator.SchemaDiffer
```

`sonic diff` 是纯分析；`sonic save` 执行差异化 SQL；`sonic publish` 将两者连在一起并加入确认步骤。

## 配置

Sonic 配置文件引用 Hermes Schema 路径：

```von
SonicConfig {
    database: {
        test: "mysql://...",
        main: "mysql://...",
    },
    schema: "schemas",      # Hermes .her 文件路径
    generators: ["csharp", "sql-ddl"],
}
```
