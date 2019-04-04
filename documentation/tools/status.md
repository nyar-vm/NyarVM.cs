# sonic status

项目状态仪表盘。将 Schema 与所有数据库的实际状态可视化对比，一眼看出差异。

## 用法

```bash
sonic status [选项]
```

## 选项

| 选项 | 短选项 | 说明 | 默认值 |
|:---|:---|:---|:---|
| `--config` | | 配置文件路径 | `sonic.config.von` |
| `--env` | `-e` | 查看指定环境 | 全部 |

## 说明

`sonic status` 读取 Schema 定义，连接配置中的数据库，逐项对比差异。

## 输出示例

```
 Sonic 项目状态

 版本:  20260501.3
 环境:  test（当前）

 Persistent（持久层）:
 ┌──────────┬──────────┬──────────┬──────────┬───────────┐
 │ 存储      │ 关联 DB   │ Schema   │ 实际 DB   │ 状态       │
 ├──────────┼──────────┼──────────┼──────────┼───────────┤
 │ app       │ app_test │ 5 model  │ 5 table  │ ✅ 同步    │
 │ log       │ log_test │ 2 model  │ 1 table  │ ⚠️ 落后    │
 │ file      │ (S3)     │ 3 bucket │ 3 bucket │ ✅ 同步    │
 └──────────┴──────────┴──────────┴──────────┴───────────┘

 Ephemeral（临时层）:
 ┌──────────┬─────────┬──────────┐
 │ 缓存      │ 后端     │ 状态     │
 ├──────────┼─────────┼──────────┤
 │ session  │ Redis   │ ✅ 连接  │
 │ query    │ Memory  │ ✅       │
 └──────────┴─────────┴──────────┘

 下一步建议: sonic save （log 存储有 1 个 model 未同步到数据库）

 main 数据库状态:
   与 test 差异: 0 张表，2 个字段 — sonic diff 查看详情
```

## 示例

```bash
sonic status             # 查看全部环境状态
sonic status -e test     # 只看 test 环境
```

## 与其他命令的关系

| 命令 | 关系 |
|:---|:---|
| `sonic diff` | status 告诉你有差异，diff 展示差异细节 |
| `sonic save` | status 建议你 save 时，直接 save 即可同步 |
