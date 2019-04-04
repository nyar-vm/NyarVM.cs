namespace Nyar.Tests.Binary.ClrTests;

public class ClrRoundTripTests
{
    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new ClrModuleData
        {
            module_name = "TestModule",
            version = "v4.0.30319",
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

        Assert.Equal("TestModule", decoded.module_name);
        Assert.Equal("v4.0.30319", decoded.version);
        Assert.Empty(decoded.methods);
        Assert.Empty(decoded.types);
        Assert.Empty(decoded.fields);
    }

    [Fact]
    public void Encode_Decode_ModuleWithMethods()
    {
        var original = new ClrModuleData
        {
            module_name = "MethodTest",
            version = "v4.0.30319",
            methods =
            [
                new ClrMethodDef
                {
                    name = "Main",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 8,
                    instructions =
                    [
                        new ClrInstruction { offset = 0, opcode = ClrOpcode.nop },
                        new ClrInstruction { offset = 1, opcode = ClrOpcode.ldc_i4_1 },
                        new ClrInstruction { offset = 2, opcode = ClrOpcode.ldc_i4_2 },
                        new ClrInstruction { offset = 3, opcode = ClrOpcode.add },
                        new ClrInstruction { offset = 4, opcode = ClrOpcode.pop },
                        new ClrInstruction { offset = 5, opcode = ClrOpcode.ret }
                    ]
                },
                new ClrMethodDef
                {
                    name = "Add",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 2,
                    instructions =
                    [
                        new ClrInstruction { offset = 0, opcode = ClrOpcode.ldarg_0 },
                        new ClrInstruction { offset = 1, opcode = ClrOpcode.ldarg_1 },
                        new ClrInstruction { offset = 2, opcode = ClrOpcode.add },
                        new ClrInstruction { offset = 3, opcode = ClrOpcode.ret }
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

        Assert.Equal("MethodTest", decoded.module_name);
        Assert.Equal(2, decoded.methods.Count);
        Assert.Equal("Main", decoded.methods[0].name);
        Assert.Equal("Add", decoded.methods[1].name);
        Assert.True(decoded.methods[0].instructions.Count > 0);
        Assert.True(decoded.methods[1].instructions.Count > 0);
    }

    [Fact]
    public void Encode_Decode_ModuleWithTypes()
    {
        var original = new ClrModuleData
        {
            module_name = "TypeTest",
            version = "v4.0.30319",
            types =
            [
                new ClrTypeDef
                {
                    name = "Program",
                    @namespace = "TestApp",
                    flags = ClrTypeAttributes.@public | ClrTypeAttributes.before_field_init
                }
            ],
            methods = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal("TypeTest", decoded.module_name);
        Assert.Equal(1, decoded.types.Count);
        Assert.Equal("Program", decoded.types[0].name);
        Assert.Equal("TestApp", decoded.types[0].@namespace);
    }

    [Fact]
    public void Encode_Decode_ModuleWithFields()
    {
        var original = new ClrModuleData
        {
            module_name = "FieldTest",
            version = "v4.0.30319",
            types =
            [
                new ClrTypeDef
                {
                    name = "MyClass",
                    @namespace = "TestApp",
                    flags = ClrTypeAttributes.@public | ClrTypeAttributes.before_field_init,
                    fields =
                    [
                        new ClrFieldDef
                        {
                            name = "_value",
                            flags = ClrFieldAttributes.@private
                        },
                        new ClrFieldDef
                        {
                            name = "Count",
                            flags = ClrFieldAttributes.@public | ClrFieldAttributes.@static
                        }
                    ]
                }
            ],
            methods = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        var decoder = new ClrDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal("FieldTest", decoded.module_name);
        Assert.Equal(1, decoded.types.Count);
        Assert.Equal("MyClass", decoded.types[0].name);
        Assert.Equal(2, decoded.types[0].fields.Count);
        Assert.Equal("_value", decoded.types[0].fields[0].Name);
        Assert.Equal("Count", decoded.types[0].fields[1].Name);
    }

    [Fact]
    public void Encode_Decode_ClrDirectoryFlags()
    {
        var original = new ClrModuleData
        {
            module_name = "FlagsTest",
            version = "v4.0.30319",
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

        Assert.True(decoded.clr_directory.is_il_only);
        Assert.True(decoded.clr_directory.cb > 0);
        Assert.True(decoded.clr_directory.metadata_size > 0);
    }

    [Fact]
    public void Encode_Decode_MetadataHeader()
    {
        var original = new ClrModuleData
        {
            module_name = "MetadataTest",
            version = "v4.0.30319",
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

        Assert.Equal(ClrConstants.metadata_signature, decoded.metadata.header.signature);
        Assert.Equal(5, decoded.metadata.header.streams);
        Assert.True(decoded.metadata.stream_headers.Count >= 5);
    }

    [Fact]
    public void Encode_Decode_InstructionsWithOperands()
    {
        var original = new ClrModuleData
        {
            module_name = "OperandTest",
            version = "v4.0.30319",
            methods =
            [
                new ClrMethodDef
                {
                    name = "Test",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 8,
                    instructions =
                    [
                        new ClrInstruction
                            { offset = 0, opcode = ClrOpcode.ldc_i4, operand = new ClrInt32Operand { value = 42 } },
                        new ClrInstruction
                        {
                            offset = 5, opcode = ClrOpcode.ldc_i8,
                            operand = new ClrInt64Operand { value = 1234567890123L }
                        },
                        new ClrInstruction
                        {
                            offset = 14, opcode = ClrOpcode.ldc_r4, operand = new ClrFloat32Operand { value = 3.14f }
                        },
                        new ClrInstruction
                        {
                            offset = 19, opcode = ClrOpcode.ldc_r8,
                            operand = new ClrFloat64Operand { value = 2.718281828 }
                        },
                        new ClrInstruction
                        {
                            offset = 28, opcode = ClrOpcode.ldloc_s, operand = new ClrLocalIndexOperand { index = 5 }
                        },
                        new ClrInstruction
                        {
                            offset = 30, opcode = ClrOpcode.stloc_s, operand = new ClrLocalIndexOperand { index = 3 }
                        },
                        new ClrInstruction
                        {
                            offset = 32, opcode = ClrOpcode.ldarg_s, operand = new ClrArgumentIndexOperand { index = 1 }
                        },
                        new ClrInstruction
                        {
                            offset = 34, opcode = ClrOpcode.call, operand = new ClrTokenOperand { value = 0x0A000001 }
                        },
                        new ClrInstruction { offset = 39, opcode = ClrOpcode.ret }
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
        Assert.Equal("Test", decoded.methods[0].name);

        var instrs = decoded.methods[0].instructions;
        Assert.True(instrs.Count >= 5);

        var ldcI4 = instrs.FirstOrDefault(i => i.opcode == ClrOpcode.ldc_i4);
        Assert.NotNull(ldcI4);
        Assert.IsType<ClrInt32Operand>(ldcI4.operand);
        Assert.Equal(42, ((ClrInt32Operand)ldcI4.operand!).value);

        var ldcR4 = instrs.FirstOrDefault(i => i.opcode == ClrOpcode.ldc_r4);
        Assert.NotNull(ldcR4);
        Assert.IsType<ClrFloat32Operand>(ldcR4.operand);
        Assert.Equal(3.14f, ((ClrFloat32Operand)ldcR4.operand!).value, 0.001f);
    }

    [Fact]
    public void Encode_Decode_ExceptionHandlers()
    {
        var original = new ClrModuleData
        {
            module_name = "EhTest",
            version = "v4.0.30319",
            methods =
            [
                new ClrMethodDef
                {
                    name = "TryCatch",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static,
                    max_stack = 2,
                    local_var_sig_tok = 0,
                    instructions =
                    [
                        new ClrInstruction { offset = 0, opcode = ClrOpcode.nop },
                        new ClrInstruction { offset = 1, opcode = ClrOpcode.ldc_i4_1 },
                        new ClrInstruction { offset = 2, opcode = ClrOpcode.pop },
                        new ClrInstruction
                        {
                            offset = 3, opcode = ClrOpcode.leave_s, operand = new ClrBranchTarget8Operand { offset = 7 }
                        },
                        new ClrInstruction { offset = 5, opcode = ClrOpcode.pop },
                        new ClrInstruction
                        {
                            offset = 6, opcode = ClrOpcode.leave_s, operand = new ClrBranchTarget8Operand { offset = 7 }
                        },
                        new ClrInstruction { offset = 7, opcode = ClrOpcode.ret }
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
        Assert.Equal("TryCatch", decoded.methods[0].name);
        Assert.True(decoded.methods[0].instructions.Count > 0);
    }

    [Fact]
    public void EncodeInstructions_DecodeInstructions_RoundTrip()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { offset = 0, opcode = ClrOpcode.ldarg_0 },
            new() { offset = 1, opcode = ClrOpcode.ldarg_1 },
            new() { offset = 2, opcode = ClrOpcode.add },
            new() { offset = 3, opcode = ClrOpcode.ret }
        };

        var encoded = ClrEncoder.encode_instructions(instructions);

        Assert.True(encoded.Length > 0);
        Assert.Equal(4, encoded.Length);
    }

    [Fact]
    public void EncodeMethodBody_TinyFormat()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { offset = 0, opcode = ClrOpcode.ldc_i4_1 },
            new() { offset = 1, opcode = ClrOpcode.ret }
        };

        var body = ClrEncoder.encode_method_body(instructions);

        Assert.True(body.Length > 0);
        Assert.True((body[0] & ClrConstants.method_header_format_mask) == ClrConstants.method_header_tiny_flag);
    }

    [Fact]
    public void EncodeMethodBody_FatFormat()
    {
        var instructions = new List<ClrInstruction>
        {
            new() { offset = 0, opcode = ClrOpcode.ldc_i4, operand = new ClrInt32Operand { value = 42 } },
            new() { offset = 5, opcode = ClrOpcode.ret }
        };

        var body = ClrEncoder.encode_method_body(instructions, 1, 0x11B);

        Assert.True(body.Length > 0);
        Assert.True((body[0] & ClrConstants.method_header_format_mask) == ClrConstants.method_header_fat_flag);
    }
}

public class ClrScannerTests
{
    [Fact]
    public void IsClrAssembly_ValidClrAssembly_ReturnsTrue()
    {
        var module = new ClrModuleData
        {
            module_name = "ScanTest",
            version = "v4.0.30319",
            methods = [],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(module);

        var scanner = new ClrScanner(bytes);
        Assert.True(scanner.is_clr_assembly());
    }

    [Fact]
    public void IsClrAssembly_InvalidData_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var scanner = new ClrScanner(data);
        Assert.False(scanner.is_clr_assembly());
    }

    [Fact]
    public void ScanHeader_ReturnsCorrectInfo()
    {
        var module = new ClrModuleData
        {
            module_name = "HeaderScanTest",
            version = "v4.0.30319",
            methods = [],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(module);

        var scanner = new ClrScanner(bytes);
        var header = scanner.scan_header();

        Assert.True(header.has_clr_directory);
        Assert.True(header.clr_metadata_rva > 0);
        Assert.True(header.clr_metadata_size > 0);
        Assert.False(header.is_pe32_plus);
    }

    [Fact]
    public void ScanHeader_NonPeData_Throws()
    {
        var data = new byte[128];
        var scanner = new ClrScanner(data);
        var threw = false;

        try
        {
            scanner.scan_header();
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw);
    }
}
