namespace Nyar.Tests.Binary.ClrTests;

/// <summary>
/// CLR 编解码器综合测试，覆盖错误路径、边界条件与往返一致性的
///</summary>
public class ClrErrorPathTests
{
    /// <summary>
    /// 解码的PE 数据应抛出异常的    
///</summary>
    [Fact]
    public void Decode_NonPeData_Throws()
    {
        var data = new byte[128];
        var decoder = new ClrDecoder();

        Assert.Throws<InvalidDataException>(() => decoder.decode(data));
    }

    /// <summary>
    /// 解码截断的PE 数据（长度不�?4 字节）应抛出异常�?   
///</summary>
    [Fact]
    public void Decode_TruncatedData_Throws()
    {
        var data = new byte[4];
        var decoder = new ClrDecoder();

        Assert.ThrowsAny<Exception>(() => decoder.decode(data));
    }

    /// <summary>
    ///     解码不含 CLR 目录的有的PE 文件应抛出异常的    
    ///     期望 <see cref="InvalidDataException" /> 并提示非 .NET 程序集的    
    /// </summary>
    [Fact]
    public void Decode_NonClrPeFile_Throws()
    {
        var peFile = new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = PeConstants.DosMagic,
                PeHeaderOffset = 0x40,
                PeMagic = PeConstants.PeMagic,
                Machine = 0x014C,
                NumberOfSections = 1,
                SizeOfOptionalHeader = 0xE0,
                Characteristics = PeConstants.CharacteristicsExecutable
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = PeConstants.OptionalMagicPE32,
                NumberOfRvaAndSizes = 16,
                DataDirectories = []
            },
            Sections =
            [
                new PeSectionData
                {
                    NameBytes = default,
                    VirtualSize = 0x1000,
                    VirtualAddress = 0x2000,
                    SizeOfRawData = 0x200,
                    PointerToRawData = 0x200,
                    Characteristics = 0x60000020
                }
            ]
        };

        var peEncoder = new PeEncoder();
        var bytes = peEncoder.Encode(peFile);

        var decoder = new ClrDecoder();

        var ex = Assert.Throws<InvalidDataException>(() => decoder.decode(bytes));
        Assert.Contains("不是 .NET 程序", ex.Message);
    }
}

/// <summary>
/// CLR 编解码器增强往返测试，补充现有 ClrRoundTripTests 中未覆盖的场景的
///</summary>
public class ClrEnhancedRoundTripTests
{
    /// <summary>
    ///     类型内方法和模块级方法混合往返测试的    
    ///     验证 Encoder 对两种来源的方法正确合并写入 MethodDef 表，
    /// 的Decoder 通过 MethodListStart 正确分配到各类型�?   
///</summary>
    [Fact]
    public void Encode_Decode_MethodsInsideTypes()
    {
        var original = new ClrModuleData
        {
            module_name = "MixedTest",
            version = "v4.0.30319",
            methods =
            [
                new ClrMethodDef
                {
                    name = "GlobalFunc",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 2,
                    instructions =
                    [
                        new ClrInstruction { offset = 0, opcode = ClrOpcode.ldc_i4_0 },
                        new ClrInstruction { offset = 1, opcode = ClrOpcode.ret }
                    ]
                }
            ],
            types =
            [
                new ClrTypeDef
                {
                    name = "MyClass",
                    @namespace = "TestApp",
                    flags = ClrTypeAttributes.@public | ClrTypeAttributes.before_field_init,
                    fields =
                    [
                        new ClrFieldDef { name = "X", flags = ClrFieldAttributes.@private }
                    ],
                    methods =
                    [
                        new ClrMethodDef
                        {
                            name = "InstanceMethod",
                            flags = ClrMethodAttributes.@public,
                            max_stack = 1,
                            instructions =
                            [
                                new ClrInstruction { offset = 0, opcode = ClrOpcode.ldarg_0 },
                                new ClrInstruction { offset = 1, opcode = ClrOpcode.ret }
                            ]
                        },
                        new ClrMethodDef
                        {
                            name = "StaticMethod",
                            flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                            max_stack = 1,
                            instructions =
                            [
                                new ClrInstruction { offset = 0, opcode = ClrOpcode.ret }
                            ]
                        }
                    ]
                },
                new ClrTypeDef
                {
                    name = "Helper",
                    @namespace = "TestApp",
                    flags = ClrTypeAttributes.@public | ClrTypeAttributes.before_field_init,
                    methods =
                    [
                        new ClrMethodDef
                        {
                            name = "DoWork",
                            flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                            max_stack = 1,
                            instructions =
                            [
                                new ClrInstruction { offset = 0, opcode = ClrOpcode.ldc_i4_1 },
                                new ClrInstruction { offset = 1, opcode = ClrOpcode.ret }
                            ]
                        }
                    ]
                }
            ],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal("MixedTest", decoded.module_name);

        Assert.True(decoded.methods.Count >= 1);
        Assert.Contains(decoded.methods, m => m.name == "GlobalFunc");

        Assert.Equal(2, decoded.types.Count);
        Assert.Equal("MyClass", decoded.types[0].name);
        Assert.Equal("Helper", decoded.types[1].name);

        Assert.Equal(2, decoded.types[0].methods.Count);
        Assert.Equal("InstanceMethod", decoded.types[0].methods[0].name);
        Assert.Equal("StaticMethod", decoded.types[0].methods[1].name);

        Assert.Equal(1, decoded.types[1].methods.Count);
        Assert.Equal("DoWork", decoded.types[1].methods[0].name);

        Assert.Equal(1, decoded.types[0].fields.Count);
        Assert.Equal("X", decoded.types[0].fields[0].Name);
    }

    /// <summary>
    /// 异常处理器往返测试的验证所的Handler 属性正确还原的    
///</summary>
    [Fact]
    public void Encode_Decode_ExceptionHandlerProperties()
    {
        var original = new ClrModuleData
        {
            module_name = "EhPropTest",
            version = "v4.0.30319",
            methods =
            [
                new ClrMethodDef
                {
                    name = "MethodWithEh",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 4,
                    instructions =
                    [
                        new ClrInstruction { offset = 0, opcode = ClrOpcode.nop },
                        new ClrInstruction { offset = 1, opcode = ClrOpcode.ldc_i4_0 },
                        new ClrInstruction { offset = 2, opcode = ClrOpcode.stloc_0 },
                        new ClrInstruction
                        {
                            offset = 3, opcode = ClrOpcode.leave_s,
                            operand = new ClrBranchTarget8Operand { offset = 10 }
                        },
                        new ClrInstruction { offset = 5, opcode = ClrOpcode.pop },
                        new ClrInstruction
                        {
                            offset = 6, opcode = ClrOpcode.leave_s,
                            operand = new ClrBranchTarget8Operand { offset = 10 }
                        },
                        new ClrInstruction { offset = 8, opcode = ClrOpcode.endfilter },
                        new ClrInstruction
                        {
                            offset = 9, opcode = ClrOpcode.leave_s,
                            operand = new ClrBranchTarget8Operand { offset = 10 }
                        },
                        new ClrInstruction { offset = 10, opcode = ClrOpcode.ret }
                    ],
                    exception_handlers =
                    [
                        new ClrExceptionHandler
                        {
                            handler_kind = ClrExceptionHandlerKind.@catch,
                            try_start = 0,
                            try_length = 3,
                            handler_start = 5,
                            handler_length = 2,
                            class_token_or_filter_offset = 0x01000001
                        },
                        new ClrExceptionHandler
                        {
                            handler_kind = ClrExceptionHandlerKind.@finally,
                            try_start = 0,
                            try_length = 3,
                            handler_start = 8,
                            handler_length = 2,
                            class_token_or_filter_offset = 0
                        }
                    ]
                }
            ],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal(1, decoded.methods.Count);

        var method = decoded.methods[0];
        Assert.Equal("MethodWithEh", method.name);
        Assert.Equal(2, method.exception_handlers.Count);

        Assert.Equal(ClrExceptionHandlerKind.@catch, method.exception_handlers[0].handler_kind);
        Assert.Equal(0u, method.exception_handlers[0].try_start);
        Assert.Equal(3u, method.exception_handlers[0].try_length);
        Assert.Equal(5u, method.exception_handlers[0].handler_start);
        Assert.Equal(2u, method.exception_handlers[0].handler_length);
        Assert.Equal(0x01000001u, method.exception_handlers[0].class_token_or_filter_offset);

        Assert.Equal(ClrExceptionHandlerKind.@finally, method.exception_handlers[1].handler_kind);
        Assert.Equal(0u, method.exception_handlers[1].try_start);
        Assert.Equal(3u, method.exception_handlers[1].try_length);
        Assert.Equal(8u, method.exception_handlers[1].handler_start);
        Assert.Equal(2u, method.exception_handlers[1].handler_length);
        Assert.Equal(0u, method.exception_handlers[1].class_token_or_filter_offset);
    }

    /// <summary>
    /// GUID 堆往返测试的验证编码器正确保的GUID 堆数据的    
///</summary>
    [Fact]
    public void Encode_Decode_GuidHeapRoundTrip()
    {
        var mvid = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        var guidBytes = mvid.ToByteArray();

        var original = new ClrModuleData
        {
            module_name = "GuidTest",
            version = "v4.0.30319",
            metadata = new ClrMetadata
            {
                guid_heap = new ClrGuidHeap { data = guidBytes }
            },
            methods = [],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal("GuidTest", decoded.module_name);
        Assert.True(decoded.metadata.guid_heap.data.Length >= 16);

        var decodedGuid = new Guid(decoded.metadata.guid_heap.data.AsSpan(0, 16));
        Assert.Equal(mvid, decodedGuid);
    }

    /// <summary>
    /// 方法体含局部变量签名令牌的往返测试的    
    ///     验证 Fat 头中的LocalVarSigTok 正确往返的    
///</summary>
    [Fact]
    public void Encode_Decode_MethodBodyWithLocals()
    {
        var original = new ClrModuleData
        {
            module_name = "LocalsTest",
            version = "v4.0.30319",
            methods =
            [
                new ClrMethodDef
                {
                    name = "MethodWithLocals",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 4,
                    local_var_sig_tok = 0x11000001,
                    instructions =
                    [
                        new ClrInstruction { offset = 0, opcode = ClrOpcode.ldc_i4_0 },
                        new ClrInstruction { offset = 1, opcode = ClrOpcode.stloc_0 },
                        new ClrInstruction
                            { offset = 2, opcode = ClrOpcode.ldc_i4, operand = new ClrInt32Operand { value = 42 } },
                        new ClrInstruction { offset = 7, opcode = ClrOpcode.stloc_1 },
                        new ClrInstruction { offset = 8, opcode = ClrOpcode.ret }
                    ]
                }
            ],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal(1, decoded.methods.Count);
        Assert.Equal("MethodWithLocals", decoded.methods[0].name);
        Assert.Equal(4, (int)decoded.methods[0].max_stack);
        Assert.Equal(0x11000001u, decoded.methods[0].local_var_sig_tok);
        Assert.True(decoded.methods[0].instructions.Count >= 3);
    }

    /// <summary>
    /// 分支指令往返测试的覆盖 Br_S、Br、Leave、Leave_S 等的    
///</summary>
    [Fact]
    public void Encode_Decode_BranchInstructions()
    {
        var original = new ClrModuleData
        {
            module_name = "BranchTest",
            version = "v4.0.30319",
            methods =
            [
                new ClrMethodDef
                {
                    name = "TestBranches",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 4,
                    instructions =
                    [
                        new ClrInstruction { offset = 0, opcode = ClrOpcode.nop },
                        new ClrInstruction
                        {
                            offset = 1, opcode = ClrOpcode.brfalse_s,
                            operand = new ClrBranchTarget8Operand { offset = 8 }
                        },
                        new ClrInstruction { offset = 3, opcode = ClrOpcode.ldc_i4_1 },
                        new ClrInstruction
                        {
                            offset = 4, opcode = ClrOpcode.br_s, operand = new ClrBranchTarget8Operand { offset = 13 }
                        },
                        new ClrInstruction { offset = 6, opcode = ClrOpcode.ldc_i4_0 },
                        new ClrInstruction
                        {
                            offset = 7, opcode = ClrOpcode.leave, operand = new ClrBranchTarget32Operand { offset = 13 }
                        },
                        new ClrInstruction { offset = 12, opcode = ClrOpcode.pop },
                        new ClrInstruction { offset = 13, opcode = ClrOpcode.ret }
                    ]
                }
            ],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal(1, decoded.methods.Count);

        var instrs = decoded.methods[0].instructions;
        Assert.True(instrs.Count >= 5);
    }

    /// <summary>
    /// Fat 方法头标志位验证 的MaxStack > 8 时使的Fat 格式�?   
///</summary>
    [Fact]
    public void EncodeMethodBody_MaxStackExceeds8_UsesFatFormat()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { offset = 0, opcode = ClrOpcode.ldc_i4_0 },
            new() { offset = 1, opcode = ClrOpcode.ret }
        };

        var body = ClrEncoder.encode_method_body(instructions, 16);

        Assert.True((body[0] & ClrConstants.method_header_format_mask) == ClrConstants.method_header_fat_flag);
    }

    /// <summary>
    /// Tiny 方法头编的的验证 Tiny 头正确编码代码大小的    
///</summary>
    [Fact]
    public void EncodeMethodBody_TinyFormat_HasCorrectCodeSize()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { offset = 0, opcode = ClrOpcode.ldarg_0 },
            new() { offset = 1, opcode = ClrOpcode.ldarg_1 },
            new() { offset = 2, opcode = ClrOpcode.add },
            new() { offset = 3, opcode = ClrOpcode.ret }
        };

        var body = ClrEncoder.encode_method_body(instructions);

        Assert.Equal(ClrConstants.method_header_tiny_flag, body[0] & ClrConstants.method_header_format_mask);

        var codeSize = body[0] >> 2;
        Assert.True(codeSize > 0);
    }
}

/// <summary>
/// CLR 扫描器补充测试，覆盖的CLR PE 文件扫描场景�?///</summary>
public class ClrScannerSupplementTests
{
    /// <summary>
    /// 扫描不含 CLR 目录的有的PE 文件应返的false�?   
///</summary>
    [Fact]
    public void IsClrAssembly_NonClrPeFile_ReturnsFalse()
    {
        var peFile = new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = PeConstants.DosMagic,
                PeHeaderOffset = 0x40,
                PeMagic = PeConstants.PeMagic,
                Machine = 0x014C,
                NumberOfSections = 1,
                SizeOfOptionalHeader = 0xE0,
                Characteristics = PeConstants.CharacteristicsDll
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = PeConstants.OptionalMagicPE32,
                NumberOfRvaAndSizes = 16,
                DataDirectories = []
            },
            Sections =
            [
                new PeSectionData
                {
                    NameBytes = default,
                    VirtualSize = 0x1000,
                    VirtualAddress = 0x2000,
                    SizeOfRawData = 0x200,
                    PointerToRawData = 0x200,
                    Characteristics = 0x60000020
                }
            ]
        };

        var peEncoder = new PeEncoder();
        var bytes = peEncoder.Encode(peFile);

        var scanner = new ClrScanner(bytes);
        Assert.False(scanner.is_clr_assembly());
    }

    /// <summary>
    /// 扫描不含 CLR 目录的PE 文件的ScanHeader 应正常完成但不含 CLR 目录�?   
///</summary>
    [Fact]
    public void ScanHeader_NonClrPeFile_ReturnsWithoutClrDirectory()
    {
        var peFile = new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = PeConstants.DosMagic,
                PeHeaderOffset = 0x40,
                PeMagic = PeConstants.PeMagic,
                Machine = 0x014C,
                NumberOfSections = 1,
                SizeOfOptionalHeader = 0xE0,
                Characteristics = PeConstants.CharacteristicsDll
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = PeConstants.OptionalMagicPE32,
                NumberOfRvaAndSizes = 16,
                DataDirectories = []
            },
            Sections =
            [
                new PeSectionData
                {
                    NameBytes = default,
                    VirtualSize = 0x1000,
                    VirtualAddress = 0x2000,
                    SizeOfRawData = 0x200,
                    PointerToRawData = 0x200,
                    Characteristics = 0x60000020
                }
            ]
        };

        var peEncoder = new PeEncoder();
        var bytes = peEncoder.Encode(peFile);

        var scanner = new ClrScanner(bytes);
        var header = scanner.scan_header();

        Assert.False(header.has_clr_directory);
        Assert.Equal((ushort)0x014C, header.machine);
    }

    /// <summary>
    /// 空数据扫描应返回 false�?   
///</summary>
    [Fact]
    public void IsClrAssembly_EmptyData_ReturnsFalse()
    {
        var scanner = new ClrScanner([]);
        Assert.False(scanner.is_clr_assembly());
    }
}
