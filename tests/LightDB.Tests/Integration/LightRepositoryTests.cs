using Std.Database.Core;

namespace LightDB.Tests.Integration;

public sealed class LightRepositoryTests : IDisposable
{
    private readonly LightDatabase _db;

    public LightRepositoryTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"LightDB_test_{Guid.NewGuid():N}");
        _db = new LightDatabase(new LightOptions { path = tempPath });
    }

    public void Dispose()
    {
        _db.Dispose();
        try
        {
            if (Directory.Exists(_db.options.path)) Directory.Delete(_db.options.path, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task AddAsync_ShouldStoreEntity()
    {
        var repo = new LightRepository<TestEntity>(_db);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        await repo.AddAsync(entity);

        var result = await repo.GetByIdAsync(entity.Id);
        Assert.NotNull(result);
        Assert.Equal(entity.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentId_ShouldReturnNull()
    {
        var repo = new LightRepository<TestEntity>(_db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyEntity()
    {
        var repo = new LightRepository<TestEntity>(_db);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
        await repo.AddAsync(entity);

        entity.Name = "Updated";
        await repo.UpdateAsync(entity);

        var result = await repo.GetByIdAsync(entity.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        var repo = new LightRepository<TestEntity>(_db);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "ToDelete" };
        await repo.AddAsync(entity);

        await repo.DeleteAsync(entity.Id);

        var result = await repo.GetByIdAsync(entity.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        var repo = new LightRepository<TestEntity>(_db);
        var e1 = new TestEntity { Id = Guid.NewGuid(), Name = "E1" };
        var e2 = new TestEntity { Id = Guid.NewGuid(), Name = "E2" };

        await repo.AddAsync(e1);
        await repo.AddAsync(e2);

        var all = await repo.GetAllAsync();
        var list = all.ToList();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotThrow()
    {
        var repo = new LightRepository<TestEntity>(_db);
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        await repo.AddAsync(entity);

        await repo.SaveChangesAsync();
    }

    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }
}