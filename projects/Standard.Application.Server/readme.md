# Olympus.Atlas 0.3.0

版本：0.3.0  
适用范围：基于 .NET 的 Web API 开发，特别适合云原生、微服务和 AI 集成场景

---

## 1. 引言

在现代后端开发中，开发者面临两个看似矛盾的挑战：一方面，业务逻辑的复杂性要求清晰的分层与隔离；另一方面，基础设施（日志、缓存、队列、云服务、AI）的日益丰富使得应用与这些服务紧密交织，代码变得臃肿且难以测试。传统全能框架（如
ASP.NET Core）提供了丰富的功能，但往往通过庞大的基类、大量的构造函数注入或魔法般的自动扫描，使得代码的依赖关系模糊，业务核心被框架代码淹没。

Olympus.Atlas 是一个 **有哲学、有边界、知取舍的 Web 整合层**。它不追求成为“另一个 Spring Boot”，而是旨在提供一套 **编写 HTTP
API 的最高效、最一致的编程模型**。Atlas 0.3.0 是一次重大演进，它吸收了 System Adapter Pattern（SAP）的核心思想——
**逻辑与适配器分离**，但摒弃了教条式的目录规范和完全显式的组合根，代之以更务实的 **约定大于配置**、 **扫描优先**但
**始终可替换**的理念。

本白皮书将全面阐述 Atlas 0.3.0 的哲学、架构、核心概念和使用方式。我们假定读者对 .NET 和 Web API 开发有基本了解，但不预设任何先前版本的
Atlas 知识。无论你是新项目的架构师，还是希望迁移旧系统的开发者，Atlas 都为你提供了一条清晰、轻量且强大的路径。

---

## 2. 设计哲学：有所不为，持续聚焦

Atlas 的设计哲学可以浓缩为四个字： **有所不为**。大多数全能框架追求“开箱即用”，为你选好 ORM、模板引擎、后台管理面板。Atlas
则只聚焦于 **Web API 层**的整合，并在其他领域刻意留白。这并非功能缺失，而是一种负责任的架构选择。

### 2.1 什么是 Atlas 认为“该做的”？

- **提供统一的系统抽象**：通过 `IAtlasSystem` 和 `AtlasSystem` 基类，将业务逻辑组织为清晰的单元。
- **极致简化的依赖管理**：用 `[Wire]` 属性注入替代复杂的构造函数链，让依赖关系一目了然，同时保留编译时安全。
- **内置关键横切基础设施**：日志、缓存、队列、事件总线直接内置于系统基类，开发者无需重复声明即可使用。
- **统一响应与错误处理**：基于 `Result<T>` 的统一业务返回类型，配合中间件自动转换为 HTTP 响应，彻底消除重复的 if-else。
- **自动化的控制器与路由**：控制器作为入站适配器，只需注入系统并调用方法，框架自动处理 REST 映射。
- **云服务和 AI 的一等公民抽象**：对象存储、短信、邮件、AI 对话等现代基础设施通过接口暴露，作为可注入的 `[Wire]`
  依赖，与业务逻辑无缝对接。
- **Code First 优先**：开发者直接编写系统类和控制器，框架自动生成 Swagger 文档和日志，无需额外的契约文件。

### 2.2 什么是 Atlas 刻意不做的？

- **不提供 ORM**：数据访问是核心决策，Atlas 绝不将特定 ORM 绑定到路由或控制器管线中。我们推荐使用 `IStore` 接口定义业务端口，具体实现可以是
  EF Core、Dapper 或任何其他方案。
- **不提供 HTML 模板引擎或视图层**：Atlas 是纯 API 框架，不参与服务端渲染。
- **不提供后台管理面板**：前端由 Vue/React 负责，Atlas 只输出契约良好的 API。
- **不绑定特定云供应商**：云服务抽象通过接口定义，官方提供常用实现，你也可以随时替换。
- **不强制目录结构**：虽然我们推荐逻辑与适配器分离，但不使用强制性的 `_` 前缀或固定文件夹。约定在代码中，不在文件夹名上。

### 2.3 吸收 SAP 的精华，避免教条

System Adapter Pattern (SAP) 是一种强大的架构范式，强调将业务系统划分为 **内核（纯逻辑） **和**适配器（技术胶水）**。Atlas
0.3.0 充分吸收了 SAP 的核心思想：

- **逻辑与适配器分离**：业务系统类继承 `AtlasSystem`，只包含业务逻辑和通过 `[Wire]` 声明的业务端口。控制器、存储实现等属于适配器，放在系统的对应位置。
- **端口与实现解耦**：通过接口（如 `IOrderStore`、`IPaymentGateway`）定义业务所需的外部能力，实现类在 DI 容器中注册。
- **显式依赖与自动连接平衡**：SAP 强调显式组合根，Atlas 则提供 `[Wire]` 声明式注入，在保持可见性的前提下大幅减少样板代码。

但我们拒绝教条化：Atlas **不会**强迫你必须将适配器放在 `_` 开头的文件夹中，也不会禁止自动扫描。我们相信
**约定大于配置，扫描优于手写**，只在必要时提供显式注册的后门。

---

## 3. 核心概念

Atlas 的设计围绕着几个清晰的核心概念：

- **System（系统）**：一个独立的业务能力单元，如“订单系统”、“用户系统”。它继承自 `AtlasSystem`，包含业务逻辑和业务端口声明。
- **`[Wire]` 注入**：一种声明式依赖注入方式，用属性标记代替构造函数参数，由框架在运行时自动填充。
- **Store（数据存储）**：业务端口的命名约定，替代传统的 Repository，强调技术中立性和与 Cache 的对称。
- **统一 Result**：`Result<T>` 作为业务方法的返回值，封装成功与失败信息，由中间件自动转换为 HTTP 响应。
- **适配器（Adapter）**：连接内核与外部技术的胶水代码，例如控制器（入站适配器）、Store 实现（持久化适配器）、云服务实现等。
- **AtlasHost**：启动引导器，通过 `AddAtlas()` 和 `UseAtlas()` 完成扫描、注册和中间件配置。

---

## 4. 系统定义与基类

### 4.1 `IAtlasSystem` 标记接口

`IAtlasSystem` 是一个空接口，仅用于标记一个类是 Atlas 业务系统。`AddAtlas()` 扫描所有实现了该接口的类，并自动注册到 DI 容器中。

```csharp
public interface IAtlasSystem { }
```

### 4.2 `AtlasSystem` 基类

为了提供开箱即用的基础设施能力，Atlas 提供了一个抽象基类 `AtlasSystem`，所有业务系统可以选择继承它。基类内置了四样几乎所有系统都会用到的横切服务：

```csharp
public abstract class AtlasSystem : IAtlasSystem
{
    // 内置服务属性（由框架自动填充）
    public IAtlasSystemLogger Logger { get; internal set; } = null!;
    public IAtlasCache        Cache  { get; internal set; } = null!;
    public IAtlasQueue        Queue  { get; internal set; } = null!;
    public IAtlasEventBus     EventBus { get; internal set; } = null!;
}
```

这些属性在系统实例化时由框架通过 DI 容器自动填充，开发者无需在构造函数中声明或使用 `[Wire]` 标记。它们就是“空气”，随时随地可用。

**为什么是这四个？**

- **Logger**：所有系统都需要日志。
- **Cache**：现代应用几乎必用缓存。
- **Queue**：异步处理、后台任务的基石。
- **EventBus**：系统间解耦通信的必要机制。

其他云服务（如邮件、短信、AI、对象存储等）不是每个系统都需要，因此保留为可选的 `[Wire]` 注入，避免基类膨胀。

### 4.3 业务端口的声明

业务系统的独有依赖（如数据存储、支付网关、外部服务）通过 `[Wire]` 属性注入声明。开发者在系统类中声明属性，并用 `[Wire]` 特性标记：

```csharp
public class OrderSystem : AtlasSystem
{
    [Wire] IOrderStore Store { get; set; } = null!;
    [Wire] IPaymentGateway Payment { get; set; } = null!;
    [Wire] OrderSystemOptions Options { get; set; } = null!;

    public async Task<Result<Order>> PlaceOrderAsync(PlaceOrderCommand cmd)
    {
        Logger.Log("PlaceOrder", "Id", cmd.OrderId);
        // 使用 Options.MaxRetries, Store, Payment 等
        ...
    }
}
```

这个类完全没有任何构造函数！所有依赖清晰可见地列在属性中，IDE 智能感知完整，单元测试时直接使用对象初始化器即可。

### 4.4 配置的处理

配置是一个复杂且高度特化的依赖，我们不将其特殊化或绑定到泛型，而是统一用 `[Wire]` 注入强类型选项类。

框架约定：如果你的系统类名为 `OrderSystem`，那么它会自动寻找名为 `OrderSystemOptions` 的类，并绑定配置节 `"OrderSystem"`
（可自定义）。实际注入时会自动从 `IOptions<OrderSystemOptions>.Value` 解包，你得到的直接就是 `OrderSystemOptions` 实例。

```csharp
// 自动发现的配置类
public class OrderSystemOptions
{
    public int MaxRetries { get; set; } = 3;
    public decimal TaxRate { get; set; } = 0.1m;
}
```

如果你不想使用约定，或者有多个配置类，可以显式声明并注册：

```csharp
// 自定义节名
[AtlasConfigSection("Order:Settings")]
public class OrderSystemOptions { ... }
```

或者根本不用约定，自己手动注入：

```csharp
// 在 DI 注册阶段
builder.Services.Configure<OrderSystemOptions>(configuration.GetSection("MySection"));
```

然后在系统中只需声明 `[Wire] OrderSystemOptions Options { get; set; }`，框架会自动找到并注入。

这种设计让配置回归普通依赖，不再享受特殊待遇，保持了模型的一致性。

---

## 5. 依赖注入与 `[Wire]`

### 5.1 `[Wire]` 特性的起源与语义

`[Wire]` 是 Atlas 依赖注入的核心。这个名字来源于计算机科学中“连接”的通用隐喻——从物理电线到软件模块之间的装配。我们选择
`[Wire]` 而非 `[Inject]` 或 `[Autowired]`，因为它更精确地表达了 SAP 中“将内核与适配器连接”的职责，并且避免了与现有 DI
容器的标签冲突。

在 Olympus 体系中，`WireAttribute` 最终将提升到 **Hypersonic** 抽象层，成为协议无关的概念标记。但在 Atlas 0.3.0 中，它直接在
Atlas 运行时中实现。

### 5.2 工作原理

当 `AddAtlas()` 被调用时，它会扫描所有实现了 `IAtlasSystem` 的类，对每个类执行以下步骤：

1. 确定生命周期（默认 Singleton）。
2. 创建一个工厂 lambda，用于 DI 容器：
  - 使用 `ActivatorUtilities.CreateInstance` 创建系统实例（调用默认构造函数或已定义的构造函数）。
  - 遍历所有带有 `[Wire]` 特性的公开属性，从 DI 容器中解析对应服务并设置值。
  - 填充内置属性（`Logger`、`Cache`、`Queue`、`EventBus`）。
3. 将工厂注册到 DI 容器。

由于属性注入在实例创建后发生，性能上在 Singleton 生命周期下毫无影响（只执行一次）。对于 Scoped 系统，每次请求额外的属性赋值开销不到
1μs，完全可忽略。

### 5.3 与手动注入的兼容

Atlas 不强制使用 `[Wire]`。如果你的系统已经有构造函数，或者你希望某些依赖是必选的并通过构造函数强制，可以混用：

```csharp
public class OrderSystem : AtlasSystem
{
    public OrderSystem(IOrderStore store)
    {
        Store = store;
    }

    // 这个依赖可以保持不可变
    public IOrderStore Store { get; }

    [Wire] IPaymentGateway Payment { get; set; } = null!;
    [Wire] OrderSystemOptions Options { get; set; } = null!;
}
```

Atlas 的工厂方法会尊重现有构造函数，并从容器解析其参数，然后再处理 `[Wire]` 属性。

### 5.4 生命周期

默认情况下，所有 `IAtlasSystem` 实现被注册为 **Singleton**。这是因为业务系统通常是无状态的，仅通过 `[Wire]` 依赖项（它们自身可能是
Scoped）进行操作。Singleton 保证了最佳性能和最简单的内存模型。

如果你的系统确实需要与请求绑定（例如存储请求级缓存或用户上下文），可以在系统类上使用
`[AtlasLifetime(ServiceLifetime.Scoped)]` 特性覆盖默认生命周期。

---

## 6. 业务端口：Store 与 Cache

### 6.1 为什么使用 Store 而非 Repository？

在传统分层架构中，`Repository` 模式几乎与持久化层划等号，尤其是与 EF Core 的 `DbSet` 紧密关联。这导致两个问题：

- 泄漏持久化细节到业务层（如 `IQueryable`）。
- 语义固化为“数据库操作”，忽略了数据可能来自缓存、外部 API、文件等。

Atlas 引入 **Store** 概念作为业务端口的命名标准。`Store` 只表达“存放和获取数据的地方”，技术完全中立。它与 `Cache` 天然对应：
`Cache` 是临时快速存储，`Store` 是权威数据源。两者配合，业务代码可以清晰地表达缓存策略。

### 6.2 Store 的定义与实现

Store 接口定义在业务系统的内核中，由业务需求驱动：

```csharp
// 订单系统的 Store 端口
public interface IOrderStore
{
    Task<Order> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId);
    Task CreateAsync(Order order);
    Task UpdateAsync(Order order);
}
```

适配器实现位于单独的项目或文件夹中，具体可以使用 EF Core：

```csharp
public class EfOrderStore : IOrderStore
{
    private readonly AppDbContext _db;
    public EfOrderStore(AppDbContext db) => _db = db;

    public Task<Order> GetByIdAsync(Guid id) => _db.Orders.FindAsync(id).AsTask();
    // ...
}
```

然后在 DI 中注册：

```csharp
builder.Services.AddScoped<IOrderStore, EfOrderStore>();
```

业务系统完全不知道 `EfOrderStore` 的存在，它只依赖 `IOrderStore`。

### 6.3 Cache 的使用

基类内置的 `Cache` 属性提供了统一的缓存接口 `IAtlasCache`，支持简单的键值存取。例如在 `OrderSystem` 中：

```csharp
public async Task<Order> GetOrderAsync(Guid id)
{
    var cacheKey = $"order:{id}";
    var cached = await Cache.GetAsync<Order>(cacheKey);
    if (cached != null)
        return cached;

    var order = await Store.GetByIdAsync(id);
    await Cache.SetAsync(cacheKey, order, TimeSpan.FromMinutes(5));
    return order;
}
```

这种模式让缓存逻辑变成一行代码，无需注入额外的服务。

---

## 7. 统一日志系统

### 7.1 日志的挑战

微服务架构中，日志的混乱是常态。请求可能经过多个系统，每个系统使用不同的日志格式，导致追踪困难。Atlas 提出了一套
**单行结构化日志格式**，强制统一并自动包含必要的上下文。

### 7.2 日志格式

Atlas 的所有日志（无论是框架自动生成的还是业务手动输出的）都遵循如下单行格式：

```
[标签:子标签] Key1=Value1; Key2=Value2; ...
```

- **标签**：标识日志来源，如 `Request`、`Response`、`Exception`、业务系统名称等。
- **子标签**：可选的细分，如 `Auth`、`WARN`、`200` 等。
- **键值对**：一系列 `Key=Value`，用分号分隔，值如有特殊字符则用双引号包裹。

示例：

- 入站请求：`[Request] POST=/api/orders; Body={"productId":"abc"}`
- 需认证：`[Request:AUTH] GET=/api/v1?query; User=alice`
- 正常响应：`[Response:200] 45ms; TraceId=tr_1`
- 业务异常：`[Exception:WARN] Code=INSUFFICIENT_STOCK; Hint="检查库存缓存"`
- 系统日志：`[OrderSystem:PlaceOrder] OrderId=123; Amount=99.9`

每条日志末尾自动附加 `TraceId`（从当前请求上下文获取），实现请求链路的快速关联。

### 7.3 日志器注入

Atlas 为每个系统提供了专属的 `IAtlasSystemLogger` 实例，它已内置系统名标签。开发者通过基类的 `Logger` 属性即可使用：

```csharp
public async Task PlaceOrderAsync(PlaceOrderCommand cmd)
{
    Logger.Log("PlaceOrder", "Id", cmd.OrderId);
    // 输出: [OrderSystem:PlaceOrder] Id=123
}
```

`IAtlasSystemLogger` 提供了便捷方法：

- `Log(action, keyValues...)`：输出 `[系统名:动作]` 格式。
- `LogWarning(action, keyValues...)`：输出时标签会标记为 WARN。
- `LogError(action, exception, keyValues...)`：记录异常。

框架会自动为每个 `IAtlasSystem` 实现创建专属日志器，你无需手动声明或注入。

### 7.4 中间件自动日志

`UseAtlas()` 会添加两个中间件：

- **请求日志中间件**：自动记录每个入站 HTTP 请求的 `[Request]` 日志。
- **响应日志中间件**：在响应返回前记录 `[Response]` 日志，包含状态码和耗时。

异常处理中间件会将未捕获异常转换为 `[Exception:ERROR]` 日志，并返回统一错误响应。

这些自动生成的日志与业务日志使用完全相同的格式和 `TraceId`，使得日志流清晰可读。

---

## 8. 控制器：入站适配器

### 8.1 控制器的角色

在 Atlas 中，控制器是 **入站适配器**，负责将 HTTP 请求转换为对业务系统的调用，并将业务结果转换为 HTTP 响应。它们不包含业务逻辑，只做协议转换。

### 8.2 定义控制器

控制器继承自 `AtlasController`（一个轻量级的 `ControllerBase` 子类），通过构造函数注入业务系统：

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : AtlasController
{
    private readonly OrderSystem _system;

    public OrdersController(OrderSystem system)
    {
        _system = system;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request)
    {
        var cmd = request.ToCommand();
        return await InvokeAsync(() => _system.PlaceOrderAsync(cmd));
    }
}
```

`AtlasController` 提供了 `InvokeAsync` 方法，它自动处理 `Result<T>` 的转换：

- `Result.Success(data)` → `200 OK` 并序列化 `data`。
- `Result.Failure(error)` → `400 Bad Request`（或合适的错误码），响应体包含 `{ "error": "...", "code": "..." }`。

这消除了每个方法重复的 `if (result.IsSuccess) return Ok(...) else return BadRequest(...)`。

### 8.3 自动路由注册

`AddAtlas()` 会扫描当前程序集中所有继承自 `AtlasController`（或 `ControllerBase`）的类，并自动注册为 API 控制器。你无需手动调用
`AddControllers()` 或逐个注册。当然，如果你需要手动控制，也可以使用 `AddAtlas(opts => opts.Controllers = false)`
禁用自动扫描，然后自行注册。

---

## 9. 启动与中间件

### 9.1 最小启动代码

Atlas 的启动极其简单：

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddAtlas();  // 扫描系统、控制器，注册 DI
var app = builder.Build();
app.UseAtlas();      // 添加日志、异常中间件
app.Run();
```

两行即完成框架集成。`AddAtlas()` 和 `UseAtlas()` 都提供了选项委托，可以按需定制。

### 9.2 `AddAtlas()` 做什么？

1. 扫描所有 `IAtlasSystem` 实现，将它们注册为 Singleton（或根据 `[AtlasLifetime]` 调整）。
2. 为每个系统创建工厂，填充内置属性和 `[Wire]` 属性。
3. 自动发现与系统同名的 `XxxOptions` 类，绑定配置到 `IOptions<T>`。
4. 扫描所有 `AtlasController` 或 `ControllerBase`，添加到 MVC 核心。
5. 注册框架核心服务：`IAtlasSystemLogger`、`IAtlasCache`、`IAtlasQueue`、`IAtlasEventBus` 的默认实现（如果尚未注册）。

所有这些步骤都是自动的，但你可以在 `AddAtlas(opts => ...)` 中禁用或覆盖其中任何部分。

### 9.3 `UseAtlas()` 做什么？

1. 添加请求日志中间件（`[Request]`）。
2. 添加响应日志中间件（`[Response]`）。
3. 添加全局异常处理中间件（将异常转为统一错误响应并记录 `[Exception]`）。
4. 添加 `Result<T>` 转 HTTP 响应的处理逻辑（如果控制器未使用 `InvokeAsync`，也可以直接返回 `Result`，中间件会自动转换）。

这些中间件按 **请求日志 → 异常处理 → 响应日志**的顺序安装，确保日志完整覆盖。

### 9.4 与标准 ASP.NET Core 共存

Atlas 完全在 ASP.NET Core 之上运行，不会隐藏底层的 `WebApplication`。你随时可以添加自己的中间件、服务，或者使用原生的
`app.MapControllers()` 等。Atlas 是增强，而非替代。

---

## 10. 云服务与 AI 抽象

Atlas 将现代云基础设施视为一等公民，提供了一组标准化接口，作为可选的 `[Wire]` 依赖。

### 10.1 内置抽象接口

这些接口定义在 `Olympus.Atlas` 核心包中，用于常见云服务：

| 接口                     | 用途                         |
|--------------------------|------------------------------|
| `IEmailSender`           | 发送邮件                     |
| `ISmsSender`             | 发送短信                     |
| `IObjectStorage`         | 对象存储（上传、下载、删除） |
| `IKeyVaultService`       | 密钥管理（获取机密）         |
| `IChatCompletionService` | AI 对话补全                  |
| `IPaymentGateway`        | 支付网关                     |

开发者可以在系统中声明 `[Wire]` 属性来使用它们：

```csharp
public class NotificationSystem : AtlasSystem
{
    [Wire] IEmailSender Email { get; set; } = null!;
    [Wire] ISmsSender Sms { get; set; } = null!;

    public async Task SendWelcomeAsync(User user)
    {
        await Email.SendAsync(user.Email, "Welcome", "Hello!");
        Logger.Log("SendWelcome", "User", user.Id);
    }
}
```

### 10.2 实现与替换

Atlas 不强制绑定特定供应商。官方提供常用实现的扩展包（如 `Atlas.Cloud.Aliyun`、`Atlas.Cloud.Azure`），安装后通过 DI
注册即可自动生效。你也可以自己实现接口，然后替换注册。

例如，对于阿里云 OSS：

```csharp
builder.Services.AddSingleton<IObjectStorage, AliyunOssStorage>();
```

如果官方实现不满足需求，你可以编写自己的适配器，并替换默认注册：

```csharp
builder.Services.Replace(ServiceDescriptor.Scoped<IEmailSender, MyEmailSender>());
```

Atlas 的 `[Wire]` 注入与容器无关，任何替换都会自动反映到所有使用该属性的系统中。

### 10.3 AI 服务集成

`IChatCompletionService` 是一个通用的 AI 对话接口，支持流式和非流式响应。Atlas 不关心底层是 OpenAI、Azure OpenAI
还是本地模型。开发者只需声明并使用，框架或适配器负责处理细节。

未来版本将引入 `IEmbeddingService`、`IVectorStore` 等，但 Atlas 坚持“先做好一件事”的克制，不会一次性膨胀。

---

## 11. 队列与事件总线

### 11.1 内置队列抽象

`IAtlasQueue` 用于发送后台任务，支持优先级、延迟等选项。其核心方法：

```csharp
public interface IAtlasQueue
{
    Task EnqueueAsync<T>(string queueName, T payload, int? priority = null, TimeSpan? delay = null);
}
```

基类中的 `Queue` 属性直接暴露该能力。业务系统可以简单地将任务入队：

```csharp
await Queue.EnqueueAsync("send-email", new { Email = user.Email, Subject = "Welcome" });
```

### 11.2 队列的降级链

Atlas 支持一个巧妙的降级链：在配置中，你可以指定队列实现按优先级尝试。框架提供以下内置选项（通过扩展包）：

- **内存队列**（开发环境默认，生产环境会触发警告）。
- **Redis 队列**（通过 `Atlas.Queue.Redis`）。
- **SQL 队列**（通过 `Atlas.Queue.Sql`）。
- **专用消息队列**（RabbitMQ/SQS 等，通过社区包）。

在 Serverless 环境中，队列自动外化，消费者由云平台事件触发，而非长驻进程。

### 11.3 事件总线

`IAtlasEventBus` 用于系统间的发布/订阅式通信。它支持在同一个进程中基于内存的事件传递，或者通过 Redis/RabbitMQ
实现分布式事件。典型用法：

```csharp
// 订单系统
await EventBus.PublishAsync(new OrderPlaced { OrderId = order.Id, UserId = order.UserId });

// 通知系统订阅
public class NotificationHandler : IEventHandler<OrderPlaced>
{
    public async Task Handle(OrderPlaced @event)
    {
        // 发送邮件等
    }
}
```

事件处理器被扫描并自动注册，无需手动订阅。这种模式完全解耦了系统之间的直接依赖，保持了 SAP 的内核纯净性。

---

## 12. Serverless 与云原生

Atlas 在设计之初就充分考虑了无服务器和云原生场景：

- **无状态**：`AtlasSystem` 不保存任何会话状态，所有缓存外置。
- **队列外化**：`IAtlasQueue` 抽象保证了任务在函数实例消亡后仍能继续处理。
- **冷启动优化**：`IStartupTask` 接口允许你在 DI 构建后、处理请求前执行预热（如模型加载、连接预建立）。
- **平台适配**：提供 `Atlas.Serverless.AwsLambda` 等适配包，将云事件转换为 Atlas 内部 HTTP 上下文。

这使得同一个 Atlas 应用可以不加修改地运行在本地 Kestrel、容器、Kubernetes 或 AWS Lambda 上。

---

## 13. 测试策略

Atlas 的设计天然适合测试：

### 13.1 单元测试

业务系统是无框架依赖的纯 C# 类。测试时直接 `new` 出来，使用 Mock 对象设置 `[Wire]` 属性，验证业务逻辑：

```csharp
[Test]
public async Task PlaceOrder_WhenStockInsufficient_ReturnsFailure()
{
    var system = new OrderSystem
    {
        Store = Mock.Of<IOrderStore>(s => s.CreateAsync(It.IsAny<Order>()) == Task.FromResult(mockOrder)),
        Payment = Mock.Of<IPaymentGateway>(),
        Options = new OrderSystemOptions { MaxRetries = 1 },
        Logger = new NullAtlasSystemLogger(),
        Cache = new MemoryAtlasCache(),
        Queue = new NullAtlasQueue(),
        EventBus = new NullAtlasEventBus()
    };

    var result = await system.PlaceOrderAsync(cmd);
    Assert.IsFalse(result.IsSuccess);
}
```

注意：内置服务的测试替身（`NullAtlasSystemLogger`、`NullAtlasQueue` 等）由 Atlas 提供，行为符合预期。

### 13.2 集成测试

针对适配器（Store 实现、控制器、中间件）的集成测试，使用 `WebApplicationFactory` 或自定义的 `AtlasApplicationFactory`，可以模拟真实
HTTP 请求。Atlas 的测试工具包提供了内建的测试服务器支持。

---

## 14. 性能考量

Atlas 的性能设计要点：

- **Singleton 系统**：默认生命周期确保业务系统创建一次，`[Wire]` 属性注入仅发生在启动时，运行时零开销。
- **工厂方法**：使用编译期反射（`ActivatorUtilities`）和缓存的表达式树，创建系统实例的开销与手工编写代码几乎相同。
- **日志最小化**：单行结构化日志避免复杂的字符串插值，在 log level 关闭时几乎零成本。
- **中间件精简**：内置中间件短小精悍，无额外堆分配。

整体上，Atlas 在运行时与手写 ASP.NET Core 应用性能相当，仅启动期多出微秒级的扫描和属性注入时间。

---

## 15. 迁移与渐进式采用

Atlas 不强求你重写现有应用。你可以采用“绞杀者模式”逐步迁移：

1. 引入 `Olympus.Atlas` NuGet 包。
2. 选择一个模块，将其服务类改为 `OrderSystem : AtlasSystem`，添加必要的 `[Wire]` 属性。
3. 创建对应的 `IOrderStore`，将现有 Repository 实现改为该接口，并调整 DI 注册。
4. 将控制器改为注入 `OrderSystem`，并使用 `InvokeAsync`。
5. 逐步替换其他模块，直到全部转换完成。

在此期间，旧的控制器和服务可以和 Atlas 系统并存，互不影响。

---

## 16. 结论

Olympus.Atlas 0.3.0 是一次深思熟虑的演进。它吸收了 System Adapter Pattern 的架构智慧，但拒绝教条，坚持实用主义。它通过
`IAtlasSystem`、`[Wire]` 注入、统一日志和内置基础设施，将后端 API 开发的复杂度降至最低，同时保留了所有必要的灵活性和可替换性。

Atlas 不是“又一个全能框架”，而是一个 **有边界、有哲学、有温度**的开发伙伴。它让你的业务代码重新成为焦点，让基础设施退居幕后，让团队可以更快地交付、更自信地重构。

我们邀请你试用 Atlas 0.3.0，感受它带来的不同。欢迎在 GitHub 上提交 issue、贡献代码，或者加入社区讨论。Atlas 的未来，期待你的参与。

**有所不为，方有所为。**