# Sonic.Plotter 完整规划（企业级 2D 数据可视化库）

## 一、核心定位与目标

### 1. 产品定位

**Sonic.Plotter**：NyarVM 体系内 **原生 C# 企业级 2D 数据可视化库**，对标
ECharts、Vega-Lite，基于图形语法设计，支持静态出图、前端可变画布、Notebook 交互式图表，深度集成 Sonic/Metis/Athena 生态。

### 2. 核心目标

- 覆盖 90% 以上业务数据可视化场景（报表、大屏、分析图表）
- 提供 **链式 API + JSON Schema** 双模式，兼顾开发效率与动态配置
- 一套代码多端运行：服务端出图、Web 前端、桌面端、Notebook
- 性能对标 ECharts，支持百万级数据平滑渲染
- 纯 C# 实现，无第三方依赖，与 NyarVM 技术栈无缝融合

### 3. 边界红线（严格遵守）

✅ 只做：2D 数据图表、可视化组件、图形语法、交互动画、多端渲染 ❌ 不做：3D 渲染、骨骼动画、游戏引擎、RHI/GPU 底层、通用 UI 组件

---

## 二、整体架构设计

### 1. 分层架构（单向依赖，无循环）

```
Sonic （全局标准库，底层依赖）
  ↓
Sonic.Plotter
├─ Core 核心层（入口、状态、生命周期）
├─ Grammar 图形语法层（核心业务逻辑）
├─ Rendering 渲染层（画布、图元、多端实现）
├─ Interaction 交互层（事件、选择、联动）
├─ Animation 动画层（过渡、帧动画）
├─ Schema 配置层（JSON 序列化/反序列化）
└─ Extensions 扩展层（主题、插件、预设）
```

### 2. 依赖关系

- 所有模块仅依赖 Sonic 标准库（基础类型、数学、集合、工具）
- 上层模块依赖下层模块，下层不依赖上层
- 模块间通过接口交互，实现解耦

---

## 三、核心模块详细设计

### 1. Core 核心层

#### 职责

- 图表总入口、全局状态管理、生命周期调度
- 画布配置、标题、边距、全局样式
- 图层管理、渲染调度、脏标记机制

#### 核心类

```csharp
namespace Sonic.Plotter.Core
{
    // 图表总入口，对外唯一门面
    public class ChartCanvas
    {
        public ChartCanvas WithSize(int width, int height) { }
        public ChartCanvas WithTitle(string main, string sub = "") { }
        public ChartCanvas WithMargin(int top, int bottom, int left, int right) { }
        
        public VisualMappingBuilder UseVisualMapping() { }
        public DataTransformBuilder UseDataTransform() { }
        public ShapeLayerBuilder AddShapeLayer() { }
        public ValueScaleBuilder UseValueScale() { }
        public CoordinateSystemBuilder UseCoordinateSystem() { }
        public LayoutPanelBuilder UseLayoutPanel() { }
        
        public InteractionBuilder UseInteraction() { }
        public AnimationBuilder UseAnimation() { }
        public ThemeBuilder UseTheme() { }
        public AnnotationBuilder AddAnnotation() { }
        
        public byte[] ExportToPng() { }
        public string ExportToSvg() { }
        public object ExportToWebCanvas() { }
    }

    // 图层基类，所有可见元素继承
    public abstract class ChartLayer
    {
        public Guid LayerId { get; } = Guid.NewGuid();
        public bool IsDirty { get; set; } = true;
        public int ZIndex { get; set; }
        public double Opacity { get; set; } = 1.0;
        
        public abstract void Update(object data);
        public abstract void Render(IRenderCanvas canvas);
    }

    // 图层管理器，负责图层增删、排序、渲染调度
    internal class LayerManager
    {
        public void AddLayer(ChartLayer layer) { }
        public void MarkDirty(Guid layerId) { }
        public void RenderAll(IRenderCanvas canvas) { }
    }
}
```

---

### 2. Grammar 图形语法层（核心业务逻辑）

严格遵循图形语法分层，全名称命名，无缩写。

#### 2.1 VisualMapping 视觉映射

- 职责：数据字段 → 视觉属性（位置、颜色、大小、形状、透明度）绑定
- 核心类：`VisualMappingBuilder`
- 支持：字段动态映射、固定常量样式、条件样式

```csharp
public class VisualMappingBuilder
{
    public VisualMappingBuilder MapXField(string field) { }
    public VisualMappingBuilder MapYField(string field) { }
    public VisualMappingBuilder MapFillColor(string field) { }
    public VisualMappingBuilder MapStrokeColor(string field) { }
    public VisualMappingBuilder MapSize(string field) { }
    
    public VisualMappingBuilder WithFixedFill(Color color) { }
    public VisualMappingBuilder WithFixedSize(double size) { }
}
```

#### 2.2 DataTransform 数据变换

- 职责：统计计算、数据聚合、分箱、平滑、密度估算
- 支持：求和、均值、计数、直方图分箱、曲线平滑、分组聚合

```csharp
public enum TransformType
{
    AggregateSum,
    AggregateAverage,
    GroupCount,
    BinHistogram,
    SmoothCurve,
    DensityEstimate
}

public class DataTransformBuilder
{
    public DataTransformBuilder WithTransformType(TransformType type) { }
    public DataTransformBuilder GroupBy(string field) { }
}
```

#### 2.3 ShapeLayer 图形图层

- 职责：定义各类图表元素，生成绘制指令
- 支持：点、线、柱、面积、饼图、雷达图、热力图、箱线图等

```csharp
public enum ShapeType
{
    Point,
    Line,
    Bar,
    HorizontalBar,
    Area,
    Pie,
    Radar,
    Heatmap,
    BoxPlot
}

public class ShapeLayerBuilder
{
    public ShapeLayerBuilder WithShapeType(ShapeType type) { }
    public ShapeLayerBuilder WithLineWidth(double width) { }
    public ShapeLayerBuilder WithLayerOpacity(double opacity) { }
}
```

#### 2.4 ValueScale 数值标尺

- 职责：数据值域 → 画布像素值域映射，坐标轴刻度计算
- 支持：数值、时间、分类、对数四种标尺类型

```csharp
public enum ScaleType
{
    Numeric,
    Time,
    Category,
    Logarithmic
}

public class ValueScaleBuilder
{
    public ValueScaleBuilder WithXScaleType(ScaleType type) { }
    public ValueScaleBuilder WithYScaleType(ScaleType type) { }
    public ValueScaleBuilder WithRange(double min, double max) { }
    public ValueScaleBuilder WithTickCount(int count) { }
}
```

#### 2.5 CoordinateSystem 坐标系

- 职责：坐标空间转换
- 支持：直角坐标系、极坐标系、翻转坐标系

```csharp
public enum CoordinateType
{
    Cartesian,
    Polar,
    Reversed
}

public class CoordinateSystemBuilder
{
    public CoordinateSystemBuilder WithCoordinateType(CoordinateType type) { }
}
```

#### 2.6 LayoutPanel 分面布局

- 职责：按字段拆分多子图，行列布局
- 支持：行分面、列分面、网格分面

```csharp
public class LayoutPanelBuilder
{
    public LayoutPanelBuilder SplitByRow(string field) { }
    public LayoutPanelBuilder SplitByColumn(string field) { }
    public LayoutPanelBuilder WithColumnCount(int count) { }
}
```

---

### 3. Rendering 渲染层

#### 职责

- 统一 2D 绘图上下文抽象
- 多端渲染后端实现
- 基础图元绘制、画布生命周期管理

#### 核心接口与实现

```csharp
// 统一渲染画布抽象，所有后端实现此接口
public interface IRenderCanvas
{
    int Width { get; }
    int Height { get; }
    void SetSize(int width, int height);
    
    void BeginRender();
    void EndRender();
    void Clear();
    
    void SetFillColor(Color color);
    void SetStrokeColor(Color color);
    void SetLineWidth(float width);
    void SetLineDash(float[] pattern);
    
    void FillRect(float x, float y, float w, float h);
    void StrokeRect(float x, float y, float w, float h);
    void FillCircle(float cx, float cy, float r);
    void StrokeCircle(float cx, float cy, float r);
    void BeginPath();
    void LineTo(float x, float y);
    void Fill();
    void Stroke();
    void FillText(string text, float x, float y, Font font);
    
    byte[] ToPngBytes();
    string ToSvgString();
    object GetNativeContext();
}

// 多端实现
public class SvgRenderCanvas : IRenderCanvas { }
public class BitmapRenderCanvas : IRenderCanvas { }
public class WebCanvasRenderCanvas : IRenderCanvas { }
```

---

### 4. Interaction 交互层

#### 职责

- 统一事件系统
- 内置交互行为
- 多图联动

#### 核心类

```csharp
public enum InteractionMode
{
    HoverTooltip,
    ClickSelect,
    BoxSelect,
    ZoomScroll,
    DragPan
}

public class InteractionBuilder
{
    public InteractionBuilder WithMode(InteractionMode mode) { }
    public InteractionBuilder OnSelected(Action<object> callback) { }
    public InteractionBuilder OnHover(Action<object> callback) { }
}
```

---

### 5. Animation 动画层

#### 职责

- 数据更新过渡动画
- 入场/更新/离场动画
- 缓动函数库

#### 核心类

```csharp
public enum EasingStyle
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    Bounce
}

public class AnimationBuilder
{
    public AnimationBuilder WithEnabled(bool enabled = true) { }
    public AnimationBuilder WithDuration(int milliseconds) { }
    public AnimationBuilder WithEasingStyle(EasingStyle style) { }
    public AnimationBuilder WithEnterAnimation() { }
    public AnimationBuilder WithUpdateAnimation() { }
    public AnimationBuilder WithExitAnimation() { }
}
```

---

### 6. Schema 配置层

#### 职责

- JSON 配置解析与导出
- 与链式 API 双向转换
- 配置校验

#### 核心类

```csharp
public class PlotterSchema
{
    public string Version { get; set; } = "1.0";
    public TitleConfig Title { get; set; }
    public SizeConfig Size { get; set; }
    public MarginConfig Margin { get; set; }
    public DataSourceConfig DataSource { get; set; }
    public VisualMappingConfig VisualMapping { get; set; }
    public List<DataTransformConfig> DataTransform { get; set; }
    public List<ShapeLayerConfig> ShapeLayers { get; set; }
    public ValueScaleConfig ValueScale { get; set; }
    public CoordinateSystemConfig CoordinateSystem { get; set; }
    public LayoutPanelConfig LayoutPanel { get; set; }
    public InteractionConfig Interaction { get; set; }
    public AnimationConfig Animation { get; set; }
    public ThemeConfig Theme { get; set; }
    public List<AnnotationConfig> Annotations { get; set; }
}

public static class SchemaConverter
{
    public static ChartCanvas FromJson(string json) { }
    public static string ToJson(ChartCanvas chart) { }
}
```

---

### 7. Extensions 扩展层

#### 7.1 Theme 主题系统

- 内置预设主题：浅色、深色、商务、学术、极简
- 支持自定义主题、主题继承
- 统一管理配色、字体、网格、图例样式

#### 7.2 插件系统

- 预留插件接口，支持自定义图表、交互、动画
- 官方扩展包：统计图表扩展、地理图表扩展、大屏组件扩展

---

## 四、完整 API 使用示例

### 1. 链式 API 模式（开发首选）

```csharp
// 加载 Metis 数据表
var data = Metis.Table.FromCsv("sales.csv");

// 创建图表
var chart = new ChartCanvas(data)
    .WithSize(900, 520)
    .WithTitle("月度销售额统计", "2026 年度")
    .WithMargin(20, 30, 25, 25);

// 视觉映射
chart.UseVisualMapping()
    .MapXField("Month")
    .MapYField("Sales")
    .MapFillColor("Category");

// 图形图层
chart.AddShapeLayer()
    .WithShapeType(ShapeType.Bar)
    .WithLayerOpacity(0.9);

// 标尺与坐标系
chart.UseValueScale()
    .WithYScaleType(ScaleType.Numeric)
    .WithRange(0, 5000);

chart.UseCoordinateSystem()
    .WithCoordinateType(CoordinateType.Cartesian);

// 交互与动画
chart.UseInteraction()
    .WithMode(InteractionMode.HoverTooltip)
    .WithMode(InteractionMode.BoxSelect);

chart.UseAnimation()
    .WithEnabled()
    .WithDuration(400)
    .WithEasingStyle(EasingStyle.EaseOut);

// 主题
chart.UseTheme()
    .WithPreset(ThemePreset.Business);

// 导出
chart.ExportToPng("sales_chart.png");
```

### 2. JSON Schema 模式（动态配置首选）

```json
{
  "Version": "1.0",
  "Title": {
    "MainText": "月度销售额统计",
    "SubText": "2026 年度"
  },
  "Size": { "Width": 900, "Height": 520 },
  "Margin": { "Top": 20, "Bottom": 30, "Left": 25, "Right": 25 },
  "DataSource": {
    "Type": "MetisTable",
    "TableName": "sales"
  },
  "VisualMapping": {
    "XField": "Month",
    "YField": "Sales",
    "FillColorField": "Category"
  },
  "ShapeLayers": [
    {
      "ShapeType": "Bar",
      "LayerOpacity": 0.9
    }
  ],
  "ValueScale": {
    "YScaleType": "Numeric",
    "YMin": 0,
    "YMax": 5000
  },
  "Interaction": {
    "Modes": ["HoverTooltip", "BoxSelect"]
  },
  "Animation": {
    "Enabled": true,
    "Duration": 400,
    "EasingStyle": "EaseOut"
  },
  "Theme": {
    "Preset": "Business"
  }
}
```

```csharp
// 从 JSON 加载图表
var chart = SchemaConverter.FromJson(schemaJson);
chart.ExportToPng("chart_from_schema.png");
```

---

## 五、多端适配方案

| 运行端   | 渲染后端                              | 能力支持                 | 适用场景                       |
|----------|---------------------------------------|--------------------------|--------------------------------|
| 服务端   | BitmapRenderCanvas                    | 静态图片导出、PDF 生成   | 定时报表、邮件图表、后台生成   |
| Web 前端 | WebCanvasRenderCanvas                 | 可变画布、完整交互、动画 | 后台管理、数据大屏、交互式分析 |
| 桌面端   | BitmapRenderCanvas / SkiaRenderCanvas | 窗口自适应、本地导出     | 桌面客户端、数据分析工具       |
| Notebook | WebCanvasRenderCanvas                 | 单元格内嵌、动态刷新     | 数据分析笔记、实验报告         |

---

## 六、性能优化策略

1. **脏图层增量重绘**：仅重绘发生变化的图层，而非全画布刷新
2. **数据采样**：内置大数据采样算法，支持百万级数据平滑展示
3. **资源复用**：画笔、样式、路径对象复用，减少 GC 压力
4. **按需渲染**：分面布局仅渲染可视区域内的子图
5. **批量绘制**：合并同类图元绘制指令，减少画布调用次数

---

## 七、生态与工具

1. **示例库**：覆盖基础图表、组合图表、交互案例、动画案例、多端案例
2. **在线文档**：API 文档、最佳实践、迁移指南
3. **调试工具**：图层调试、数据校验、渲染性能分析
4. **主题编辑器**：可视化主题定制工具，导出主题配置
5. **图表模板**：常用业务图表模板，一键生成

---

## 八、分阶段落地路线图

### 第一阶段（核心能力，1-2 个月）

- 实现 Core 核心层（ChartCanvas、LayerManager）
- 实现 Rendering 层（IRenderCanvas + SVG/位图后端）
- 实现基础图形语法（VisualMapping、ShapeLayer、ValueScale、CoordinateSystem）
- 支持首批核心图表：折线、柱状、饼图、散点
- 实现基础导出能力（PNG/SVG）

### 第二阶段（完整能力，2-3 个月）

- 实现 DataTransform、LayoutPanel 分面布局
- 实现 Interaction 交互层（悬停、点击、缩放、平移）
- 实现 Animation 动画层（过渡动画、入场/离场动画）
- 实现 Schema 配置层（JSON 解析与导出）
- 支持更多图表：面积、雷达、热力图、箱线图
- 实现主题系统与内置预设

### 第三阶段（生态完善，3-4 个月）

- 实现 Web 可变画布支持
- 实现 Notebook 集成
- 开发官方扩展包（统计图表、地理图表）
- 完善示例库与在线文档
- 性能优化与 Bug 修复

### 第四阶段（企业级特性，4-6 个月）

- 支持大数据渲染优化
- 实现多图联动
- 开发主题编辑器与图表模板库
- 完善调试与监控工具
- 企业级稳定性与兼容性测试