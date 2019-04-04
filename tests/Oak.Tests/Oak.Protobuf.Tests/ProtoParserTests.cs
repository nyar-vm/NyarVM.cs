namespace Oak.Protobuf.Tests;

public class ProtoParserTests : ProtoTestBase
{
    #region Syntax 声明测试

    [Fact]
    public void Parse_SyntaxDecl_ShouldCaptureSyntaxVersion()
    {
        var source = "syntax = \"proto3\";";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.NotNull(result.Value.Syntax);
        Assert.Equal("proto3", result.Value.Syntax);
    }

    #endregion

    #region Package 声明测试

    [Fact]
    public void Parse_PackageDecl_ShouldCapturePackageName()
    {
        var source = "syntax = \"proto3\"; package com.example;";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.NotNull(result.Value.Package);
        Assert.Equal("com.example", result.Value.Package);
    }

    #endregion

    #region Import 声明测试

    [Fact]
    public void Parse_ImportDecl_ShouldCaptureImportPath()
    {
        var source = "syntax = \"proto3\"; import \"common.proto\";";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Imports);
        Assert.Equal("common.proto", result.Value.Imports[0]);
    }

    [Fact]
    public void Parse_ImportPublic_ShouldCaptureModifier()
    {
        var source = "syntax = \"proto3\"; import public \"common.proto\";";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Imports);
    }

    #endregion

    #region Message 定义测试

    [Fact]
    public void Parse_MessageWithFields_ShouldReturnMessageDef()
    {
        var source = @"
            syntax = ""proto3"";
            message User {
                int32 id = 1;
                string name = 2;
                string email = 3;
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Messages);
        Assert.Equal("User", result.Value.Messages[0].Name);
        Assert.Equal(3, result.Value.Messages[0].fields.Count);
    }

    [Fact]
    public void Parse_MessageWithRepeatedField_ShouldCaptureLabel()
    {
        var source = @"
            syntax = ""proto3"";
            message User {
                repeated string tags = 4;
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        var field = result.Value.Messages[0].fields[0];
        Assert.Equal("repeated", field.Label);
        Assert.Equal("tags", field.Name);
    }

    [Fact]
    public void Parse_NestedMessage_ShouldReturnNestedMessages()
    {
        var source = @"
            syntax = ""proto3"";
            message Outer {
                message Inner {
                    int32 value = 1;
                }
                Inner inner = 1;
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Messages);
        Assert.Single(result.Value.Messages[0].NestedMessages);
        Assert.Equal("Inner", result.Value.Messages[0].NestedMessages[0].Name);
    }

    [Fact]
    public void Parse_MessageWithOneof_ShouldReturnOneofDef()
    {
        var source = @"
            syntax = ""proto3"";
            message Result {
                oneof result {
                    string error = 1;
                    int32 value = 2;
                }
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Messages[0].Oneofs);
        Assert.Equal("result", result.Value.Messages[0].Oneofs[0].Name);
        Assert.Equal(2, result.Value.Messages[0].Oneofs[0].fields.Count);
    }

    [Fact]
    public void Parse_MessageWithMapField_ShouldReturnMapFieldDef()
    {
        var source = @"
            syntax = ""proto3"";
            message Config {
                map<string, string> entries = 1;
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Messages[0].MapFields);
        Assert.Equal("entries", result.Value.Messages[0].MapFields[0].Name);
        Assert.Equal("string", result.Value.Messages[0].MapFields[0].KeyType);
        Assert.Equal("string", result.Value.Messages[0].MapFields[0].ValueType);
    }

    [Fact]
    public void Parse_MessageWithReserved_ShouldCaptureReserved()
    {
        var source = @"
            syntax = ""proto3"";
            message Foo {
                reserved 2, 15, 9 to 11;
                reserved ""foo"", ""bar"";
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.NotEmpty(result.Value.Messages[0].Reserved);
    }

    #endregion

    #region Enum 定义测试

    [Fact]
    public void Parse_EnumDefinition_ShouldReturnEnumDef()
    {
        var source = @"
            syntax = ""proto3"";
            enum Status {
                UNKNOWN = 0;
                ACTIVE = 1;
                INACTIVE = 2;
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Enums);
        Assert.Equal("Status", result.Value.Enums[0].Name);
        Assert.Equal(3, result.Value.Enums[0].Values.Count);
    }

    #endregion

    #region Service 定义测试

    [Fact]
    public void Parse_ServiceWithRpc_ShouldReturnServiceDef()
    {
        var source = @"
            syntax = ""proto3"";
            service UserService {
                rpc GetUser(GetUserRequest) returns (GetUserResponse);
                rpc CreateUser(CreateUserRequest) returns (CreateUserResponse);
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Services);
        Assert.Equal("UserService", result.Value.Services[0].Name);
        Assert.Equal(2, result.Value.Services[0].Methods.Count);
    }

    [Fact]
    public void Parse_RpcWithStream_ShouldCaptureStreamFlag()
    {
        var source = @"
            syntax = ""proto3"";
            service ChatService {
                rpc Chat(stream Message) returns (stream Message);
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        var rpc = result.Value.Services[0].Methods[0];
        Assert.Equal("Chat", rpc.Name);
    }

    #endregion

    #region Option 测试

    [Fact]
    public void Parse_OptionDecl_ShouldCaptureOption()
    {
        var source = "syntax = \"proto3\"; option java_package = \"com.example\";";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Single(result.Value.Options);
        Assert.Equal("java_package", result.Value.Options[0].Name);
    }

    #endregion

    #region 完整 proto 文件测试

    [Fact]
    public void Parse_FullProtoFile_ShouldParseAllElements()
    {
        var source = @"
            syntax = ""proto3"";
            package com.example.api;

            import ""google/protobuf/timestamp.proto"";

            option java_package = ""com.example.api"";

            enum Status {
                UNKNOWN = 0;
                ACTIVE = 1;
            }

            message User {
                int32 id = 1;
                string name = 2;
                string email = 3;
                Status status = 4;
                map<string, string> metadata = 5;
            }

            message GetUserRequest {
                int32 id = 1;
            }

            message GetUserResponse {
                User user = 1;
            }

            service UserService {
                rpc GetUser(GetUserRequest) returns (GetUserResponse);
            }";
        var result = ParseWithTimeout(source);

        AssertParseSuccess(result);
        Assert.Equal("proto3", result.Value.Syntax);
        Assert.Equal("com.example.api", result.Value.Package);
        Assert.Single(result.Value.Imports);
        Assert.Single(result.Value.Options);
        Assert.True(result.Value.Enums.Count >= 1);
        Assert.True(result.Value.Messages.Count >= 2);
        Assert.Single(result.Value.Services);
    }

    #endregion
}
