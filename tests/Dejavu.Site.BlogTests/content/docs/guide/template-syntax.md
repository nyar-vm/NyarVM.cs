---
title: 模板语法
layout: guide
date: 2026-01-01
weight: 2
chapter: 进阶教程
---

## 模板语法

DejaVu 使用 `.dora` 模板文件，语法基于 `<% %>` 标签。

### 变量输出

使用 `<% variable %>` 输出变量：

```html
<h1><% page.title %></h1>
<p><% site.description %></p>
```

支持点号访问嵌套属性：

```html
<span><% page.author.name %></span>
```

### 条件判断

```html
<% if page.draft %>
  <span class="badge">草稿</span>
<% end if %>
```

`if-else` 结构：

```html
<% if page.tags %>
  <div class="tags">...</div>
<% else %>
  <p>暂无标签</p>
<% end if %>
```

### 循环

```html
<% loop posts %>
  <article>
    <h2><% item.title %></h2>
    <p><% item.summary %></p>
  </article>
<% end loop %>
```

循环中使用 `item` 引用当前迭代对象。

### 模板继承

定义基础布局：

```html
<!-- layouts/default.dora -->
<html>
<body>
  <% block content %>
  <% end block %>
</body>
</html>
```

子模板继承并填充：

```html
<% extends 'layouts/default.dora' %>
<% block content %>
  <h1>页面内容</h1>
<% end block %>
```

### 模板包含

```html
<% include 'partials/header.dora' %>
```

### 模式匹配

```html
<% match page.type %>
  <% if "article" %>
    <article>...</article>
  <% end if %>
  <% if "page" %>
    <div class="page">...</div>
  <% end if %>
<% end match %>
```

### 注释

```html
<%# 这是注释，不会输出 %>
```
