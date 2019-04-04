# Argus — 全视文档工具集

对标 `pandoc` 的通用文档转换引擎。 **唯一核心语言为 Notedown**。

## 核心架构（对齐 pandoc）

```
任意输入格式 ──→ NotedownDocument AST ──→ 任意输出格式
     ↑              (唯一中间表示)              ↑
  Language        双向引用系统            Language
 .ToNotedown()   ReferenceMap         .FromNotedown()
 (Oak 格式包)                         (Oak 格式包)
```

> 这是 pandoc 的精髓： **Reader → AST → Writer**。Notedown 就是那个 AST。
> 每种格式的 Language 类内置 ToNotedown / FromNotedown 方法，无接口、无委托、无独立转换器类。

## Notedown — 唯一核心语言

**Notedown 是整个文档系统的唯一核心语言**，它不是"Markdown 的某种实现"，而是自己的语言，其 AST (`NotedownDocument`)
是所有格式转换的唯一中间表示。

Notedown 对标 pandoc 的 Pandoc AST，具备以下特点：

- **完整的块级模型**：`Header`、`Para`、`CodeBlock`、`BlockQuote`、`OrderedList`、`BulletList`、`DefinitionList`、`Table`（含
  `TableHead`/`TableBody`/`TableFoot`/`ColSpec`）、`LineBlock`、`RawBlock`、`Div`、`HorizontalRule`
- **完整的行内模型**：`Str`、`Emph`、`Strong`、`Strikeout`、`Superscript`、`Subscript`、`SmallCaps`、`Quoted`、`Cite`（含
  `Citation`）、`Code`、`Math`（行内/块级）、`RawInline`、`Link`、`Image`、`Note`（脚注）、`Span`、`Space`、`SoftBreak`、`LineBreak`
- **完整的元数据模型**：`Meta` → `MetaValue`（`MetaString` / `MetaBool` / `MetaList` / `MetaMap`）
- **完整的属性模型**：`Attr`（id / classes / key-values）、`Target`（url / title）
- **完整的双向引用系统**：`ReferencePath`（引用来源位置）、`ReferenceTarget`（引用目标）、`ReferenceMap`（正向 + 反向索引），支持外部
  URL、内部锚点、文献、脚注、图片等引用类型

### Notedown 语言定义（`Oak.Notedown`）

| 组件                                                    | 职责                                                                                                                                                                                     |
|:--------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `NotedownLanguage`                                      | 语言入口，继承 `Oak.Syntax.Language`，提供 `Parse(string) → NotedownDocument`、`Format(NotedownDocument) → string`、`ToNotedown`（恒等 + 构建引用）、`FromNotedown`（恒等 → 格式化文本） |
| `NotedownLanguageConfig`                                | 语言配置（控制 GFM 表格、任务列表、数学公式、脚注、定义列表、行块、上标下标、高亮等扩展）                                                                                                |
| `NotedownLexer`                                         | 词法分析器，逐行识别块级结构                                                                                                                                                             |
| `NotedownParser`                                        | 语法分析器，将 Token 序列解析为 AST，支持 YAML frontmatter                                                                                                                               |
| `NotedownFormatter`                                     | 文本格式化器，将 AST 完整序列化为 Notedown 文本                                                                                                                                          |
| `NotedownDocument` / `NotedownBlock` / `NotedownInline` | AST 节点定义（`Oak.Notedown.Syntax`）                                                                                                                                                    |
| `ReferencePath` / `ReferenceTarget` / `ReferenceMap`    | 双向引用系统（`Oak.Notedown.Syntax`）                                                                                                                                                    |

### Notedown 文本语法

Notedown 拥有自己的文本语法，是 Markdown 的超集：

- **YAML frontmatter**：`---` 包裹的元数据
- **ATX 标题**：`#` 至 `######`
- **Setext 标题**：`===` 和 `---`
- **代码围栏**：` ``` ` 和 `~~~`，支持 `{.language #id .class}` 属性语法
- **Div 围栏**：`:::` 和 `:::{...}` 属性语法
- **行块**：`|` 前缀行
- **表格**：`|` 分隔的 GFM 表格
- **属性语法**：`{#id .class key=value}` 可附加到标题、代码块、链接、图片、Span、Div
- **脚注**：`[^id]:` 定义，`[^id]` 引用
- **引用文献**：`[@id]` 和 `[@id1; @id2]`
- **上下标**：`^superscript^` 和 `~subscript~`
- **小大写**：`[.smallcaps]...[/.smallcaps]`
- **高亮**：`[.highlight]...`（通过 `Span` 实现）

## 格式转换（Language 类内置，无独立转换器）

每种格式的转换逻辑内置于其 `Language` 类，通过覆写 `ToNotedown(object ast)` 和 `FromNotedown(NotedownDocument)` 实现与 IR
的双向转换。 **没有独立的转换器类，没有接口，没有委托**。

| 格式         | Language 类          | → Notedown                     | ← Notedown                | 所在包                |
|:-------------|:---------------------|:-------------------------------|:--------------------------|:----------------------|
| **Notedown** | `NotedownLanguage`   | `Parse` + `ToNotedown`（恒等） | `FromNotedown` → `Format` | `Oak.Notedown`        |
| **Markdown** | `MarkdownLanguage`   | `Parse` → `ToNotedown`         | `FromNotedown`            | `Oak.Markdown`        |
| **JSON**     | `JsonLanguage`       | `ToNotedown`                   | `FromNotedown`            | `Oak.Json`            |
| **CSV**      | `CsvLanguage`        | `ToNotedown`                   | `FromNotedown`            | `Oak.Csv`             |
| **YAML**     | `YamlLanguage`       | `Parse` → `ToNotedown`         | `FromNotedown`            | `Oak.Yaml`            |
| **TOML**     | `TomlLanguage`       | `Parse` → `ToNotedown`         | `FromNotedown`            | `Oak.Toml`            |
| **INI**      | `IniLanguage`        | `Parse` → `ToNotedown`         | `FromNotedown`            | `Oak.Ini`             |
| **XML**      | `XmlLanguage`        | `ToNotedown`                   | `FromNotedown`            | `Oak.Xml`             |
| HTML         | 待实现               | 待实现                         | 待实现                    | `Oak.Html`            |
| Plain Text   | 内置于 `ArgusEngine` | 内置于 `ArgusEngine`           | 内置于 `ArgusEngine`      | `Std.Document.Engine` |

## 职责边界

```
Oak.Notedown          → Notedown 语言定义（AST、Lexer、Parser、Formatter、LanguageConfig、双向引用系统）
Oak.{格式}            → 自身格式 Language 类（Parse + ToNotedown + FromNotedown），无独立转换器文件
Sonic.Standard.Document → 纯编排层。ArgusEngine 持有各格式 Language 实例，按格式分发调用
```

**Sonic.Standard.Document 不包含任何解析逻辑**，所有文本解析和格式化都在 Oak 的 Language 类中。

## ArgusEngine — 编排入口

```csharp
var engine = new ArgusEngine();

// 文件到文件
engine.Convert("input.md", "output.json");

// 字符串到字符串
var json = engine.ConvertToString(markdownText, SourceFormat.Markdown, TargetFormat.Json);

// 分步操作
var doc = engine.Read(source, SourceFormat.Markdown);
var output = engine.WriteToString(doc, TargetFormat.Json);

// 查询双向引用
doc.WithReferenceMap();
var refs = doc.ReferenceMap?.GetBackReferences("https://example.com");
```

`ArgusEngine.Read()` 和 `ArgusEngine.WriteToString()` 通过 `switch` 表达式直接调用对应 Language 实例的 `ToNotedown` /
`FromNotedown` 方法，零抽象开销。

## 双向引用系统

`NotedownDocument` 内置 `ReferenceMap`，维护文档中所有引用的双向索引：

- **正向索引**（引用者 → 目标）：`ReferencePath` → `ReferenceTarget`
- **反向索引**（目标 → 引用者）：目标 ID → `IReadOnlyList<ReferencePath>`
- **引用类型**：外部 URL、内部锚点、文献引用、脚注、图片
- **惰性构建**：通过 `WithReferenceMap()` 在需要时构建，避免不必要的遍历

## 支持的格式

### 输入格式 (`SourceFormat`)

| 格式        | 枚举值      | 状态 |
|:------------|:------------|:----:|
| Notedown    | `Notedown`  |  ✅  |
| Markdown    | `Markdown`  |  ✅  |
| JSON        | `Json`      |  ✅  |
| CSV         | `Csv`       |  ✅  |
| Plain Text  | `PlainText` |  ✅  |
| Auto Detect | `Auto`      |  ✅  |
| YAML        | `Yaml`      |  ✅  |
| TOML        | `Toml`      |  ✅  |
| INI         | `Ini`       |  ✅  |
| XML         | `Xml`       |  ✅  |
| HTML        | `Html`      |  📋  |

### 输出格式 (`TargetFormat`)

| 格式       | 枚举值      | 状态 |
|:-----------|:------------|:----:|
| Notedown   | `Notedown`  |  ✅  |
| Markdown   | `Markdown`  |  ✅  |
| JSON       | `Json`      |  ✅  |
| CSV        | `Csv`       |  ✅  |
| Plain Text | `PlainText` |  ✅  |
| YAML       | `Yaml`      |  ✅  |
| TOML       | `Toml`      |  ✅  |
| INI        | `Ini`       |  ✅  |
| XML        | `Xml`       |  ✅  |
| HTML       | `Html`      |  📋  |

## 对齐 pandoc 功能

| pandoc 能力                          | Notedown 对应                                                             | 状态 |
|:-------------------------------------|:--------------------------------------------------------------------------|:----:|
| 多格式输入                           | `SourceFormat` + `ArgusEngine.Read()`                                     |  ✅  |
| 多格式输出                           | `TargetFormat` + `ArgusEngine.Write()`                                    |  ✅  |
| 统一 AST                             | `NotedownDocument` → `NotedownBlock` / `NotedownInline`                   |  ✅  |
| 元数据                               | `Meta` / `MetaValue`                                                      |  ✅  |
| 双向引用（正向+反向）                | `ReferencePath` / `ReferenceTarget` / `ReferenceMap`                      |  ✅  |
| 表格（含表头/表体/表脚/列规格/标题） | `Table` / `TableHead` / `TableBody` / `TableFoot` / `ColSpec` / `Caption` |  ✅  |
| 代码块（含语言标识/属性）            | `CodeBlock`                                                               |  ✅  |
| 引用块                               | `BlockQuote`                                                              |  ✅  |
| 有序/无序列表（含编号属性）          | `OrderedList` / `BulletList` / `ListAttributes`                           |  ✅  |
| 定义列表                             | `DefinitionList` / `DefinitionItem`                                       |  ✅  |
| 行块（诗歌/地址）                    | `LineBlock`                                                               |  ✅  |
| 通用块容器                           | `Div`                                                                     |  ✅  |
| 原始块/行内                          | `RawBlock` / `RawInline`                                                  |  ✅  |
| 引用文献                             | `Cite` / `Citation` / `CitationMode`                                      |  ✅  |
| 脚注                                 | `Note`                                                                    |  ✅  |
| 数学公式（行内/块级）                | `Math` / `MathType`                                                       |  ✅  |
| 任务列表                             | `BulletList` 中 `[x]` / `[ ]` 标记                                        |  ✅  |
| 上下标                               | `Superscript` / `Subscript`                                               |  ✅  |
| 小大写                               | `SmallCaps`                                                               |  ✅  |
| 高亮                                 | `Span` with `.highlight` class                                            |  ✅  |
| 通用行内容器                         | `Span`                                                                    |  ✅  |
| 属性（id/class/key-value）           | `Attr`                                                                    |  ✅  |
| 链接/图片（含属性）                  | `Link` / `Image` / `Target`                                               |  ✅  |
| 格式自动检测                         | `FormatDetector`（扩展名 + 内容特征）                                     |  ✅  |

### 待实现（按 pandoc 路线）

| pandoc 能力   | 说明                                            |
|:--------------|:------------------------------------------------|
| 目录生成      | 从 `Header` 层级提取                            |
| 交叉引用      | 基于 `#id` 的引用解析（基础双向引用系统已就位） |
| 模板系统      | 输出格式模板                                    |
| 过滤器/插件   | AST 变换管线                                    |
| EPUB 输出     | 需 EPUB 二进制格式支持                          |
| PDF 输出      | 需 LaTeX 或直接 PDF 生成                        |
| DOCX 输出     | 需 OOXML 格式支持                               |
| LaTeX 输出    | 需 LaTeX 格式支持                               |
| HTML 输入输出 | 需 Oak.Html Language 类                         |