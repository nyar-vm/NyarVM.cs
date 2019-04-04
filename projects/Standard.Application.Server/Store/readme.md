# Olympus.Atlas 0.3.0 数据库运维指南

**版本**：1.0  
**适用范围**：已全面阅读并采用 `Olympus.Atlas 0.3.0` 设计白皮书的项目  
**核心前提**：Atlas 不内置 ORM 与数据库迁移，但通过 `IStore` 端口隔离业务逻辑，本指南为适配器层的数据库运维提供可落地的工程规范。

---

## 1. 引言

`Olympus.Atlas 0.3.0` 选择了一条克制的道路：不提供 ORM，不内置数据库迁移工具，仅通过 `IStore`
端口将数据访问定义为业务契约。这种设计赋予了团队极高的自由度，但也留下了一个无法回避的现实难题： **如何安全、有序地管理数据库
schema 的变更？**

传统“逐个 apply 迁移脚本”的方法在项目初期尚可应付，但随着迭代加速、多人协作、字段命名混乱，迁移历史会变成谁也看不懂的债务。而一些“声明式自动同步
schema”的工具又过于激进，直接在生产库上自动执行差异，可能瞬间引发灾难。

本指南旨在为 Atlas 0.3.0 使用者提供一套 **平衡的数据库运维方案**
，它既吸收了“迁移状态”的思想（用代码描述期望结构，自动生成迁移草案），也坚守“迁移脚本必须版本化、人工审核、部署前强制验证”的底线。我们将聚焦于如何在
Atlas 的适配器层组织代码、配置 CI/CD、编写测试，以及选择像 **DbUp**、 **FluentMigrator** 甚至 **EF Core** 的迁移能力，与
Atlas 的业务内核无缝协作。

阅读本指南前，请确保你已熟悉 Atlas 0.3.0 的以下概念：

- `AtlasSystem` 基类与 `[Wire]` 依赖注入
- `IStore` 端口作为业务数据访问约定
- 适配器（如 `EfOrderStore`）的实现模式
- 统一日志与 `IAtlasSystemLogger`
- Atlas 的启动与中间件（`AddAtlas()` / `UseAtlas()`）

---

## 2. 核心原则：版本化迁移，状态生成草案

### 2.1 不可动摇的底线

在 Atlas 体系下，我们坚持以下三条铁律：

1. **迁移脚本（Migration）必须版本化，存储在源码仓库中。**
2. **任何迁移脚本都必须经过人工审核，并且包含回滚方案。**
3. **在部署新版本应用之前，自动且强制地将未应用的迁移脚本执行到目标数据库，执行失败则阻止部署。**

这三条杜绝了“自动 apply 差异”或“跳过迁移直接上线”的侥幸心理。

### 2.2 “迁移状态”的正确用法

Atlas 不反对“迁移状态”的思想——你完全可以将数据库的 **期望结构**声明为代码（如 EF Core 的 `IEntityTypeConfiguration` 或未来
Hermes 的 DSL），然后利用工具 **生成**迁移草案。但这个草案只是供开发人员修改的毛坯，最终必须转化为版本化、可审核的脚本。换言之：

> **期望状态是源代码，迁移脚本是编译后的二进制，部署系统只认二进制。**

### 2.3 工具选择

Atlas 不强制绑定任何特定工具，但我们基于 .NET 生态的成熟度，推荐以下组合：

| 组件            | 推荐选项                                       | 说明                                       |
|-----------------|------------------------------------------------|--------------------------------------------|
| 数据访问适配器  | EF Core（主流）、Dapper（轻量）                | 实现 `IOrderStore` 等                      |
| 迁移脚本执行器  | **DbUp**（极简，适合 SQL 党）                  | 按序号执行 `.sql` 文件，自动维护版本历史表 |
|                 | **FluentMigrator**（C# 编写迁移）              | 可编写复杂数据迁移逻辑，支持回滚           |
|                 | EF Core 自带的迁移（谨慎使用）                 | 仅当团队充分掌控其行为时可用               |
| 声明式状态源    | EF Core `IEntityTypeConfiguration`             | 作为生成迁移草案的依据                     |
|                 | 手工维护 `schema.sql` 基线文件                 | 不依赖 ORM 时的备选                        |
| Schema 差异生成 | JetBrains Rider CLI、`pg-diff`、`mysqldiff` 等 | 对比期望状态与当前库，辅助生成初始草案     |
| 测试辅助        | SQLite in-memory / Testcontainers              | 用于 Store 集成测试和迁移安全性测试        |

**DbUp** 因其轻量、对 SQL 的完全透明、无侵入，与我们 Atlas 的克制哲学最为契合，本指南将以它为主进行示范，同时给出
FluentMigrator 的等效示例。

---

## 3. 项目目录结构

假设你的解决方案遵循 Atlas 推荐的分层（内核 vs 适配器），则数据库运维相关文件的组织如下：

```
src/
   MyApp.Core/                          # 内核（零框架引用）
       Systems/
           OrderSystem/
               OrderSystem.cs
               IOrderStore.cs           # 端口
               OrderSystemOptions.cs
   MyApp.Infrastructure/                # 适配器
       Data/
           MyAppDbContext.cs
           Configurations/              # IEntityTypeConfiguration 实现
               OrderConfiguration.cs
               UserConfiguration.cs
           Migrations/                  # 版本化迁移脚本
               Script0001_InitialSchema.sql
               Script0002_AddOrderMetadata.sql
               Script0003_UpdateOrderStatus.sql
           Stores/                      # IStore 实现
               EfOrderStore.cs
   MyApp.Host/                          # Web 主机
       ...
```

**要点**：

- `IEntityTypeConfiguration` 放在 `Infrastructure/Data/Configurations/`，与 `DbContext` 同层。
- 迁移脚本放在 `Infrastructure/Data/Migrations/`，按顺序命名（如 `Script0001_...`）。DbUp 会自动按文件名排序执行。
- `IStore` 的实现（如 `EfOrderStore`）放在 `Stores/` 目录，它们依赖 `DbContext`，完全属于适配器。

---

## 4. 迁移脚本的生命周期

### 4.1 生成迁移草案（开发阶段）

当业务需求变更导致新的 `IEntityTypeConfiguration` 修改或新增实体时，开发人员需要生成迁移草案。

**使用 DbUp 的团队（推荐）**  
直接手写 SQL 脚本草案，因为你对变更最清楚。也可借助 IDE 的 schema diff 工具生成草案，然后调整为符合命名规范的 SQL 文件。

**使用 EF Core 的团队**  
可以使用 EF Core 的工具生成初始草案：

```bash
dotnet ef migrations add AddOrderMetadata --project MyApp.Infrastructure --startup-project MyApp.Host
```

这会生成一个 C# 迁移文件。但 EF Core 的迁移文件对非 EF 用户不透明，且容易产生混乱的元数据。我们建议： **将 EF Core
生成的迁移视为草案，然后通过 EF Core 的 `Script-Migration` 命令导出为纯 SQL**：

```bash
dotnet ef migrations script --idempotent -o ..\Migrations\Script0002_AddOrderMetadata.sql
```

然后 **删除** C# 迁移文件，只保留生成的 SQL 脚本。这样可以确保迁移是纯 SQL，不受 ORM 耦合，且可被 DbUp 执行。之后将生成的 SQL
加入 `Migrations/` 目录。

**使用 FluentMigrator 的团队**  
则直接用 C# 编写迁移类，并放在 `Migrations/` 目录下，例如 `Migration0002_AddOrderMetadata.cs`。

### 4.2 迁移脚本编写规范

无论使用哪种方式，版本化迁移脚本必须满足：

- **幂等性**：涉及数据修补的 SQL 必须使用条件（`IF NOT EXISTS`, `WHERE status = 'OLD'` 等），确保重复执行不产生副作用。
- **单一职责**：一个脚本只做一件事（要么 DDL，要么 DML），并以清晰描述命名。
- **包含回滚注释**：在脚本头部用 SQL 注释写明如何回滚：

```sql
-- UP: Adds column 'discount' to orders table
ALTER TABLE orders ADD COLUMN discount DECIMAL(18,2) NOT NULL DEFAULT 0;
-- DOWN: ALTER TABLE orders DROP COLUMN discount;
```

- **避免破坏性操作**：除非经过特别评审，禁止直接在脚本中 `DROP TABLE`、`DROP COLUMN`
  等。这类操作需要多版本兼容策略（先添加新列，数据迁移后，下个版本再删除旧列）。

### 4.3 人工审核与版本提交

草案生成后，开发人员需：

1. 在本地或临时数据库执行迁移脚本，验证其正确性。
2. 运行 Store 集成测试，确认适配器与新 schema 兼容。
3. 创建 Pull Request，包含迁移脚本及相关的 `IEntityTypeConfiguration` 修改。
4. 由同行（至少一人）审核：
  - 是否有破坏性变更？
  - 是否有回滚方案？
  - 数据迁移是否幂等？
5. 合并到主分支后，迁移脚本与期望状态代码同时进入仓库，成为发布的一部分。

---

## 5. 部署与强制执行

### 5.1 嵌入应用程序启动

DbUp 的典型用法是在应用启动时执行未应用的迁移。在 `MyApp.Host/Program.cs` 中，可以在 `builder.AddAtlas()` 之后，
`app.UseAtlas()` 之前插入迁移逻辑：

```csharp
var app = builder.Build();

// 执行数据库迁移（必须成功才能继续）
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
EnsureDatabase.For.PostgresqlDatabase(connectionString); // 如果使用 PG
var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), 
        scriptName => scriptName.StartsWith("MyApp.Infrastructure.Data.Migrations."))
    .WithTransactionPerScript()
    .LogToAutodetectedLog()
    .Build();

var result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(result.Error, "Database migration failed! Aborting startup.");
    throw new InvalidOperationException("Migration failed", result.Error);
}

app.UseAtlas();
app.Run();
```

这段代码的核心：

- 在请求进入之前、路由注册之后，执行迁移。
- 如果任何脚本失败， **整个应用终止启动**，并记录错误。
- 使用 `WithTransactionPerScript()` 保证每个脚本原子性。
- 迁移脚本可嵌入为资源，也可直接放在文件系统。推荐使用嵌入资源，避免部署时路径问题。

### 5.2 使用 InitContainer（Kubernetes）

在 Kubernetes 环境下，建议将迁移与主应用分离，通过 `initContainers` 运行一个轻量级的迁移任务：

```yaml
spec:
  initContainers:
    - name: db-migrator
      image: myapp-db-migrator:latest  # 包含 DbUp 的控制台程序
      env:
        - name: ConnectionString
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connectionString
  containers:
    - name: myapp
      image: myapp:latest
```

这样主容器启动时，数据库肯定处于正确版本。Atlas 应用本身不携带迁移逻辑（除非你希望内嵌）。

### 5.3 迁移历史表

DbUp 自动创建一个名为 `SchemaVersions` 的表来记录已执行的脚本。这个名字可以自定义。部署系统或监控工具可以查询此表获取当前数据库版本，判断是否需要回滚。

FluentMigrator 有类似的 `VersionInfo` 表。这些表是迁移系统自我管理的，不要手动修改。

---

## 6. 从期望状态自动生成迁移草案

虽然我们坚持版本化，但“手工写 SQL”在 schema 复杂时容易出错。我们可以借助声明式状态源（`IEntityTypeConfiguration`
）自动生成草案，作为起点。

### 6.1 生成草案脚本

**方法一：利用 EF Core 的 `dotnet ef migrations script`**

当通过 `IEntityTypeConfiguration` 定义了期望状态后，你可以用 EF Core 的 Code First 迁移工具生成 SQL，如前所述。但务必之后删除
C# 迁移类，只留 SQL。或者你可以不用 `Add-Migration`，而用这样的工作流：

1. 维护一个“当前生产库的基线模型快照”（上一个版本的 EF Core 模型），存放在代码库中。
2. 运行自定义工具，加载最新模型和基线模型，利用 EF Core 的 `ModelDiffer` API（它内部就是用来生成迁移的），直接产生 SQL 差异。

这种方案需要额外开发，但能完美契合“迁移状态”思想且完全控制输出。

**方法二：使用 `dotnet-ef` 的 `migrations script` 命令并指定上次基线**

```bash
dotnet ef migrations script PreviousMigrationName -o draft.sql
```

这会生成从 `PreviousMigrationName` 到最新模型的增量脚本。`PreviousMigrationName` 即上一次发布时应用的迁移名（可以记录在
`schema_versions` 表中）。这个方法利用了 EF Core 的线性迁移历史，但我们只是用它来生成草案，生成后仍要人工审查并转化为 DbUp
的脚本。

### 6.2 非 EF 方案

如果使用 Dapper 或纯 ADO.NET，你的期望状态可能是一个手写的 `schema.sql` 文件（如 `baseline.sql`）。你可以用数据库 diff 工具（如
`pg-diff`）对比当前生产库和这个基线文件，生成升级脚本草案。

---

## 7. 测试策略

Atlas 的分离架构让数据库测试变得清晰且局部化。我们针对迁移和 Store 有不同的测试层级。

### 7.1 Store 集成测试（必须）

验证每个 `IStore` 实现与数据库 schema 的兼容性。

```csharp
[TestFixture]
public class EfOrderStoreTests
{
    private AppDbContext _context;
    private EfOrderStore _store;
    private string _connectionString;

    [OneTimeSetUp]
    public void Setup()
    {
        // 使用 Testcontainers 或临时数据库
        _connectionString = TestDatabase.Start();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.Migrate(); // 或执行 DbUp 迁移
        _store = new EfOrderStore(_context);
    }

    [Test]
    public async Task CreateAndRetrieve_Order_ShouldWork()
    {
        var order = new Order("Test", 10m);
        await _store.CreateAsync(order);
        var retrieved = await _store.GetByIdAsync(order.Id);
        Assert.NotNull(retrieved);
        Assert.AreEqual("Test", retrieved.CustomerName);
    }

    [OneTimeTearDown]
    public void Teardown() => TestDatabase.Stop();
}
```

这个测试确保每次 schema 变更后，Store 操作仍然有效。

### 7.2 配置合法性测试（快速反馈）

验证所有 `IEntityTypeConfiguration` 能构建有效的模型，而不会产生无效映射。可在 CI 中快速运行，无需真实数据库（用 SQLite
in-memory 或内存数据库）。

```csharp
[Test]
public void AllEntityConfigurations_ShouldBuildModel_WithoutError()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite("DataSource=:memory:")
        .Options;

    using var context = new AppDbContext(options);
    context.Model;  // 触发模型创建
    Assert.Pass();
}
```

如果某个配置写错（例如字段名有冲突），该测试会立即失败。

### 7.3 迁移安全性测试（门禁）

在 CI 中，当 Pull Request 包含迁移脚本时，自动化步骤应该：

1. 从上一个发布版本创建一个临时数据库（镜像生产库的 schema 和数据快照）。
2. 应用本次 PR 中的所有迁移脚本。
3. 运行 Store 集成测试。
4. 执行破坏性检查：扫描迁移脚本中是否包含 `DROP TABLE`、`DROP COLUMN`、修改列类型等危险操作。如果包含且没有在 PR
   描述中明确声明并获得批准，则失败。

这项测试可以借助 `DbUp` 的 `PerformUpgrade` 在临时库上执行，然后利用 SQL 解析器检查脚本内容。

### 7.4 基线快照测试（可选，推荐）

将当前“期望状态”的完整 DDL 导出为一个快照文件（如 `snapshot.sql`），并将其与上一次的基线做
diff，确保差异与迁移脚本描述的变更一致。这可以避免“迁移脚本写了但模型配置没变”或“模型变了但迁移脚本漏了”的问题。你可以通过一个简单的测试实现：

```csharp
[Test]
public void GeneratedMigrationScripts_ShouldMatchModelDiff()
{
    var fromModel = LoadPreviousModelSnapshot();  // 从文件加载
    var toModel = BuildCurrentModel();
    var expectedSql = GenerateDiff(fromModel, toModel);
    var actualSql = File.ReadAllText(Path.Combine(migrationsFolder, "Script0002_xxx.sql"));
    Assert.AreEqual(NormalizeSql(expectedSql), NormalizeSql(actualSql));
}
```

但这个测试维护成本稍高，适合成熟项目。

---

## 8. 故障恢复与回滚

### 8.1 单版本向前修复

大多数情况下，不回滚数据库，而是 **向前修复**：如果某个版本发布后发现问题，立即写一个新的迁移脚本来修正数据或
schema，然后通过紧急发布执行。这是因为回滚可能涉及数据丢失，风险更高。

### 8.2 回滚脚本准备

即使如此，每个迁移脚本仍应保留回滚 SQL（写在注释中）。当必须回滚时，可以手动执行回滚脚本。DbUp 本身不提供回滚，但你可以维护一个
`down` 目录，每个脚本对应一个 `down` 版本，通过手动运维执行。

### 8.3 灾难恢复

定期备份生产数据库，并在 CI/CD 中定期执行迁移演习（将最新备份恢复到临时环境，执行全部迁移，验证无错误）。

---

## 9. 与 Atlas 系统日志的集成

数据库操作中的异常或关键操作应通过 Atlas 的统一日志系统记录。因为 `IStore` 实现属于适配器，你可以在 `EfOrderStore` 中注入
`IAtlasSystemLogger`（或直接使用 `ILogger<T>`，因为适配器不需要区分系统）。但为了日志标签的一致性，建议在 Store 中调用通用
`ILogger`，并采用 Atlas 日志格式：

```csharp
public class EfOrderStore : IOrderStore
{
    private readonly AppDbContext _db;
    private readonly ILogger<EfOrderStore> _logger;

    public EfOrderStore(AppDbContext db, ILogger<EfOrderStore> logger)
        => (_db, _logger) = (db, logger);

    public async Task CreateAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[OrderStore:Create] OrderId={OrderId}", order.Id);
    }
}
```

这样，当迁移后字段不匹配导致 `SaveChangesAsync` 抛出异常时，日志会清晰显示是哪个 Store 出错，加速排错。

---

## 10. 常见问题解答

**Q：为什么不用 EF Core 的 `Database.Migrate()`？它已经版本化了。**  
A：可以用，但将其生成的 C# 迁移文件保留下来会导致“黑盒”问题：团队成员很难直接看懂变更内容，且迁移历史依赖 EF Core
内部数据结构，有升级风险。我们推荐将其导出为纯 SQL，然后用 DbUp 执行。

**Q：我们团队已经深度使用 FluentMigrator，与 Atlas 冲突吗？**  
A：完全不冲突。你只需将 FluentMigrator 的迁移类视为与 DbUp 脚本等价的版本化迁移，同样需要审核。FluentMigrator 的 migration
runner 可以放在应用启动代码中，替代 DbUp 的逻辑。

**Q：如何处理长时间运行的重大 schema 重构（比如拆分表）？**  
A：采用兼容性版本策略：

- V1：新增目标表，同时保留旧表，业务代码同时读写两张表（通过 Store 接口适配）。
- V2：数据迁移脚本，将旧数据转换到新表。
- V3：移除对旧表的读写，删除旧表。  
  这种操作横跨多个发布版本，每次都有对应的迁移脚本和 Store 修改。

**Q：Atlas 未来会提供官方的迁移工具吗？**  
A：短期内不会。我们相信数据库运维方案应该由团队根据自身情况定制，Atlas 只通过 `IStore` 提供干净的接口。但 Hermes Schema
远期计划会提供从 DSL 到迁移脚本草案的生成能力，届时仍会输出版本化的 SQL 脚本供审核。

---

## 11. 总结

数据库运维是严肃的工程问题，不能交给自动化工具“一键搞定”，也不能手工零散操作。Atlas 0.3.0 的数据库运维指南强调：

1. **版本化迁移脚本是唯一的生产变更方式**，它们必须存储在代码库中。
2. **声明式期望状态（`IEntityTypeConfiguration`）是生成迁移草案的输入**，而非直接执行的命令。
3. **部署流水线强制在应用启动前执行未应用的迁移，失败即阻止上线**。
4. **Store 集成测试和迁移安全性测试是质量保证的防线**，必须在 CI 中执行。
5. **DbUp 或 FluentMigrator 等轻量工具，与 Atlas 的适配器层天然契合**。

遵循本指南，你既可以利用现代工具体验“迁移状态”的便利，又能守住生产环境的安全底线。数据库变更不再是噩梦，而是可预期、可追溯、可回滚的正常发布流程。