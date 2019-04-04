using Nyar.Assembler;
using Nyar.Types;

namespace Nyar.Tests.Execution;

public class BackendSelectorTests
{
    [Fact]
    public void SelectBackend_TypedSelector_ShouldPreferExactArchMatch()
    {
        var selector = new BackendSelector(
        [
            new FakeBackend<byte[]>("generic-wasm", [Arch.wasm32, Arch.wasm64], [1]),
            new FakeBackend<byte[]>("exact-wasm32", [Arch.wasm32], [2]),
            new FakeBackend<string>("text-wasm32", [Arch.wasm32], "wat")
        ]);

        var backend = selector.SelectBackend<byte[]>(Arch.wasm32);

        Assert.NotNull(backend);
        Assert.Equal("exact-wasm32", backend!.Name);
    }

    [Fact]
    public void TrySelectBackend_MissingOutputType_ShouldReturnFalse()
    {
        var selector = new BackendSelector(
        [
            new FakeBackend<string>("text-wasm32", [Arch.wasm32], "wat")
        ]);

        var success = selector.TrySelectBackend<byte[]>(Arch.wasm32, out var backend);

        Assert.False(success);
        Assert.Null(backend);
    }

    [Fact]
    public void GetBackends_ShouldReturnOnlyRequestedOutputType()
    {
        var selector = new BackendSelector(
        [
            new FakeBackend<byte[]>("wasm32", [Arch.wasm32], [1]),
            new FakeBackend<byte[]>("wasm64", [Arch.wasm64], [2]),
            new FakeBackend<string>("text", [Arch.wasm32], "wat")
        ]);

        var backends = selector.GetBackends<byte[]>();

        Assert.Equal(2, backends.Count);
        Assert.All(backends, backend => Assert.IsAssignableFrom<ICodeGenBackend<byte[]>>(backend));
        Assert.DoesNotContain(backends, backend => backend.Name == "text");
    }

    private sealed class FakeBackend<TOutput> : ICodeGenBackend<TOutput>
    {
        private readonly TOutput _output;

        public FakeBackend(string name, IReadOnlyList<Arch> supportedArchs, TOutput output)
        {
            this.name = name;
            supported_archs = supportedArchs;
            _output = output;
        }

        public string name { get; }

        public IReadOnlyList<Arch> supported_archs { get; }

        public bool validate(GenerateModule module, out List<Diagnostic> diagnostics)
        {
            diagnostics = [];
            return true;
        }

        public OutputSpec<TOutput> compile(GenerateModule module, CompilationOptions options)
        {
            return new OutputSpec<TOutput>
            {
                Data = _output,
                FileExtension = ".bin",
                MediaType = "application/octet-stream"
            };
        }

        OutputSpec ICodeGenBackend.Compile(GenerateModule module, CompilationOptions options)
        {
            return compile(module, options);
        }
    }
}
