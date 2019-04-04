---
title: 站点配置
layout: guide
date: 2026-01-01
weight: 3
chapter: 入门指南
---

## 站点配置

DejaVu 使用 VON 格式的 `config.von` 文件配置站点。

### 基本配置

```
{
  title: "站点标题",
  description: "站点描述",
  author: "作者",
  language: "zh-CN",
  baseUrl: "/"
}
```

### 导航配置

```
{
  navigation: [
    {"首页": "/"},
    {"文章": "/posts/"},
    {"关于": "/about/"}
  ]
}
```

### 侧边栏配置

```
{
  sidebar: [
    {"最近文章": "/posts/"},
    {"标签": "/tags/"}
  ]
}
```

### 多语言配置

```
{
  language: "zh-CN",
  languages: {
    "zh-CN": {
      home: "首页",
      posts: "文章"
    },
    en: {
      home: "Home",
      posts: "Posts"
    }
  }
}
```

切换语言时，所有 `data-i18n` 标记的元素会自动翻译。

### 内容配置

| 字段 | 类型 | 默认值 | 说明 |
|:---|:---|:---|:---|
| `paginate` | 整数 | 10 | 每页文章数 |
| `permalink` | 字符串 | `/:section/:slug/` | 永久链接模式 |
| `includeDrafts` | 布尔 | false | 是否包含草稿 |
| `defaultLayout` | 字符串 | `default` | 默认布局名称 |

### 永久链接模式

支持以下占位符：

| 占位符 | 说明 |
|:---|:---|
| `:section` | 内容分区 |
| `:slug` | 文章别名 |
| `:year` | 年份 |
| `:month` | 月份 |
| `:day` | 日期 |
| `:title` | 标题别名 |

示例：`/:year/:month/:slug/` → `/2026/01/my-post/`
