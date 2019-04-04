using Acorn.Jvm.Data;
using Acorn.Jvm.Decode;
using Acorn.Jvm.Encode;

namespace Nyar.Tests.Binary.JvmTests;

public class JvmAttributeRoundTripTests
{
    /// <summary>
    /// �������������أ����� Utf8 �ͼ��������ͣ���Long/Double ��������ƫ�Ƶ�    
///</summary>
    private static (JvmConstant[] Pool, Dictionary<string, ushort> Names) build_base_pool()
    {
        var pool = new List<JvmConstant>
        {
            new JvmConstantUtf8 { value = "java/lang/Object" }, // 1
            new JvmConstantUtf8 { value = "<init>" }, // 2
            new JvmConstantUtf8 { value = "()V" }, // 3
            new JvmConstantUtf8 { value = "Code" }, // 4
            new JvmConstantUtf8 { value = "LineNumberTable" }, // 5
            new JvmConstantUtf8 { value = "LocalVariableTable" }, // 6
            new JvmConstantUtf8 { value = "LocalVariableTypeTable" }, // 7
            new JvmConstantUtf8 { value = "ConstantValue" }, // 8
            new JvmConstantUtf8 { value = "SourceFile" }, // 9
            new JvmConstantUtf8 { value = "Exceptions" }, // 10
            new JvmConstantUtf8 { value = "InnerClasses" }, // 11
            new JvmConstantUtf8 { value = "BootstrapMethods" }, // 12
            new JvmConstantUtf8 { value = "Signature" }, // 13
            new JvmConstantUtf8 { value = "Synthetic" }, // 14
            new JvmConstantUtf8 { value = "Deprecated" }, // 15
            new JvmConstantUtf8 { value = "EnclosingMethod" }, // 16
            new JvmConstantUtf8 { value = "SourceDebugExtension" }, // 17
            new JvmConstantUtf8 { value = "MethodParameters" }, // 18
            new JvmConstantUtf8 { value = "StackMapTable" }, // 19
            new JvmConstantUtf8 { value = "NestHost" }, // 20
            new JvmConstantUtf8 { value = "NestMembers" }, // 21
            new JvmConstantUtf8 { value = "Record" }, // 22
            new JvmConstantUtf8 { value = "PermittedSubclasses" }, // 23
            new JvmConstantUtf8 { value = "RuntimeVisibleAnnotations" }, // 24
            new JvmConstantUtf8 { value = "RuntimeInvisibleAnnotations" }, // 25
            new JvmConstantUtf8 { value = "RuntimeVisibleParameterAnnotations" }, // 26
            new JvmConstantUtf8 { value = "RuntimeInvisibleParameterAnnotations" }, // 27
            new JvmConstantUtf8 { value = "AnnotationDefault" }, // 28
            new JvmConstantUtf8 { value = "Module" }, // 29
            new JvmConstantUtf8 { value = "I" }, // 30
            new JvmConstantUtf8 { value = "testField" }, // 31
            new JvmConstantUtf8 { value = "Ljava/lang/Object;" }, // 32
            new JvmConstantUtf8 { value = "Ljava/lang/String;" }, // 33
            new JvmConstantUtf8 { value = "Ljava/lang/Integer;" }, // 34
            new JvmConstantUtf8 { value = "Ljava/util/List;" }, // 35
            new JvmConstantUtf8 { value = "Lcom/example/Outer;" }, // 36
            new JvmConstantUtf8 { value = "Lcom/example/Inner;" }, // 37
            new JvmConstantUtf8 { value = "Lcom/example/Host;" }, // 38
            new JvmConstantUtf8 { value = "Lcom/example/Member1;" }, // 39
            new JvmConstantUtf8 { value = "Lcom/example/Member2;" }, // 40
            new JvmConstantUtf8 { value = "Lcom/example/Sealed;" }, // 41
            new JvmConstantUtf8 { value = "Lcom/example/Sub1;" }, // 42
            new JvmConstantUtf8 { value = "Lcom/example/Sub2;" }, // 43
            new JvmConstantUtf8 { value = "Lcom/example/Annotation;" }, // 44
            new JvmConstantUtf8 { value = "value" }, // 45
            new JvmConstantUtf8 { value = "name" }, // 46
            new JvmConstantUtf8 { value = "com/example/module" }, // 47
            new JvmConstantUtf8 { value = "1.0" }, // 48
            new JvmConstantUtf8 { value = "com/example/exported" }, // 49
            new JvmConstantUtf8 { value = "com/example/opened" }, // 50
            new JvmConstantUtf8 { value = "com/example/uses" }, // 51
            new JvmConstantUtf8 { value = "com/example/provides" }, // 52
            new JvmConstantUtf8 { value = "com/example/with" }, // 53
            new JvmConstantUtf8 { value = "x" }, // 54
            new JvmConstantUtf8 { value = "y" }, // 55
            new JvmConstantUtf8 { value = "SourceFile.java" }, // 56
            new JvmConstantUtf8 { value = "debug info" }, // 57
            new JvmConstantUtf8 { value = "hello world" }, // 58
            new JvmConstantClass { name_index = 1 }, // 59
            new JvmConstantNameAndType { name_index = 2, descriptor_index = 3 }, // 60
            new JvmConstantMethodref { class_index = 59, name_and_type_index = 60 }, // 61
            new JvmConstantInvokeDynamic { bootstrap_method_attr_index = 1, name_and_type_index = 60 } // 62
        };

        var names = new Dictionary<string, ushort>();
        for (var i = 0; i < pool.Count; i++)
        {
            if (pool[i] is JvmConstantUtf8 utf8)
            {
                names[utf8.value] = (ushort)(i + 1);
            }
        }

        return (pool.ToArray(), names);
    }

    /// <summary>
    ///     �������ClassFileData ��������
    /// </summary>
    private static JvmClassFileData make_class_file(JvmConstant[] pool, Dictionary<string, ushort> names,
        List<JvmAttributeInfo> attributes)
    {
        return new JvmClassFileData
        {
            magic = JvmConstants.magic,
            minor_version = 0,
            major_version = 61,
            constant_pool = pool,
            access_flags = 0x0021,
            this_class = names["java/lang/Object"],
            super_class = names["java/lang/Object"],
            interfaces = [],
            fields = [],
            methods = [],
            attributes = attributes
        };
    }

    private static JvmClassFileData round_trip(JvmClassFileData original)
    {
        var encoder = new JvmEncoder();
        var bytes = encoder.encode(original);
        var decoder = new JvmDecoder();
        return decoder.decode(bytes);
    }

    /// <summary>
    /// Signature ������������    
///</summary>
    [Fact]
    public void RoundTrip_SignatureAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmSignatureAttribute
            {
                attribute_name_index = names["Signature"],
                attribute_length = 2,
                signature_index = names["Ljava/util/List;"]
            }
        ]);

        var decoded = round_trip(original);
        var sig = Assert.IsType<JvmSignatureAttribute>(decoded.attributes[0]);
        Assert.Equal(names["Ljava/util/List;"], sig.signature_index);
    }

    /// <summary>
    /// Synthetic ��Deprecated ������������    
///</summary>
    [Fact]
    public void RoundTrip_SyntheticAndDeprecatedAttributes()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmSyntheticAttribute { attribute_name_index = names["Synthetic"], attribute_length = 0 },
            new JvmDeprecatedAttribute { attribute_name_index = names["Deprecated"], attribute_length = 0 }
        ]);

        var decoded = round_trip(original);
        Assert.Equal(2, decoded.attributes.Count);
        Assert.IsType<JvmSyntheticAttribute>(decoded.attributes[0]);
        Assert.IsType<JvmDeprecatedAttribute>(decoded.attributes[1]);
    }

    /// <summary>
    /// EnclosingMethod ������������    
///</summary>
    [Fact]
    public void RoundTrip_EnclosingMethodAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmEnclosingMethodAttribute
            {
                attribute_name_index = names["EnclosingMethod"],
                attribute_length = 4,
                class_index = names["java/lang/Object"],
                method_index = names["<init>"]
            }
        ]);

        var decoded = round_trip(original);
        var em = Assert.IsType<JvmEnclosingMethodAttribute>(decoded.attributes[0]);
        Assert.Equal(names["java/lang/Object"], em.class_index);
        Assert.Equal(names["<init>"], em.method_index);
    }

    /// <summary>
    /// SourceDebugExtension ������������    
///</summary>
    [Fact]
    public void RoundTrip_SourceDebugExtensionAttribute()
    {
        var (pool, names) = build_base_pool();
        var debugData = System.Text.Encoding.UTF8.GetBytes("SMAP\nTest.java\n*Scala\n*E\n");
        var original = make_class_file(pool, names,
        [
            new JvmSourceDebugExtensionAttribute
            {
                attribute_name_index = names["SourceDebugExtension"],
                attribute_length = (uint)debugData.Length,
                debug_extension = debugData
            }
        ]);

        var decoded = round_trip(original);
        var sde = Assert.IsType<JvmSourceDebugExtensionAttribute>(decoded.attributes[0]);
        Assert.Equal(debugData, sde.debug_extension);
    }

    /// <summary>
    /// InnerClasses ������������    
///</summary>
    [Fact]
    public void RoundTrip_InnerClassesAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmInnerClassesAttribute
            {
                attribute_name_index = names["InnerClasses"],
                attribute_length = 18,
                number_of_classes = 2,
                classes =
                [
                    new JvmInnerClassInfo
                    {
                        inner_class_info_index = names["Lcom/example/Inner;"],
                        outer_class_info_index = names["Lcom/example/Outer;"],
                        inner_name_index = names["testField"],
                        inner_class_access_flags = 0x0002
                    },
                    new JvmInnerClassInfo
                    {
                        inner_class_info_index = names["Lcom/example/Host;"],
                        outer_class_info_index = 0,
                        inner_name_index = 0,
                        inner_class_access_flags = 0x0400
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var ic = Assert.IsType<JvmInnerClassesAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)2, ic.number_of_classes);
        Assert.Equal(2, ic.classes.Count);
        Assert.Equal(names["Lcom/example/Inner;"], ic.classes[0].inner_class_info_index);
        Assert.Equal(names["Lcom/example/Outer;"], ic.classes[0].outer_class_info_index);
        Assert.Equal((ushort)0x0002, ic.classes[0].inner_class_access_flags);
        Assert.Equal(names["Lcom/example/Host;"], ic.classes[1].inner_class_info_index);
        Assert.Equal((ushort)0, ic.classes[1].outer_class_info_index);
    }

    /// <summary>
    /// BootstrapMethods ������������    
///</summary>
    [Fact]
    public void RoundTrip_BootstrapMethodsAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmBootstrapMethodsAttribute
            {
                attribute_name_index = names["BootstrapMethods"],
                attribute_length = 12,
                num_bootstrap_methods = 1,
                bootstrap_methods =
                [
                    new JvmBootstrapMethod
                    {
                        bootstrap_method_ref = names["java/lang/Object"],
                        num_bootstrap_arguments = 2,
                        bootstrap_arguments = [names["I"], names["testField"]]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var bm = Assert.IsType<JvmBootstrapMethodsAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)1, bm.num_bootstrap_methods);
        Assert.Single(bm.bootstrap_methods);
        Assert.Equal(names["java/lang/Object"], bm.bootstrap_methods[0].bootstrap_method_ref);
        Assert.Equal((ushort)2, bm.bootstrap_methods[0].num_bootstrap_arguments);
        Assert.Equal(2, bm.bootstrap_methods[0].bootstrap_arguments.Count);
    }

    /// <summary>
    /// MethodParameters ������������    
///</summary>
    [Fact]
    public void RoundTrip_MethodParametersAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmMethodParametersAttribute
            {
                attribute_name_index = names["MethodParameters"],
                attribute_length = 9,
                parameter_count = 2,
                parameters =
                [
                    new JvmMethodParameterInfo { name_index = names["x"], access_flags = 0x0000 },
                    new JvmMethodParameterInfo { name_index = names["y"], access_flags = 0x0010 }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var mp = Assert.IsType<JvmMethodParametersAttribute>(decoded.attributes[0]);
        Assert.Equal((byte)2, mp.parameter_count);
        Assert.Equal(2, mp.parameters.Count);
        Assert.Equal(names["x"], mp.parameters[0].name_index);
        Assert.Equal((ushort)0x0000, mp.parameters[0].access_flags);
        Assert.Equal(names["y"], mp.parameters[1].name_index);
        Assert.Equal((ushort)0x0010, mp.parameters[1].access_flags);
    }

    /// <summary>
    /// NestHost ������������    
///</summary>
    [Fact]
    public void RoundTrip_NestHostAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmNestHostAttribute
            {
                attribute_name_index = names["NestHost"],
                attribute_length = 2,
                host_class_index = names["Lcom/example/Host;"]
            }
        ]);

        var decoded = round_trip(original);
        var nh = Assert.IsType<JvmNestHostAttribute>(decoded.attributes[0]);
        Assert.Equal(names["Lcom/example/Host;"], nh.host_class_index);
    }

    /// <summary>
    /// NestMembers ������������    
///</summary>
    [Fact]
    public void RoundTrip_NestMembersAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmNestMembersAttribute
            {
                attribute_name_index = names["NestMembers"],
                attribute_length = 6,
                number_of_classes = 2,
                class_indexes = [names["Lcom/example/Member1;"], names["Lcom/example/Member2;"]]
            }
        ]);

        var decoded = round_trip(original);
        var nm = Assert.IsType<JvmNestMembersAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)2, nm.number_of_classes);
        Assert.Equal(2, nm.class_indexes.Count);
        Assert.Equal(names["Lcom/example/Member1;"], nm.class_indexes[0]);
        Assert.Equal(names["Lcom/example/Member2;"], nm.class_indexes[1]);
    }

    /// <summary>
    /// PermittedSubclasses �����������ԣ�JVM 17+��    
///</summary>
    [Fact]
    public void RoundTrip_PermittedSubclassesAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmPermittedSubclassesAttribute
            {
                attribute_name_index = names["PermittedSubclasses"],
                attribute_length = 6,
                number_of_classes = 2,
                class_indexes = [names["Lcom/example/Sub1;"], names["Lcom/example/Sub2;"]]
            }
        ]);

        var decoded = round_trip(original);
        var ps = Assert.IsType<JvmPermittedSubclassesAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)2, ps.number_of_classes);
        Assert.Equal(2, ps.class_indexes.Count);
        Assert.Equal(names["Lcom/example/Sub1;"], ps.class_indexes[0]);
    }

    /// <summary>
    /// Record �����������ԣ�JVM 16+��    
///</summary>
    [Fact]
    public void RoundTrip_RecordAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmRecordAttribute
            {
                attribute_name_index = names["Record"],
                attribute_length = 14,
                components_count = 2,
                components =
                [
                    new JvmRecordComponentInfo
                    {
                        name_index = names["x"],
                        descriptor_index = names["I"],
                        attributes = []
                    },
                    new JvmRecordComponentInfo
                    {
                        name_index = names["y"],
                        descriptor_index = names["Ljava/lang/String;"],
                        attributes =
                        [
                            new JvmSignatureAttribute
                            {
                                attribute_name_index = names["Signature"],
                                attribute_length = 2,
                                signature_index = names["Ljava/util/List;"]
                            }
                        ]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var rec = Assert.IsType<JvmRecordAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)2, rec.components_count);
        Assert.Equal(2, rec.components.Count);
        Assert.Equal(names["x"], rec.components[0].name_index);
        Assert.Equal(names["I"], rec.components[0].descriptor_index);
        Assert.Equal(names["y"], rec.components[1].name_index);
        Assert.Equal(names["Ljava/lang/String;"], rec.components[1].descriptor_index);
        Assert.Single(rec.components[1].attributes);
        var sig = Assert.IsType<JvmSignatureAttribute>(rec.components[1].attributes[0]);
        Assert.Equal(names["Ljava/util/List;"], sig.signature_index);
    }

    /// <summary>
    /// StackMapTable �����������ԣ�same_frame + same_locals_1_stack_item + full_frame��    
///</summary>
    [Fact]
    public void RoundTrip_StackMapTableAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmStackMapTableAttribute
            {
                attribute_name_index = names["StackMapTable"],
                attribute_length = 20,
                number_of_entries = 3,
                entries =
                [
                    new JvmSameFrame { frame_type = 0 },
                    new JvmSameLocals1StackItemFrame
                    {
                        frame_type = 64,
                        stack = [new JvmVerificationTypeInfo { tag = 1 }]
                    },
                    new JvmFullFrame
                    {
                        frame_type = 255,
                        offset_delta = 5,
                        number_of_locals = 2,
                        locals =
                        [
                            new JvmVerificationTypeInfo { tag = 1 },
                            new JvmVerificationTypeInfo { tag = 7, cpool_index = names["java/lang/Object"] }
                        ],
                        number_of_stack_items = 1,
                        stack = [new JvmVerificationTypeInfo { tag = 2 }]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var smt = Assert.IsType<JvmStackMapTableAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)3, smt.number_of_entries);
        Assert.Equal(3, smt.entries.Count);

        var same = Assert.IsType<JvmSameFrame>(smt.entries[0]);
        Assert.Equal((byte)0, same.frame_type);

        var slsif = Assert.IsType<JvmSameLocals1StackItemFrame>(smt.entries[1]);
        Assert.Equal((byte)64, slsif.frame_type);
        Assert.Single(slsif.stack);
        Assert.Equal((byte)1, slsif.stack[0].tag);

        var full = Assert.IsType<JvmFullFrame>(smt.entries[2]);
        Assert.Equal((byte)255, full.frame_type);
        Assert.Equal((ushort)5, full.offset_delta);
        Assert.Equal((ushort)2, full.number_of_locals);
        Assert.Equal((ushort)1, full.number_of_stack_items);
        Assert.Equal((byte)7, full.locals[1].tag);
        Assert.Equal(names["java/lang/Object"], full.locals[1].cpool_index);
    }

    /// <summary>
    /// StackMapTable chop/append/same_frame_extended ֡������������    
///</summary>
    [Fact]
    public void RoundTrip_StackMapTable_SpecialFrameTypes()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmStackMapTableAttribute
            {
                attribute_name_index = names["StackMapTable"],
                attribute_length = 20,
                number_of_entries = 3,
                entries =
                [
                    new JvmChopFrame { frame_type = 248, offset_delta = 10 },
                    new JvmSameFrameExtended { frame_type = 251, offset_delta = 20 },
                    new JvmAppendFrame
                    {
                        frame_type = 253,
                        offset_delta = 30,
                        locals =
                        [
                            new JvmVerificationTypeInfo { tag = 1 },
                            new JvmVerificationTypeInfo { tag = 3 }
                        ]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var smt = Assert.IsType<JvmStackMapTableAttribute>(decoded.attributes[0]);
        Assert.Equal(3, smt.entries.Count);

        var chop = Assert.IsType<JvmChopFrame>(smt.entries[0]);
        Assert.Equal((byte)248, chop.frame_type);
        Assert.Equal((ushort)10, chop.offset_delta);

        var sfe = Assert.IsType<JvmSameFrameExtended>(smt.entries[1]);
        Assert.Equal((byte)251, sfe.frame_type);
        Assert.Equal((ushort)20, sfe.offset_delta);

        var append = Assert.IsType<JvmAppendFrame>(smt.entries[2]);
        Assert.Equal((byte)253, append.frame_type);
        Assert.Equal((ushort)30, append.offset_delta);
        Assert.Equal(2, append.locals.Count);
        Assert.Equal((byte)1, append.locals[0].tag);
        Assert.Equal((byte)3, append.locals[1].tag);
    }

    /// <summary>
    /// RuntimeVisibleAnnotations ������������    
///</summary>
    [Fact]
    public void RoundTrip_RuntimeVisibleAnnotationsAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmRuntimeVisibleAnnotationsAttribute
            {
                attribute_name_index = names["RuntimeVisibleAnnotations"],
                attribute_length = 12,
                num_annotations = 1,
                annotations =
                [
                    new JvmAnnotation
                    {
                        type_index = names["Lcom/example/Annotation;"],
                        num_element_value_pairs = 2,
                        element_value_pairs =
                        [
                            new JvmElementValuePair
                            {
                                element_name_index = names["value"],
                                value = new JvmElementValue
                                {
                                    tag = (byte)'s',
                                    const_value_index = names["hello world"]
                                }
                            },
                            new JvmElementValuePair
                            {
                                element_name_index = names["name"],
                                value = new JvmElementValue
                                {
                                    tag = (byte)'e',
                                    type_name_index = names["Ljava/lang/String;"],
                                    enum_const_name_index = names["testField"]
                                }
                            }
                        ]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var rva = Assert.IsType<JvmRuntimeVisibleAnnotationsAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)1, rva.num_annotations);
        Assert.Single(rva.annotations);
        Assert.Equal(names["Lcom/example/Annotation;"], rva.annotations[0].type_index);
        Assert.Equal((ushort)2, rva.annotations[0].num_element_value_pairs);
        Assert.Equal((byte)'s', rva.annotations[0].element_value_pairs[0].value.tag);
        Assert.Equal(names["hello world"], rva.annotations[0].element_value_pairs[0].value.const_value_index);
        Assert.Equal((byte)'e', rva.annotations[0].element_value_pairs[1].value.tag);
        Assert.Equal(names["Ljava/lang/String;"], rva.annotations[0].element_value_pairs[1].value.type_name_index);
    }

    /// <summary>
    /// AnnotationDefault ������������    
///</summary>
    [Fact]
    public void RoundTrip_AnnotationDefaultAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmAnnotationDefaultAttribute
            {
                attribute_name_index = names["AnnotationDefault"],
                attribute_length = 3,
                default_value = new JvmElementValue
                {
                    tag = (byte)'I',
                    const_value_index = names["I"]
                }
            }
        ]);

        var decoded = round_trip(original);
        var ad = Assert.IsType<JvmAnnotationDefaultAttribute>(decoded.attributes[0]);
        Assert.Equal((byte)'I', ad.default_value.tag);
        Assert.Equal(names["I"], ad.default_value.const_value_index);
    }

    /// <summary>
    /// AnnotationDefault ����������������    
///</summary>
    [Fact]
    public void RoundTrip_AnnotationDefault_ArrayValue()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmAnnotationDefaultAttribute
            {
                attribute_name_index = names["AnnotationDefault"],
                attribute_length = 8,
                default_value = new JvmElementValue
                {
                    tag = (byte)'[',
                    array_num_values = 2,
                    array_values =
                    [
                        new JvmElementValue { tag = (byte)'I', const_value_index = names["I"] },
                        new JvmElementValue { tag = (byte)'s', const_value_index = names["testField"] }
                    ]
                }
            }
        ]);

        var decoded = round_trip(original);
        var ad = Assert.IsType<JvmAnnotationDefaultAttribute>(decoded.attributes[0]);
        Assert.Equal((byte)'[', ad.default_value.tag);
        Assert.Equal((ushort)2, ad.default_value.array_num_values);
        Assert.Equal(2, ad.default_value.array_values!.Count);
        Assert.Equal((byte)'I', ad.default_value.array_values[0].tag);
        Assert.Equal((byte)'s', ad.default_value.array_values[1].tag);
    }

    /// <summary>
    /// Module �����������ԣ�JVM 9+��    
///</summary>
    [Fact]
    public void RoundTrip_ModuleAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmModuleAttribute
            {
                attribute_name_index = names["Module"],
                attribute_length = 40,
                module_name_index = names["com/example/module"],
                module_flags = 0x0020,
                module_version_index = names["1.0"],
                requires =
                [
                    new JvmModuleRequire
                    {
                        requires_index = names["com/example/module"],
                        requires_flags = 0x0040,
                        requires_version_index = names["1.0"]
                    }
                ],
                exports =
                [
                    new JvmModuleExport
                    {
                        exports_index = names["com/example/exported"],
                        exports_flags = 0x0000,
                        exports_to_index = [names["com/example/module"]]
                    }
                ],
                opens =
                [
                    new JvmModuleOpen
                    {
                        opens_index = names["com/example/opened"],
                        opens_flags = 0x0000,
                        opens_to_index = []
                    }
                ],
                uses_index = [names["com/example/uses"]],
                provides =
                [
                    new JvmModuleProvide
                    {
                        provides_index = names["com/example/provides"],
                        provides_with_index = [names["com/example/with"]]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var mod = Assert.IsType<JvmModuleAttribute>(decoded.attributes[0]);
        Assert.Equal(names["com/example/module"], mod.module_name_index);
        Assert.Equal((ushort)0x0020, mod.module_flags);
        Assert.Equal(names["1.0"], mod.module_version_index);
        Assert.Single(mod.requires);
        Assert.Equal(names["com/example/module"], mod.requires[0].requires_index);
        Assert.Equal((ushort)0x0040, mod.requires[0].requires_flags);
        Assert.Single(mod.exports);
        Assert.Equal(names["com/example/exported"], mod.exports[0].exports_index);
        Assert.Single(mod.exports[0].exports_to_index);
        Assert.Single(mod.opens);
        Assert.Single(mod.uses_index);
        Assert.Single(mod.provides);
        Assert.Single(mod.provides[0].provides_with_index);
    }

    /// <summary>
    /// Dynamic/Module/Package ��������������    
///</summary>
    [Fact]
    public void RoundTrip_DynamicModulePackageConstants()
    {
        var pool = new JvmConstant[]
        {
            new JvmConstantUtf8 { value = "java/lang/Object" }, // 1
            new JvmConstantUtf8 { value = "<init>" }, // 2
            new JvmConstantUtf8 { value = "()V" }, // 3
            new JvmConstantUtf8 { value = "com/example/module" }, // 4
            new JvmConstantUtf8 { value = "com/example/pkg" }, // 5
            new JvmConstantClass { name_index = 1 }, // 6
            new JvmConstantNameAndType { name_index = 2, descriptor_index = 3 }, // 7
            new JvmConstantDynamic { bootstrap_method_attr_index = 1, name_and_type_index = 7 }, // 8
            new JvmConstantModule { name_index = 4 }, // 9
            new JvmConstantPackage { name_index = 5 } // 10
        };

        var original = new JvmClassFileData
        {
            magic = JvmConstants.magic,
            minor_version = 0,
            major_version = 55,
            constant_pool = pool,
            access_flags = 0x0021,
            this_class = 6,
            super_class = 6,
            interfaces = [],
            fields = [],
            methods = [],
            attributes = []
        };

        var decoded = round_trip(original);
        Assert.Equal(pool.Length, decoded.constant_pool.Count);

        var dyn = Assert.IsType<JvmConstantDynamic>(decoded.constant_pool[7]);
        Assert.Equal((ushort)1, dyn.bootstrap_method_attr_index);
        Assert.Equal((ushort)7, dyn.name_and_type_index);

        var mod = Assert.IsType<JvmConstantModule>(decoded.constant_pool[8]);
        Assert.Equal((ushort)4, mod.name_index);

        var pkg = Assert.IsType<JvmConstantPackage>(decoded.constant_pool[9]);
        Assert.Equal((ushort)5, pkg.name_index);
    }

    /// <summary>
    /// Long/Double �������������ԣ���֤ռλ����������    
///</summary>
    [Fact]
    public void RoundTrip_LongDoubleConstantPool()
    {
        var pool = new JvmConstant[]
        {
            new JvmConstantUtf8 { value = "test" }, // 1
            new JvmConstantLong { value = 0x123456789ABCDEF0L }, // 2 (��2,3)
            new JvmConstantDouble { value = 2.718281828 }, // 4 (��4,5)
            new JvmConstantUtf8 { value = "after" } // 6
        };

        var original = new JvmClassFileData
        {
            magic = JvmConstants.magic,
            minor_version = 0,
            major_version = 61,
            constant_pool = pool,
            access_flags = 0,
            this_class = 0,
            super_class = 0,
            interfaces = [],
            fields = [],
            methods = [],
            attributes = []
        };

        var decoded = round_trip(original);
        Assert.Equal(4, decoded.constant_pool.Count);
        var l = Assert.IsType<JvmConstantLong>(decoded.constant_pool[1]);
        Assert.Equal(0x123456789ABCDEF0L, l.value);
        var d = Assert.IsType<JvmConstantDouble>(decoded.constant_pool[2]);
        Assert.Equal(2.718281828, d.value, 9);
        var after = Assert.IsType<JvmConstantUtf8>(decoded.constant_pool[3]);
        Assert.Equal("after", after.value);
    }

    /// <summary>
    /// LocalVariableTypeTable ������������    
///</summary>
    [Fact]
    public void RoundTrip_LocalVariableTypeTableAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmLocalVariableTypeTableAttribute
            {
                attribute_name_index = names["LocalVariableTypeTable"],
                attribute_length = 12,
                local_variable_type_table_length = 1,
                local_variable_type_table =
                [
                    new JvmLocalVariableTypeEntry
                    {
                        start_pc = 0,
                        length = 10,
                        name_index = names["x"],
                        signature_index = names["Ljava/util/List;"],
                        index = 1
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var lvtt = Assert.IsType<JvmLocalVariableTypeTableAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)1, lvtt.local_variable_type_table_length);
        Assert.Single(lvtt.local_variable_type_table);
        Assert.Equal((ushort)0, lvtt.local_variable_type_table[0].start_pc);
        Assert.Equal((ushort)10, lvtt.local_variable_type_table[0].length);
        Assert.Equal(names["x"], lvtt.local_variable_type_table[0].name_index);
        Assert.Equal(names["Ljava/util/List;"], lvtt.local_variable_type_table[0].signature_index);
        Assert.Equal((ushort)1, lvtt.local_variable_type_table[0].index);
    }

    /// <summary>
    /// RuntimeVisibleParameterAnnotations ������������    
///</summary>
    [Fact]
    public void RoundTrip_RuntimeVisibleParameterAnnotationsAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmRuntimeVisibleParameterAnnotationsAttribute
            {
                attribute_name_index = names["RuntimeVisibleParameterAnnotations"],
                attribute_length = 10,
                num_parameters = 1,
                parameter_annotations =
                [
                    new JvmParameterAnnotations
                    {
                        num_annotations = 1,
                        annotations =
                        [
                            new JvmAnnotation
                            {
                                type_index = names["Lcom/example/Annotation;"],
                                num_element_value_pairs = 0,
                                element_value_pairs = []
                            }
                        ]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var rvpa = Assert.IsType<JvmRuntimeVisibleParameterAnnotationsAttribute>(decoded.attributes[0]);
        Assert.Equal((byte)1, rvpa.num_parameters);
        Assert.Single(rvpa.parameter_annotations);
        Assert.Equal((ushort)1, rvpa.parameter_annotations[0].num_annotations);
        Assert.Single(rvpa.parameter_annotations[0].annotations);
        Assert.Equal(names["Lcom/example/Annotation;"], rvpa.parameter_annotations[0].annotations[0].type_index);
    }

    /// <summary>
    /// Ƕ��ע�⣨AnnotationDefault ����Ƕ�� @ ע�⣩��������    
///</summary>
    [Fact]
    public void RoundTrip_NestedAnnotationInAnnotationDefault()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmAnnotationDefaultAttribute
            {
                attribute_name_index = names["AnnotationDefault"],
                attribute_length = 8,
                default_value = new JvmElementValue
                {
                    tag = (byte)'@',
                    annotation_value = new JvmAnnotation
                    {
                        type_index = names["Lcom/example/Annotation;"],
                        num_element_value_pairs = 1,
                        element_value_pairs =
                        [
                            new JvmElementValuePair
                            {
                                element_name_index = names["value"],
                                value = new JvmElementValue
                                {
                                    tag = (byte)'c',
                                    class_info_index = names["Ljava/lang/String;"]
                                }
                            }
                        ]
                    }
                }
            }
        ]);

        var decoded = round_trip(original);
        var ad = Assert.IsType<JvmAnnotationDefaultAttribute>(decoded.attributes[0]);
        Assert.Equal((byte)'@', ad.default_value.tag);
        Assert.NotNull(ad.default_value.annotation_value);
        Assert.Equal(names["Lcom/example/Annotation;"], ad.default_value.annotation_value.type_index);
        Assert.Equal((ushort)1, ad.default_value.annotation_value.num_element_value_pairs);
        Assert.Equal((byte)'c', ad.default_value.annotation_value.element_value_pairs[0].value.tag);
        Assert.Equal(names["Ljava/lang/String;"],
            ad.default_value.annotation_value.element_value_pairs[0].value.class_info_index);
    }

    /// <summary>
    /// Code ���԰���LineNumberTable + LocalVariableTable �����Ե���������    
///</summary>
    [Fact]
    public void RoundTrip_CodeAttribute_WithSubAttributes()
    {
        var (pool, names) = build_base_pool();
        var original = new JvmClassFileData
        {
            magic = JvmConstants.magic,
            minor_version = 0,
            major_version = 61,
            constant_pool = pool,
            access_flags = 0x0021,
            this_class = names["java/lang/Object"],
            super_class = names["java/lang/Object"],
            interfaces = [],
            fields = [],
            methods =
            [
                new JvmMethodInfo
                {
                    access_flags = 0x0001,
                    name_index = names["<init>"],
                    descriptor_index = names["()V"],
                    attributes =
                    [
                        new JvmCodeAttribute
                        {
                            attribute_name_index = names["Code"],
                            attribute_length = 40,
                            max_stack = 1,
                            max_locals = 1,
                            code_length = 1,
                            code = [0xB1],
                            exception_table_length = 0,
                            exception_table = [],
                            attributes_count = 2,
                            attributes =
                            [
                                new JvmLineNumberTableAttribute
                                {
                                    attribute_name_index = names["LineNumberTable"],
                                    attribute_length = 6,
                                    line_number_table_length = 1,
                                    line_number_table =
                                    [
                                        new JvmLineNumberEntry { start_pc = 0, line_number = 42 }
                                    ]
                                },
                                new JvmLocalVariableTableAttribute
                                {
                                    attribute_name_index = names["LocalVariableTable"],
                                    attribute_length = 12,
                                    local_variable_table_length = 1,
                                    local_variable_table =
                                    [
                                        new JvmLocalVariableEntry
                                        {
                                            start_pc = 0,
                                            length = 5,
                                            name_index = names["x"],
                                            descriptor_index = names["I"],
                                            index = 0
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ],
            attributes = []
        };

        var decoded = round_trip(original);
        Assert.Single(decoded.methods);
        var code = Assert.IsType<JvmCodeAttribute>(decoded.methods[0].attributes[0]);
        Assert.Equal((ushort)1, code.max_stack);
        Assert.Equal((ushort)1, code.max_locals);
        Assert.Single(code.code);
        Assert.Equal((byte)0xB1, code.code[0]);
        Assert.Equal(2, code.attributes.Count);

        var lnt = Assert.IsType<JvmLineNumberTableAttribute>(code.attributes[0]);
        Assert.Single(lnt.line_number_table);
        Assert.Equal((ushort)0, lnt.line_number_table[0].start_pc);
        Assert.Equal((ushort)42, lnt.line_number_table[0].line_number);

        var lvt = Assert.IsType<JvmLocalVariableTableAttribute>(code.attributes[1]);
        Assert.Single(lvt.local_variable_table);
        Assert.Equal(names["x"], lvt.local_variable_table[0].name_index);
        Assert.Equal(names["I"], lvt.local_variable_table[0].descriptor_index);
    }

    /// <summary>
    /// RuntimeInvisibleAnnotations ������������    
///</summary>
    [Fact]
    public void RoundTrip_RuntimeInvisibleAnnotationsAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmRuntimeInvisibleAnnotationsAttribute
            {
                attribute_name_index = names["RuntimeInvisibleAnnotations"],
                attribute_length = 6,
                num_annotations = 1,
                annotations =
                [
                    new JvmAnnotation
                    {
                        type_index = names["Lcom/example/Annotation;"],
                        num_element_value_pairs = 0,
                        element_value_pairs = []
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var ria = Assert.IsType<JvmRuntimeInvisibleAnnotationsAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)1, ria.num_annotations);
        Assert.Single(ria.annotations);
        Assert.Equal(names["Lcom/example/Annotation;"], ria.annotations[0].type_index);
    }

    /// <summary>
    /// RuntimeInvisibleParameterAnnotations ������������    
///</summary>
    [Fact]
    public void RoundTrip_RuntimeInvisibleParameterAnnotationsAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmRuntimeInvisibleParameterAnnotationsAttribute
            {
                attribute_name_index = names["RuntimeInvisibleParameterAnnotations"],
                attribute_length = 4,
                num_parameters = 1,
                parameter_annotations =
                [
                    new JvmParameterAnnotations
                    {
                        num_annotations = 0,
                        annotations = []
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var ripa = Assert.IsType<JvmRuntimeInvisibleParameterAnnotationsAttribute>(decoded.attributes[0]);
        Assert.Equal((byte)1, ripa.num_parameters);
        Assert.Single(ripa.parameter_annotations);
        Assert.Equal((ushort)0, ripa.parameter_annotations[0].num_annotations);
    }

    /// <summary>
    /// Exceptions ������������    
///</summary>
    [Fact]
    public void RoundTrip_ExceptionsAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmExceptionsAttribute
            {
                attribute_name_index = names["Exceptions"],
                attribute_length = 6,
                number_of_exceptions = 2,
                exception_index_table = [names["Lcom/example/Inner;"], names["Lcom/example/Host;"]]
            }
        ]);

        var decoded = round_trip(original);
        var exc = Assert.IsType<JvmExceptionsAttribute>(decoded.attributes[0]);
        Assert.Equal((ushort)2, exc.number_of_exceptions);
        Assert.Equal(2, exc.exception_index_table.Count);
        Assert.Equal(names["Lcom/example/Inner;"], exc.exception_index_table[0]);
        Assert.Equal(names["Lcom/example/Host;"], exc.exception_index_table[1]);
    }

    /// <summary>
    /// SourceFile ������������    
///</summary>
    [Fact]
    public void RoundTrip_SourceFileAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmSourceFileAttribute
            {
                attribute_name_index = names["SourceFile"],
                attribute_length = 2,
                source_file_index = names["SourceFile.java"]
            }
        ]);

        var decoded = round_trip(original);
        var sf = Assert.IsType<JvmSourceFileAttribute>(decoded.attributes[0]);
        Assert.Equal(names["SourceFile.java"], sf.source_file_index);
    }

    /// <summary>
    /// ConstantValue ������������    
///</summary>
    [Fact]
    public void RoundTrip_ConstantValueAttribute()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmConstantValueAttribute
            {
                attribute_name_index = names["ConstantValue"],
                attribute_length = 2,
                constant_value_index = names["I"]
            }
        ]);

        var decoded = round_trip(original);
        var cv = Assert.IsType<JvmConstantValueAttribute>(decoded.attributes[0]);
        Assert.Equal(names["I"], cv.constant_value_index);
    }

    /// <summary>
    /// StackMapTable Uninitialized verification type info ��������    
///</summary>
    [Fact]
    public void RoundTrip_StackMapTable_UninitializedVti()
    {
        var (pool, names) = build_base_pool();
        var original = make_class_file(pool, names,
        [
            new JvmStackMapTableAttribute
            {
                attribute_name_index = names["StackMapTable"],
                attribute_length = 10,
                number_of_entries = 1,
                entries =
                [
                    new JvmSameLocals1StackItemFrame
                    {
                        frame_type = 64,
                        stack = [new JvmVerificationTypeInfo { tag = 8, offset = 42 }]
                    }
                ]
            }
        ]);

        var decoded = round_trip(original);
        var smt = Assert.IsType<JvmStackMapTableAttribute>(decoded.attributes[0]);
        var slsif = Assert.IsType<JvmSameLocals1StackItemFrame>(smt.entries[0]);
        Assert.Equal((byte)8, slsif.stack[0].tag);
        Assert.Equal((ushort)42, slsif.stack[0].offset);
    }
}

