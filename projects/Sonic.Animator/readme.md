# Sonic.Animator 完整整体设计文档

## 前置约定 & 全局规范（强制执行）

### 1. 整体架构定位

- 所属生态：基于 **Sonic** 全局标准底座，与 `Sonic.Plotter` 成对设计、风格完全对齐
- 模块职责：统一承载 **Spine、Live2D、序列帧** 等2D骨骼/形变动画能力
- 顶层命名：`Sonic.Animator`（与 `Plotter` 词性、层级对等）
- 依赖规则：仅依赖底层 `Sonic`，与 `Plotter`、`Render3D` 平级，代码无强耦合，运行时支持画布叠加渲染

### 2. 编码&命名规范（全局统一）

1. **禁用项**：禁止任何英文缩写、禁止 `SetXXX`/`WithXXX` 方法前缀；
2. **命名规则**：全部使用完整英文单词，语义直白；
3. **方法风格**：

- 配置类：动宾结构，链式调用、返回自身实例；
- 行为类：纯动词命名，表达动作指令；

4. **状态字段**：对外暴露只读属性，不封装 `Get/Set` 方法；
5. **架构原则**： **接口抽象 + 工厂注册 + 插拔式实现**，消除硬编码耦合，遵循开闭原则。

---

## 一、整体目录结构

```
Sonic.Animator
├─ Abstractions          # 统一抽象契约（核心接口，无具体业务实现）
├─ Core                  # 核心通用逻辑、全局管理器、状态枚举、格式标识
├─ Runtime               # 通用运行时能力（骨骼、形变、状态机、帧运算）
├─ Render                # 通用渲染层（复用 Sonic.IRenderCanvas）
├─ Implementations       # 各动画格式独立实现（可插拔、按需编译/注册）
│  ├─ Spine              # Spine 专属解析、运行、渲染实现
│  ├─ Live2D             # Live2D 专属解析、运行、渲染实现
│  └─ SpriteSheet       # 序列帧动画专属实现
└─ Extensions            # 通用扩展工具、辅助能力、插件接口
```

---

## 二、分层详细设计 & 代码实现

### 模块 1：Abstractions 抽象层（全模块契约，稳定不常变更）

定义资源、解析器、运行实例、实例工厂四大核心接口，所有格式实现均遵循该契约。

#### 1.1 动画资源接口

```csharp
namespace Sonic.Animator.Abstractions
{
    /// <summary>动画资源统一契约（原始静态资源，支持多实例复用）</summary>
    public interface IAnimationResource
    {
        /// <summary>资源唯一标识</summary>
        string ResourceIdentifier { get; }
        /// <summary>动画格式唯一标识</summary>
        string FormatIdentifier { get; }
        /// <summary>内容原始宽度</summary>
        float ContentWidth { get; }
        /// <summary>内容原始高度</summary>
        float ContentHeight { get; }
    }
}
```

#### 1.2 资源解析器接口

```csharp
namespace Sonic.Animator.Abstractions
{
    /// <summary>文件解析器统一契约</summary>
    public interface IResourceParser
    {
        /// <summary>判断当前解析器是否支持该文件</summary>
        bool CanParse(string filePath);

        /// <summary>同步解析文件为动画资源</summary>
        IAnimationResource Parse(string filePath);

        /// <summary>异步解析文件为动画资源</summary>
        Task<IAnimationResource> ParseAsync(string filePath);
    }
}
```

#### 1.3 动画运行实例接口（对外核心调用入口）

```csharp
namespace Sonic.Animator.Abstractions
{
    using Sonic;
    using Sonic.Animator.Core;

    /// <summary>动画运行实例统一契约</summary>
    public interface IAnimationInstance
    {
        #region 只读状态属性
        PlayStatus CurrentPlayStatus { get; }
        float HorizontalPosition { get; }
        float VerticalPosition { get; }
        float ScaleRatio { get; }
        float OpacityValue { get; }
        float PlaybackSpeed { get; }
        #endregion

        #region 配置类方法（动宾结构、链式返回）
        IAnimationInstance MoveTo(float horizontal, float vertical);
        IAnimationInstance ScaleBy(float ratio);
        IAnimationInstance ApplyOpacity(float value);
        IAnimationInstance AdjustPlaybackSpeed(float speed);
        #endregion

        #region 行为类方法（纯动词、动作指令）
        IAnimationInstance Play(string animationName, bool loopExecution = true);
        IAnimationInstance Pause();
        IAnimationInstance Stop();
        #endregion

        #region 帧更新 & 渲染
        void UpdateFrame(float deltaTime);
        void DrawToCanvas(IRenderCanvas canvas);
        #endregion
    }
}
```

#### 1.4 实例工厂接口

```csharp
namespace Sonic.Animator.Abstractions
{
    /// <summary>动画实例工厂契约（根据资源匹配并创建对应运行实例）</summary>
    public interface IAnimationInstanceFactory
    {
        /// <summary>判断工厂是否匹配当前动画资源</summary>
        bool MatchResource(IAnimationResource resource);

        /// <summary>创建动画运行实例</summary>
        IAnimationInstance CreateInstance(IAnimationResource resource);
    }
}
```

---

### 模块 2：Core 核心层（通用状态、标识、全局注册管理器）

无任何具体格式硬编码，仅提供通用枚举、格式常量、全局注册与调度能力。

#### 2.1 播放状态枚举（通用状态，全格式共用）

```csharp
namespace Sonic.Animator.Core
{
    /// <summary>动画播放状态</summary>
    public enum PlayStatus
    {
        Idle,
        Playing,
        Paused,
        Stopped
    }
}
```

#### 2.2 动画格式标识常量（集中管理格式名称，低耦合）

```csharp
namespace Sonic.Animator.Core
{
    /// <summary>动画格式唯一标识常量</summary>
    public static class AnimationFormatIdentifier
    {
        public const string Spine = "Animation.Format.Spine";
        public const string Live2D = "Animation.Format.Live2D";
        public const string SpriteSheet = "Animation.Format.SpriteSheet";
    }
}
```

#### 2.3 全局管理器 & 注册中心（核心调度）

负责解析器、工厂的动态注册，资源加载、实例创建，完全依赖抽象，不感知具体格式。

```csharp
namespace Sonic.Animator.Core
{
    using System.Collections.Generic;
    using Sonic.Animator.Abstractions;

    /// <summary>动画全局管理器（注册、加载、实例创建统一入口）</summary>
    public static class AnimationManager
    {
        private static readonly List<IResourceParser> _resourceParsers = new List<IResourceParser>();
        private static readonly List<IAnimationInstanceFactory> _instanceFactories = new List<IAnimationInstanceFactory>();

        #region 注册能力（外部格式实现主动注册）
        public static void RegisterResourceParser(IResourceParser parser)
        {
            if (!_resourceParsers.Contains(parser))
                _resourceParsers.Add(parser);
        }

        public static void RegisterInstanceFactory(IAnimationInstanceFactory factory)
        {
            if (!_instanceFactories.Contains(factory))
                _instanceFactories.Add(factory);
        }
        #endregion

        #region 资源加载
        public static IAnimationResource LoadResource(string filePath)
        {
            foreach (var parser in _resourceParsers)
            {
                if (parser.CanParse(filePath))
                    return parser.Parse(filePath);
            }
            throw new NotSupportedException("未找到对应解析器，不支持当前文件格式");
        }

        public static async Task<IAnimationResource> LoadResourceAsync(string filePath)
        {
            foreach (var parser in _resourceParsers)
            {
                if (parser.CanParse(filePath))
                    return await parser.ParseAsync(filePath);
            }
            throw new NotSupportedException("未找到对应解析器，不支持当前文件格式");
        }
        #endregion

        #region 实例创建
        public static IAnimationInstance CreateAnimationInstance(IAnimationResource resource)
        {
            foreach (var factory in _instanceFactories)
            {
                if (factory.MatchResource(resource))
                    return factory.CreateInstance(resource);
            }
            throw new NotSupportedException("未找到对应实例工厂，无法创建动画实例");
        }
        #endregion
    }
}
```

---

### 模块 3：Runtime 通用运行层

存放 **全格式通用**的动画运算逻辑，与具体格式解耦：

1. 骨骼通用计算：关节变换、正向运动学、简易IK；
2. 网格形变通用算法：顶点偏移、坐标插值；
3. 动画状态机：状态切换、动画混合、过渡插值；
4. 帧时间管理：统一帧率、时间缩放、缓动函数库。

> 说明：该层提供通用算法工具类，具体格式（Spine/Live2D）按需调用，不耦合业务逻辑。

---

### 模块 4：Render 通用渲染层

1. **核心规则**：完全复用底层 `Sonic.IRenderCanvas` 绘制接口，不重复定义画布；
2. 能力范围：

- 通用图元批量绘制、纹理采样、透明度混合；
- 图层层级排序、遮罩裁剪、坐标矩阵转换；
- 多端渲染适配（桌面、Web、服务端）；

3. 设计：仅提供通用渲染工具类，具体格式的绘制逻辑下沉至 `Implementations`。

---

### 模块 5：Implementations 格式实现层（可插拔）

每个动画格式独立文件夹， **修改/新增/删除格式无需改动抽象层、核心层**，满足开闭原则。 通用结构（Spine / Live2D / SpriteSheet
统一模板）：

1. 格式专属 `IAnimationResource` 实现类；
2. 格式专属 `IResourceParser` 解析器；
3. 格式专属 `IAnimationInstanceFactory` 工厂；
4. 格式专属 `IAnimationInstance` 运行实例（实现链式方法、播放逻辑）；
5. 格式专属渲染逻辑。

以 Spine 为例，其余格式同结构复刻：

```csharp
namespace Sonic.Animator.Implementations.Spine
{
    using Sonic.Animator.Abstractions;
    using Sonic.Animator.Core;
    using Sonic;

    // 1. Spine 资源实现
    internal class SpineAnimationResource : IAnimationResource
    {
        public string ResourceIdentifier { get; }
        public string FormatIdentifier => AnimationFormatIdentifier.Spine;
        public float ContentWidth { get; }
        public float ContentHeight { get; }

        public SpineAnimationResource(string identifier, float width, float height)
        {
            ResourceIdentifier = identifier;
            ContentWidth = width;
            ContentHeight = height;
        }
    }

    // 2. Spine 解析器
    public class SpineResourceParser : IResourceParser
    {
        public bool CanParse(string filePath)
        {
            return filePath.EndsWith(".skel") || filePath.EndsWith(".json");
        }

        public IAnimationResource Parse(string filePath)
        {
            // 原生Spine文件解析逻辑
            return new SpineAnimationResource(filePath, 512, 512);
        }

        public async Task<IAnimationResource> ParseAsync(string filePath)
        {
            await Task.CompletedTask;
            return Parse(filePath);
        }
    }

    // 3. Spine 实例工厂
    public class SpineInstanceFactory : IAnimationInstanceFactory
    {
        public bool MatchResource(IAnimationResource resource)
        {
            return resource.FormatIdentifier == AnimationFormatIdentifier.Spine;
        }

        public IAnimationInstance CreateInstance(IAnimationResource resource)
        {
            return new SpineAnimationInstance(resource);
        }
    }

    // 4. Spine 运行实例（遵循统一接口与编码规范）
    internal class SpineAnimationInstance : IAnimationInstance
    {
        private readonly IAnimationResource _innerResource;
        public PlayStatus CurrentPlayStatus { get; private set; }
        public float HorizontalPosition { get; private set; }
        public float VerticalPosition { get; private set; }
        public float ScaleRatio { get; private set; } = 1.0f;
        public float OpacityValue { get; private set; } = 1.0f;
        public float PlaybackSpeed { get; private set; } = 1.0f;

        public SpineAnimationInstance(IAnimationResource resource)
        {
            _innerResource = resource;
            CurrentPlayStatus = PlayStatus.Idle;
        }

        #region 配置方法
        public IAnimationInstance MoveTo(float horizontal, float vertical)
        {
            HorizontalPosition = horizontal;
            VerticalPosition = vertical;
            return this;
        }

        public IAnimationInstance ScaleBy(float ratio)
        {
            ScaleRatio = ratio;
            return this;
        }

        public IAnimationInstance ApplyOpacity(float value)
        {
            OpacityValue = value;
            return this;
        }

        public IAnimationInstance AdjustPlaybackSpeed(float speed)
        {
            PlaybackSpeed = speed;
            return this;
        }
        #endregion

        #region 行为方法
        public IAnimationInstance Play(string animationName, bool loopExecution = true)
        {
            CurrentPlayStatus = PlayStatus.Playing;
            // Spine 播放逻辑
            return this;
        }

        public IAnimationInstance Pause()
        {
            CurrentPlayStatus = PlayStatus.Paused;
            return this;
        }

        public IAnimationInstance Stop()
        {
            CurrentPlayStatus = PlayStatus.Stopped;
            return this;
        }
        #endregion

        public void UpdateFrame(float deltaTime)
        {
            // 骨骼帧更新、变换计算
        }

        public void DrawToCanvas(IRenderCanvas canvas)
        {
            // 顶点、纹理绘制逻辑
        }
    }
}
```

---

### 模块 6：Extensions 扩展层

1. 通用辅助工具：坐标转换、资源批量管理、实例分组管理；
2. 插件扩展接口：支持自定义动画逻辑、自定义渲染效果；
3. 工具方法：动画预览、资源校验、格式转换辅助。

---

## 三、初始化 & 全局注册（应用启动入口）

按需注册格式，不需要的格式直接注释/删除，实现模块化裁剪：

```csharp
namespace Sonic.Animator
{
    using Sonic.Animator.Core;
    using Sonic.Animator.Implementations.Spine;
    using Sonic.Animator.Implementations.Live2D;
    using Sonic.Animator.Implementations.SpriteSheet;

    public static class AnimatorStartup
    {
        /// <summary>初始化动画模块，注册所需解析器与工厂</summary>
        public static void Initialize()
        {
            // 按需启用对应动画格式
            AnimationManager.RegisterResourceParser(new SpineResourceParser());
            AnimationManager.RegisterInstanceFactory(new SpineInstanceFactory());

            AnimationManager.RegisterResourceParser(new Live2DResourceParser());
            AnimationManager.RegisterInstanceFactory(new Live2DInstanceFactory());

            AnimationManager.RegisterResourceParser(new SpriteSheetResourceParser());
            AnimationManager.RegisterInstanceFactory(new SpriteSheetInstanceFactory());
        }
    }
}
```

---

## 四、标准调用示例（对外使用，风格与 Plotter 完全对齐）

### 4.1 基础使用流程

```csharp
using Sonic;
using Sonic.Animator;
using Sonic.Animator.Abstractions;

// 1. 模块初始化（应用启动执行一次）
AnimatorStartup.Initialize();

// 2. 加载动画资源（自动匹配解析器）
IAnimationResource animationResource = AnimationManager.LoadResource("Character.skel");

// 3. 创建实例 + 链式配置 + 播放控制（无缩写、无Set/With）
IAnimationInstance animationInstance = AnimationManager.CreateAnimationInstance(animationResource)
    .MoveTo(120, 200)
    .ScaleBy(1.0f)
    .ApplyOpacity(1.0f)
    .AdjustPlaybackSpeed(1.0f)
    .Play("Standby", loopExecution: true);

// 4. 帧循环（每帧执行）
float deltaTime = 0.016f;
animationInstance.UpdateFrame(deltaTime);
animationInstance.DrawToCanvas(targetRenderCanvas);
```

### 4.2 Live2D 扩展调用（同接口、同风格）

```csharp
var live2dResource = AnimationManager.LoadResource("Model.model3.json");
var live2dInstance = AnimationManager.CreateAnimationInstance(live2dResource)
    .MoveTo(200, 180)
    .Play("IdleMotion");
```

---

## 五、核心设计亮点 & 解耦说明

1. **彻底解耦**
   移除硬编码格式枚举，采用「接口+注册工厂」模式，新增/下线动画格式，仅修改 `Implementations` 和注册代码，核心、抽象层完全不变。
2. **模块化插拔**
   Spine、Live2D、序列帧相互独立，支持按需编译、按需注册，精简包体积。
3. **风格统一**
   命名、方法写法、链式调用规则与 `Sonic.Plotter` 完全对齐，团队学习、维护成本低。
4. **分层职责清晰**

- 抽象层：定义契约，长期稳定；
- 核心层：通用状态、调度、注册；
- 运行/渲染层：通用算法与绘制能力；
- 实现层：单一格式专属逻辑，职责单一。

5. **兼容性**
   复用底层 `Sonic.IRenderCanvas`，可与 `Plotter` 图表画布叠加渲染，满足混合可视化场景。

---

## 六、边界约束（红线规则）

1. 所有格式实现 **禁止直接依赖其他格式代码**；
2. 核心层、抽象层 **禁止出现任何具体格式名称、业务逻辑**；
3. 严格遵守编码规范：无缩写、无 `Set/With` 前缀；
4. 资源与运行实例分离，一份资源支持创建多个独立实例。

---

## 七、词义辨析（规避 Unity 错误命名误导）

### 7.1 基础词汇词源与标准语义

1. **animation** /ˌænɪˈmeɪʃn/（名词） 本义： **动画内容、动画资源、动画行为**，指向 **被驱动的客体**。 通用构词范式：
   `名词 + Controller` 是工业/编程领域标准命名，意为「管控XX内容的控制器」，例如 `Plot Controller`（绘图控制器）、
   `Audio Controller`（音频控制器）。 从语义逻辑上， **Animation Controller** 才是描述动画控制器/动画状态机的合理名称。

2. **animate** /ˈænɪmeɪt/（动词） 本义：赋予动态、驱动动画。

3. **animator** /ˈænɪmeɪtə (r)/（名词，动词 + -or 后缀） 标准词意： **执行动画动作的主体**，原生语义分为两类：

- 通用场景：动画师（人）；
- 技术场景：动画驱动器、动画执行载体（对标 `plotter` 绘图器，本项目沿用此语义，保持成对命名一致性）。

### 7.2 Unity 命名问题分析

1. **错误命名**：Unity 使用 `Animator Controller` 作为动画状态机/配置文件名称，属于 **语义冗余、层级错乱**。
   `animator` 本身已是「动画驱动器/执行载体」，叠加 `Controller` 后字面含义变为「管控驱动器的控制器」，多一层无效语义嵌套，违背英语通用命名习惯。
2. **历史成因**：该命名仅为 Unity 内部区分新旧动画系统的妥协方案，并非语言学、软件工程领域的通用标准，不具备跨项目参考价值。

### 7.3 本项目命名原则与对齐规则

1. **顶层模块对齐**
   遵循 `plot(绘图) → plotter(绘图器)` 标准范式，统一使用 `animate(驱动动画) → animator(动画驱动器)`，`Sonic.Animator` 与
   `Sonic.Plotter` 词性、定位、层级完全对等，逻辑自洽。
2. **控制器命名规范**
   本项目 **拒绝照搬 Unity 错误命名**，严格遵循通用英语构词逻辑：

- 若后续扩展动画控制器、状态机配置类组件，统一使用 **Animation Controller** 标准名称，表意直白、无语义冗余；
- 区分概念边界：
  - `Animator`：动画执行/驱动载体（本项目顶层模块）；
  - `Animation Controller`：动画状态、逻辑、切换规则的控制器；
  - `Animation Resource`：原始动画资源文件。

3. **开发约束**
   团队开发过程中，禁止以 Unity 术语作为参照标准；所有命名、概念释义均以 **词源语义 + 通用编程命名范式**为准，杜绝被非标准行业黑话带偏。

### 7.4 对标对照表（统一认知）

| 动作动词         | 执行载体（本项目标准命名） | 对应控制器（标准命名）          | Unity 非标准命名（仅作区分，不参考） |
|------------------|----------------------------|---------------------------------|--------------------------------------|
| plot 绘图        | plotter 绘图器             | Plot Controller 绘图控制器      | -                                    |
| animate 驱动动画 | animator 动画驱动器        | Animation Controller 动画控制器 | Animator Controller                  |