namespace Galatea.Tests;

using Galatea.Execution;
using Galatea.Flux;
using Galatea.Compiler;
using Galatea.Runtime;

/// <summary>
///     编译执行管线测试 —— 验证 Compiler + Runtime 端到端正确性
/// </summary>
public class CompilerRuntimeTests : GradientTestBase
{
    [Fact]
    public void CompileAndRun_DenseReLU_MatchesDirectForward()
    {
        var dense = new Dense(fanIn: 4, fanOut: 3);
        var input = RandomInput(2, 4);

        var graph = GalateaCompiler.Compile("test", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("dense1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 4 },
                { "fanOut", 3 }
            }, new[] { dense.Weight, dense.Bias }, "x");
            builder.Operation("relu1", "ReLU", new Dictionary<string, object>(), "dense1");
            builder.Output("relu1");
        });

        var runtime = new GalateaRuntime();
        var result = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });

        Assert.True(result.Success);
        Assert.Single(result.Outputs);

        var compiledOutput = result.Outputs["relu1"];
        var directOutput = Activations.ReLUForward(dense.Forward(input));

        var error = MaxAbsoluteError(directOutput, compiledOutput);
        Assert.True(error < GradientTolerance, $"编译执行误差 {error} 超过容差");
    }

    [Fact]
    public void CompileAndRun_MultipleOps_MatchesDirectForward()
    {
        var dense1 = new Dense(fanIn: 4, fanOut: 3);
        var dense2 = new Dense(fanIn: 3, fanOut: 2);
        var input = RandomInput(2, 4);

        var graph = GalateaCompiler.Compile("multi", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 4 }, { "fanOut", 3 }
            }, new[] { dense1.Weight, dense1.Bias }, "x");
            builder.Operation("a1", "Sigmoid", new Dictionary<string, object>(), "d1");
            builder.ParameterizedOp("d2", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 3 }, { "fanOut", 2 }
            }, new[] { dense2.Weight, dense2.Bias }, "a1");
            builder.Operation("a2", "Tanh", new Dictionary<string, object>(), "d2");
            builder.Output("a2");
        });

        var runtime = new GalateaRuntime();
        var result = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });

        Assert.True(result.Success);

        var compiledOutput = result.Outputs["a2"];
        var x = Activations.SigmoidForward(dense1.Forward(input));
        x = Activations.TanhForward(dense2.Forward(x));
        var directOutput = x;

        var error = MaxAbsoluteError(directOutput, compiledOutput);
        Assert.True(error < GradientTolerance, $"多操作编译执行误差 {error} 超过容差");
    }

    [Fact]
    public void CompileAndRun_Conv2DReLU_MatchesDirectForward()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 2, kernelSize: 3, padding: 0);
        var input = RandomInput(1, 1 * 5 * 5);

        var graph = GalateaCompiler.Compile("conv_test", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("conv1", "Conv2D", new Dictionary<string, object>
            {
                { "inChannels", 1 },
                { "outChannels", 2 },
                { "kernelSize", 3 },
                { "inH", 5 },
                { "inW", 5 }
            }, new[] { conv.Weight, conv.Bias }, "x");
            builder.Operation("relu1", "ReLU", new Dictionary<string, object>(), "conv1");
            builder.Output("relu1");
        });

        var runtime = new GalateaRuntime();
        var result = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });

        Assert.True(result.Success);

        var compiledOutput = result.Outputs["relu1"];
        var (convOut, _, _) = conv.Forward(input, inH: 5, inW: 5);
        var directOutput = Activations.ReLUForward(convOut);

        var error = MaxAbsoluteError(directOutput, compiledOutput);
        Assert.True(error < GradientTolerance, $"Conv2D 编译执行误差 {error} 超过容差");
    }

    [Fact]
    public void CompileAndRun_AllActivations_MatchesDirectForward()
    {
        var input = RandomInput(2, 6);

        void TestActivation(string opType, Func<ArrayND, ArrayND> directFn)
        {
            var graph = GalateaCompiler.Compile($"test_{opType}", builder =>
            {
                builder.Input("x");
                builder.Operation("act", opType, new Dictionary<string, object>(), "x");
                builder.Output("act");
            });

            var runtime = new GalateaRuntime();
            var result = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });

            Assert.True(result.Success);
            var error = MaxAbsoluteError(directFn(input), result.Outputs["act"]);
            Assert.True(error < GradientTolerance, $"{opType} 编译执行误差 {error} 超过容差");
        }

        TestActivation("ReLU", Activations.ReLUForward);
        TestActivation("Sigmoid", Activations.SigmoidForward);
        TestActivation("Tanh", Activations.TanhForward);
        TestActivation("Softmax", Activations.SoftmaxForward);
        TestActivation("GELU", Activations.GELUForward);
        TestActivation("SiLU", Activations.SiLUForward);
    }

    [Fact]
    public void CompileAndRun_ExecutionMetrics_AreTracked()
    {
        var dense = new Dense(fanIn: 4, fanOut: 3);
        var input = RandomInput(2, 4);

        var graph = GalateaCompiler.Compile("metrics", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 4 }, { "fanOut", 3 }
            }, new[] { dense.Weight, dense.Bias }, "x");
            builder.Output("d1");
        });

        var runtime = new GalateaRuntime();
        var result = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });

        Assert.True(result.Success);
        Assert.True(result.Metrics.Duration > TimeSpan.Zero);
        Assert.Equal(1, result.Metrics.OperationsCount);
    }

    [Fact]
    public void CompileAndRun_UnsupportedOpType_ReturnsError()
    {
        var graph = new CompiledGraph
        {
            Id = "error_test",
            InputNames = new[] { "x" },
            OutputNames = new[] { "bad" },
            Operations = new[]
            {
                new GraphOperation
                {
                    Name = "bad",
                    OpType = "NonExistentOp",
                    InputNames = new[] { "x" }
                }
            }
        };

        var runtime = new GalateaRuntime();
        var result = runtime.Run(graph, new Dictionary<string, ArrayND>
        {
            { "x", RandomInput(1, 4) }
        });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("不支持", result.ErrorMessage);
    }

    [Fact]
    public void CompileAndRun_MissingInput_ReturnsError()
    {
        var graph = new CompiledGraph
        {
            Id = "missing",
            InputNames = new[] { "x" },
            OutputNames = new[] { "out1" },
            Operations = new[]
            {
                new GraphOperation
                {
                    Name = "out1",
                    OpType = "ReLU",
                    InputNames = new[] { "missing_input" }
                }
            }
        };

        var runtime = new GalateaRuntime();
        var result = runtime.Run(graph, new Dictionary<string, ArrayND>
        {
            { "x", RandomInput(1, 4) }
        });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("找不到输入", result.ErrorMessage);
    }

    [Fact]
    public void CompileAndRun_InputOutputNames_ArePreserved()
    {
        var graph = GalateaCompiler.Compile("named", builder =>
        {
            builder.Input("my_input");
            builder.Operation("my_op", "ReLU", new Dictionary<string, object>(), "my_input");
            builder.Output("my_op");
        });

        Assert.Contains("my_input", graph.InputNames);
        Assert.Contains("my_op", graph.OutputNames);
        Assert.Single(graph.Operations);
        Assert.Equal("my_op", graph.Operations[0].Name);
        Assert.Equal("ReLU", graph.Operations[0].OpType);
    }
}
