using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.Jvm.Data;

namespace Std.Data.Binary.Jvm.Decode;

/// <summary>
///     JVM ClassFile 解码器，的.class 二进制解码为 <see cref="JvmClassFileData" />
/// </summary>
public sealed class JvmDecoder
{
    /// <summary>
    ///     从字节数组解的ClassFile
    /// </summary>
    public JvmClassFileData decode(byte[] data)
    {
        var span = new ReadOnlySpan<byte>(data);
        var offset = 0;

        var magic = BinaryPrimitives.ReadUInt32BigEndian(span);
        offset += 4;
        if (magic != JvmConstants.magic) throw new InvalidDataException("非法的ClassFile 魔数");

        var minorVersion = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var majorVersion = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;

        var constantPoolCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var constantPool = new List<JvmConstant>();
        var utf8Lookup = new Dictionary<ushort, string>();
        for (var i = 1; i < constantPoolCount; i++)
        {
            var (constant, consumed) = decode_constant(span[offset..]);
            constantPool.Add(constant);
            offset += consumed;
            if (constant is JvmConstantUtf8 utf8) utf8Lookup[(ushort)i] = utf8.value;

            if (constant.kind is JvmConstantKind.@long or JvmConstantKind.@double) i++;
        }

        var accessFlags = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var thisClass = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var superClass = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;

        var interfacesCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var interfaces = new List<ushort>();
        for (var i = 0; i < interfacesCount; i++)
        {
            interfaces.Add(BinaryPrimitives.ReadUInt16BigEndian(span[offset..]));
            offset += 2;
        }

        var fieldsCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var fields = new List<JvmFieldInfo>();
        for (var i = 0; i < fieldsCount; i++)
        {
            var (field, consumed) = decode_field_info(span[offset..], utf8Lookup);
            fields.Add(field);
            offset += consumed;
        }

        var methodsCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var methods = new List<JvmMethodInfo>();
        for (var i = 0; i < methodsCount; i++)
        {
            var (method, consumed) = decode_method_info(span[offset..], utf8Lookup);
            methods.Add(method);
            offset += consumed;
        }

        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = decode_attribute_info(span[offset..], utf8Lookup);
            if (attr is not null) attributes.Add(attr);

            offset += consumed;
        }

        return new JvmClassFileData
        {
            magic = magic,
            minor_version = minorVersion,
            major_version = majorVersion,
            constant_pool = constantPool,
            access_flags = accessFlags,
            this_class = thisClass,
            super_class = superClass,
            interfaces = interfaces,
            fields = fields,
            methods = methods,
            attributes = attributes
        };
    }

    #region 常量池解的

    private static (JvmConstant Constant, int Consumed) decode_constant(ReadOnlySpan<byte> data)
    {
        var tag = data[0];
        var offset = 1;
        return tag switch
        {
            (byte)JvmConstantKind.utf8 => (new JvmConstantUtf8
            {
                value = Encoding.UTF8.GetString(
                    data.Slice(offset + 2, BinaryPrimitives.ReadUInt16BigEndian(data[offset..])))
            }, offset + 2 + BinaryPrimitives.ReadUInt16BigEndian(data[offset..])),
            (byte)JvmConstantKind.integer => (new JvmConstantInteger
            {
                value = BinaryPrimitives.ReadInt32BigEndian(data[offset..])
            }, offset + 4),
            (byte)JvmConstantKind.@float => (new JvmConstantFloat
            {
                value = BinaryPrimitives.ReadSingleBigEndian(data[offset..])
            }, offset + 4),
            (byte)JvmConstantKind.@long => (new JvmConstantLong
            {
                value = BinaryPrimitives.ReadInt64BigEndian(data[offset..])
            }, offset + 8),
            (byte)JvmConstantKind.@double => (new JvmConstantDouble
            {
                value = BinaryPrimitives.ReadDoubleBigEndian(data[offset..])
            }, offset + 8),
            (byte)JvmConstantKind.@class => (new JvmConstantClass
            {
                name_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.@string => (new JvmConstantString
            {
                string_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.fieldref => (new JvmConstantFieldref
            {
                class_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                name_and_type_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.methodref => (new JvmConstantMethodref
            {
                class_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                name_and_type_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.interface_methodref => (new JvmConstantInterfaceMethodref
            {
                class_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                name_and_type_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.name_and_type => (new JvmConstantNameAndType
            {
                name_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                descriptor_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.method_handle => (new JvmConstantMethodHandle
            {
                reference_kind = data[offset],
                reference_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 1)..])
            }, offset + 3),
            (byte)JvmConstantKind.method_type => (new JvmConstantMethodType
            {
                descriptor_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.dynamic => (new JvmConstantDynamic
            {
                bootstrap_method_attr_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                name_and_type_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.invoke_dynamic => (new JvmConstantInvokeDynamic
            {
                bootstrap_method_attr_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                name_and_type_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            }, offset + 4),
            (byte)JvmConstantKind.module => (new JvmConstantModule
            {
                name_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            (byte)JvmConstantKind.package => (new JvmConstantPackage
            {
                name_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..])
            }, offset + 2),
            _ => throw new InvalidDataException($"未知的常量池标记: 0x{tag:X2}")
        };
    }

    #endregion

    #region 字段/方法解码

    private static (JvmFieldInfo Field, int Consumed) decode_field_info(ReadOnlySpan<byte> data,
        Dictionary<ushort, string> utf8Lookup)
    {
        var offset = 0;
        var accessFlags = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var nameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var descriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = decode_attribute_info(data[offset..], utf8Lookup);
            offset += consumed;
            if (attr is not null) attributes.Add(attr);
        }

        return (new JvmFieldInfo
        {
            access_flags = accessFlags,
            name_index = nameIndex,
            descriptor_index = descriptorIndex,
            attributes = attributes
        }, offset);
    }

    private static (JvmMethodInfo Method, int Consumed) decode_method_info(ReadOnlySpan<byte> data,
        Dictionary<ushort, string> utf8Lookup)
    {
        var offset = 0;
        var accessFlags = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var nameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var descriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = decode_attribute_info(data[offset..], utf8Lookup);
            offset += consumed;
            if (attr is not null) attributes.Add(attr);
        }

        return (new JvmMethodInfo
        {
            access_flags = accessFlags,
            name_index = nameIndex,
            descriptor_index = descriptorIndex,
            attributes = attributes
        }, offset);
    }

    #endregion

    #region 属性解的

    private static (JvmAttributeInfo? Attribute, int Consumed) decode_attribute_info(
        ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup)
    {
        var offset = 0;
        var attributeNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var attributeLength = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;
        var attributeDataStart = offset;

        var attributeName = utf8Lookup.GetValueOrDefault(attributeNameIndex);

        JvmAttributeInfo? attribute;
        switch (attributeName)
        {
            case "Code":
                attribute = decode_code_attribute(data[offset..], utf8Lookup, attributeNameIndex, attributeLength);
                break;
            case "LineNumberTable":
                attribute = decode_line_number_table(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "LocalVariableTable":
                attribute = decode_local_variable_table(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "LocalVariableTypeTable":
                attribute = decode_local_variable_type_table(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "ConstantValue":
                attribute = decode_constant_value(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "SourceFile":
                attribute = decode_source_file(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Exceptions":
                attribute = decode_exceptions(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "InnerClasses":
                attribute = decode_inner_classes(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "BootstrapMethods":
                attribute = decode_bootstrap_methods(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Signature":
                attribute = decode_signature(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Synthetic":
                attribute = new JvmSyntheticAttribute
                {
                    attribute_name_index = attributeNameIndex,
                    attribute_length = attributeLength
                };
                break;
            case "Deprecated":
                attribute = new JvmDeprecatedAttribute
                {
                    attribute_name_index = attributeNameIndex,
                    attribute_length = attributeLength
                };
                break;
            case "EnclosingMethod":
                attribute = decode_enclosing_method(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "SourceDebugExtension":
                attribute = decode_source_debug_extension(data[offset..], (int)attributeLength, attributeNameIndex,
                    attributeLength);
                break;
            case "MethodParameters":
                attribute = decode_method_parameters(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "StackMapTable":
                attribute = decode_stack_map_table(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "NestHost":
                attribute = decode_nest_host(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "NestMembers":
                attribute = decode_nest_members(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Record":
                attribute = decode_record(data[offset..], utf8Lookup, attributeNameIndex, attributeLength);
                break;
            case "PermittedSubclasses":
                attribute = decode_permitted_subclasses(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "RuntimeVisibleAnnotations":
                attribute = decode_runtime_visible_annotations(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "RuntimeInvisibleAnnotations":
                attribute = decode_runtime_invisible_annotations(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "RuntimeVisibleParameterAnnotations":
                attribute = decode_runtime_visible_parameter_annotations(data[offset..], attributeNameIndex,
                    attributeLength);
                break;
            case "RuntimeInvisibleParameterAnnotations":
                attribute = decode_runtime_invisible_parameter_annotations(data[offset..], attributeNameIndex,
                    attributeLength);
                break;
            case "AnnotationDefault":
                attribute = decode_annotation_default(data[offset..], attributeNameIndex, attributeLength);
                break;
            case "Module":
                attribute = decode_module(data[offset..], attributeNameIndex, attributeLength);
                break;
            default:
                attribute = read_raw_bytes(data[offset..], (int)attributeLength, attributeName ?? "Unknown",
                    attributeNameIndex, attributeLength);
                break;
        }

        offset = attributeDataStart + (int)attributeLength;
        return (attribute, offset);
    }

    private static JvmCodeAttribute decode_code_attribute(
        ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var maxStack = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var maxLocals = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var codeLength = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;
        var code = data.Slice(offset, (int)codeLength).ToArray();
        offset += (int)codeLength;
        var exceptionTableLength = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var exceptionTable = new List<JvmExceptionTableEntry>();
        for (var i = 0; i < exceptionTableLength; i++)
        {
            exceptionTable.Add(new JvmExceptionTableEntry
            {
                start_pc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                end_pc = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                handler_pc = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                catch_type = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..])
            });
            offset += 8;
        }

        var attributesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var attributes = new List<JvmAttributeInfo>();
        for (var i = 0; i < attributesCount; i++)
        {
            var (attr, consumed) = decode_attribute_info(data[offset..], utf8Lookup);
            offset += consumed;
            if (attr is not null) attributes.Add(attr);
        }

        return new JvmCodeAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            max_stack = maxStack,
            max_locals = maxLocals,
            code_length = codeLength,
            code = code,
            exception_table_length = exceptionTableLength,
            exception_table = exceptionTable,
            attributes_count = attributesCount,
            attributes = attributes
        };
    }

    private static JvmLineNumberTableAttribute decode_line_number_table(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var tableLength = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var table = new List<JvmLineNumberEntry>();
        for (var i = 0; i < tableLength; i++)
        {
            table.Add(new JvmLineNumberEntry
            {
                start_pc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                line_number = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            });
            offset += 4;
        }

        return new JvmLineNumberTableAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            line_number_table_length = tableLength,
            line_number_table = table
        };
    }

    private static JvmLocalVariableTableAttribute decode_local_variable_table(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var tableLength = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var table = new List<JvmLocalVariableEntry>();
        for (var i = 0; i < tableLength; i++)
        {
            table.Add(new JvmLocalVariableEntry
            {
                start_pc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                length = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                name_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                descriptor_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..]),
                index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 8)..])
            });
            offset += 10;
        }

        return new JvmLocalVariableTableAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            local_variable_table_length = tableLength,
            local_variable_table = table
        };
    }

    private static JvmLocalVariableTypeTableAttribute decode_local_variable_type_table(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var tableLength = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var table = new List<JvmLocalVariableTypeEntry>();
        for (var i = 0; i < tableLength; i++)
        {
            table.Add(new JvmLocalVariableTypeEntry
            {
                start_pc = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                length = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                name_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                signature_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..]),
                index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 8)..])
            });
            offset += 10;
        }

        return new JvmLocalVariableTypeTableAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            local_variable_type_table_length = tableLength,
            local_variable_type_table = table
        };
    }

    private static JvmConstantValueAttribute decode_constant_value(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmConstantValueAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            constant_value_index = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmSourceFileAttribute decode_source_file(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmSourceFileAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            source_file_index = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmExceptionsAttribute decode_exceptions(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var count = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var exceptions = new List<ushort>();
        for (var i = 0; i < count; i++)
        {
            exceptions.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        return new JvmExceptionsAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            number_of_exceptions = count,
            exception_index_table = exceptions
        };
    }

    private static JvmInnerClassesAttribute decode_inner_classes(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfClasses = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var classes = new List<JvmInnerClassInfo>();
        for (var i = 0; i < numberOfClasses; i++)
        {
            classes.Add(new JvmInnerClassInfo
            {
                inner_class_info_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                outer_class_info_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]),
                inner_name_index = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 4)..]),
                inner_class_access_flags = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 6)..])
            });
            offset += 8;
        }

        return new JvmInnerClassesAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            number_of_classes = numberOfClasses,
            classes = classes
        };
    }

    private static JvmBootstrapMethodsAttribute decode_bootstrap_methods(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numBootstrapMethods = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var methods = new List<JvmBootstrapMethod>();
        for (var i = 0; i < numBootstrapMethods; i++)
        {
            var methodRef = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var numArgs = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var args = new List<ushort>();
            for (var j = 0; j < numArgs; j++)
            {
                args.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            methods.Add(new JvmBootstrapMethod
            {
                bootstrap_method_ref = methodRef,
                num_bootstrap_arguments = numArgs,
                bootstrap_arguments = args
            });
        }

        return new JvmBootstrapMethodsAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            num_bootstrap_methods = numBootstrapMethods,
            bootstrap_methods = methods
        };
    }

    private static JvmSignatureAttribute decode_signature(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmSignatureAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            signature_index = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmEnclosingMethodAttribute decode_enclosing_method(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmEnclosingMethodAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            class_index = BinaryPrimitives.ReadUInt16BigEndian(data),
            method_index = BinaryPrimitives.ReadUInt16BigEndian(data[2..])
        };
    }

    private static JvmSourceDebugExtensionAttribute decode_source_debug_extension(
        ReadOnlySpan<byte> data, int length, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmSourceDebugExtensionAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            debug_extension = [.. data[..length]]
        };
    }

    private static JvmMethodParametersAttribute decode_method_parameters(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var parametersCount = data[offset];
        offset += 1;
        var parameters = new List<JvmMethodParameterInfo>();
        for (var i = 0; i < parametersCount; i++)
        {
            parameters.Add(new JvmMethodParameterInfo
            {
                name_index = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]),
                access_flags = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..])
            });
            offset += 4;
        }

        return new JvmMethodParametersAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            parameter_count = parametersCount,
            parameters = parameters
        };
    }

    private static JvmNestHostAttribute decode_nest_host(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmNestHostAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            host_class_index = BinaryPrimitives.ReadUInt16BigEndian(data)
        };
    }

    private static JvmNestMembersAttribute decode_nest_members(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfClasses = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var classes = new List<ushort>();
        for (var i = 0; i < numberOfClasses; i++)
        {
            classes.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        return new JvmNestMembersAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            number_of_classes = numberOfClasses,
            class_indexes = classes
        };
    }

    private static JvmPermittedSubclassesAttribute decode_permitted_subclasses(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfClasses = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var classes = new List<ushort>();
        for (var i = 0; i < numberOfClasses; i++)
        {
            classes.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        return new JvmPermittedSubclassesAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            number_of_classes = numberOfClasses,
            class_indexes = classes
        };
    }

    private static JvmRecordAttribute decode_record(
        ReadOnlySpan<byte> data, Dictionary<ushort, string> utf8Lookup, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var componentsCount = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var components = new List<JvmRecordComponentInfo>();
        for (var i = 0; i < componentsCount; i++)
        {
            var nameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var descriptorIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var attrCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var attrs = new List<JvmAttributeInfo>();
            for (var j = 0; j < attrCount; j++)
            {
                var (attr, consumed) = decode_attribute_info(data[offset..], utf8Lookup);
                offset += consumed;
                if (attr is not null) attrs.Add(attr);
            }

            components.Add(new JvmRecordComponentInfo
            {
                name_index = nameIndex,
                descriptor_index = descriptorIndex,
                attributes = attrs
            });
        }

        return new JvmRecordAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            components_count = componentsCount,
            components = components
        };
    }

    private static JvmStackMapTableAttribute decode_stack_map_table(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numberOfEntries = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var entries = new List<JvmStackMapFrame>();
        for (var i = 0; i < numberOfEntries; i++)
        {
            var frame = decode_stack_map_frame(data, ref offset);
            entries.Add(frame);
        }

        return new JvmStackMapTableAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            number_of_entries = numberOfEntries,
            entries = entries
        };
    }

    private static JvmStackMapFrame decode_stack_map_frame(ReadOnlySpan<byte> data, ref int offset)
    {
        var frameType = data[offset];
        offset += 1;

        if (frameType <= 63) return new JvmSameFrame { frame_type = frameType };

        if (frameType <= 127)
        {
            var stack = decode_verification_type_info_list(data, ref offset, 1);
            return new JvmSameLocals1StackItemFrame
            {
                frame_type = frameType,
                stack = stack
            };
        }

        if (frameType <= 246)
        {
            offset += 2;
            return new JvmSameFrame { frame_type = frameType };
        }

        if (frameType is 247)
        {
            offset += 2;
            return new JvmSameFrame { frame_type = frameType };
        }

        if (frameType is >= 248 and <= 250)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            return new JvmChopFrame
            {
                frame_type = frameType,
                offset_delta = offsetDelta
            };
        }

        if (frameType is 251)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            return new JvmSameFrameExtended
            {
                frame_type = frameType,
                offset_delta = offsetDelta
            };
        }

        if (frameType is >= 252 and <= 254)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var count = frameType - 251;
            var locals = decode_verification_type_info_list(data, ref offset, count);
            return new JvmAppendFrame
            {
                frame_type = frameType,
                offset_delta = offsetDelta,
                locals = locals
            };
        }

        if (frameType == 255)
        {
            var offsetDelta = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var numberOfLocals = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var locals = decode_verification_type_info_list(data, ref offset, numberOfLocals);
            var numberOfStackItems = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var stack = decode_verification_type_info_list(data, ref offset, numberOfStackItems);
            return new JvmFullFrame
            {
                frame_type = frameType,
                offset_delta = offsetDelta,
                number_of_locals = numberOfLocals,
                locals = locals,
                number_of_stack_items = numberOfStackItems,
                stack = stack
            };
        }

        return new JvmSameFrame { frame_type = frameType };
    }

    private static List<JvmVerificationTypeInfo> decode_verification_type_info_list(
        ReadOnlySpan<byte> data, ref int offset, int count)
    {
        var result = new List<JvmVerificationTypeInfo>();
        for (var i = 0; i < count; i++)
        {
            var tag = data[offset];
            offset += 1;
            ushort? cpoolIndex = null;
            ushort? vtiOffset = null;

            switch (tag)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    cpoolIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                    offset += 2;
                    break;
                case 8:
                    vtiOffset = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                    offset += 2;
                    break;
            }

            result.Add(new JvmVerificationTypeInfo
            {
                tag = tag,
                cpool_index = cpoolIndex,
                offset = vtiOffset
            });
        }

        return result;
    }

    private static JvmRuntimeVisibleAnnotationsAttribute decode_runtime_visible_annotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var annotations = new List<JvmAnnotation>();
        for (var i = 0; i < numAnnotations; i++) annotations.Add(decode_annotation(data, ref offset));

        return new JvmRuntimeVisibleAnnotationsAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            num_annotations = numAnnotations,
            annotations = annotations
        };
    }

    private static JvmRuntimeInvisibleAnnotationsAttribute decode_runtime_invisible_annotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data);
        offset += 2;
        var annotations = new List<JvmAnnotation>();
        for (var i = 0; i < numAnnotations; i++) annotations.Add(decode_annotation(data, ref offset));

        return new JvmRuntimeInvisibleAnnotationsAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            num_annotations = numAnnotations,
            annotations = annotations
        };
    }

    private static JvmRuntimeVisibleParameterAnnotationsAttribute decode_runtime_visible_parameter_annotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numParameters = data[offset];
        offset += 1;
        var parameterAnnotations = new List<JvmParameterAnnotations>();
        for (var i = 0; i < numParameters; i++)
        {
            var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var annotations = new List<JvmAnnotation>();
            for (var j = 0; j < numAnnotations; j++) annotations.Add(decode_annotation(data, ref offset));

            parameterAnnotations.Add(new JvmParameterAnnotations
            {
                num_annotations = numAnnotations,
                annotations = annotations
            });
        }

        return new JvmRuntimeVisibleParameterAnnotationsAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            num_parameters = numParameters,
            parameter_annotations = parameterAnnotations
        };
    }

    private static JvmRuntimeInvisibleParameterAnnotationsAttribute decode_runtime_invisible_parameter_annotations(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var numParameters = data[offset];
        offset += 1;
        var parameterAnnotations = new List<JvmParameterAnnotations>();
        for (var i = 0; i < numParameters; i++)
        {
            var numAnnotations = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var annotations = new List<JvmAnnotation>();
            for (var j = 0; j < numAnnotations; j++) annotations.Add(decode_annotation(data, ref offset));

            parameterAnnotations.Add(new JvmParameterAnnotations
            {
                num_annotations = numAnnotations,
                annotations = annotations
            });
        }

        return new JvmRuntimeInvisibleParameterAnnotationsAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            num_parameters = numParameters,
            parameter_annotations = parameterAnnotations
        };
    }

    private static JvmAnnotationDefaultAttribute decode_annotation_default(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var defaultValue = decode_element_value(data, ref offset);
        return new JvmAnnotationDefaultAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            default_value = defaultValue
        };
    }

    private static JvmModuleAttribute decode_module(
        ReadOnlySpan<byte> data, ushort attributeNameIndex, uint attributeLength)
    {
        var offset = 0;
        var moduleNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var moduleFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var moduleVersionIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        var requiresCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var requires = new List<JvmModuleRequire>();
        for (var i = 0; i < requiresCount; i++)
        {
            var requiresIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var requiresFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var requiresVersionIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            requires.Add(new JvmModuleRequire
            {
                requires_index = requiresIndex,
                requires_flags = requiresFlags,
                requires_version_index = requiresVersionIndex
            });
        }

        var exportsCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var exports = new List<JvmModuleExport>();
        for (var i = 0; i < exportsCount; i++)
        {
            var exportsIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var exportsFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var exportsToCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var exportsTo = new List<ushort>();
            for (var j = 0; j < exportsToCount; j++)
            {
                exportsTo.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            exports.Add(new JvmModuleExport
            {
                exports_index = exportsIndex,
                exports_flags = exportsFlags,
                exports_to_index = exportsTo
            });
        }

        var opensCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var opens = new List<JvmModuleOpen>();
        for (var i = 0; i < opensCount; i++)
        {
            var opensIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var opensFlags = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var opensToCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var opensTo = new List<ushort>();
            for (var j = 0; j < opensToCount; j++)
            {
                opensTo.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            opens.Add(new JvmModuleOpen
            {
                opens_index = opensIndex,
                opens_flags = opensFlags,
                opens_to_index = opensTo
            });
        }

        var usesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var usesIndex = new List<ushort>();
        for (var i = 0; i < usesCount; i++)
        {
            usesIndex.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
            offset += 2;
        }

        var providesCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var provides = new List<JvmModuleProvide>();
        for (var i = 0; i < providesCount; i++)
        {
            var providesIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var providesWithCount = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var providesWith = new List<ushort>();
            for (var j = 0; j < providesWithCount; j++)
            {
                providesWith.Add(BinaryPrimitives.ReadUInt16BigEndian(data[offset..]));
                offset += 2;
            }

            provides.Add(new JvmModuleProvide
            {
                provides_index = providesIndex,
                provides_with_index = providesWith
            });
        }

        return new JvmModuleAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            module_name_index = moduleNameIndex,
            module_flags = moduleFlags,
            module_version_index = moduleVersionIndex,
            requires = requires,
            exports = exports,
            opens = opens,
            uses_index = usesIndex,
            provides = provides
        };
    }

    private static JvmAnnotation decode_annotation(ReadOnlySpan<byte> data, ref int offset)
    {
        var typeIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var numElementValuePairs = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        var pairs = new List<JvmElementValuePair>();
        for (var i = 0; i < numElementValuePairs; i++)
        {
            var elementNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            offset += 2;
            var value = decode_element_value(data, ref offset);
            pairs.Add(new JvmElementValuePair
            {
                element_name_index = elementNameIndex,
                value = value
            });
        }

        return new JvmAnnotation
        {
            type_index = typeIndex,
            num_element_value_pairs = numElementValuePairs,
            element_value_pairs = pairs
        };
    }

    private static JvmElementValue decode_element_value(ReadOnlySpan<byte> data, ref int offset)
    {
        var tag = (char)data[offset];
        offset += 1;

        ushort? constValueIndex = null;
        ushort? typeNameIndex = null;
        ushort? classInfoIndex = null;
        JvmAnnotation? annotationValue = null;
        ushort? arrayNumValues = null;
        List<JvmElementValue>? arrayValues = null;
        ushort? enumConstNameIndex = null;

        switch (tag)
        {
            case 'B':
            case 'C':
            case 'D':
            case 'F':
            case 'I':
            case 'J':
            case 'S':
            case 'Z':
            case 's':
                constValueIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                break;
            case 'e':
                typeNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                enumConstNameIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                break;
            case 'c':
                classInfoIndex = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                break;
            case '@':
                annotationValue = decode_annotation(data, ref offset);
                break;
            case '[':
                var numValues = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
                offset += 2;
                arrayNumValues = numValues;
                arrayValues = [];
                for (var i = 0; i < numValues; i++) arrayValues.Add(decode_element_value(data, ref offset));

                break;
        }

        return new JvmElementValue
        {
            tag = (byte)tag,
            const_value_index = constValueIndex,
            type_name_index = typeNameIndex,
            class_info_index = classInfoIndex,
            annotation_value = annotationValue,
            array_num_values = arrayNumValues,
            array_values = arrayValues,
            enum_const_name_index = enumConstNameIndex
        };
    }

    private static JvmRawAttribute read_raw_bytes(
        ReadOnlySpan<byte> data, int length, string name, ushort attributeNameIndex, uint attributeLength)
    {
        return new JvmRawAttribute
        {
            attribute_name_index = attributeNameIndex,
            attribute_length = attributeLength,
            name = name,
            raw_data = [.. data[..length]]
        };
    }

    #endregion
}