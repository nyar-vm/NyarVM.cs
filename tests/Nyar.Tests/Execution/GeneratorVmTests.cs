using Nyar.Types;
using Std.Data.Binary.NyarIR.Data;

namespace Nyar.Tests.Execution;

public sealed class GeneratorVmTests
{
    [Fact]
    public void NyarVm_RegisterYielderHandlers_ShouldReceiveYieldAndYieldBreak()
    {
        var bytecode = new BytecodeBuilder()
            .emit(NyarHeadCode.@const, 0)
            .emit(NyarHeadCode.new_object)
            .emit(NyarHeadCode.dup)
            .emit(NyarHeadCode.@const, 1)
            .emit(NyarHeadCode.@const, 2)
            .emit(NyarHeadCode.set_field)
            .emit(NyarHeadCode.perform_effect)
            .emit(NyarHeadCode.pop)
            .emit(NyarHeadCode.@const, 3)
            .emit(NyarHeadCode.@const, 4)
            .emit(NyarHeadCode.perform_effect)
            .emit(NyarHeadCode.@return)
            .to_array();

        var function = new NyarFunction("main", 0, 0, 0, bytecode.Length);
        var module = new NyarModule("generator_vm_test")
        {
            constants =
            [
                Value.from_string("Yielder::Yield"),
                Value.from_string("value"),
                Value.from_int(7),
                Value.from_string("Yielder::YieldBreak"),
                Value.from_int(0)
            ],
            functions = [function],
            raw_bytecode = bytecode
        };
        function.module = module;

        var vm = new NyarVm();
        var yielded = new List<Value>();
        var completed = false;
        vm.load(module);
        vm.register_yielder_handlers(value => yielded.Add(value), () => completed = true);

        var result = vm.run(module.name, function.name);

        Assert.Equal(Value.@null, result);
        Assert.Single(yielded);
        Assert.Equal(7, yielded[0].i32);
        Assert.True(completed);
    }

    private sealed class BytecodeBuilder
    {
        private readonly List<byte> _bytes = [];

        public BytecodeBuilder emit(NyarHeadCode op)
        {
            _bytes.Add((byte)op);
            return this;
        }

        public BytecodeBuilder emit(NyarHeadCode op, int operand)
        {
            _bytes.Add((byte)op);
            _bytes.AddRange(BitConverter.GetBytes(operand));
            return this;
        }

        public byte[] to_array()
        {
            return _bytes.ToArray();
        }
    }
}
