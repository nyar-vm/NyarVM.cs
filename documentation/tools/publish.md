# sonic publish

从 test 环境发布到 main 环境。组合了 diff 预览 + 确认 + save 到 main + deploy 的完整流程。

## 用法

```bash
sonic publish [选项]
```

## 选项

| 选项 | 短选项 | 说明 | 默认值 |
|:---|:---|:---|:---|
| `--config` | | 配置文件路径 | `sonic.config.von` |
| `--yes` | | 跳过 diff 确认，直接执行 | `false` |
| `--dry-run` | | 只生成 diff 和 SQL，不实际执行 | `false` |
| `--tag` | | 发布标签/版本号 | 自动生成时间戳 |

## 说明

`sonic publish` 是发往生产的一站式命令。它与 `git push` 不同——不直接操作数据，而是运行 diff 分析、让开发者确认后再执行。

## 发布流程

```
sonic publish
      │
      ▼
┌─────────────────────────────────────────┐
│ 1. sonic diff --from test --to main     │ ← 自动运行
│    显示将要执行的 SQL 和兼容性风险        │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│ 2. 用户确认                              │
│    兼容性风险项会特别标注，需二次确认      │
│    [Y/n]                                │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│ 3. sonic save --env main                │
│    执行 SQL DDL 到 main 数据库           │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│ 4. sonic deploy                         │
│    部署前后端到生产环境                   │
└─────────────────────────────────────────┘
```

## 输出示例

```
$ sonic publish

📊 Step 1/4: Schema diff（test ⇄ main）
   [+] ItemV2         ✅ 兼容
   [~] User           ✅ 兼容（新增 bio 列）
   共 2 项变更，0 个风险

   是否继续发布到 main？[Y/n] y

🔧 Step 2/4: 正在执行 Schema 迁移...
   ✅ CREATE TABLE item_v2 成功
   ✅ ALTER TABLE user 成功

📦 Step 3/4: 部署中...
   ✅ 前端已部署
   ✅ 后端已部署

🎉 Step 4/4: 发布完成！
   标签: 20260501.1503
```

## 示例

```bash
sonic publish                   # 标准发布流程
sonic publish --yes             # 跳过确认，直接发布
sonic publish --dry-run         # 预览流程，不实际执行
```

## 与其他命令的关系

| 命令 | 关系 |
|:---|:---|
| `sonic diff` | publish 的第一步，可单独运行 |
| `sonic save` | publish 的第三步，可单独运行 |
| `sonic deploy` | publish 的最后一步，可单独运行 |
