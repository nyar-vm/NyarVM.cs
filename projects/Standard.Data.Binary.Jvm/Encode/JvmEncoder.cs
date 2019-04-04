using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Jvm.Data;

namespace Std.Data.Binary.Jvm.Encode;

/// <summary>
///     JVM ClassFile 编码器，的JvmClassFileData 编码的.class 二进制格式的
///     JVM ClassFile 格式要求大端序（Big-Endian），本编码器使用 Nyar.Binary.Frame.ByteBufferWriter 的大端序写入方法的
/// </summary>
public sealed class JvmEncoder
{
    /// <summary>
    ///     编码 ClassFile 为字节数的
    /// </summary>
    public byte[] encode(JvmClassFileData classFile)
    {
        var size = estimate_size(classFile);
        var writer = new ByteBufferWriter(size);

        write_header(ref writer, classFile);
        write_constant_pool(ref writer, classFile.constant_pool);
        write_access_flags(ref writer, classFile.access_flags);
        write_class_indices(ref writer, classFile);
        write_interfaces(ref writer, classFile.interfaces);
        write_fields(ref writer, classFile.fields);
        write_methods(ref writer, classFile.methods);
        write_attributes(ref writer, classFile.attributes);

        return writer.to_array();
    }

    #region 头部

    private static void write_header(ref ByteBufferWriter writer, JvmClassFileData classFile)
    {
        writer.write_u32_be(classFile.magic);
        writer.write_u16_be(classFile.minor_version);
        writer.write_u16_be(classFile.major_version);
    }

    #endregion

    #region 接口

    private static void write_interfaces(ref ByteBufferWriter writer, IReadOnlyList<ushort> interfaces)
    {
        writer.write_u16_be((ushort)interfaces.Count);

        foreach (var iface in interfaces) writer.write_u16_be(iface);
    }

    #endregion

    #region 常量的

    private static void write_constant_pool(ref ByteBufferWriter writer, IReadOnlyList<JvmConstant> constantPool)
    {
        writer.write_u16_be((ushort)(constantPool.Count + 1));

        foreach (var constant in constantPool)
        {
            if (constant is JvmConstantPadding) continue;

            encode_constant(ref writer, constant);
        }
    }

    private static void encode_constant(ref ByteBufferWriter writer, JvmConstant constant)
    {
        writer.write_u8((byte)constant.kind);

        switch (constant)
        {
            case JvmConstantUtf8 utf8:
                var bytes = Encoding.UTF8.GetBytes(utf8.value);
                writer.write_u16_be((ushort)bytes.Length);
                writer.write(bytes);
                break;
            case JvmConstantInteger integer:
                writer.write_i32_be(integer.value);
                break;
            case JvmConstantFloat single:
                writer.write_f32_be(single.value);
                break;
            case JvmConstantLong l:
                writer.write_i64_be(l.value);
                break;
            case JvmConstantDouble d:
                writer.write_f64_be(d.value);
                break;
            case JvmConstantClass cls:
                writer.write_u16_be(cls.name_index);
                break;
            case JvmConstantString str:
                writer.write_u16_be(str.string_index);
                break;
            case JvmConstantFieldref fieldref:
                writer.write_u16_be(fieldref.class_index);
                writer.write_u16_be(fieldref.name_and_type_index);
                break;
            case JvmConstantMethodref methodref:
                writer.write_u16_be(methodref.class_index);
                writer.write_u16_be(methodref.name_and_type_index);
                break;
            case JvmConstantInterfaceMethodref imethodref:
                writer.write_u16_be(imethodref.class_index);
                writer.write_u16_be(imethodref.name_and_type_index);
                break;
            case JvmConstantNameAndType nat:
                writer.write_u16_be(nat.name_index);
                writer.write_u16_be(nat.descriptor_index);
                break;
            case JvmConstantMethodHandle mh:
                writer.write_u8(mh.reference_kind);
                writer.write_u16_be(mh.reference_index);
                break;
            case JvmConstantMethodType mt:
                writer.write_u16_be(mt.descriptor_index);
                break;
            case JvmConstantDynamic dyn:
                writer.write_u16_be(dyn.bootstrap_method_attr_index);
                writer.write_u16_be(dyn.name_and_type_index);
                break;
            case JvmConstantInvokeDynamic id:
                writer.write_u16_be(id.bootstrap_method_attr_index);
                writer.write_u16_be(id.name_and_type_index);
                break;
            case JvmConstantModule mod:
                writer.write_u16_be(mod.name_index);
                break;
            case JvmConstantPackage pkg:
                writer.write_u16_be(pkg.name_index);
                break;
        }
    }

    #endregion

    #region 访问标志和类索引

    private static void write_access_flags(ref ByteBufferWriter writer, ushort accessFlags)
    {
        writer.write_u16_be(accessFlags);
    }

    private static void write_class_indices(ref ByteBufferWriter writer, JvmClassFileData classFile)
    {
        writer.write_u16_be(classFile.this_class);
        writer.write_u16_be(classFile.super_class);
    }

    #endregion

    #region 字段

    private static void write_fields(ref ByteBufferWriter writer, IReadOnlyList<JvmFieldInfo> fields)
    {
        writer.write_u16_be((ushort)fields.Count);

        foreach (var field in fields) write_field_info(ref writer, field);
    }

    private static void write_field_info(ref ByteBufferWriter writer, JvmFieldInfo field)
    {
        writer.write_u16_be(field.access_flags);
        writer.write_u16_be(field.name_index);
        writer.write_u16_be(field.descriptor_index);
        write_attributes(ref writer, field.attributes);
    }

    #endregion

    #region 方法

    private static void write_methods(ref ByteBufferWriter writer, IReadOnlyList<JvmMethodInfo> methods)
    {
        writer.write_u16_be((ushort)methods.Count);

        foreach (var method in methods) write_method_info(ref writer, method);
    }

    private static void write_method_info(ref ByteBufferWriter writer, JvmMethodInfo method)
    {
        writer.write_u16_be(method.access_flags);
        writer.write_u16_be(method.name_index);
        writer.write_u16_be(method.descriptor_index);
        write_attributes(ref writer, method.attributes);
    }

    #endregion

    #region 属的

    private static void write_attributes(ref ByteBufferWriter writer, IReadOnlyList<JvmAttributeInfo> attributes)
    {
        writer.write_u16_be((ushort)attributes.Count);

        foreach (var attribute in attributes) write_attribute_info(ref writer, attribute);
    }

    private static void write_attribute_info(ref ByteBufferWriter writer, JvmAttributeInfo attribute)
    {
        writer.write_u16_be(attribute.attribute_name_index);

        switch (attribute)
        {
            case JvmCodeAttribute code:
                write_code_attribute(ref writer, code);
                break;
            case JvmConstantValueAttribute cv:
                writer.write_u32_be(2);
                writer.write_u16_be(cv.constant_value_index);
                break;
            case JvmSourceFileAttribute sf:
                writer.write_u32_be(2);
                writer.write_u16_be(sf.source_file_index);
                break;
            case JvmLineNumberTableAttribute lnt:
                write_line_number_table_attribute(ref writer, lnt);
                break;
            case JvmLocalVariableTableAttribute lvt:
                write_local_variable_table_attribute(ref writer, lvt);
                break;
            case JvmLocalVariableTypeTableAttribute lvtt:
                write_local_variable_type_table_attribute(ref writer, lvtt);
                break;
            case JvmInnerClassesAttribute ic:
                write_inner_classes_attribute(ref writer, ic);
                break;
            case JvmBootstrapMethodsAttribute bm:
                write_bootstrap_methods_attribute(ref writer, bm);
                break;
            case JvmSignatureAttribute sig:
                writer.write_u32_be(2);
                writer.write_u16_be(sig.signature_index);
                break;
            case JvmSyntheticAttribute:
                writer.write_u32_be(0);
                break;
            case JvmDeprecatedAttribute:
                writer.write_u32_be(0);
                break;
            case JvmEnclosingMethodAttribute em:
                writer.write_u32_be(4);
                writer.write_u16_be(em.class_index);
                writer.write_u16_be(em.method_index);
                break;
            case JvmSourceDebugExtensionAttribute sde:
                writer.write_u32_be((uint)sde.debug_extension.Length);
                writer.write(sde.debug_extension);
                break;
            case JvmMethodParametersAttribute mp:
                write_method_parameters_attribute(ref writer, mp);
                break;
            case JvmStackMapTableAttribute smt:
                write_stack_map_table_attribute(ref writer, smt);
                break;
            case JvmNestHostAttribute nh:
                writer.write_u32_be(2);
                writer.write_u16_be(nh.host_class_index);
                break;
            case JvmNestMembersAttribute nm:
                write_nest_members_attribute(ref writer, nm);
                break;
            case JvmRecordAttribute rec:
                write_record_attribute(ref writer, rec);
                break;
            case JvmPermittedSubclassesAttribute ps:
                write_permitted_subclasses_attribute(ref writer, ps);
                break;
            case JvmRuntimeVisibleAnnotationsAttribute rva:
                write_runtime_visible_annotations_attribute(ref writer, rva);
                break;
            case JvmRuntimeInvisibleAnnotationsAttribute ria:
                write_runtime_invisible_annotations_attribute(ref writer, ria);
                break;
            case JvmRuntimeVisibleParameterAnnotationsAttribute rvpa:
                write_runtime_visible_parameter_annotations_attribute(ref writer, rvpa);
                break;
            case JvmRuntimeInvisibleParameterAnnotationsAttribute ripa:
                write_runtime_invisible_parameter_annotations_attribute(ref writer, ripa);
                break;
            case JvmAnnotationDefaultAttribute ad:
                write_annotation_default_attribute(ref writer, ad);
                break;
            case JvmExceptionsAttribute exc:
                write_exceptions_attribute(ref writer, exc);
                break;
            case JvmModuleAttribute mod:
                write_module_attribute(ref writer, mod);
                break;
            case JvmRawAttribute raw:
                writer.write_u32_be((uint)raw.raw_data.Length);
                writer.write(raw.raw_data);
                break;
            default:
                writer.write_u32_be(attribute.attribute_length);
                break;
        }
    }

    private static void write_code_attribute(ref ByteBufferWriter writer, JvmCodeAttribute code)
    {
        var codeAttrWriter = new ByteBufferWriter(code.code.Length + 128);

        codeAttrWriter.write_u16_be(code.max_stack);
        codeAttrWriter.write_u16_be(code.max_locals);
        codeAttrWriter.write_i32_be(code.code.Length);
        codeAttrWriter.write(code.code);

        codeAttrWriter.write_u16_be((ushort)code.exception_table.Count);

        foreach (var entry in code.exception_table)
        {
            codeAttrWriter.write_u16_be(entry.start_pc);
            codeAttrWriter.write_u16_be(entry.end_pc);
            codeAttrWriter.write_u16_be(entry.handler_pc);
            codeAttrWriter.write_u16_be(entry.catch_type);
        }

        write_attributes(ref codeAttrWriter, code.attributes);

        var codeAttrBytes = codeAttrWriter.to_array();
        writer.write_u32_be((uint)codeAttrBytes.Length);
        writer.write(codeAttrBytes);
    }

    private static void write_line_number_table_attribute(ref ByteBufferWriter writer, JvmLineNumberTableAttribute lnt)
    {
        var attrSize = 2 + lnt.line_number_table.Count * 4;
        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be((ushort)lnt.line_number_table.Count);

        foreach (var entry in lnt.line_number_table)
        {
            writer.write_u16_be(entry.start_pc);
            writer.write_u16_be(entry.line_number);
        }
    }

    private static void write_local_variable_table_attribute(ref ByteBufferWriter writer,
        JvmLocalVariableTableAttribute lvt)
    {
        var attrSize = 2 + lvt.local_variable_table.Count * 10;
        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be((ushort)lvt.local_variable_table.Count);

        foreach (var entry in lvt.local_variable_table)
        {
            writer.write_u16_be(entry.start_pc);
            writer.write_u16_be(entry.length);
            writer.write_u16_be(entry.name_index);
            writer.write_u16_be(entry.descriptor_index);
            writer.write_u16_be(entry.index);
        }
    }

    private static void write_local_variable_type_table_attribute(ref ByteBufferWriter writer,
        JvmLocalVariableTypeTableAttribute lvtt)
    {
        var attrSize = 2 + lvtt.local_variable_type_table.Count * 10;
        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be((ushort)lvtt.local_variable_type_table.Count);

        foreach (var entry in lvtt.local_variable_type_table)
        {
            writer.write_u16_be(entry.start_pc);
            writer.write_u16_be(entry.length);
            writer.write_u16_be(entry.name_index);
            writer.write_u16_be(entry.signature_index);
            writer.write_u16_be(entry.index);
        }
    }

    private static void write_inner_classes_attribute(ref ByteBufferWriter writer, JvmInnerClassesAttribute ic)
    {
        var attrSize = 2 + ic.classes.Count * 8;
        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be((ushort)ic.classes.Count);

        foreach (var entry in ic.classes)
        {
            writer.write_u16_be(entry.inner_class_info_index);
            writer.write_u16_be(entry.outer_class_info_index);
            writer.write_u16_be(entry.inner_name_index);
            writer.write_u16_be(entry.inner_class_access_flags);
        }
    }

    private static void write_bootstrap_methods_attribute(ref ByteBufferWriter writer, JvmBootstrapMethodsAttribute bm)
    {
        var attrSize = 2;

        foreach (var method in bm.bootstrap_methods) attrSize += 4 + method.bootstrap_arguments.Count * 2;

        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be((ushort)bm.bootstrap_methods.Count);

        foreach (var method in bm.bootstrap_methods)
        {
            writer.write_u16_be(method.bootstrap_method_ref);
            writer.write_u16_be((ushort)method.bootstrap_arguments.Count);

            foreach (var arg in method.bootstrap_arguments) writer.write_u16_be(arg);
        }
    }

    private static void write_method_parameters_attribute(ref ByteBufferWriter writer, JvmMethodParametersAttribute mp)
    {
        var attrSize = 1 + mp.parameters.Count * 4;
        writer.write_u32_be((uint)attrSize);
        writer.write_u8(mp.parameter_count);

        foreach (var param in mp.parameters)
        {
            writer.write_u16_be(param.name_index);
            writer.write_u16_be(param.access_flags);
        }
    }

    private static void write_stack_map_table_attribute(ref ByteBufferWriter writer, JvmStackMapTableAttribute smt)
    {
        var attrWriter = new ByteBufferWriter(256);
        attrWriter.write_u16_be(smt.number_of_entries);

        foreach (var frame in smt.entries) write_stack_map_frame(ref attrWriter, frame);

        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_stack_map_frame(ref ByteBufferWriter writer, JvmStackMapFrame frame)
    {
        writer.write_u8(frame.frame_type);

        switch (frame)
        {
            case JvmSameFrame:
                break;
            case JvmSameLocals1StackItemFrame slsif:
                write_verification_type_info_list(ref writer, slsif.stack);
                break;
            case JvmChopFrame cf:
                writer.write_u16_be(cf.offset_delta);
                break;
            case JvmSameFrameExtended sfe:
                writer.write_u16_be(sfe.offset_delta);
                break;
            case JvmAppendFrame af:
                writer.write_u16_be(af.offset_delta);
                write_verification_type_info_list(ref writer, af.locals);
                break;
            case JvmFullFrame ff:
                writer.write_u16_be(ff.offset_delta);
                writer.write_u16_be(ff.number_of_locals);
                write_verification_type_info_list(ref writer, ff.locals);
                writer.write_u16_be(ff.number_of_stack_items);
                write_verification_type_info_list(ref writer, ff.stack);
                break;
        }
    }

    private static void write_verification_type_info_list(ref ByteBufferWriter writer,
        IReadOnlyList<JvmVerificationTypeInfo> items)
    {
        foreach (var item in items)
        {
            writer.write_u8(item.tag);
            if (item is { tag: 7, cpool_index: not null })
                writer.write_u16_be(item.cpool_index.Value);
            else if (item is { tag: 8, offset: not null }) writer.write_u16_be(item.offset.Value);
        }
    }

    private static void write_nest_members_attribute(ref ByteBufferWriter writer, JvmNestMembersAttribute nm)
    {
        var attrSize = 2 + nm.class_indexes.Count * 2;
        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be(nm.number_of_classes);

        foreach (var idx in nm.class_indexes) writer.write_u16_be(idx);
    }

    private static void write_permitted_subclasses_attribute(ref ByteBufferWriter writer,
        JvmPermittedSubclassesAttribute ps)
    {
        var attrSize = 2 + ps.class_indexes.Count * 2;
        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be(ps.number_of_classes);

        foreach (var idx in ps.class_indexes) writer.write_u16_be(idx);
    }

    private static void write_record_attribute(ref ByteBufferWriter writer, JvmRecordAttribute rec)
    {
        var attrWriter = new ByteBufferWriter(256);
        attrWriter.write_u16_be(rec.components_count);

        foreach (var component in rec.components)
        {
            attrWriter.write_u16_be(component.name_index);
            attrWriter.write_u16_be(component.descriptor_index);
            write_attributes(ref attrWriter, component.attributes);
        }

        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_runtime_visible_annotations_attribute(ref ByteBufferWriter writer,
        JvmRuntimeVisibleAnnotationsAttribute rva)
    {
        var attrWriter = new ByteBufferWriter(256);
        attrWriter.write_u16_be(rva.num_annotations);

        foreach (var annotation in rva.annotations) write_annotation(ref attrWriter, annotation);

        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_runtime_invisible_annotations_attribute(ref ByteBufferWriter writer,
        JvmRuntimeInvisibleAnnotationsAttribute ria)
    {
        var attrWriter = new ByteBufferWriter(256);
        attrWriter.write_u16_be(ria.num_annotations);

        foreach (var annotation in ria.annotations) write_annotation(ref attrWriter, annotation);

        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_runtime_visible_parameter_annotations_attribute(ref ByteBufferWriter writer,
        JvmRuntimeVisibleParameterAnnotationsAttribute rvpa)
    {
        var attrWriter = new ByteBufferWriter(256);
        attrWriter.write_u8(rvpa.num_parameters);

        foreach (var pa in rvpa.parameter_annotations)
        {
            attrWriter.write_u16_be(pa.num_annotations);

            foreach (var annotation in pa.annotations) write_annotation(ref attrWriter, annotation);
        }

        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_runtime_invisible_parameter_annotations_attribute(ref ByteBufferWriter writer,
        JvmRuntimeInvisibleParameterAnnotationsAttribute ripa)
    {
        var attrWriter = new ByteBufferWriter(256);
        attrWriter.write_u8(ripa.num_parameters);

        foreach (var pa in ripa.parameter_annotations)
        {
            attrWriter.write_u16_be(pa.num_annotations);

            foreach (var annotation in pa.annotations) write_annotation(ref attrWriter, annotation);
        }

        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_annotation_default_attribute(ref ByteBufferWriter writer,
        JvmAnnotationDefaultAttribute ad)
    {
        var attrWriter = new ByteBufferWriter(64);
        write_element_value(ref attrWriter, ad.default_value);
        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_exceptions_attribute(ref ByteBufferWriter writer, JvmExceptionsAttribute exc)
    {
        var attrSize = 2 + exc.exception_index_table.Count * 2;
        writer.write_u32_be((uint)attrSize);
        writer.write_u16_be(exc.number_of_exceptions);

        foreach (var idx in exc.exception_index_table) writer.write_u16_be(idx);
    }

    private static void write_module_attribute(ref ByteBufferWriter writer, JvmModuleAttribute mod)
    {
        var attrWriter = new ByteBufferWriter(256);
        attrWriter.write_u16_be(mod.module_name_index);
        attrWriter.write_u16_be(mod.module_flags);
        attrWriter.write_u16_be(mod.module_version_index);

        attrWriter.write_u16_be((ushort)mod.requires.Count);
        foreach (var req in mod.requires)
        {
            attrWriter.write_u16_be(req.requires_index);
            attrWriter.write_u16_be(req.requires_flags);
            attrWriter.write_u16_be(req.requires_version_index ?? 0);
        }

        attrWriter.write_u16_be((ushort)mod.exports.Count);
        foreach (var exp in mod.exports)
        {
            attrWriter.write_u16_be(exp.exports_index);
            attrWriter.write_u16_be(exp.exports_flags);
            attrWriter.write_u16_be((ushort)exp.exports_to_index.Count);
            foreach (var idx in exp.exports_to_index) attrWriter.write_u16_be(idx);
        }

        attrWriter.write_u16_be((ushort)mod.opens.Count);
        foreach (var open in mod.opens)
        {
            attrWriter.write_u16_be(open.opens_index);
            attrWriter.write_u16_be(open.opens_flags);
            attrWriter.write_u16_be((ushort)open.opens_to_index.Count);
            foreach (var idx in open.opens_to_index) attrWriter.write_u16_be(idx);
        }

        attrWriter.write_u16_be((ushort)mod.uses_index.Count);
        foreach (var idx in mod.uses_index) attrWriter.write_u16_be(idx);

        attrWriter.write_u16_be((ushort)mod.provides.Count);
        foreach (var prov in mod.provides)
        {
            attrWriter.write_u16_be(prov.provides_index);
            attrWriter.write_u16_be((ushort)prov.provides_with_index.Count);
            foreach (var idx in prov.provides_with_index) attrWriter.write_u16_be(idx);
        }

        var attrBytes = attrWriter.to_array();
        writer.write_u32_be((uint)attrBytes.Length);
        writer.write(attrBytes);
    }

    private static void write_annotation(ref ByteBufferWriter writer, JvmAnnotation annotation)
    {
        writer.write_u16_be(annotation.type_index);
        writer.write_u16_be(annotation.num_element_value_pairs);

        foreach (var pair in annotation.element_value_pairs)
        {
            writer.write_u16_be(pair.element_name_index);
            write_element_value(ref writer, pair.value);
        }
    }

    private static void write_element_value(ref ByteBufferWriter writer, JvmElementValue ev)
    {
        writer.write_u8(ev.tag);

        var tag = (char)ev.tag;
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
                if (ev.const_value_index.HasValue) writer.write_u16_be(ev.const_value_index.Value);

                break;
            case 'e':
                if (ev.type_name_index.HasValue) writer.write_u16_be(ev.type_name_index.Value);

                if (ev.enum_const_name_index.HasValue) writer.write_u16_be(ev.enum_const_name_index.Value);

                break;
            case 'c':
                if (ev.class_info_index.HasValue) writer.write_u16_be(ev.class_info_index.Value);

                break;
            case '@':
                if (ev.annotation_value is not null) write_annotation(ref writer, ev.annotation_value);

                break;
            case '[':
                if (ev.array_num_values.HasValue) writer.write_u16_be(ev.array_num_values.Value);

                if (ev.array_values is not null)
                    foreach (var val in ev.array_values)
                        write_element_value(ref writer, val);

                break;
        }
    }

    #endregion

    #region 大小预估

    private static int estimate_size(JvmClassFileData classFile)
    {
        var size = 10;

        size += 2;

        foreach (var constant in classFile.constant_pool) size += estimate_constant_size(constant);

        size += 2 + 4;
        size += 2 + classFile.interfaces.Count * 2;
        size += 2;

        foreach (var field in classFile.fields) size += 8 + estimate_attributes_size(field.attributes);

        size += 2;

        foreach (var method in classFile.methods) size += 8 + estimate_attributes_size(method.attributes);

        size += 2 + estimate_attributes_size(classFile.attributes);

        return size;
    }

    private static int estimate_constant_size(JvmConstant constant)
    {
        return constant switch
        {
            JvmConstantPadding => 0,
            JvmConstantUtf8 utf8 => 3 + Encoding.UTF8.GetByteCount(utf8.value),
            JvmConstantInteger => 5,
            JvmConstantFloat => 5,
            JvmConstantLong => 9,
            JvmConstantDouble => 9,
            JvmConstantClass => 3,
            JvmConstantString => 3,
            JvmConstantFieldref => 5,
            JvmConstantMethodref => 5,
            JvmConstantInterfaceMethodref => 5,
            JvmConstantNameAndType => 5,
            JvmConstantMethodHandle => 4,
            JvmConstantMethodType => 3,
            JvmConstantDynamic => 5,
            JvmConstantInvokeDynamic => 5,
            JvmConstantModule => 3,
            JvmConstantPackage => 3,
            _ => 3
        };
    }

    private static int estimate_attributes_size(IReadOnlyList<JvmAttributeInfo> attributes)
    {
        var size = 2;

        foreach (var attribute in attributes) size += 6 + (int)attribute.attribute_length;

        return size;
    }

    #endregion
}