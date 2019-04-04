# sonic diff

对比两个数据库的 Schema 差异，预览即将执行的 SQL 变更。

## 用法

```bash
sonic diff [选项]
```

## 选项

| 选项 | 短选项 | 说明 | 默认值 |
|:---|:---|:---|:---|
| `--from` | | 源环境 | `test` |
| `--to` | | 目标环境 | `main` |
| `--config` | | 配置文件路径 | `sonic.config.von` |
| `--schema` | `-s` | Schema 文件路径（对比 Schema 文件与数据库） | 配置文件中的 `schema` |

## 说明

`sonic diff` 调用 Hermes Migrator 的 `SchemaDiffer`，对比两端 Schema 并显示差异：

1. 编译 Schema 为 SchemaIR
2. 读取 from 和 to 两端数据库/文件的 Schema
3. 计算差异
4. 生成迁移 SQL 预览
5. 标注兼容性风险

## 输出示例

```
$ sonic diff --from test --to main

📊 Schema 差异分析 (test ⇄ main):

 app 存储:
   [+] ItemV2          (新增表)        — ✅ 兼容，无风险
   [~] User            (修改表)
       + bio: utf8?                    — ✅ 兼容
   [~] Item            (修改表)
       - legacy_tag: utf8              — ⚠️ 不兼容！删除列会丢失数据

 即将在 main 数据库执行的变更:
   CREATE TABLE IF NOT EXISTS `item_v2` (...)
   ALTER TABLE `user` ADD COLUMN `bio` VARCHAR(500)
   ALTER TABLE `item` DROP COLUMN `legacy_tag`        ← ⚠️ 风险项

 兼容性总结: 2 个兼容变更，1 个风险变更
 执行: sonic publish  ← 会自动先跑本 diff，确认后执行
```

## 对比场景

| 场景 | 命令 |
|:---|:---|
| 开发时：查看本地 Schema vs test 数据库 | `sonic diff --from schema --to test` |
| 发布前：查看 test vs main 数据库 | `sonic diff --from test --to main` |
| 回溯：查看 main vs 本地 Schema | `sonic diff --from main --to schema` |

## 示例

```bash
sonic diff                              # test vs main（默认）
sonic diff --from test --to main        # 同上，显式指定
sonic diff --from schema --to test      # Schema 文件 vs test 数据库
```

## 与其他命令的关系

| 命令 | 关系 |
|:---|:---|
| `sonic publish` | publish 的第一步就是 diff，确认后自动 save 到 main + deploy |
| `sonic save` | diff 只是预览，save 才实际写入 |
| `sonic status` | status 概览状态，diff 展示差异细节 |
