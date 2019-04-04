---
title: DejaVu API 概览
layout: api-doc
date: 2026-01-01
version: "0.1.0"
modules: [core, template, site, markdown, cli]
---

## API 概览

DejaVu 提供以下核心模块：

| 模块 | 说明 |
|:---|:---|
| `Dejavu` | 模板渲染引擎核心 |
| `Dejavu.Template` | 模板解析与执行 |
| `Dejavu.Site` | 静态站点生成器 |
| `Dejavu.Markdown` | Markdown 渲染 |
| `Dejavu.Cli` | 命令行工具 |

## 快速开始

```csharp
using Dejavu;

var renderer = new DejavuRenderer();
var result = renderer.Render("Hello, <% name %>!", new Dictionary<string, object>
{
    ["name"] = "World"
});

Console.WriteLine(result); // Hello, World!
```

## 渲染器 API

### `DejavuRenderer`

模板渲染器，负责将 DejaVu 模板渲染为最终输出。

```csharp
public class DejavuRenderer
{
    public string Render(string template, Dictionary<string, object> context);
    public string RenderFile(string templatePath, Dictionary<string, object> context);
}
```

#### `Render(string, Dictionary<string, object>)`

渲染模板字符串。

**参数：**

| 参数 | 类型 | 说明 |
|:---|:---|:---|
| `template` | `string` | DejaVu 模板字符串 |
| `context` | `Dictionary<string, object>` | 模板上下文变量 |

**返回值：** `string` — 渲染后的字符串

**异常：**

| 异常 | 条件 |
|:---|:---|
| `TemplateSyntaxException` | 模板语法错误 |
| `VariableNotFoundException` | 引用不存在的变量 |

#### `RenderFile(string, Dictionary<string, object>)`

渲染模板文件。

**参数：**

| 参数 | 类型 | 说明 |
|:---|:---|:---|
| `templatePath` | `string` | 模板文件路径 |
| `context` | `Dictionary<string, object>` | 模板上下文变量 |

**返回值：** `string` — 渲染后的字符串
