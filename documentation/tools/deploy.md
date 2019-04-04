# sonic deploy

部署前后端到目标环境。只部署应用代码，不涉及数据库 Schema 变更。

## 用法

```bash
sonic deploy [选项]
```

## 选项

| 选项 | 短选项 | 说明 | 默认值 |
|:---|:---|:---|:---|
| `--config` | | 配置文件路径 | `sonic.config.von` |
| `--env` | `-e` | 部署目标环境 | `production` |
| `--preview` | | 仅预览部署计划，不执行 | `false` |
| `--region` | `-r` | 部署区域 | Asia |
| `--instances` | `-n` | 实例数 | 1 |

## 说明

`sonic deploy` 负责将编译好的前后端部署到目标环境。Schema 变更应在部署前通过 `sonic publish` 完成（或单独 `sonic save`），`sonic deploy` 不修改数据库 Schema。

## 示例

```bash
sonic deploy                    # 部署到 production
sonic deploy -e staging         # 部署到 staging
sonic deploy --preview          # 预览部署计划
```

## 与其他命令的关系

| 命令 | 关系 |
|:---|:---|
| `sonic publish` | publish 包含 deploy，一站式操作 |
| `sonic save` | 先 save Schema 变更，再 deploy 应用 |
