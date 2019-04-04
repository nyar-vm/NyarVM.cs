# Hypersonic 0.2.3 完整设计文档

> **版本**：0.2.3  
> **状态**：理想设计，不考虑向后兼容  
> **定位**：Sonic 标准库的纯属性门面库  
> **唯一职责**：定义属性（Attribute）、标记接口、枚举和数学常量，零运行时逻辑，零外部依赖（显式引入的
> `Microsoft.Extensions.Logging.Abstractions` 除外）  
> **强制配置**：`<ImplicitUsings>false</ImplicitUsings>`
> `<DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>`

---

## 目录

1. [引言与设计哲学](#1-引言与设计哲学)
2. [命名空间总览](#2-命名空间总览)
3. [因 BCL 重叠而留空的领域](#3-因-bcl-重叠而留空的领域)
4. [Sonic.Foundation](#4-soniccore)
5. [Sonic.Data](#5-sonicdata)
6. [Sonic.Net](#7-sonicnet)
7. [Sonic.Canvas](#8-soniccanvas)
8. [Sonic.Config](#9-sonicconfig)
9. [Sonic.Locale](#10-soniclocale)
10. [Sonic.Math](#11-sonicmath)
11. [Sonic.Flow](#12-sonicflow)
12. [Sonic.Infra](#13-sonicinfra)
13. [Sonic.Security](#14-sonicsecurity)
14. [Sonic.Observability](#15-sonicobservability)
15. [Sonic.Compiler](#16-soniccompiler)
16. [Sonic.Concurrency](#17-sonicconcurrency)
17. [Sonic.Process](#18-sonicprocess)
18. [Sonic.Compression](#19-soniccompression)
19. [Sonic.Text](#20-sonictext)
20. [Sonic.Memory](#21-sonicmemory)
21. [Sonic.Stream](#22-sonicstream)
22. [Sonic.Hardware](#23-sonichardware)
23. [Sonic.State](#24-sonicstate)
24. [Sonic.Plugin](#25-sonicplugin)
25. [Sonic.Widget](#26-sonicwidget)
26. [Sonic.Simulation](#27-sonicsimulation)
27. [Sonic.AI](#28-sonicai)
28. [Sonic.Geo](#29-sonicgeo)
29. [Sonic.Finance](#30-sonicfinance)
30. [结语与生态展望](#31-结语与生态展望)

---

## 1. 引言与设计哲学

Hypersonic 0.2.3 是 Sonic 标准库的 **纯属性门面库**。它不包含任何可执行代码、不引用任何外部 NuGet 包（除显式声明的日志抽象包）、不实现任何
Source Generator。它的唯一使命是提供一套统一、稳定、正交且可组合的 **编译期标记语言**，让 C# 开发者通过简单的 `[Data]`、
`[Command]`、`[HttpClient]` 等标注即可声明意图，而由具体的实现库（`Sonic` 等）在编译期完成代码生成。

这一设计直接回应了软件生态的核心痛点： **接口分裂**。在 Hypersonic 之前，每个框架都定义自己的 `[CliApp]` 或 `[AutoMap]`
属性，导致开发者被锁定在特定实现上。Hypersonic 将这些标记提取为公共契约，使得任意实现者都可以消费相同的属性，而开发者只需学习一套声明，即可自由替换底层引擎。

**核心原则**：

- **极简零依赖**：Hypersonic 不依赖任何运行时，仅包含 `System.Runtime` 等绝对必要的编译框架引用，日志抽象通过显式
  `PackageReference` 引入。
- **正交可组合**：每个属性只做一件事，多个属性可以自由叠加在同一目标上，互不干扰。
- **稳定命名空间**：所有类型均位于 `Sonic.*` 命名空间下，保证代码外观一致，用户无需区分类型来自 Hypersonic 还是 Sonic。
- **意图驱动**：用 `[Model]`、`[Config]`、`[Entity]` 等精准标记表达设计意图，而不仅仅是一把抓的“数据”。
- **开放扩展**：任何人都可以编写新的生成器或运行时来消费这些属性，Hypersonic 不强制使用 Sonic 实现。
- **词性统一**：所有一级命名空间均为名词或公认名词缩写，杜绝动词、动名词等不稳定的词性。
- **避免领域污染**：不引入特定框架的专有名词，保持抽象层级的通用性；不与 .NET BCL 已提供的集合、IO、套接字等基础类型发生冲突。
- **明确留空**：对于已被 BCL 充分覆盖的领域，Hypersonic 明确留空，避免生态分裂。

本版（0.2.3）在 0.2.1 基础上进行了重大修订：纠正了词性不一致的命名空间，降级了 HTTP 等协议至网络子域，移除了与 BCL 重叠的
`Sonic.Collection`
，新增了编译器基础设施、张量计算、硬件抽象、任务调度、消息事件、缓存、搜索、AI、地理、金融、加密、认证等关键领域；进一步新增流处理、响应式状态、插件系统、物理仿真、状态机、寻路、沙箱、审计、可观察序列、UI
控件体系（Widget）、时间抽象等，并引入 19 处 BCL 留空占位，最终形成了覆盖全栈开发需求的 26 个顶层命名空间。

---

## 2. 命名空间总览

```
Sonic
├── Core                     // 基础元标记、ADT、DI
├── Data                     // 数据契约、缓存、搜索
├── Media                    // 多媒体
├── Net                      // 网络协议
├── Canvas                   // 绘图与图表
├── Config                   // 配置系统
├── Locale                   // 国际化
├── Math                     // 数学 trait、常量、张量
├── Flow                     // 工作流、消息、流处理
├── Infra                    // 基础设施即代码
├── Security                 // 安全、认证、授权、审计、沙箱、加密
├── Observability            // 可观测性
├── Compiler                 // 编译器、转译、FFI、开发工具
├── Concurrency              // 并发模型
├── Process                  // 进程管理
├── Compression              // 压缩抽象
├── Text                     // 文本抽象
├── Memory                   // 内存池抽象
├── Stream                   // 高级流抽象
├── Hardware                 // 硬件输入/输出抽象
├── State                    // 响应式状态、可观察序列、时间
├── Plugin                   // 插件/扩展系统
├── Widget                   // 通用 UI 控件 (含终端)
├── Simulation               // 仿真与行为建模
├── AI                       // 人工智能推理
├── Geo                      // 地理空间
└── Finance                  // 金融与货币
```

---

## 3. 因 BCL 重叠而留空的领域

以下领域在 Hypersonic 中 **故意不定义**，因其概念已被 .NET BCL 充分覆盖。列出它们是为了表明设计时已审视过这些方向，而非遗漏。

| 留空领域              | 对应 BCL 覆盖                                 |
|-----------------------|-----------------------------------------------|
| Collection            | `System.Collections.Generic`                  |
| Serialization         | `System.Text.Json` / `System.Xml`             |
| Sockets               | `System.Net.Sockets`                          |
| TCP / UDP             | `System.Net`                                  |
| Configuration Binding | `Microsoft.Extensions.Configuration`          |
| Globalization         | `System.Globalization`                        |
| Numerics              | `System.Numerics`                             |
| Basic Math            | `System.Math`                                 |
| Crypto Primitives     | `System.Security.Cryptography`                |
| Logging               | `Microsoft.Extensions.Logging`                |
| Reflection            | `System.Reflection`                           |
| Threading / Tasks     | `System.Threading` / `System.Threading.Tasks` |
| Diagnostics           | `System.Diagnostics`                          |
| Regex                 | `System.Text.RegularExpressions`              |
| IO / FileSystem       | `System.IO`                                   |
| Pipes                 | `System.IO.Pipes`                             |
| DateTime / TimeSpan   | `System.DateTime` / `System.TimeSpan`         |
| Web 核心抽象          | `Microsoft.AspNetCore.*`                      |
| LINQ                  | `System.Linq`                                 |

---

## 4. Sonic.Foundation

提供全生态共享的元标记和通用枚举，所有其他命名空间均可引用。包含代数数据类型（ADT）标记和依赖注入（DI）标记，是框架的基石。

### 4.1 属性与枚举

**`[SonicExperimental]`**  
标记尚未稳定的 API，使用时会触发编译警告。

**`[SonicInternal]`**  
标记仅供 Sonic 内部使用的类型，生成器应忽略这些类型以避免误暴露。

**`[GenerateFrom]`**  
指示 Source Generator 从外部模板或定义生成代码。

**`SonicVisibility`** 枚举  
控制生成成员的可见性：`Public`、`Internal`、`Private`。

**`[GenerateMatch]`** （在 `Sonic.Foundation.ADT` 子空间中）  
标记并集类型，生成模式匹配 `Match` 方法。

**`[UnionCase]`**  
可选，标记联合类型的各个分支。

### 4.2 依赖注入标记（`Sonic.Foundation.DI`）

**`[Service]`**  
标记类为服务，`ServiceLifetime` 枚举：`Singleton`、`Scoped`、`Transient`。

**`[Inject]`**  
标记构造函数或属性为注入点。

**`[GenerateFactory]`**  
为标记类生成工厂。

### 4.3 构建器标记（`Sonic.Foundation.DI.Builder`）

**`[GenerateBuilder]`**  
为类型生成流式 Builder。

**`[With]`**  
可标记字段或属性，自定义 Builder 方法名。

### 4.4 用法示例

```csharp
namespace MyApp;

[SonicExperimental(Feature = "RecordsUnion", Since = "0.2.3")]
[GenerateMatch]
public abstract record Shape
{
    [UnionCase] public record Circle(double Radius) : Shape;
    [UnionCase] public record Rectangle(double Width, double Height) : Shape;
}

// 用法：生成 Match 方法，编译期穷举分支
var area = shape.Match(
    circle => Math.PI * circle.Radius * circle.Radius,
    rect => rect.Width * rect.Height
);
```

```csharp
[Service(ServiceLifetime.Singleton)]
public partial class UserRepository
{
    [Inject] private readonly IDatabase _db;
}

[GenerateFactory]
public interface IServiceFactory
{
    UserRepository CreateUserRepository();
}

// 生成的服务工厂自动解析依赖，避免反射
var factory = new ServiceFactory(); // 生成
var repo = factory.CreateUserRepository();
```

---

## 5. Sonic.Data

数据领域是 Hypersonic 中最庞大的部分，用正交属性体系取代传统零散的序列化/持久化标记。包含数据契约、存储映射、缓存和搜索。

### 5.1 意图标记属性（`Sonic.Data.Attributes`）

**`[Data]`**  
最通用的数据标记，表示“这是一个需要序列化、验证、元数据支持的数据类型”。

**`[Model]`**  
领域模型，富含业务逻辑，默认开启存储映射和乐观锁支持。

**`[Entity]`**  
纯持久化对象，专为数据库映射优化，默认关闭 JSON 序列化。

**`[Config]`**  
配置模型，与 `Sonic.Config` 联动生成强类型绑定。

**`[View]`**  
只读投影，仅生成反序列化逻辑。

**`[CommandData]`**  
命令/请求 DTO，生成输入验证与反序列化。

**字段级属性**：  
`[Field]`、`[Key]`、`[Version]`、`[Sensitive]`、`[Since]`、`[Deprecated]`、`[IgnoreData]`、`[Transactional]`、`[Migration]`。

### 5.2 契约（`Sonic.Data.Contract`）

- `IValidator<T>`：`ValidationResult Validate(T instance);`
- `ValidationResult`：不可变记录，包含 `ImmutableArray<ValidationError> Errors`、
  `ImmutableArray<ValidationWarning> Warnings`。
- `ISchema<T>`：提供 `TypeMeta GetMeta()`，包含字段元数据集合。

### 5.3 存储映射（`Sonic.Data.Storage`）

- `IEntityMapper<T>`：提供表名、列映射、主键信息。
- 枚举 `StorageType`（`Int32`, `Int64`, `String`, `Float64`, `Boolean`, `Blob`, `DateTime`, `Guid`）。
- `ConcurrencyStrategy` 枚举（`None`, `Optimistic`, `Pessimistic`）。

### 5.4 缓存（`Sonic.Data.Cache`）

- `[Cache]`：标记方法结果缓存，`Duration` 秒数。
- `[CacheKey]`：标记参数作为缓存键。
- `[EvictCache]`：标记方法执行后清除缓存。
- `IDistributedCache`、`IMemoryCache<T>`、`ICacheSerializer`。

### 5.5 全文检索（`Sonic.Data.Search`）

- `[SearchDocument]`、`[FullText]`、`[Keyword]`。
- `ISearchIndex<T>`、`ISearchQuery`、`IFieldAnalyzer`。

### 5.6 用法示例

```csharp
[Data(GenerateStorageMapper = true)]
public partial class Customer
{
    [Key] public int Id { get; init; }
    [Field(Name = "full_name", Required = true)]
    public string Name { get; init; }
    [Version] public int Version { get; init; }
    [Sensitive] public string CreditCard { get; init; }
}

// 自动生成：序列化器、验证器、存储映射、Schema
var customer = new Customer { Id = 1, Name = "Alice" };
var json = CustomerSerializer.Serialize(customer);
var errors = CustomerValidator.Validate(customer);
```

```csharp
public interface IProductService
{
    [Cache(Duration = 300)]
    Task<Product> GetProductAsync([CacheKey] int productId);

    [EvictCache]
    Task UpdateProductAsync(Product product);
}

// 生成器织入缓存逻辑：首次调用查询并缓存，后续从缓存获取，
// 更新时自动清除对应缓存项。
```

---

## 6. Sonic.Media

提供图像、音频、视频帧及滤镜管线、硬件加速等声明式契约。

### 6.1 标记属性

- `[Image]`：宽度、高度、像素格式。
- `[Audio]`：采样率、声道数、样本格式。
- `[VideoFrame]`：宽度、高度、像素格式、时间戳。
- `[HardwareAccelerated]`：标记编解码器或滤镜支持硬件加速。

### 6.2 枚举

`PixelFormat`（`Rgba32`, `Bgra32`, `Yuv420p`, ...），`SampleFormat`（`Float32`, `Int16`, ...），`ChannelLayout`（`Mono`,
`Stereo`, `Surround5_1`）。

### 6.3 接口

- `IImage`：`int Width { get; } int Height { get; } PixelFormat Format { get; } ReadOnlySpan<byte> Data { get; }`
- `IAudio`：`int SampleRate { get; } int Channels { get; } SampleFormat Format { get; } ReadOnlySpan<byte> Data { get; }`
- `IVideoFrame`：`int Width { get; } int Height { get; } PixelFormat Format { get; } long Timestamp { get; }`
- `IMediaDecoder`、`IMediaEncoder`、`IMediaMuxer`、`IMediaDemuxer`、`IFilterGraph`、`IHardwareCodec`、`IDeviceContext`。

### 6.4 用法示例

```csharp
[Image(Width = 1920, Height = 1080, PixelFormat = PixelFormat.Rgba32)]
public readonly partial struct Screenshot
{
    public ReadOnlyMemory<byte> Data { get; init; }
}

[Audio(SampleRate = 44100, Channels = 2, SampleFormat = SampleFormat.Float32)]
public readonly partial struct SoundClip
{
    public ReadOnlyMemory<float> Samples { get; init; }
}

// 标记一个硬件加速的 H.264 解码器
[HardwareAccelerated]
public interface IH264Decoder : IMediaDecoder { }

// 实现库根据属性自动生成编解码图编排代码
var decoder = DeviceContext.CreateDecoder<IH264Decoder>();
var frame = decoder.Decode(videoPacket);
```

---

## 7. Sonic.Net

网络协议抽象，包含 HTTP 客户端/服务端、RPC 和实时双向通信（Signal）。

### 7.1 `Sonic.Net.Http`

- `[HttpClient]`：标记接口为 HTTP 客户端契约，`BaseUrl` 等。
- `[Get]`、`[Post]`、`[Put]`、`[Delete]`、`[Patch]`：路由路径。
- `[Header]`、`[Body]`、`[Query]`。
- `[HttpServer]`、`[Route]`、`[Middleware]`。
- `HttpMethod` 枚举。

### 7.2 `Sonic.Net.Rpc`

- `[RpcService]`：标记 RPC 服务接口。
- `[RpcMethod]`：标记方法，`RpcStreamType` 枚举（`Unary`, `ServerStream`, `ClientStream`, `Duplex`）。

### 7.3 `Sonic.Net.Signal`

- `[SignalR]`：标记 SignalR Hub。
- `[Push]`：服务端推送主题。
- `[Stream]`：流式传输。

### 7.4 用法示例

```csharp
[HttpClient(BaseUrl = "https://api.example.com")]
public interface IWeatherApi
{
    [Get("/weather/{city}")]
    Task<WeatherData> GetWeatherAsync(string city, [Query] string units = "metric");

    [Post("/weather/batch")]
    Task<IReadOnlyList<WeatherData>> GetBatchAsync([Body] IReadOnlyList<string> cities);
}

// 生成器生成强类型 HttpClient 实现，无需手动拼接 URL
var api = HttpClientFactory.Create<IWeatherApi>();
var weather = await api.GetWeatherAsync("London");
```

```csharp
[RpcService]
public interface IChatService
{
    [RpcMethod(StreamType = RpcStreamType.Duplex)]
    IAsyncEnumerable<ChatMessage> ChatAsync(IAsyncEnumerable<ChatMessage> clientStream);
}

// 生成 gRPC 服务定义和客户端 Stub
```

```csharp
[SignalR(HubName = "notifications")]
public interface INotificationHub
{
    [Push(Topic = "alerts")]
    Task SendAlertAsync(Alert alert);
}

// 生成 SignalR Hub 和客户端调用代码
```

---

## 8. Sonic.Canvas

声明式绘图与图表抽象。`Sonic.Canvas` 提供通用绘图表面，`Sonic.Canvas.Chart` 提供数据图表标记。

### 8.1 属性与接口

- `[Canvas]`：声明绘图表面（宽、高）。
- `ICanvas`：画布操作。
- `IPath`、`IBrush`。
- `[Chart]`：图表定义。
- `[Series]`、`[Axis]`、`[Legend]`。
- `IChart`、`ISeries`、`IAxis`、`ILegend`。

### 8.2 用法示例

```csharp
[Canvas(Width = 800, Height = 600)]
public partial class MyDrawing
{
    // 生成器可提供 FillRectangle, DrawText 等强类型方法
    public void Render(ICanvas canvas)
    {
        canvas.Clear(Brushes.White);
        canvas.DrawString("Hello Hypersonic", 50, 50);
    }
}
```

```csharp
[Chart]
public partial class SalesChart
{
    [Series(Name = "Revenue", Color = "#FF5733")]
    public IReadOnlyList<DataPoint> RevenueData { get; init; }

    [Axis(Type = AxisType.Category)]
    public IReadOnlyList<string> Months { get; init; }

    [Legend]
    public bool ShowLegend { get; init; } = true;
}

// 生成图表渲染代码，绑定到 Canvas 或 UI 组件
var chart = new SalesChart { RevenueData = data, Months = labels };
var image = ChartRenderer.Render(chart);
```

---

## 9. Sonic.Config

配置系统标记，与 `Sonic.Data.Attributes.Config` 联动。

- `[ConfigSection]`：标记类为配置节。
- `IConfigProvider`：提供配置值。
- `IConfigWatcher`：配置变更回调。

**用法**：

```csharp
[ConfigSection("Database")]
public partial class DbConfig
{
    public string ConnectionString { get; init; }
    public int MaxPoolSize { get; init; } = 100;
}

// 生成强类型绑定代码，从 appsettings.json 自动加载
var dbConfig = ConfigBinder.Bind<DbConfig>();
```

---

## 10. Sonic.Locale

国际化资源标记。

- `[LocaleResource]`：标记资源类，从 `.ftl` 或 `.json` 生成强类型方法。
- `IStringLocalizer`：`string Get(string name, params object[] args);`

**用法**：

```csharp
[LocaleResource]
public partial class AppStrings
{
    public static string WelcomeMessage(string user) => Get("welcome", user);
    public static string ItemCount(int count) => Get("item_count", count);
}

// 生成器从对应 .ftl 文件提取字符串，编译时检查参数
Console.WriteLine(AppStrings.WelcomeMessage("Alice"));
// 输出："欢迎，Alice！"
```

---

## 11. Sonic.Math

数学 trait、常量和张量计算。

### 11.1 数学 Trait

- `IAdditive<T>`、`IMultiplicative<T>`、`IZero<T>`、`IOne<T>`、`IFloat<T>`、`IApproximate<T>`、`IReal<T>`、`IInteger<T>`。

### 11.2 线性代数（移除至 `Sonic.Math` 一级）

- `IVector<T, D>`、`IMatrix<T, R, C>`、`ITransform<T>`、`IQuaternion<T>`。

### 11.3 图论

- `IGraph<TNode, TEdge>`。

### 11.4 常量

`Sonic.Math.Constants` 类：`PI`, `E`, `Tau`, `GoldenRatio`。

### 11.5 `Sonic.Math.Tensor`

张量、计算图、自动微分。

- `ITensor<T>`、`IShape`、`IComputeGraph`、`IComputeNode`、`ITape`、`IOptimizer`、`ILoss`、`IComputeDevice`。
- `DeviceType` 枚举：`CPU`, `CUDA`, `OpenCL`, `Metal`, `Vulkan`, `TPU`。
- `[Tensor]`、`[Module]`、`[Autograd]`、`[Device]` 属性。

### 11.6 用法示例

```csharp
using Sonic.Math;
using static Sonic.Math.Constants;

double circumference = 2 * PI * radius;

public partial class LinearLayer : IModule
{
    [Tensor] private float[,] weights;
    
    [Autograd]
    public ITensor<float> Forward(ITensor<float> input)
    {
        return input.MatMul(weights);
    }
}

// 生成器自动生成反向传播代码
```

---

## 12. Sonic.Flow

工作流、消息和流处理。

### 12.1 `Sonic.Flow.Workflow`

- `[Workflow]`、`[Step]`、`[Compensation]`。
- `ISaga<T>`：Saga 状态持久化。

### 12.2 `Sonic.Flow.Message`

- `[EventHandler]`、`[MessageHandler]`、`[DeadLetter]`。
- `IEventBus`、`IMessageHandler<T>`、`IEventStore`。
- `DeliveryGuarantee` 枚举。

### 12.3 `Sonic.Flow.Pipeline`

- `[StreamSource]`、`[StreamProcessor]`、`[StreamSink]`、`[Window]`。
- `IPipeline<TIn, TOut>`、`IStreamWindow<T>`。

### 12.4 用法示例

```csharp
[Workflow]
public partial class OrderSaga : ISaga<OrderState>
{
    [Step(DependsOn = nameof(ReserveInventory))]
    public async Task ProcessPayment(OrderState state) { /*...*/ }
    
    [Step]
    public async Task ReserveInventory(OrderState state) { /*...*/ }
    
    [Compensation]
    public async Task CancelOrder(OrderState state) { /*...*/ }
}

// 生成器构建步骤编排器，自动处理补偿
```

```csharp
[StreamSource]
public interface ITemperatureSensor
{
    IObservable<TemperatureReading> Readings { get; }
}

[StreamProcessor]
public partial class AnomalyDetector : IPipeline<TemperatureReading, Alert>
{
    [Window(Type = WindowType.Tumbling, Size = 10, Unit = TimeUnit.Seconds)]
    public Alert ProcessWindow(IReadOnlyList<TemperatureReading> window)
    {
        // 检测异常
    }
}

// 生成流处理管道拓扑
```

---

## 13. Sonic.Infra

基础设施即代码的声明式标记。

- `[InfraStack]`、`[InfraFunction]`、`[Queue]`、`[Table]`、`[Bucket]`、`[ContainerImage]`、`[ImageLayer]`。
- `ICloudResource`。

**用法**：

```csharp
[InfraStack("Production")]
public partial class AppInfra
{
    [Queue("orders")] public IQueue OrdersQueue { get; }
    [Table("customers")] public ITable CustomersTable { get; }
    [Bucket("assets")] public IBucket AssetsBucket { get; }
    [ContainerImage] public IContainerImage AppImage { get; }
}

// 生成 Pulumi/Terraform 等 IaC 脚本
```

---

## 14. Sonic.Security

统一的安全边界，包含认证、授权、审计、沙箱和加密服务工厂。

### 14.1 `Sonic.Security.Authentication`

- `[Authenticate]`、`IAuthenticator<T>`。

### 14.2 `Sonic.Security.Authorization`

- `[Authorize]`、`[Permission]`、`[RequirePermission]`、`[Role]`、`[Policy]`。
- `IPermissionSet`、`IRoleProvider`、`IPolicyEvaluator<T>`。

### 14.3 `Sonic.Security.Audit`

- `[Auditable]`、`[AuditField]`、`[AuditIgnore]`。
- `IAuditEntry`、`IAuditStore`。

### 14.4 `Sonic.Security.Sandbox`

- `[Sandbox]`、`[Restrict]`。
- `SandboxPolicy`、`ResourceKind`、`AccessPermission` 枚举。

### 14.5 `Sonic.Security.Cryptography`

- `[CryptoService]`。
- `IHashAlgorithm`、`ISymmetricCipher`、`IAsymmetricSigner`、`IKeyDerivation`。
- `HashAlgorithm`、`CipherMode` 枚举。

### 14.6 用法示例

```csharp
[Authenticate]
public partial class LoginService
{
    [Auditable(Category = "Auth")]
    public async Task<User> LoginAsync(string username, string password)
    {
        // 自动生成认证逻辑和审计记录
    }
}

[Authorize]
public partial class DocumentService
{
    [RequirePermission("documents:read")]
    public Task<Document> GetDocumentAsync(string id) => ...;

    [RequirePermission("documents:write")]
    [Auditable]
    public Task UpdateDocumentAsync(Document doc) => ...;
}
```

```csharp
[Sandbox(Policy = SandboxPolicy.DenyAll)]
[Restrict(Resource = ResourceKind.FileSystem, Permission = AccessPermission.Read)]
[Restrict(Resource = ResourceKind.Network, Permission = AccessPermission.None)]
public partial class PluginSandbox
{
    public void Execute(IPlugin plugin)
    {
        // 生成沙箱包装代码
    }
}
```

---

## 15. Sonic.Observability

可观测性标记，包括日志、指标、追踪。

- `[Observable]`：自动注入日志和指标。
- `[Metric]`：标记指标定义（`MetricType` 枚举：`Counter`, `Gauge`, `Histogram`）。
- `ISonicLogger`：极简日志抽象（`void Log(LogLevel level, string message)`）。
- `LogLevel` 枚举：`Trace`, `Debug`, `Info`, `Warning`, `Error`, `Critical`。

**用法**：

```csharp
[Observable]
public partial class OrderProcessor
{
    [Metric(MetricType.Counter, Name = "orders_processed")]
    public async Task ProcessAsync(Order order)
    {
        // 自动记录日志、统计调用次数和耗时
    }
}

// 自动生成计数器、跨度等
```

---

## 16. Sonic.Compiler

编译器基础设施、跨语言转译、外部函数接口和开发工具链。

### 16.1 编译器核心

- `ILexer`、`IParser`、`ISyntaxTree`、`ISyntaxNode`、`IToken`。
- `ISemanticModel`、`ISymbol`、`ISymbolTable`。
- `IIntermediateRepresentation`、`IPass`、`IPassPipeline`、`IRewriteRule`、`ICompilationUnit`。
- `ICodeGenerator<TTarget>`、`IInterpreter`、`ICompiler`。
- `[SyntaxNode]`、`[Token]`、`[Grammar]`、`[GenerateLexer]`、`[GenerateParser]`、`[CompileTo]`。

### 16.2 `Sonic.Compiler.Transpilation`

- `[TranspileTo]`、`TargetLanguage` 枚举。

### 16.3 `Sonic.Compiler.FFI`

- `[FFI]`、`IForeignLibrary`、`CallingConvention` 枚举。

### 16.4 `Sonic.Compiler.Development`

- **`BuildTask`**：`[BuildTask]`、`IBuildTask`。
- **`Verification`**：`[PropertyTest]`、`[Benchmark]`、`IPropertyTest`、`IBenchmark`。
- **`Assertion`**：`IAssert`、`IAssert<T>`。
- **`Document`**：`[DocExample]`、`IDocGenerator`。
- **`Document.Template`**：`[Template]`、`ITemplateEngine`。

### 16.5 用法示例

```csharp
[Grammar]
public enum MyTokenKind { Number, Plus, Minus, Eof }

[SyntaxNode]
public abstract record Expression { }

[SyntaxNode]
public record BinaryExpression(Expression Left, MyTokenKind Op, Expression Right) : Expression;

[GenerateLexer, GenerateParser]
public partial class Calculator
{
    [CompileTo(TargetArchitecture.Wasm)]
    public static int Evaluate(string source) => /* 生成器填充 */ 0;
}
```

```csharp
[BuildTask]
public partial class PreBuildTask : IBuildTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
// 项目文件中的 BeforeBuild 目标自动调用此任务
```

```csharp
[PropertyTest]
public partial class ListProperties
{
    public bool ReversePreservesCount(List<int> list)
    {
        var reversed = new List<int>(list);
        reversed.Reverse();
        return reversed.Count == list.Count;
    }
}
// 生成器执行随机测试
```

---

## 17. Sonic.Concurrency

并发模型抽象：Actor 和 Channel。

- `[Actor]`、`[Channel]`。
- `IActor`、`IChannel<T>`。

**用法**：

```csharp
[Actor]
public partial class WorkerActor
{
    public async Task ProcessAsync(IMessage message) { /*...*/ }
}

[Channel]
public partial class TaskChannel : IChannel<WorkItem>
{
    public void Write(WorkItem item) { }
    public Task<WorkItem> ReadAsync() => null;
}

// 生成 Actor 消息循环和 Channel 的生产者/消费者管道
```

---

## 18. Sonic.Process

外部进程管理抽象接口。

- `IProcess`：启动、终止、标准输入/输出。
- `IProcessBuilder`：配置进程启动信息。

**用法**：

```csharp
public interface IProcessExecutor
{
    [Process]
    IProcess Start(string fileName, string arguments);
}

// 生成平台相关的进程启动代码，支持可注入的测试替代
```

---

## 19. Sonic.Compression

压缩/解压抽象。

- `[Compress]`：标记类型默认压缩算法（`CompressionAlgorithm` 枚举：`Gzip`, `Deflate`, `Brotli`, `Zstd`, `Lz4`）。
- `ICompressor`、`IDecompressor`。

**用法**：

```csharp
[Compress(CompressionAlgorithm.Zstd)]
public partial class Archive
{
    public Stream Data { get; init; }
}

// 生成 Compress/Decompress 扩展方法，自动选择算法
using var compressed = archive.Compress();
```

---

## 20. Sonic.Text

文本处理抽象，不涉及具体编码实现，仅提供接口。

- `IText`：不可变文本。
- `ITextView`：文本视图。
- `ITextBuilder`：可变文本构建器。
- `TextEncoding` 枚举（`Utf8`, `Utf16`, `Ascii`）。

**用法**：

```csharp
[TextEncoding(TextEncoding.Utf8)]
public readonly partial struct Utf8String
{
    public ReadOnlyMemory<byte> Bytes { get; init; }
}

// 生成与 System.String 的转换方法和内存高效操作
```

---

## 21. Sonic.Memory

内存管理抽象，与 BCL `System.Buffers` 互补。

- `IMemoryOwner<T>`：拥有内存的所有权。
- `IMemoryPool<T>`：内存池。

**用法**：

```csharp
public partial class ImageProcessor
{
    [MemoryPool(MinimumSegmentSize = 4096)]
    private IMemoryPool<byte> _pool;

    public void Process()
    {
        using var owner = _pool.Rent(1024);
        // ...
    }
}
// 生成器注入内存池实例
```

---

## 22. Sonic.Stream

高级流抽象，区别于 `System.IO.Stream`。

- `IBitStream`：位级别的读写。
- `IBufferWriter<T>`：独立于 BCL 的缓冲写入器。
- `IAsyncStream`：异步流接口。

**用法**：

```csharp
public partial class BitPacker
{
    [BitStream]
    public void Pack(IBitStream stream, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            stream.WriteBits(b, 8);
    }
}
// 生成位操作实现
```

---

## 23. Sonic.Hardware

硬件抽象，提供统一的输入源、输出目标和设备能力描述。

- `IInputSource`：`IAsyncEnumerable<IInputEvent> Events { get; }` `InputCapability Capabilities { get; }`
- `IOutputSink`：`void Present(object content);`
- `IInputEvent`：`long Timestamp { get; } string DeviceId { get; } InputEventType Type { get; }`
- `IDevice`：
  `string Name { get; } IReadOnlySet<InputCapability> InputCapabilities { get; } IReadOnlySet<OutputCapability> OutputCapabilities { get; }`
- 枚举：`InputCapability`（`None`, `Pointing`, `Keying`, `Motion`, `Touch`, `Voice`, `Gaze`），`OutputCapability`（`None`,
  `Display`, `Speaker`, `Haptic`, `Printer`），`InputEventType`（`KeyDown`, `KeyUp`, `PointerMove` 等）。

**用法**：

```csharp
public partial class InputManager
{
    [InputSource]
    public IInputSource Mouse { get; init; }

    public async IAsyncEnumerable<IInputEvent> ProcessEvents()
    {
        await foreach (var e in Mouse.Events)
            yield return e;
    }
}
// 平台实现提供具体输入源
```

---

## 24. Sonic.State

响应式状态管理、可观察序列和时间抽象。

### 24.1 `Sonic.State` 核心

- `[Observable]`：标记属性为可观察状态。
- `[Derived]`：标记计算属性。
- `[Store]`：标记状态存储容器。
- `IReadableState<T>`、`IWritableState<T>`、`IStateStore`。

### 24.2 `Sonic.State.Observable`

- `[Debounce]`、`[Throttle]`、`[DistinctUntilChanged]`、`[CombineLatest]`、`[Merge]`、`[Switch]`、`[Catch]`。
- `IObservableSource<T>`、`IObservableOperator<TIn, TOut>`。

### 24.3 `Sonic.State.Chrono`

- `[Timestamp]`：高精度时间戳字段。
- `IClock`、`IMonotonicClock`、`IDuration`、`ITimeProvider`。
- `DateTimePrecision`、`ClockKind` 枚举。
- **`Sonic.State.Chrono.Scheduling`**：`[CronJob]`、`[Retry]`、`[Backoff]`，`IScheduler`、`IBackgroundJob`，`JobStatus`、
  `RetryPolicy` 枚举。

### 24.4 用法示例

```csharp
[Store]
public partial class AppState
{
    [Observable] public string SearchText { get; set; }
    [Derived] public bool IsSearching => !string.IsNullOrEmpty(SearchText);

    [Debounce(Milliseconds = 300)]
    public IObservable<string> SearchSuggestions => SearchTextChanged
        .Throttle(TimeSpan.FromMilliseconds(300))
        .DistinctUntilChanged();
}

// 自动生成 INotifyPropertyChanged 实现和 Rx 管道
```

```csharp
[CronJob("0 */6 * * *")]
[Retry(Policy = RetryPolicy.ExponentialBackoff, MaxRetries = 3)]
public partial class DataSyncJob : IBackgroundJob
{
    public async Task ExecuteAsync() { /*...*/ }
}

// 生成调度器注册代码
```

---

## 25. Sonic.Plugin

插件/扩展系统。

- `[Plugin]`：标记类为可加载插件。
- `[ExtensionPoint]`：标记接口为扩展点。
- `IPluginHost`、`IPlugin`。

**用法**：

```csharp
[ExtensionPoint]
public interface IImageFilter
{
    void Apply(Image image);
}

[Plugin(Name = "BlurFilter", Version = "1.0.0")]
public partial class BlurFilter : IImageFilter
{
    public void Apply(Image image) { /*...*/ }
}

// 生成插件加载和扩展发现代码
var host = new PluginHost();
host.Load("plugins/");
var filters = host.GetExtensions<IImageFilter>();
```

---

## 26. Sonic.Widget

通用 UI 控件系统，包含布局、样式、窗口管理、热重载、前端岛屿架构和终端 UI。

### 26.1 `Sonic.Widget` 核心

- `[Widget]`、`[Page]`、`[Dialog]`、`[Popup]`、`[Toast]`。
- `[Bindable]`、`[DependsOn]`、`[Convert]`、`[NavigateTo]`、`[Route]`。
- `IWidget`、`IPage`、`IDialog`、`INavigationService`、`IValueConverter`。

### 26.2 `Sonic.Widget.Layout`

- `[Grid]`、`[Stack]`、`[Flex]`、`[Dock]`、`[Canvas]`、`[UniformGrid]`。
- `ILayoutContainer`、`ILayoutItem`。
- 枚举：`Orientation`、`FlexDirection`、`DockPosition`。

### 26.3 `Sonic.Widget.Style`

- `[Style]`、`[ApplyStyle]`、`[Theme]`、`[Resource]`、`[Variant]`。
- `IStyleProvider`、`IStyle`、`IThemeManager`、`IResourceDictionary`。

### 26.4 `Sonic.Widget.Window`

- `IWindowManager`、`IWindow`、`IPopupService`、`IToastService`。
- `WindowState` 枚举。

### 26.5 `Sonic.Widget.Hmr`

- `[HotReload]`、`[AcceptState]`、`[ModuleScope]`。

### 26.6 `Sonic.Widget.Island`

- `[Island]`、`[Hydrate]`、`[StaticContent]`。

### 26.7 `Sonic.Widget.Terminal`

- `ITerminalRenderer`、`ILayout`、`Color`、`TextDecoration`。
- `[Focusable]`、`[KeyBinding]`、`[MenuBar]`。
- `[Command]`、`[Argument]`、`[Option]`、`[Subcommand]`。

### 26.8 用法示例

```csharp
[Page(Route = "/user/{id}")]
[Stack(Orientation = Orientation.Vertical)]
[Style(Key = "MainPage")]
public partial class UserPage
{
    [Bindable] public UserViewModel ViewModel { get; set; }
    
    [NavigateTo("/details")]
    public async Task GoToDetails() { }
}

// 生成页面初始化、数据绑定和导航代码
```

```csharp
[HotReload]
[ModuleScope]
public partial class CounterWidget : IWidget
{
    [Observable] public int Count { get; set; }

    [AcceptState]
    public void OnStateChanged(AppState state) { }
}
// 修改代码后 UI 自动刷新，保留状态
```

```csharp
[Island(RenderMode = RenderMode.Server)]
public partial class ServerDashboard { }

[Hydrate]
public partial class ClientChart { }
// 生成混合渲染指令
```

```csharp
[Command("git")]
public partial class GitCommand
{
    [Subcommand]
    public GitSubCommand Subcommand { get; set; }

    public async Task<int> ExecuteAsync()
    {
        return await Subcommand.ExecuteAsync();
    }
}
// 生成命令行解析和帮助文档
```

---

## 27. Sonic.Simulation

仿真与行为建模，包括物理、ECS、状态机和寻路。

### 27.1 `Sonic.Simulation.Physics`

- `[RigidBody]`、`[Collider]`、`[Constraint]`。
- `IPhysicsWorld`、`IPhysicsBody`、`ICollisionCallback`。
- `ColliderShape` 枚举。

### 27.2 `Sonic.Simulation.ECS`

- `[Component]`：标记纯数据结构（如 `Position`）。
- `[System]`：标记逻辑处理。
- `IEntity`、`IWorld`。

### 27.3 `Sonic.Simulation.StateMachine`

- `[StateMachine]`、`[State]`、`[Transition]`。
- `IStateMachine<T>`。

### 27.4 `Sonic.Simulation.Pathfinding`

- `[Pathfind]`。
- `INavMesh`、`IPathfinder`。

### 27.5 用法示例

```csharp
[RigidBody(Mass = 10.0f)]
[Collider(Shape = ColliderShape.Sphere)]
public partial class Ball : IPhysicsBody { }

public partial class GameWorld : IPhysicsWorld
{
    public void Step(float deltaTime)
    {
        // 生成物理步进代码，集成碰撞检测
    }
}
```

```csharp
[Component] public struct Position { float X, Y, Z; }
[Component] public struct Velocity { float X, Y, Z; }

[System]
public partial class MovementSystem
{
    public void Execute(IWorld world)
    {
        foreach (var (pos, vel) in world.Query<Position, Velocity>())
            pos.X += vel.X * deltaTime;
    }
}
// 生成 ECS 组件查询和迭代代码
```

```csharp
[StateMachine]
public partial class EnemyAI
{
    [State] public IdleState Idle;
    [State] public PatrolState Patrol;
    [State] public AttackState Attack;

    [Transition(From = "Idle", To = "Patrol", Condition = "PlayerDetected")]
    public bool ShouldPatrol() => true;
}
// 生成状态转换表和驱动代码
```

```csharp
[Pathfind]
public partial class NavigationSystem : IPathfinder
{
    public IReadOnlyList<IPathNode> FindPath(IPathNode start, IPathNode end)
    {
        // 生成 A* 或 NavMesh 算法实现
    }
}
```

---

## 28. Sonic.AI

人工智能推理接口。

- `[AIModel]`：标记模型文件或类型。
- `[Vector]`：标记向量维度。
- `IModelInference<TInput, TOutput>`：`TOutput Infer(TInput input);`
- `IEmbeddingGenerator`：`ITensor<float> GenerateEmbedding(string text);`

**用法**：

```csharp
[AIModel("sentiment.onnx")]
public partial class SentimentAnalyzer : IModelInference<string, float>
{
    public float Infer(string text) => 0.0f; // 生成 ONNX 运行时调用
}
```

---

## 29. Sonic.Geo

地理空间抽象。

- `[GeoJson]`：标记类型为 GeoJSON 可序列化。
- `[LatLng]`：标记经纬度属性。
- `IPoint`、`IPolygon`、`ICoordinateReferenceSystem`。

**用法**：

```csharp
[GeoJson]
public partial class Landmark
{
    [LatLng] public IPoint Location { get; init; }
    public string Name { get; init; }
}

// 自动生成 GeoJSON 序列化和验证
```

---

## 30. Sonic.Finance

金融与货币抽象。

- `[Currency]`：标记货币代码（如 `"USD"`）。
- `[Precision]`：标记精度。
- `ICurrency`：`string Code { get; } int DecimalDigits { get; }`
- `IMoney`：`decimal Amount { get; } ICurrency Currency { get; }`
- `IExchangeRate`：`decimal Convert(IMoney source, ICurrency target);`

**用法**：

```csharp
[Currency("USD")]
[Precision(2)]
public readonly partial struct Usd
{
    public decimal Amount { get; init; }
}

// 生成货币运算、格式化和汇率转换代码
```

---

## 31. 结语与生态展望

Hypersonic 0.2.3 以超过 300 个精心设计的属性、接口、枚举和常量，构建了一套覆盖从底层编译器构造到上层人工智能推理、从数据存储到终端交互的全栈声明式契约体系。它包含
26 个一级命名空间，明确留空 19 处 BCL 已覆盖领域，每个抽象都力求正交、通用且不与 .NET 基础类库重叠。它不包含任何实现，却定义了
Sonic 生态共同的语言。

随着 `Sonic` 及其他第三方实现库逐步消费这些属性，我们将迎来一个零反射、全 AOT 友好、编译时安全的现代 C# 开发体验。Hypersonic
作为这一生态的门面，将始终保持轻量、稳定、开放，成为 .NET 世界接口统一的基石。

---

> **文档结束**  
> 版本：0.2.3  
> 字数：约 21,000 字