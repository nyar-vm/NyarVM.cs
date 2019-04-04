using Xunit;

namespace Hermes.ORM.Tests;

public sealed class OrmCrudTests
{
    [Fact]
    public async Task Insert_CreatesRecord_Success()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        var user = new User { Id = 1, Name = "张三", Email = "zhangsan@test.com", Age = 25 };
        var affected = await executor.Query<User>().InsertAsync(user);

        Assert.Equal(1, affected);
        var rows = db.GetTable(nameof(User));
        Assert.Single(rows);
        Assert.Equal("张三", rows[0]["Name"]);
    }

    [Fact]
    public async Task Insert_MultipleRecords_AllStored()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "王五", Age = 35 });

        var rows = db.GetTable(nameof(User));
        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public async Task FindById_ReturnsEntity()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });

        var found = await executor.Query<User>()
            .Filter(new FieldEquals("Id", 1))
            .FirstOrDefaultAsync();

        Assert.NotNull(found);
        Assert.Equal("张三", found.Name);
        Assert.Equal(25, found.Age);
    }

    [Fact]
    public async Task FindById_NotFound_ReturnsNull()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        var found = await executor.Query<User>()
            .Filter(new FieldEquals("Id", 1))
            .FirstOrDefaultAsync();

        Assert.Null(found);
    }

    [Fact]
    public async Task FindAll_ReturnsAllRecords()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });

        var users = await executor.Query<User>().ToListAsync();

        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task Filter_ByName_ReturnsMatch()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "张三丰", Age = 28 });

        var found = await executor.Query<User>()
            .Filter(new FieldContains("Name", "张三"))
            .ToListAsync();

        Assert.Equal(2, found.Count);
    }

    [Fact]
    public async Task Filter_ByAge_ReturnsMatch()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "王五", Age = 35 });

        var adults = await executor.Query<User>()
            .Filter(new FieldGreaterThan("Age", 28))
            .ToListAsync();

        Assert.Equal(2, adults.Count);
    }

    [Fact]
    public async Task Filter_AndPredicate_CombinesConditions()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25, IsActive = true });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30, IsActive = false });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "王五", Age = 35, IsActive = true });

        var activeAdults = await executor.Query<User>()
            .Filter(new AndPredicate(
                new FieldGreaterThan("Age", 28),
                new FieldEquals("IsActive", true)))
            .ToListAsync();

        Assert.Single(activeAdults);
        Assert.Equal("王五", activeAdults[0].Name);
    }

    [Fact]
    public async Task Filter_OrPredicate_ReturnsUnion()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "王五", Age = 35 });

        var result = await executor.Query<User>()
            .Filter(new OrPredicate(
                new FieldEquals("Name", "张三"),
                new FieldEquals("Name", "王五")))
            .ToListAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Update_UpdatesFieldValue()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });

        await executor.Query<User>()
            .Filter(new FieldEquals("Id", 1))
            .Set("Name", "张三改")
            .UpdateAsync();

        var rows = db.GetTable(nameof(User));
        Assert.Equal("张三改", rows[0]["Name"]);
    }

    [Fact]
    public async Task Update_ReturnsAffectedCount()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });

        var affected = await executor.Query<User>()
            .Filter(new FieldGreaterThan("Age", 26))
            .Set("Age", 999)
            .UpdateAsync();

        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });

        var deleted = await executor.Query<User>()
            .Filter(new FieldEquals("Id", 1))
            .DeleteAsync();

        Assert.Equal(1, deleted);
        var rows = db.GetTable(nameof(User));
        Assert.Single(rows);
        Assert.Equal("李四", rows[0]["Name"]);
    }

    [Fact]
    public async Task Delete_ReturnsAffectedCount()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });

        var deleted = await executor.Query<User>()
            .Filter(new FieldEquals("Id", 1))
            .DeleteAsync();

        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task Count_ReturnsRecordCount()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });

        var count = await executor.Query<User>().CountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Count_Filtered_ReturnsFilteredCount()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });

        var count = await executor.Query<User>()
            .Filter(new FieldGreaterThan("Age", 26))
            .CountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Pagination_SkipTake()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "A", Age = 10 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "B", Age = 20 });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "C", Age = 30 });
        await executor.Query<User>().InsertAsync(new User { Id = 4, Name = "D", Age = 40 });

        var page2 = await executor.Query<User>()
            .Skip(2)
            .Take(2)
            .ToListAsync();

        Assert.Equal(2, page2.Count);
        Assert.Equal("C", page2[0].Name);
        Assert.Equal("D", page2[1].Name);
    }

    [Fact]
    public async Task Sort_Ascending()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "C", Age = 30 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "A", Age = 10 });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "B", Age = 20 });

        var sorted = await executor.Query<User>()
            .Sort("Name")
            .ToListAsync();

        Assert.Equal(3, sorted.Count);
        Assert.Equal("A", sorted[0].Name);
        Assert.Equal("B", sorted[1].Name);
        Assert.Equal("C", sorted[2].Name);
    }

    [Fact]
    public async Task Sort_Descending()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "A", Age = 10 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "B", Age = 20 });

        var sorted = await executor.Query<User>()
            .Sort("Age", descending: true)
            .ToListAsync();

        Assert.Equal(20, sorted[0].Age);
        Assert.Equal(10, sorted[1].Age);
    }

    [Fact]
    public async Task FirstOrDefault_ReturnsFirstMatch()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });

        var first = await executor.Query<User>()
            .Filter(new FieldGreaterThan("Age", 20))
            .FirstOrDefaultAsync();

        Assert.NotNull(first);
        Assert.Equal("张三", first.Name);
    }

    [Fact]
    public async Task MultiTable_IndependentOperations()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<Order>().InsertAsync(new Order { Id = 1, ProductName = "键盘", Price = 299, Quantity = 1 });
        await executor.Query<Product>().InsertAsync(new Product { Id = 1, Name = "鼠标", Price = 149, Category = "外设" });

        var users = await executor.Query<User>().ToListAsync();
        var orders = await executor.Query<Order>().ToListAsync();
        var products = await executor.Query<Product>().ToListAsync();

        Assert.Single(users);
        Assert.Single(orders);
        Assert.Single(products);
    }

    [Fact]
    public async Task EntityMapper_RoundTrip_PreservesValues()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        var original = new User
        {
            Id = 42,
            Name = "测试用户",
            Email = "test@example.com",
            Age = 28,
            IsActive = true
        };

        await executor.Query<User>().InsertAsync(original);

        var retrieved = await executor.Query<User>()
            .Filter(new FieldEquals("Id", 42))
            .FirstOrDefaultAsync();

        Assert.NotNull(retrieved);
        Assert.Equal(original.Id, retrieved.Id);
        Assert.Equal(original.Name, retrieved.Name);
        Assert.Equal(original.Email, retrieved.Email);
        Assert.Equal(original.Age, retrieved.Age);
        Assert.Equal(original.IsActive, retrieved.IsActive);
    }

    [Fact]
    public async Task ExecuteAsync_DirectQuery_Works()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "直接查询", Age = 100 });

        var findQuery = new FindQuery(nameof(User),
            new FieldEquals("Age", 100));

        var result = await executor.ExecuteAsync(findQuery);

        Assert.True(result.Success);
        Assert.Single(result.Rows);
        Assert.Equal("直接查询", result.Rows[0]["Name"]);
    }
}