# sonic env

环境管理。查看和切换当前操作环境。

## 用法

```bash
sonic env [选项]
sonic env use <环境名>
```

## 子命令

| 命令 | 说明 |
|:---|:---|
| `sonic env` | 显示当前环境和可用环境列表 |
| `sonic env use <env>` | 切换到指定环境 |

## 环境说明

| 环境 | 含义 | 可执行操作 |
|:---|:---|:---|
| `test` | 开发/测试数据库 | save |
| `main` | 生产数据库（只读） | load, diff（对比用） |

## 示例

```bash
$ sonic env
   当前环境: test
   可用环境: test, main
   数据库 test: mysql://user@host:3306/myapp_test ✅

$ sonic env use main
   ⚠️  切换到 main 环境。save 操作不会影响 main（用 sonic publish）。
   当前环境: main
```

## 与其他命令的关系

| 命令 | 关系 |
|:---|:---|
| `sonic save` | save 默认写入当前环境（test） |
| `sonic load` | load 默认从 main 读取 |
| `sonic publish` | publish 不受 env 影响，始终 test → main |
