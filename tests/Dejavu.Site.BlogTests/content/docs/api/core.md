---
title: DejaVu.Core 模块
layout: api-doc
date: 2026-01-01
version: "0.1.0"
modules: [core, template, site, markdown, cli]
currentModule: core
---

## DejaVu.Core

核心渲染引擎，提供模板解析、变量求值和控制流执行。

### 类

#### `DejavuRenderer`

模板渲染器主类。

```csharp
public class DejavuRenderer
{
    public DejavuRenderer();
    public string Render(string template, Dictionary<string, object> context);
    public string RenderFile(string templatePath, Dictionary<string, object> context);
}
```

#### `DejavuTemplate`

已解析的模板对象。

```csharp
public class DejavuTemplate
{
    public string Source { get; }
    public List<DejaVuNode> Nodes { get; }
    public string Execute(Dictionary<string, object> context);
}
```

### 接口

#### `IDejavuLoader`

模板加载器接口。

```csharp
public interface IDejavuLoader
{
    string LoadTemplate(string name);
    bool TemplateExists(string name);
}
```

### 枚举

#### `DejaVuNodeType`

模板节点类型。

| 值 | 说明 |
|:---|:---|
| `Text` | 纯文本节点 |
| `Variable` | 变量输出节点 |
| `If` | 条件判断节点 |
| `Loop` | 循环节点 |
| `Block` | 块定义节点 |
| `Extends` | 模板继承节点 |
| `Include` | 模板包含节点 |
| `Match` | 模式匹配节点 |

### 异常

#### `TemplateSyntaxException`

模板语法错误。

```csharp
public class TemplateSyntaxException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public string TemplateSource { get; }
}
```
