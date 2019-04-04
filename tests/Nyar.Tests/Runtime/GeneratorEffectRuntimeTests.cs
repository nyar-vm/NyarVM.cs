using Nyar.Types;
using Nyar.VM.NyarVM.Runtime;

namespace Nyar.Tests.Runtime;

public sealed class GeneratorEffectRuntimeTests
{
    [Fact]
    public void RegisterYielderHandlers_YieldShouldExtractValueField()
    {
        var runtime = new EffectRuntime();
        var yielded = new List<Value>();
        var completed = false;
        runtime.register_yielder_handlers(value => yielded.Add(value), () => completed = true);

        var payload = Value.from_object(new Dictionary<string, Value>
        {
            ["value"] = Value.from_int(7)
        });

        var result = runtime.perform("Yielder::Yield", payload);

        Assert.Equal(Value.@null, result);
        Assert.Single(yielded);
        Assert.Equal(7, yielded[0].i32);
        Assert.False(completed);
    }

    [Fact]
    public void RegisterYielderHandlers_YieldBreakShouldNotifyCompletion()
    {
        var runtime = new EffectRuntime();
        var yielded = new List<Value>();
        var completed = false;
        runtime.register_yielder_handlers(value => yielded.Add(value), () => completed = true);

        var result = runtime.perform("Yielder::YieldBreak", Value.@null);

        Assert.Equal(Value.@null, result);
        Assert.Empty(yielded);
        Assert.True(completed);
    }
}
