using System.Text.Json;
using EffectResult = Valkyrie.Asgard.Effect.EffectResult;

namespace VOA.ToolChain.Tests;

public sealed class EffectFacade
{
    public static EffectResult create_pending_result(string entryId)
    {
        return new()
        {
            status = EffectStatus.pending,
            entry_id = entryId
        };
    }

    public static EffectResult create_resolved_result(string entryId, JsonElement data)
    {
        return new()
        {
            status = EffectStatus.resolved,
            entry_id = entryId,
            data = data
        };
    }

    public static EffectResult create_rejected_result(string entryId, string error)
    {
        return new()
        {
            status = EffectStatus.rejected,
            entry_id = entryId,
            error = error
        };
    }

    public static bool is_pending(EffectResult result)
    {
        return result.status == EffectStatus.pending;
    }

    public static bool is_resolved(EffectResult result)
    {
        return result.status == EffectStatus.resolved;
    }

    public static bool is_rejected(EffectResult result)
    {
        return result.status == EffectStatus.rejected;
    }
}