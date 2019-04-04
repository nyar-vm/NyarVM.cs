namespace Nyar.Tests.Binary;

public sealed class OnnxRoundTripTests
{
    #region 最小模的

    [Fact]
    public void EncodeDecode_MinimalModel_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            ProducerName = "Test",
            ProducerVersion = "1.0",
            Domain = "ai.onnx",
            ModelVersion = 1
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(8L, decoded.IrVersion);
        Assert.Equal("Test", decoded.ProducerName);
        Assert.Equal("1.0", decoded.ProducerVersion);
        Assert.Equal("ai.onnx", decoded.Domain);
        Assert.Equal(1L, decoded.ModelVersion);
    }

    [Fact]
    public void EncodeDecode_OnlyIrVersion_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 10
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(10L, decoded.IrVersion);
        Assert.Empty(decoded.ProducerName);
        Assert.Null(decoded.Graph);
        Assert.Empty(decoded.OpsetImport);
    }

    #endregion

    #region 算子的

    [Fact]
    public void EncodeDecode_OpsetImport_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            OpsetImport =
            [
                new OnnxOperatorSetId { Domain = "ai.onnx", Version = 18 },
                new OnnxOperatorSetId { Domain = "ai.onnx.ml", Version = 3 }
            ]
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(2, decoded.OpsetImport.Count);
        Assert.Equal("ai.onnx", decoded.OpsetImport[0].Domain);
        Assert.Equal(18L, decoded.OpsetImport[0].Version);
        Assert.Equal("ai.onnx.ml", decoded.OpsetImport[1].Domain);
        Assert.Equal(3L, decoded.OpsetImport[1].Version);
    }

    #endregion

    #region 空图

    [Fact]
    public void EncodeDecode_EmptyGraph_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "empty_graph",
                DocString = "An empty test graph"
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        Assert.Equal("empty_graph", decoded.Graph.Name);
        Assert.Equal("An empty test graph", decoded.Graph.DocString);
        Assert.Empty(decoded.Graph.Node);
    }

    #endregion

    #region 图输入输的

    [Fact]
    public void EncodeDecode_GraphWithInputOutput_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "io_graph",
                Input =
                [
                    new OnnxValueInfo
                    {
                        Name = "input",
                        DataType = OnnxDataType.Float,
                        Shape = [1, 3, 224, 224]
                    }
                ],
                Output =
                [
                    new OnnxValueInfo
                    {
                        Name = "output",
                        DataType = OnnxDataType.Float,
                        Shape = [1, 1000]
                    }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        Assert.Single(decoded.Graph.Input);
        Assert.Equal("input", decoded.Graph.Input[0].Name);
        Assert.Equal(OnnxDataType.Float, decoded.Graph.Input[0].DataType);
        Assert.Equal([1L, 3L, 224L, 224L], decoded.Graph.Input[0].Shape);

        Assert.Single(decoded.Graph.Output);
        Assert.Equal("output", decoded.Graph.Output[0].Name);
        Assert.Equal(OnnxDataType.Float, decoded.Graph.Output[0].DataType);
        Assert.Equal([1L, 1000L], decoded.Graph.Output[0].Shape);
    }

    #endregion

    #region 节点（无属性）

    [Fact]
    public void EncodeDecode_SimpleNode_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "node_graph",
                Node =
                [
                    new OnnxNode
                    {
                        Name = "conv1",
                        OpType = "Conv",
                        Domain = "",
                        Input = ["input", "weight"],
                        Output = ["output"]
                    }
                ],
                Input =
                [
                    new OnnxValueInfo { Name = "input", DataType = OnnxDataType.Float, Shape = [1, 3, 224, 224] },
                    new OnnxValueInfo { Name = "weight", DataType = OnnxDataType.Float, Shape = [64, 3, 7, 7] }
                ],
                Output =
                [
                    new OnnxValueInfo { Name = "output", DataType = OnnxDataType.Float, Shape = [1, 64, 112, 112] }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        Assert.Single(decoded.Graph.Node);
        Assert.Equal("conv1", decoded.Graph.Node[0].Name);
        Assert.Equal("Conv", decoded.Graph.Node[0].OpType);
        Assert.Equal(["input", "weight"], decoded.Graph.Node[0].Input);
        Assert.Equal(["output"], decoded.Graph.Node[0].Output);
    }

    [Fact]
    public void EncodeDecode_MultipleNodes_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "multi_node_graph",
                Node =
                [
                    new OnnxNode
                    {
                        Name = "relu1",
                        OpType = "Relu",
                        Input = ["input"],
                        Output = ["relu_out"]
                    },
                    new OnnxNode
                    {
                        Name = "gemm1",
                        OpType = "Gemm",
                        Input = ["relu_out", "weight", "bias"],
                        Output = ["output"]
                    }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        Assert.Equal(2, decoded.Graph.Node.Count);
        Assert.Equal("relu1", decoded.Graph.Node[0].Name);
        Assert.Equal("Relu", decoded.Graph.Node[0].OpType);
        Assert.Equal("gemm1", decoded.Graph.Node[1].Name);
        Assert.Equal("Gemm", decoded.Graph.Node[1].OpType);
    }

    #endregion

    #region 张量初始化器

    [Fact]
    public void EncodeDecode_TensorInitializer_Roundtrip()
    {
        var rawData = new byte[] { 0, 0, 128, 63 };
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "tensor_graph",
                Initialization =
                [
                    new OnnxTensor
                    {
                        Name = "weight",
                        DataType = OnnxDataType.Float,
                        Dims = [2, 3],
                        RawData = rawData
                    }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        Assert.Single(decoded.Graph.Initialization);
        var tensor = decoded.Graph.Initialization[0];
        Assert.Equal("weight", tensor.Name);
        Assert.Equal(OnnxDataType.Float, tensor.DataType);
        Assert.Equal([2L, 3L], tensor.Dims);
        Assert.NotNull(tensor.RawData);
        Assert.Equal(rawData, tensor.RawData);
    }

    [Fact]
    public void EncodeDecode_TensorWithFloatData_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "float_tensor_graph",
                Initialization =
                [
                    new OnnxTensor
                    {
                        Name = "bias",
                        DataType = OnnxDataType.Float,
                        Dims = [4],
                        FloatData = [1.0f, 2.0f, 3.0f, 4.0f]
                    }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        Assert.Single(decoded.Graph.Initialization);
        var tensor = decoded.Graph.Initialization[0];
        Assert.Equal("bias", tensor.Name);
        Assert.Equal(OnnxDataType.Float, tensor.DataType);
        Assert.Equal([4L], tensor.Dims);
        Assert.Equal(4, tensor.FloatData.Count);
        Assert.Equal(1.0f, tensor.FloatData[0]);
        Assert.Equal(4.0f, tensor.FloatData[3]);
    }

    [Fact]
    public void EncodeDecode_TensorWithInt64Data_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "int64_graph",
                Initialization =
                [
                    new OnnxTensor
                    {
                        Name = "shape",
                        DataType = OnnxDataType.Int64,
                        Dims = [2],
                        Int64Data = [100L, 200L]
                    }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        Assert.Single(decoded.Graph.Initialization);
        var tensor = decoded.Graph.Initialization[0];
        Assert.Equal("shape", tensor.Name);
        Assert.Equal(OnnxDataType.Int64, tensor.DataType);
        Assert.Equal(2, tensor.Int64Data.Count);
        Assert.Equal(100L, tensor.Int64Data[0]);
        Assert.Equal(200L, tensor.Int64Data[1]);
    }

    [Fact]
    public void EncodeDecode_TensorWithDoubleData_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "double_graph",
                Initialization =
                [
                    new OnnxTensor
                    {
                        Name = "values",
                        DataType = OnnxDataType.Double,
                        Dims = [1],
                        DoubleData = [3.14159]
                    }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        var tensor = decoded.Graph.Initialization[0];
        Assert.Equal(OnnxDataType.Double, tensor.DataType);
        Assert.Single(tensor.DoubleData);
        Assert.Equal(3.14159, tensor.DoubleData[0], 5);
    }

    [Fact]
    public void EncodeDecode_TensorWithInt32Data_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            Graph = new OnnxGraph
            {
                Name = "int32_graph",
                Initialization =
                [
                    new OnnxTensor
                    {
                        Name = "indices",
                        DataType = OnnxDataType.Int32,
                        Dims = [3],
                        Int32Data = [10, 20, 30]
                    }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded.Graph);
        var tensor = decoded.Graph.Initialization[0];
        Assert.Equal(OnnxDataType.Int32, tensor.DataType);
        Assert.Equal(3, tensor.Int32Data.Count);
        Assert.Equal(10, tensor.Int32Data[0]);
        Assert.Equal(30, tensor.Int32Data[2]);
    }

    #endregion

    #region 完整模型

    [Fact]
    public void Scanner_ScanStatistics_CountsGraphElements()
    {
        var data = new OnnxModelData
        {
            IrVersion = 8,
            ProducerName = "TestScanner",
            OpsetImport =
            [
                new OnnxOperatorSetId { Domain = "ai.onnx", Version = 18 }
            ],
            Graph = new OnnxGraph
            {
                Name = "scanner_graph",
                Node =
                [
                    new OnnxNode { Name = "n1", OpType = "Relu", Input = ["in1"], Output = ["out1"] },
                    new OnnxNode { Name = "n2", OpType = "Gemm", Input = ["out1"], Output = ["out2"] }
                ],
                Input =
                [
                    new OnnxValueInfo { Name = "in1", DataType = OnnxDataType.Float, Shape = [1, 10] }
                ],
                Output =
                [
                    new OnnxValueInfo { Name = "out2", DataType = OnnxDataType.Float, Shape = [1, 10] }
                ],
                Initialization =
                [
                    new OnnxTensor
                        { Name = "w", DataType = OnnxDataType.Float, Dims = [10, 10], RawData = new byte[400] },
                    new OnnxTensor { Name = "b", DataType = OnnxDataType.Float, Dims = [10], RawData = new byte[40] }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var scanner = new Nyar.Binary.Onnx.Scanner.OnnxScanner(bytes);
        var stats = scanner.ScanStatistics();

        Assert.Equal(8L, stats.IrVersion);
        Assert.Equal("TestScanner", stats.ProducerName);
        Assert.Equal(1, stats.OpsetCount);
        Assert.Equal(2, stats.NodeCount);
        Assert.Equal(1, stats.InputCount);
        Assert.Equal(1, stats.OutputCount);
        Assert.Equal(2, stats.InitializerCount);
    }

    [Fact]
    public void EncodeDecode_FullModel_Roundtrip()
    {
        var data = new OnnxModelData
        {
            IrVersion = 9,
            ProducerName = "TestProducer",
            ProducerVersion = "2.0.0",
            Domain = "ai.onnx",
            ModelVersion = 1,
            DocString = "A full test model",
            OpsetImport =
            [
                new OnnxOperatorSetId { Domain = "ai.onnx", Version = 20 }
            ],
            Graph = new OnnxGraph
            {
                Name = "full_graph",
                DocString = "Full graph documentation",
                Node =
                [
                    new OnnxNode
                    {
                        Name = "add_node",
                        OpType = "Add",
                        Input = ["A", "B"],
                        Output = ["C"],
                        DocString = "Addition node"
                    }
                ],
                Input =
                [
                    new OnnxValueInfo { Name = "A", DataType = OnnxDataType.Float, Shape = [1, 10] },
                    new OnnxValueInfo { Name = "B", DataType = OnnxDataType.Float, Shape = [1, 10] }
                ],
                Output =
                [
                    new OnnxValueInfo { Name = "C", DataType = OnnxDataType.Float, Shape = [1, 10] }
                ],
                ValueInfo =
                [
                    new OnnxValueInfo { Name = "hidden", DataType = OnnxDataType.Float, Shape = [1, 10] }
                ],
                Initialization =
                [
                    new OnnxTensor { Name = "const", DataType = OnnxDataType.Float, Dims = [1], RawData = "\0\0\0\0"u8.ToArray() }
                ]
            }
        };

        var bytes = OnnxEncoder.Encode(data);
        var decoder = new OnnxDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(9L, decoded.IrVersion);
        Assert.Equal("TestProducer", decoded.ProducerName);
        Assert.Equal("2.0.0", decoded.ProducerVersion);
        Assert.Equal(1L, decoded.ModelVersion);
        Assert.Equal("A full test model", decoded.DocString);

        Assert.Single(decoded.OpsetImport);
        Assert.Equal("ai.onnx", decoded.OpsetImport[0].Domain);
        Assert.Equal(20L, decoded.OpsetImport[0].Version);

        Assert.NotNull(decoded.Graph);
        Assert.Equal("full_graph", decoded.Graph.Name);
        Assert.Single(decoded.Graph.Node);
        Assert.Equal("add_node", decoded.Graph.Node[0].Name);
        Assert.Equal("Add", decoded.Graph.Node[0].OpType);
        Assert.Equal(2, decoded.Graph.Input.Count);
        Assert.Single(decoded.Graph.Output);
        Assert.Single(decoded.Graph.ValueInfo);
        Assert.Single(decoded.Graph.Initialization);
    }

    #endregion
}
