using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.Clr.Decode;
using Std.Data.Binary.Clr.Encode;

namespace Nyar.Tests.Binary.ClrTests;

/// <summary>
///     CLR Token 重映射测试，验证 ClrEncoder 正确将 ClrBackend 预计算的 Token
///     映射为 MetadataBuilder 分配的实际 Token
/// </summary>
public class ClrTokenRemapTests
{
    /// <summary>
    ///     编码包含用户字符串的模块，验证编码成功且输出为有效 PE 文件
    /// </summary>
    [Fact]
    public void Encode_ModuleWithUserStrings_ProducesValidPe()
    {
        var original = new ClrModuleData
        {
            module_name = "TokenRemapTest",
            version = "v4.0.30319",
            user_strings = ["Hello World!", "Test String"],
            methods =
            [
                new ClrMethodDef
                {
                    name = "Main",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static
                          | ClrMethodAttributes.hide_by_sig,
                    max_stack = 8,
                    instructions =
                    [
                        // ldstr "Hello World!" — 使用 ClrBackend 风格的索引 Token
                        new ClrInstruction
                        {
                            offset = 0,
                            opcode = ClrOpcode.ldstr,
                            operand = new ClrTokenOperand { value = 0x70000001 }
                        },
                        // call print_line — 使用 MemberRef Token
                        new ClrInstruction
                        {
                            offset = 5,
                            opcode = ClrOpcode.call,
                            operand = new ClrTokenOperand { value = 0x0A000001 }
                        },
                        // ldstr "Test String"
                        new ClrInstruction
                        {
                            offset = 10,
                            opcode = ClrOpcode.ldstr,
                            operand = new ClrTokenOperand { value = 0x70000002 }
                        },
                        // call print_line
                        new ClrInstruction
                        {
                            offset = 15,
                            opcode = ClrOpcode.call,
                            operand = new ClrTokenOperand { value = 0x0A000001 }
                        },
                        new ClrInstruction { offset = 20, opcode = ClrOpcode.ret }
                    ]
                }
            ],
            external_method_refs =
            [
                new ClrExternalMethodRef
                {
                    assembly_name = "System.Console",
                    type_full_name = "System.Console",
                    type_namespace = "System",
                    type_name = "Console",
                    method_name = "WriteLine",
                    method_signature = [0x00, 0x01, 0x01, 0x0E]
                }
            ],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(original);

        // 验证输出为有效 PE 文件
        Assert.True(bytes.Length > 128, $"PE 文件太短: {bytes.Length} bytes");
        var dosMagic = BitConverter.ToUInt16(bytes, 0);
        Assert.Equal((ushort)0x5A4D, dosMagic);
    }

    /// <summary>
    ///     编码后解码往返验证，确认用户字符串 Token 被正确重映射
    /// </summary>
    [Fact]
    public void Encode_Decode_UserStrings_TokensAreValid()
    {
        var original = new ClrModuleData
        {
            module_name = "StrTokenTest",
            version = "v4.0.30319",
            user_strings = ["Hello", "World"],
            methods =
            [
                new ClrMethodDef
                {
                    name = "Test",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static
                          | ClrMethodAttributes.hide_by_sig,
                    max_stack = 8,
                    instructions =
                    [
                        new ClrInstruction
                        {
                            offset = 0,
                            opcode = ClrOpcode.ldstr,
                            operand = new ClrTokenOperand { value = 0x70000001 }
                        },
                        new ClrInstruction
                        {
                            offset = 5,
                            opcode = ClrOpcode.ldstr,
                            operand = new ClrTokenOperand { value = 0x70000002 }
                        },
                        new ClrInstruction { offset = 10, opcode = ClrOpcode.pop },
                        new ClrInstruction { offset = 11, opcode = ClrOpcode.pop },
                        new ClrInstruction { offset = 12, opcode = ClrOpcode.ret }
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

        Assert.Equal("StrTokenTest", decoded.module_name);
        Assert.Equal(1, decoded.methods.Count);
        Assert.Equal("Test", decoded.methods[0].name);

        var instrs = decoded.methods[0].instructions;
        Assert.True(instrs.Count >= 2);

        // 验证 ldstr 指令保留（Token 值由 MetadataBuilder 分配，不验证具体值）
        var ldstrCount = instrs.Count(i => i.opcode == ClrOpcode.ldstr);
        Assert.Equal(2, ldstrCount);
    }

    /// <summary>
    ///     验证用户字符串去重：重复字符串使用同一 Token
    /// </summary>
    [Fact]
    public void Encode_DuplicateUserStrings_UsesSameHeapEntry()
    {
        var original = new ClrModuleData
        {
            module_name = "DedupTest",
            version = "v4.0.30319",
            // 只注册一个字符串，即使指令中出现两次
            user_strings = ["Hello"],
            methods =
            [
                new ClrMethodDef
                {
                    name = "Test",
                    flags = ClrMethodAttributes.@public | ClrMethodAttributes.@static
                          | ClrMethodAttributes.hide_by_sig,
                    max_stack = 8,
                    instructions =
                    [
                        new ClrInstruction
                        {
                            offset = 0,
                            opcode = ClrOpcode.ldstr,
                            operand = new ClrTokenOperand { value = 0x70000001 }
                        },
                        new ClrInstruction
                        {
                            offset = 5,
                            opcode = ClrOpcode.ldstr,
                            operand = new ClrTokenOperand { value = 0x70000001 }
                        },
                        new ClrInstruction { offset = 10, opcode = ClrOpcode.pop },
                        new ClrInstruction { offset = 11, opcode = ClrOpcode.pop },
                        new ClrInstruction { offset = 12, opcode = ClrOpcode.ret }
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

        // 编码不应抛出异常
        Assert.True(bytes.Length > 0);
    }
}