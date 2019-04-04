using Acorn.Jvm.Data;
using Acorn.Jvm.Decode;
using Acorn.Jvm.Encode;

namespace Nyar.Tests.Binary.JvmTests;

public class JvmRoundTripTests
{
    /// <summary>
    /// ���ClassFile �������ԣ�ʹ�ý��������ֶε����JvmClassFileData �������룬��֤�����ֶ�һ��    
///</summary>
    [Fact]
    public void Encode_Decode_MinimalClassFile()
    {
        var original = new JvmClassFileData
        {
            magic = JvmConstants.magic,
            minor_version = 0,
            major_version = 61,
            constant_pool = [],
            access_flags = 0,
            this_class = 0,
            super_class = 0,
            interfaces = [],
            fields = [],
            methods = [],
            attributes = []
        };

        var encoder = new JvmEncoder();
        var bytes = encoder.encode(original);

        var decoder = new JvmDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal(original.magic, decoded.magic);
        Assert.Equal(original.minor_version, decoded.minor_version);
        Assert.Equal(original.major_version, decoded.major_version);
        Assert.Empty(decoded.constant_pool);
        Assert.Equal(original.access_flags, decoded.access_flags);
        Assert.Equal(original.this_class, decoded.this_class);
        Assert.Equal(original.super_class, decoded.super_class);
        Assert.Empty(decoded.interfaces);
        Assert.Empty(decoded.fields);
        Assert.Empty(decoded.methods);
        Assert.Empty(decoded.attributes);
    }

    /// <summary>
    /// ���� ClassFile �������ԣ��������ֳ�������Ŀ��Utf8��Class��MethodRef��FieldRef��NameAndType��    
    ///     String��Integer��Float��MethodType��MethodHandle��InterfaceMethodref��InvokeDynamic����    
    ///     �ֶΡ��������������룬��֤����������ȫ����    
///</summary>
    [Fact]
    public void Encode_Decode_FullClassFile()
    {
        var constantPool = new JvmConstant[]
        {
            new JvmConstantUtf8 { value = "java/lang/Object" },
            new JvmConstantUtf8 { value = "<init>" },
            new JvmConstantUtf8 { value = "()V" },
            new JvmConstantUtf8 { value = "Code" },
            new JvmConstantUtf8 { value = "hello world" },
            new JvmConstantUtf8 { value = "I" },
            new JvmConstantUtf8 { value = "testField" },
            new JvmConstantUtf8 { value = "testMethod" },
            new JvmConstantUtf8 { value = "(I)I" },
            new JvmConstantClass { name_index = 1 },
            new JvmConstantNameAndType { name_index = 2, descriptor_index = 3 },
            new JvmConstantMethodref { class_index = 10, name_and_type_index = 11 },
            new JvmConstantNameAndType { name_index = 7, descriptor_index = 6 },
            new JvmConstantFieldref { class_index = 10, name_and_type_index = 13 },
            new JvmConstantString { string_index = 5 },
            new JvmConstantInteger { value = 42 },
            new JvmConstantFloat { value = 3.14f },
            new JvmConstantMethodType { descriptor_index = 3 },
            new JvmConstantMethodHandle { reference_kind = 6, reference_index = 12 },
            new JvmConstantInterfaceMethodref { class_index = 10, name_and_type_index = 11 },
            new JvmConstantInvokeDynamic { bootstrap_method_attr_index = 0, name_and_type_index = 13 }
        };

        var fields = new JvmFieldInfo[]
        {
            new()
            {
                access_flags = 0,
                name_index = 7,
                descriptor_index = 6,
                attributes = []
            }
        };

        var methods = new JvmMethodInfo[]
        {
            new()
            {
                access_flags = 0,
                name_index = 2,
                descriptor_index = 3,
                attributes = []
            },
            new()
            {
                access_flags = 0,
                name_index = 8,
                descriptor_index = 9,
                attributes = []
            }
        };

        var original = new JvmClassFileData
        {
            magic = JvmConstants.magic,
            minor_version = 0,
            major_version = 61,
            constant_pool = constantPool,
            access_flags = 0x0021,
            this_class = 10,
            super_class = 10,
            interfaces = [],
            fields = fields,
            methods = methods,
            attributes = []
        };

        var encoder = new JvmEncoder();
        var bytes = encoder.encode(original);

        var decoder = new JvmDecoder();
        var decoded = decoder.decode(bytes);

        Assert.Equal(original.magic, decoded.magic);
        Assert.Equal(original.minor_version, decoded.minor_version);
        Assert.Equal(original.major_version, decoded.major_version);

        Assert.Equal(constantPool.Length, decoded.constant_pool.Count);

        var decodedUtf81 = Assert.IsType<JvmConstantUtf8>(decoded.constant_pool[0]);
        Assert.Equal("java/lang/Object", decodedUtf81.value);

        var decodedUtf85 = Assert.IsType<JvmConstantUtf8>(decoded.constant_pool[4]);
        Assert.Equal("hello world", decodedUtf85.value);

        var decodedClass = Assert.IsType<JvmConstantClass>(decoded.constant_pool[9]);
        Assert.Equal((ushort)1, decodedClass.name_index);

        var decodedNat = Assert.IsType<JvmConstantNameAndType>(decoded.constant_pool[10]);
        Assert.Equal((ushort)2, decodedNat.name_index);
        Assert.Equal((ushort)3, decodedNat.descriptor_index);

        var decodedMethodref = Assert.IsType<JvmConstantMethodref>(decoded.constant_pool[11]);
        Assert.Equal((ushort)10, decodedMethodref.class_index);
        Assert.Equal((ushort)11, decodedMethodref.name_and_type_index);

        var decodedFieldref = Assert.IsType<JvmConstantFieldref>(decoded.constant_pool[13]);
        Assert.Equal((ushort)10, decodedFieldref.class_index);
        Assert.Equal((ushort)13, decodedFieldref.name_and_type_index);

        var decodedString = Assert.IsType<JvmConstantString>(decoded.constant_pool[14]);
        Assert.Equal((ushort)5, decodedString.string_index);

        var decodedInteger = Assert.IsType<JvmConstantInteger>(decoded.constant_pool[15]);
        Assert.Equal(42, decodedInteger.value);

        var decodedFloat = Assert.IsType<JvmConstantFloat>(decoded.constant_pool[16]);
        Assert.Equal(3.14f, decodedFloat.value, 2);

        var decodedMethodType = Assert.IsType<JvmConstantMethodType>(decoded.constant_pool[17]);
        Assert.Equal((ushort)3, decodedMethodType.descriptor_index);

        var decodedMethodHandle = Assert.IsType<JvmConstantMethodHandle>(decoded.constant_pool[18]);
        Assert.Equal((byte)6, decodedMethodHandle.reference_kind);
        Assert.Equal((ushort)12, decodedMethodHandle.reference_index);

        var decodedImethodref = Assert.IsType<JvmConstantInterfaceMethodref>(decoded.constant_pool[19]);
        Assert.Equal((ushort)10, decodedImethodref.class_index);
        Assert.Equal((ushort)11, decodedImethodref.name_and_type_index);

        var decodedInvokeDynamic = Assert.IsType<JvmConstantInvokeDynamic>(decoded.constant_pool[20]);
        Assert.Equal((ushort)0, decodedInvokeDynamic.bootstrap_method_attr_index);
        Assert.Equal((ushort)13, decodedInvokeDynamic.name_and_type_index);

        Assert.Equal(original.access_flags, decoded.access_flags);
        Assert.Equal(original.this_class, decoded.this_class);
        Assert.Equal(original.super_class, decoded.super_class);

        Assert.Empty(decoded.interfaces);

        Assert.Equal(fields.Length, decoded.fields.Count);
        Assert.Equal(fields[0].access_flags, decoded.fields[0].AccessFlags);
        Assert.Equal(fields[0].name_index, decoded.fields[0].NameIndex);
        Assert.Equal(fields[0].descriptor_index, decoded.fields[0].DescriptorIndex);

        Assert.Equal(methods.Length, decoded.methods.Count);
        Assert.Equal(methods[0].access_flags, decoded.methods[0].access_flags);
        Assert.Equal(methods[0].name_index, decoded.methods[0].name_index);
        Assert.Equal(methods[0].descriptor_index, decoded.methods[0].descriptor_index);
        Assert.Equal(methods[1].access_flags, decoded.methods[1].access_flags);
        Assert.Equal(methods[1].name_index, decoded.methods[1].name_index);
        Assert.Equal(methods[1].descriptor_index, decoded.methods[1].descriptor_index);
    }

    /// <summary>
    ///     ��Чħ������ʧ�ܣ�ʹ�ò����� CAFEBABE ħ�����ֽ�������룬��֤�׳��쳣
    /// </summary>
    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[32];
        var decoder = new JvmDecoder();

        var ex = Assert.ThrowsAny<Exception>(() => decoder.decode(data));
        Assert.Contains("�Ƿ���ClassFile ħ��", ex.Message);
    }

    /// <summary>
    /// �ض����ݽ���ʧ�ܣ�ʹ�ù��̣����� 4 �ֽڣ����ֽ�������룬��֤�׳����    
///</summary>
    [Fact]
    public void Decode_TruncatedData_Throws()
    {
        var data = new byte[4];
        var decoder = new JvmDecoder();

        Assert.ThrowsAny<Exception>(() => decoder.decode(data));
    }
}
