namespace Nyar.Tests.Binary;

public sealed class SafeTensorsRoundTripTests
{
    #region 单张的

    [Fact]
    public void EncodeDecode_SingleTensor_Roundtrip()
    {
        var tensors = new List<SafeTensorData>
        {
            new()
            {
                Name = "weight",
                DType = SafeTensorDType.Float32,
                Shape = [2, 3],
                Data = [0, 0, 128, 63, 0, 0, 0, 64, 0, 0, 64, 64, 0, 0, 128, 64, 0, 0, 160, 64, 0, 0, 192, 64]
            }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.NotNull(decoded);
        Assert.NotNull(decoded.Tensors);
        Assert.True(decoded.Tensors.ContainsKey("weight"));

        var meta = decoded.Tensors["weight"];
        Assert.Equal(SafeTensorDType.Float32, meta.DType);
        Assert.Equal([2L, 3L], meta.Shape);
        Assert.Equal(24, meta.DataLength);

        var headerDecoder = new SafeTensorsDecoder(bytes);
        var header = headerDecoder.DecodeHeader();
        Assert.True(header.ContainsKey("weight"));
    }

    [Fact]
    public void EncodeDecode_SingleTensor_DataPreserved()
    {
        var originalData = new byte[] { 1, 2, 3, 4 };
        var tensors = new List<SafeTensorData>
        {
            new()
            {
                Name = "data",
                DType = SafeTensorDType.Int32,
                Shape = [1, 4],
                Data = originalData
            }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var result = decoder.DecodeTensor("data");

        Assert.NotNull(result);
        Assert.Equal("data", result.Name);
        Assert.Equal(SafeTensorDType.Int32, result.DType);
        Assert.Equal(originalData, result.Data);
    }

    #endregion

    #region 多张的

    [Fact]
    public void EncodeDecode_MultipleTensors_Roundtrip()
    {
        var tensors = new List<SafeTensorData>
        {
            new()
            {
                Name = "weight",
                DType = SafeTensorDType.Float32,
                Shape = [4, 4],
                Data = new byte[64]
            },
            new()
            {
                Name = "bias",
                DType = SafeTensorDType.Float32,
                Shape = [4],
                Data = new byte[16]
            },
            new()
            {
                Name = "scale",
                DType = SafeTensorDType.Float64,
                Shape = [1],
                Data = [0, 0, 0, 0, 0, 0, 240, 63]
            }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal(3, decoded.Tensors.Count);
        Assert.True(decoded.Tensors.ContainsKey("weight"));
        Assert.True(decoded.Tensors.ContainsKey("bias"));
        Assert.True(decoded.Tensors.ContainsKey("scale"));

        Assert.Equal([4L, 4L], decoded.Tensors["weight"].Shape);
        Assert.Equal(SafeTensorDType.Float32, decoded.Tensors["weight"].DType);
        Assert.Equal(64, decoded.Tensors["weight"].DataLength);

        Assert.Equal([4L], decoded.Tensors["bias"].Shape);
        Assert.Equal(SafeTensorDType.Float32, decoded.Tensors["bias"].DType);
        Assert.Equal(16, decoded.Tensors["bias"].DataLength);

        Assert.Equal([1L], decoded.Tensors["scale"].Shape);
        Assert.Equal(SafeTensorDType.Float64, decoded.Tensors["scale"].DType);
        Assert.Equal(8, decoded.Tensors["scale"].DataLength);
    }

    [Fact]
    public void EncodeDecode_MultipleTensors_DataOffsetsCorrect()
    {
        var tensors = new List<SafeTensorData>
        {
            new() { Name = "a", DType = SafeTensorDType.Float32, Shape = [2], Data = new byte[8] },
            new() { Name = "b", DType = SafeTensorDType.Float32, Shape = [3], Data = new byte[12] },
            new() { Name = "c", DType = SafeTensorDType.Float32, Shape = [4], Data = new byte[16] }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal(0, decoded.Tensors["a"].DataOffset);
        Assert.Equal(8, decoded.Tensors["a"].DataLength);
        Assert.Equal(8, decoded.Tensors["b"].DataOffset);
        Assert.Equal(12, decoded.Tensors["b"].DataLength);
        Assert.Equal(20, decoded.Tensors["c"].DataOffset);
        Assert.Equal(16, decoded.Tensors["c"].DataLength);
    }

    #endregion

    #region 数据类型

    [Fact]
    public void EncodeDecode_AllDTypeStrings_Roundtrip()
    {
        var dtypes = new (SafeTensorDType DType, string Expected)[]
        {
            (SafeTensorDType.Bool, "BOOL"),
            (SafeTensorDType.UInt8, "U8"),
            (SafeTensorDType.Int8, "I8"),
            (SafeTensorDType.Int16, "I16"),
            (SafeTensorDType.Int32, "I32"),
            (SafeTensorDType.Int64, "I64"),
            (SafeTensorDType.Float16, "F16"),
            (SafeTensorDType.Float32, "F32"),
            (SafeTensorDType.Float64, "F64"),
            (SafeTensorDType.BFloat16, "BF16")
        };

        foreach (var (dtype, _) in dtypes)
        {
            var tensors = new List<SafeTensorData>
            {
                new() { Name = $"tensor_{dtype}", DType = dtype, Shape = [1], Data = [0] }
            };

            var encoder = new SafeTensorsEncoder();
            var bytes = encoder.EncodeTensors(tensors);

            var decoder = new SafeTensorsDecoder(bytes);
            var decoded = decoder.Decode();

            Assert.True(decoded.Tensors.ContainsKey($"tensor_{dtype}"));
            Assert.Equal(dtype, decoded.Tensors[$"tensor_{dtype}"].DType);
        }
    }

    #endregion

    #region 文件数据编码

    [Fact]
    public void EncodeDecode_FileData_Roundtrip()
    {
        var rawData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var meta = new Dictionary<string, SafeTensorMeta>
        {
            ["t1"] = new()
            {
                DType = SafeTensorDType.Int32,
                Shape = [1],
                DataOffset = 0,
                DataLength = 4
            }
        };

        var data = new SafeTensorsFileData
        {
            Tensors = meta,
            Data = rawData
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new SafeTensorsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.True(decoded.Tensors.ContainsKey("t1"));
        Assert.Equal(rawData, decoded.Data);
        Assert.Equal(SafeTensorDType.Int32, decoded.Tensors["t1"].DType);
        Assert.Equal([1L], decoded.Tensors["t1"].Shape);
    }

    #endregion

    #region 形状

    [Fact]
    public void EncodeDecode_ScalarShape_Roundtrip()
    {
        var tensors = new List<SafeTensorData>
        {
            new() { Name = "scalar", DType = SafeTensorDType.Float32, Shape = [], Data = new byte[4] }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.True(decoded.Tensors.ContainsKey("scalar"));
        Assert.Empty(decoded.Tensors["scalar"].Shape);
    }

    [Fact]
    public void EncodeDecode_HighDimShape_Roundtrip()
    {
        var tensors = new List<SafeTensorData>
        {
            new()
            {
                Name = "tensor4d",
                DType = SafeTensorDType.Float32,
                Shape = [1, 3, 224, 224],
                Data = new byte[1 * 3 * 224 * 224 * 4]
            }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal([1L, 3L, 224L, 224L], decoded.Tensors["tensor4d"].Shape);
    }

    [Fact]
    public void EncodeDecode_1DShape_Roundtrip()
    {
        var tensors = new List<SafeTensorData>
        {
            new() { Name = "vec", DType = SafeTensorDType.Float32, Shape = [768], Data = new byte[768 * 4] }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal([768L], decoded.Tensors["vec"].Shape);
    }

    #endregion

    #region 缺少张量

    [Fact]
    public void DecodeTensor_MissingName_ReturnsNull()
    {
        var tensors = new List<SafeTensorData>
        {
            new() { Name = "existing", DType = SafeTensorDType.Float32, Shape = [1], Data = new byte[4] }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var result = decoder.DecodeTensor("missing");

        Assert.Null(result);
    }

    #endregion

    #region 大数据量

    [Fact]
    public void EncodeDecode_LargeTensor_Roundtrip()
    {
        var dataSize = 1024 * 1024;
        var data = new byte[dataSize];
        new Random(42).NextBytes(data);

        var tensors = new List<SafeTensorData>
        {
            new()
            {
                Name = "large",
                DType = SafeTensorDType.UInt8,
                Shape = [dataSize],
                Data = data
            }
        };

        var encoder = new SafeTensorsEncoder();
        var bytes = encoder.EncodeTensors(tensors);

        var decoder = new SafeTensorsDecoder(bytes);
        var result = decoder.DecodeTensor("large");

        Assert.NotNull(result);
        Assert.Equal(dataSize, result.Data.Length);
        Assert.Equal(data, result.Data);
    }

    #endregion
}