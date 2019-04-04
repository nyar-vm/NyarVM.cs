using System.Text.Json;
using Xunit;
using EffectConfig = Valkyrie.Asgard.Effect.EffectConfig;
using EffectEntry = Valkyrie.Asgard.Effect.EffectEntry;
using EffectStore = Valkyrie.Asgard.Effect.EffectStore;

namespace VOA.ToolChain.Tests;

public sealed class VoaEffectIntegrationTests
{
    public const double default_retry_delay_ms = 1000;

    [Fact]
    public void EffectConfig_Default_Values()
    {
        var config = EffectConfig.@default;

        Assert.Equal(3, config.retry_count);
        Assert.Equal(1000, config.retry_delay_ms);
        Assert.Equal(30000, config.timeout_ms);
        Assert.Equal(0, config.cache_ttl_ms);
        Assert.True(config.dedupe);
    }

    [Fact]
    public void EffectConfig_Custom_ValuesSnapped()
    {
        var config = new EffectConfig
        {
            retry_count = 5,
            retry_delay_ms = 200,
            timeout_ms = 5000,
            cache_ttl_ms = 60000
        };

        Assert.Equal(5, config.retry_count);
        Assert.Equal(200, config.retry_delay_ms);
        Assert.Equal(5000, config.timeout_ms);
        Assert.Equal(60000, config.cache_ttl_ms);
    }

    [Fact]
    public void EffectResult_Pending_HasCorrectStatus()
    {
        var result = EffectFacade.create_pending_result("ef-001");

        Assert.Equal(EffectStatus.pending, result.status);
        Assert.Equal("ef-001", result.entry_id);
        Assert.Null(result.data);
        Assert.Null(result.error);
    }

    [Fact]
    public void EffectResult_Resolved_HasData()
    {
        var data = JsonSerializer.SerializeToElement(@"{""name"": ""Alice""}");
        var result = EffectFacade.create_resolved_result("ef-002", data);

        Assert.Equal(EffectStatus.resolved, result.status);
        Assert.Equal("ef-002", result.entry_id);
        Assert.NotNull(result.data);
        Assert.Null(result.error);
    }

    [Fact]
    public void EffectResult_Rejected_HasError()
    {
        var error = "Fetch failed: timeout";
        var result = EffectFacade.create_rejected_result("ef-003", error);

        Assert.Equal(EffectStatus.rejected, result.status);
        Assert.Equal("ef-003", result.entry_id);
        Assert.Null(result.data);
        Assert.Equal(error, result.error);
    }

    [Fact]
    public void EffectResult_IsPending_ReturnsTrue()
    {
        var result = EffectFacade.create_pending_result("ef-004");

        Assert.True(EffectFacade.is_pending(result));
        Assert.False(EffectFacade.is_resolved(result));
        Assert.False(EffectFacade.is_rejected(result));
    }

    [Fact]
    public void EffectResult_IsResolved_ReturnsTrue()
    {
        var data = JsonSerializer.SerializeToElement("{}");
        var result = EffectFacade.create_resolved_result("ef-005", data);

        Assert.True(EffectFacade.is_resolved(result));
        Assert.False(EffectFacade.is_pending(result));
        Assert.False(EffectFacade.is_rejected(result));
    }

    [Fact]
    public void EffectResult_IsRejected_ReturnsTrue()
    {
        var result = EffectFacade.create_rejected_result("ef-006", "fail");

        Assert.True(EffectFacade.is_rejected(result));
        Assert.False(EffectFacade.is_pending(result));
        Assert.False(EffectFacade.is_resolved(result));
    }

    [Fact]
    public void EffectEntry_Initial_StatusPending()
    {
        var entry = new EffectEntry
        {
            id = "entry-001",
            function_name = "fetchUser",
            args = ["123"],
            status = EffectStatus.pending,
            created_at = 1000L,
            updated_at = 1000L
        };

        Assert.Equal(EffectStatus.pending, entry.status);
        Assert.Equal("fetchUser", entry.function_name);
        Assert.Empty(entry.error);
        Assert.Null(entry.result);
    }

    [Fact]
    public void EffectEntry_TransitionPendingToResolved()
    {
        var entry = new EffectEntry
        {
            id = "entry-002",
            function_name = "loadData",
            args = [],
            status = EffectStatus.pending,
            created_at = 1000L,
            updated_at = 1000L
        };

        Assert.Equal(EffectStatus.pending, entry.status);

        var resolved = entry with
        {
            status = EffectStatus.resolved,
            result = JsonSerializer.SerializeToElement(@"{""ok"": true}"),
            updated_at = 2000L
        };

        Assert.Equal(EffectStatus.resolved, resolved.status);
        Assert.NotEqual(entry.updated_at, resolved.updated_at);
        Assert.NotNull(resolved.result);
    }

    [Fact]
    public void EffectEntry_TransitionPendingToRejected()
    {
        var entry = new EffectEntry
        {
            id = "entry-003",
            function_name = "loadData",
            args = [],
            status = EffectStatus.pending,
            created_at = 1000L,
            updated_at = 1000L
        };

        var rejected = entry with
        {
            status = EffectStatus.rejected,
            error = "Network error",
            updated_at = 2000L
        };

        Assert.Equal(EffectStatus.rejected, rejected.status);
        Assert.Equal("Network error", rejected.error);
    }

    [Fact]
    public void EffectStore_RegisterAndResolve()
    {
        var store = new EffectStore();

        var entryId = store.register("fetchUser", ["42"], EffectConfig.@default);
        Assert.NotNull(entryId);

        var data = JsonSerializer.SerializeToElement(@"{""name"": ""Bob""}");
        store.resolve(entryId, data);
        var result = store.get_result(entryId);

        Assert.Equal(EffectStatus.resolved, result!.status);
        Assert.NotNull(result.data);
        Assert.Null(result.error);
    }

    [Fact]
    public void EffectStore_RegisterAndReject()
    {
        var store = new EffectStore();

        var entryId = store.register("fetchUser", ["99"], EffectConfig.@default);
        store.reject(entryId, "Not found");
        var result = store.get_result(entryId);

        Assert.Equal(EffectStatus.rejected, result!.status);
        Assert.Null(result.data);
        Assert.Equal("Not found", result.error);
    }

    [Fact]
    public void EffectStore_Duplicate_NullIdReturned()
    {
        var store = new EffectStore();

        var id1 = store.register("fetch", ["1"], EffectConfig.@default);
        Assert.NotNull(id1);

        var result = store.get_result(id1);
        Assert.Equal(EffectStatus.pending, result!.status);

        store.resolve(id1, JsonSerializer.SerializeToElement("{}"));
        Assert.Equal(0, store.pending_count);
    }

    [Fact]
    public void EffectStore_PendingCount_DecrementsOnResolve()
    {
        var store = new EffectStore();

        var id1 = store.register("fn1", [], new EffectConfig { timeout_ms = 500 });
        var id2 = store.register("fn2", [], new EffectConfig { timeout_ms = 500 });

        Assert.Equal(2, store.pending_count);

        store.resolve(id1!, JsonSerializer.SerializeToElement("{}"));
        Assert.Equal(1, store.pending_count);

        store.reject(id2!, "fail");
        Assert.Equal(0, store.pending_count);
    }

    [Fact]
    public void EffectStore_ExpireEntries_RemovesOld()
    {
        var store = new EffectStore();

        var id = store.register("fn", [], new EffectConfig { timeout_ms = 0 });
        Assert.NotNull(id);

        var resultBeforeTimeout = store.get_result(id!)!;

        Assert.Equal(EffectStatus.pending, resultBeforeTimeout.status);

        store.expire(now: 100L);

        var resultAfterTimeout = store.get_result(id!);

        Assert.Equal(EffectStatus.rejected, resultAfterTimeout!.status);
        Assert.Equal("timeout", resultAfterTimeout.error);
    }

    [Fact]
    public void EffectStore_FullLifecycle()
    {
        var store = new EffectStore();

        var id = store.register("fetchUser", ["42"], new EffectConfig
        {
            retry_count = 3,
            retry_delay_ms = 100
        });

        Assert.NotNull(id);
        Assert.Equal(1, store.pending_count);

        var result = store.get_result(id!);
        Assert.Equal(EffectStatus.pending, result!.status);

        var data = JsonSerializer.SerializeToElement(@"{""id"": 42, ""name"": ""Alice""}");
        store.resolve(id!, data);

        var resolved = store.get_result(id!);
        Assert.Equal(EffectStatus.resolved, resolved!.status);
        Assert.NotNull(resolved.data);
    }
}