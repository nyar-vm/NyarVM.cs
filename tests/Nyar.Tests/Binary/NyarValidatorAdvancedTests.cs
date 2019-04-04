namespace Nyar.Tests.Binary;

public sealed class NyarValidatorAdvancedTests
{
    // -region-

    [Fact]
    public void Validate_MultipleFunctions_AllValid_ReturnsTrue()
    {
        var bytecode = new byte[20];
        bytecode[0] = (byte)NyarHeadCode.Return;
        bytecode[5] = (byte)NyarHeadCode.Nop;
        bytecode[6] = (byte)NyarHeadCode.Return;

        var module = new NyarModuleData
        {
            Name = "multi_func",
            Functions =
            [
                new NyarFunction(name: "func1", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1),
                new NyarFunction(name: "func2", arity: 1, localCount: 2, codeOffset: 5, codeLength: 2)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.True(result);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_MultipleFunctions_OneInvalid_ReturnsFalse()
    {
        var bytecode = new byte[20];
        bytecode[0] = (byte)NyarHeadCode.Return;
        bytecode[5] = (byte)NyarHeadCode.Return;

        var module = new NyarModuleData
        {
            Name = "multi_func",
            Functions =
            [
                new NyarFunction(name: "func1", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1),
                new NyarFunction(name: "func2", arity: -1, localCount: 0, codeOffset: 5, codeLength: 1)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.True(diagnostics.Count >= 1);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_OverlappingFunctions_ReturnsFalse()
    {
        var bytecode = new byte[20];
        bytecode[0] = (byte)NyarHeadCode.Return;
        bytecode[5] = (byte)NyarHeadCode.Return;

        var module = new NyarModuleData
        {
            Name = "overlap",
            Functions =
            [
                new NyarFunction(name: "func1", arity: 0, localCount: 0, codeOffset: 0, codeLength: 10),
                new NyarFunction(name: "func2", arity: 0, localCount: 0, codeOffset: 5, codeLength: 5)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_ValidConstants_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "constants",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants =
            [
                new NyarConstant(NyarConstantKind.Integer32, 42),
                new NyarConstant(NyarConstantKind.Float64, 3.14),
                new NyarConstant(NyarConstantKind.Boolean, true),
                new NyarConstant(NyarConstantKind.Null, null),
                new NyarConstant(NyarConstantKind.String, "hello")
            ],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out var diagnostics);

        Assert.True(result);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_ValidExports_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "exports",
            Functions =
            [
                new NyarFunction(name: "add", arity: 2, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [],
            Exports =
            [
                new NyarExport(NyarExportKind.Function, "add", 0)
            ]
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out var diagnostics);

        Assert.True(result);
    }

    [Fact]
    public void Validate_ExportFunctionIndexOutOfRange_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "bad_exports",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [],
            Exports =
            [
                new NyarExport(NyarExportKind.Function, "missing", 99)
            ]
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out var diagnostics);

        Assert.False(result);
    }

    [Fact]
    public void Validate_ExportEmptySymbolName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "bad_exports",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [],
            Exports =
            [
                new NyarExport(NyarExportKind.Function, "", 0)
            ]
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out var diagnostics);

        Assert.False(result);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_EmptyModule_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "empty",
            Functions = [],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [], out var diagnostics);

        Assert.True(result);
    }

    [Fact]
    public void Validate_EmptyModuleName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out var diagnostics);

        Assert.False(result);
    }

    [Fact]
    public void Validate_EmptyFunctionName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "test",
            Functions =
            [
                new NyarFunction(name: "", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out var diagnostics);

        Assert.False(result);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_ZeroCodeLength_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "zero_code",
            Functions =
            [
                new NyarFunction(name: "empty_func", arity: 0, localCount: 0, codeOffset: 0, codeLength: 0)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [], out var diagnostics);

        Assert.False(result);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_JumpToExactBoundary_ReturnsTrue()
    {
        var bytecode = new byte[5];
        bytecode[0] = (byte)NyarHeadCode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 5);

        var module = new NyarModuleData
        {
            Name = "jump_boundary",
            Functions =
            [
                new NyarFunction(name: "func", arity: 0, localCount: 0, codeOffset: 0, codeLength: 5)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_JumpToOnePastBoundary_ReturnsFalse()
    {
        var bytecode = new byte[5];
        bytecode[0] = (byte)NyarHeadCode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 6);

        var module = new NyarModuleData
        {
            Name = "jump_boundary",
            Functions =
            [
                new NyarFunction(name: "func", arity: 0, localCount: 0, codeOffset: 0, codeLength: 5)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, bytecode, out _);

        Assert.False(result);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void NyarHeadCode_AllValuesAreUnique()
    {
        var values = Enum.GetValues<NyarHeadCode>();
        var uniqueValues = new HashSet<int>();

        foreach (var value in values)
        {
            Assert.True(uniqueValues.Add((int)value), $"�ظ���NyarHeadCode �� {value}");
        }
    }

    [Fact]
    public void NyarHeadCode_NopIs0()
    {
        Assert.Equal(0, (int)NyarHeadCode.Nop);
    }

    [Fact]
    public void NyarHeadCode_ReturnIs5()
    {
        Assert.Equal(5, (int)NyarHeadCode.Return);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_FunctionImport_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "import_test",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports =
            [
                new NyarImport(NyarImportKind.Function, "math", "sin")
            ],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_GlobalImport_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "import_test",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports =
            [
                new NyarImport(NyarImportKind.Global, "env", "PI")
            ],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_ModuleImport_ReturnsTrue()
    {
        var module = new NyarModuleData
        {
            Name = "import_test",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports =
            [
                new NyarImport(NyarImportKind.Module, "utils", "*")
            ],
            Exports = []
        };

        var validator = new NyarValidator();
        var result = validator.Validate(module, [(byte)NyarHeadCode.Return], out _);

        Assert.True(result);
    }

    // [/section renamed - encoding fix]
}
