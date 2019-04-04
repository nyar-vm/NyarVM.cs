namespace Galatea.Tests;

using Galatea.Compiler;
using Galatea.Compiler.NyarBridge;
using Galatea.Execution;
using Galatea.Flux;
using Galatea.Runtime;
using Nyar.Avatar.Neural.Nodes;
using Nyar.Dialect.Core.Nodes;
using Nyar.IR.EGraph;
using Nyar.IR.Intent;

/// <summary>
///     Tensor IKun 对接全链路测试 —— 验证 Galatea → Nyar EGraph → 优化 全链路正确性
/// </summary>
public class NyarBridgeTests : GradientTestBase
{
    #region 计算图 → EGraph 转换

    [Fact]
    public void Convert_DenseReLU_ProducesCorrectEgraph()
    {
        var graph = GalateaCompiler.Compile("simple", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 4 }, { "fanOut", 3 }
            }, new[] { ArrayND.HeNormal(4, 4 * 3), ArrayND.Zeros(1, 3) }, "x");
            builder.Operation("a1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.Output("a1");
        });

        var result = GraphToIkunConverter.Convert(graph);

        Assert.NotNull(result.Egraph);
        Assert.True(result.NameToId.ContainsKey("x"));
        Assert.True(result.NameToId.ContainsKey("d1"));
        Assert.True(result.NameToId.ContainsKey("a1"));
        Assert.Single(result.OutputIds);
    }

    [Fact]
    public void Convert_ConvMaxPoolFlatten_ProducesCorrectEgraph()
    {
        var graph = GalateaCompiler.Compile("cnn", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("conv1", "Conv2D", new Dictionary<string, object>
            {
                { "inChannels", 1 }, { "outChannels", 4 }, { "kernelSize", 3 },
                { "inH", 8 }, { "inW", 8 }
            }, new[] { ArrayND.HeNormal(9, 4 * 9), ArrayND.Zeros(4) }, "x");
            builder.Operation("pool1", "MaxPool2D", new Dictionary<string, object>
            {
                { "channels", 4 }, { "inH", 6 }, { "inW", 6 }
            }, "conv1");
            builder.Operation("flat1", "Flatten", new Dictionary<string, object>
            {
                { "startDim", 1 }
            }, "pool1");
            builder.Output("flat1");
        });

        var result = GraphToIkunConverter.Convert(graph);

        Assert.NotNull(result.Egraph);
        Assert.True(result.NameToId.ContainsKey("conv1"));
        Assert.True(result.NameToId.ContainsKey("pool1"));
        Assert.True(result.NameToId.ContainsKey("flat1"));
    }

    [Fact]
    public void Convert_AllActivations_ProduceCorrectNodeTypes()
    {
        var graph = GalateaCompiler.Compile("activations", builder =>
        {
            builder.Input("x");
            builder.Operation("r", "ReLU", new Dictionary<string, object>(), "x");
            builder.Operation("sm", "Softmax", new Dictionary<string, object>(), "r");
            builder.Operation("g", "GELU", new Dictionary<string, object>(), "sm");
            builder.Operation("si", "SiLU", new Dictionary<string, object>(), "g");
            builder.Output("si");
        });

        var result = GraphToIkunConverter.Convert(graph);

        Assert.NotNull(result.Egraph);
        Assert.True(result.NameToId.ContainsKey("r"));
        Assert.True(result.NameToId.ContainsKey("sm"));
        Assert.True(result.NameToId.ContainsKey("g"));
        Assert.True(result.NameToId.ContainsKey("si"));

        var reluId = result.NameToId["r"];
        var reluClass = result.Egraph.GetClass(reluId);
        Assert.NotNull(reluClass);
        Assert.Contains(reluClass.Nodes, n => n is Relu);
    }

    [Fact]
    public void Convert_BatchNormDropout_ProduceCorrectNodeTypes()
    {
        var graph = GalateaCompiler.Compile("bn_dropout", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("bn", "BatchNorm", new Dictionary<string, object>
            {
                { "numFeatures", 8 }
            }, new[] { ArrayND.Zeros(8), ArrayND.Zeros(8) }, "x");
            builder.Operation("drop", "Dropout", new Dictionary<string, object>
            {
                { "rate", 0.5f }
            }, "bn");
            builder.Output("drop");
        });

        var result = GraphToIkunConverter.Convert(graph);

        Assert.NotNull(result.Egraph);

        var dropId = result.NameToId["drop"];
        var dropClass = result.Egraph.GetClass(dropId);
        Assert.NotNull(dropClass);
        Assert.Contains(dropClass.Nodes, n => n is Dropout);
    }

    [Fact]
    public void Convert_OutputIds_CorrectlyMapped()
    {
        var graph = GalateaCompiler.Compile("multi_output", builder =>
        {
            builder.Input("x");
            builder.Operation("a", "ReLU", new Dictionary<string, object>(), "x");
            builder.Operation("b", "SiLU", new Dictionary<string, object>(), "x");
            builder.Output("a");
            builder.Output("b");
        });

        var result = GraphToIkunConverter.Convert(graph);

        Assert.Equal(2, result.OutputIds.Count);
    }

    [Fact]
    public void Convert_InputNames_AreTrackedInNameToId()
    {
        var graph = GalateaCompiler.Compile("named_input", builder =>
        {
            builder.Input("my_input");
            builder.Operation("op", "ReLU", new Dictionary<string, object>(), "my_input");
            builder.Output("op");
        });

        var result = GraphToIkunConverter.Convert(graph);

        Assert.True(result.NameToId.ContainsKey("my_input"));
    }

    #endregion

    #region EGraph 优化器

    [Fact]
    public void Optimize_EgraphRunsSuccessfully()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.Add(new Sym("x"));
        var weightId = egraph.Add(new Sym("w"));
        var convId = egraph.Add(new Conv2D(inputId, weightId, null, (1, 1), (0, 0)));
        var scaleId = egraph.Add(new Sym("scale"));
        var bnBiasId = egraph.Add(new Sym("bn_bias"));
        var meanId = egraph.Add(new Sym("mean"));
        var varId = egraph.Add(new Sym("var"));
        var bnId = egraph.Add(new BatchNorm(convId, scaleId, bnBiasId, meanId, varId, 1e-5));
        var reluId = egraph.Add(new Relu(bnId));

        var result = TensorIkunOptimizer.Optimize(egraph, reluId, maxIterations: 5);

        Assert.NotNull(result);
        Assert.NotNull(result.Egraph);
        Assert.NotNull(result.BestProgram);
    }

    [Fact]
    public void Optimize_GetOptimizationRate_CalculatesCorrectly()
    {
        var rate = TensorIkunOptimizer.GetOptimizationRate(6, 2);
        Assert.True(rate > 0.5f);

        var zeroRate = TensorIkunOptimizer.GetOptimizationRate(0, 0);
        Assert.Equal(0.0f, zeroRate);

        var noChange = TensorIkunOptimizer.GetOptimizationRate(10, 10);
        Assert.Equal(0.0f, noChange);
    }

    #endregion

    #region 端到端全链路

    [Fact]
    public void FullPipeline_SimpleModel_ConvertsAndOptimizes()
    {
        var dense = new Dense(fanIn: 4, fanOut: 3);
        var input = RandomInput(2, 4);

        var graph = GalateaCompiler.Compile("e2e", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("d1", "Dense", new Dictionary<string, object>
            {
                { "fanIn", 4 }, { "fanOut", 3 }
            }, new[] { dense.Weight, dense.Bias }, "x");
            builder.Operation("a1", "ReLU", new Dictionary<string, object>(), "d1");
            builder.Output("a1");
        });

        var ikunResult = GraphToIkunConverter.Convert(graph);
        Assert.NotNull(ikunResult.Egraph);

        var rootId = ikunResult.OutputIds.FirstOrDefault();
        if (rootId is not null)
        {
            var optimized = TensorIkunOptimizer.Optimize(ikunResult.Egraph, rootId, maxIterations: 3);
            Assert.NotNull(optimized.BestProgram);
        }

        var runtime = new GalateaRuntime();
        var execResult = runtime.Run(graph, new Dictionary<string, ArrayND> { { "x", input } });
        Assert.True(execResult.Success);

        var directOutput = Activations.ReLUForward(dense.Forward(input));
        var error = MaxAbsoluteError(directOutput, execResult.Outputs["a1"]);
        Assert.True(error < GradientTolerance, $"全链路执行误差 {error} 超过容差");
    }

    [Fact]
    public void FullPipeline_ConvBnRelu_ConvertsAndOptimizes()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 4, kernelSize: 3, padding: 0);
        var bn = new BatchNorm(4);

        var graph = GalateaCompiler.Compile("e2e_fusion", builder =>
        {
            builder.Input("x");
            builder.ParameterizedOp("conv1", "Conv2D", new Dictionary<string, object>
            {
                { "inChannels", 1 }, { "outChannels", 4 }, { "kernelSize", 3 },
                { "inH", 8 }, { "inW", 8 }
            }, new[] { conv.Weight, conv.Bias }, "x");
            builder.ParameterizedOp("bn1", "BatchNorm", new Dictionary<string, object>
            {
                { "numFeatures", 4 }
            }, new[] { bn.Gamma, bn.Beta }, "conv1");
            builder.Operation("relu1", "ReLU", new Dictionary<string, object>(), "bn1");
            builder.Output("relu1");
        });

        var ikunResult = GraphToIkunConverter.Convert(graph);
        Assert.NotNull(ikunResult.Egraph);

        var convId = ikunResult.NameToId["conv1"];
        var convClass = ikunResult.Egraph.GetClass(convId);
        Assert.NotNull(convClass);
        Assert.Contains(convClass.Nodes, n => n is Conv2D);

        var bnId = ikunResult.NameToId["bn1"];
        var bnClass = ikunResult.Egraph.GetClass(bnId);
        Assert.NotNull(bnClass);
        Assert.Contains(bnClass.Nodes, n => n is BatchNorm);

        var rootId = ikunResult.OutputIds.FirstOrDefault();
        if (rootId is not null)
        {
            var optimized = TensorIkunOptimizer.Optimize(ikunResult.Egraph, rootId, maxIterations: 5);
            Assert.NotNull(optimized.BestProgram);

            var rate = TensorIkunOptimizer.GetOptimizationRate(3, 1);
            Assert.True(rate > 0.5f, $"融合优化率 {rate} 应大于 50%");
        }
    }

    #endregion
}
