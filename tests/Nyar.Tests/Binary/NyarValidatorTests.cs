using Nyar.Binary.Nyar.Data;
using Nyar.Binary.Nyar.Validate;

namespace Nyar.Tests.Binary;

public sealed class NyarValidatorTests
{
    // -region-

    [Fact]
    public void Validate_ValidModule_ReturnsTrue()
    {
        var module = create_valid_module();
        var bytecode = create_valid_bytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.True(result);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Validate_ValidModuleWithImports_ReturnsTrue()
    {
        var module = create_valid_module_with_imports();
        var bytecode = create_valid_bytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.True(result);
        Assert.Empty(diagnostics);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_CodeOffsetExceedsBytecodeLength_ReturnsFalse()
    {
        var module = create_module_with_function(1000, 10);
        var bytecode = new byte[100];
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("invalid code offset"));
    }

    [Fact]
    public void Validate_CodeRangeExceedsBytecodeLength_ReturnsFalse()
    {
        var module = create_module_with_function(50, 100);
        var bytecode = new byte[100];
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("exceeds bytecode length"));
    }

    [Fact]
    public void Validate_NegativeCodeOffset_ReturnsFalse()
    {
        var module = create_module_with_function(-1, 10);
        var bytecode = new byte[100];
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("invalid code offset"));
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Validate_NegativeArity_ReturnsFalse()
    {
        var module = create_module_with_function(-1);
        var bytecode = create_valid_bytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("negative arity"));
    }

    [Fact]
    public void Validate_NegativeLocalCount_ReturnsFalse()
    {
        var module = create_module_with_function(-1);
        var bytecode = create_valid_bytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("negative local count"));
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_EmptyModuleName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "test_module",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [new NyarImport(NyarImportKind.Global, "", "func")],
            Exports = []
        };

        var bytecode = create_valid_bytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("module or symbol name is empty"));
    }

    [Fact]
    public void Validate_EmptySymbolName_ReturnsFalse()
    {
        var module = new NyarModuleData
        {
            Name = "test_module",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [new NyarImport(NyarImportKind.Global, "mod", "")],
            Exports = []
        };

        var bytecode = create_valid_bytecode();
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("module or symbol name is empty"));
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_UndefinedOpcode_ReturnsFalse()
    {
        var bytecode = new byte[] { 0x14 };
        var module = create_module_with_function(0, 1);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("undefined opcode"));
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Validate_JumpTargetOutOfRange_ReturnsFalse()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 100);

        var module = create_module_with_function(0, 5);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("jump target") && d.Contains("out of range"));
    }

    [Fact]
    public void Validate_JumpTargetInSameFunction_ReturnsTrue()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.Jump;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 5);
        bytecode[5] = (byte)NyarOpcode.Nop;

        var module = create_module_with_function(0, 6);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out _);

        Assert.True(result);
    }

    [Fact]
    public void Validate_JumpIfTrueTargetOutOfRange_ReturnsFalse()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.JumpIfTrue;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), -1);

        var module = create_module_with_function(0, 5);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
    }

    [Fact]
    public void Validate_JumpIfFalseTargetOutOfRange_ReturnsFalse()
    {
        var bytecode = new byte[10];
        bytecode[0] = (byte)NyarOpcode.JumpIfFalse;
        BitConverter.TryWriteBytes(bytecode.AsSpan(1), 9999);

        var module = create_module_with_function(0, 5);
        var validator = new NyarValidator();

        var result = validator.Validate(module, bytecode, out var diagnostics);

        Assert.False(result);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Theory]
    [InlineData(NyarOpcode.Nop, 0x00)]
    [InlineData(NyarOpcode.Jump, 0x01)]
    [InlineData(NyarOpcode.Return, 0x05)]
    [InlineData(NyarOpcode.Const, 0x10)]
    [InlineData(NyarOpcode.LoadLocal, 0x20)]
    [InlineData(NyarOpcode.StoreLocal, 0x21)]
    [InlineData(NyarOpcode.I32Add, 0x30)]
    [InlineData(NyarOpcode.I32Eq, 0x40)]
    [InlineData(NyarOpcode.I64Add, 0x50)]
    [InlineData(NyarOpcode.F32Add, 0x60)]
    [InlineData(NyarOpcode.F64Add, 0x70)]
    [InlineData(NyarOpcode.I32ExtendI64S, 0x80)]
    [InlineData(NyarOpcode.Alloc, 0x90)]
    [InlineData(NyarOpcode.NewObject, 0xA0)]
    [InlineData(NyarOpcode.utf8_concat, 0xB0)]
    [InlineData(NyarOpcode.BigIntAdd, 0xC0)]
    [InlineData(NyarOpcode.CallIntrinsic, 0xD0)]
    public void NyarOpcode_HasCorrectValue(NyarOpcode opcode, byte expectedValue)
    {
        Assert.Equal(expectedValue, (byte)opcode);
    }

    // [/section renamed - encoding fix]

    // -region-

    private static NyarModuleData create_valid_module()
    {
        return new NyarModuleData
        {
            Name = "test_module",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };
    }

    private static NyarModuleData create_valid_module_with_imports()
    {
        return new NyarModuleData
        {
            Name = "test_module",
            Functions =
            [
                new NyarFunction(name: "main", arity: 0, localCount: 0, codeOffset: 0, codeLength: 1)
            ],
            Constants = [],
            Imports =
            [
                new NyarImport(moduleName: "math", symbolName: "sin", kind: NyarImportKind.Function)
            ],
            Exports = []
        };
    }

    private static byte[] create_valid_bytecode()
    {
        return [(byte)NyarOpcode.Return];
    }

    private static NyarModuleData create_module_with_function(
        int codeOffset = 0, int codeLength = 1, int arity = 0, int localCount = 0)
    {
        return new NyarModuleData
        {
            Name = "test",
            Functions =
            [
                new NyarFunction(name: "func", arity: arity, localCount: localCount, codeOffset: codeOffset,
                    codeLength: codeLength)
            ],
            Constants = [],
            Imports = [],
            Exports = []
        };
    }

    // [/section renamed - encoding fix]
}
